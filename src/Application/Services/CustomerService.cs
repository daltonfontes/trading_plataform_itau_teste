using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Interfaces;

namespace Application.Services;

public class CustomerService : ICustomerService
{
    private const decimal MinimumMonthlyContribution = 100m;

    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerCustodyRepository _custodyRepository;
    private readonly IAssetPriceRepository _assetPriceRepository;
    private readonly IEventStore _eventStore;

    public CustomerService(
        ICustomerRepository customerRepository,
        ICustomerCustodyRepository custodyRepository,
        IAssetPriceRepository assetPriceRepository,
        IEventStore eventStore)
    {
        _customerRepository = customerRepository;
        _custodyRepository = custodyRepository;
        _assetPriceRepository = assetPriceRepository;
        _eventStore = eventStore;
    }

    public async Task<EnrollResult> EnrollAsync(string name, string cpf, string email, decimal monthlyValue, CancellationToken ct = default)
    {
        if (monthlyValue < MinimumMonthlyContribution)
            throw new InvalidOperationException($"VALOR_MENSAL_INVALIDO: O valor mensal mínimo é de R$ {MinimumMonthlyContribution:F2}.");

        var existing = await _customerRepository.GetByCpfAsync(cpf);
        if (existing is not null)
            throw new InvalidOperationException("CLIENTE_CPF_DUPLICADO: CPF já cadastrado no sistema.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            CPF = cpf,
            Email = email,
            MonthlyContribution = monthlyValue,
            Status = CustomerStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);

        // Número de conta no formato FLH-{6 dígitos do GUID}
        string numeroConta = $"FLH-{customer.Id.ToString("N")[..6].ToUpper()}";

        return new EnrollResult(
            customer.Id,
            customer.Name,
            customer.CPF,
            customer.Email,
            customer.MonthlyContribution,
            true,
            customer.EnrolledAt,
            numeroConta
        );
    }

    public async Task<ExitResult> ExitAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO: Cliente não encontrado.");

        if (customer.Status == CustomerStatus.Inactive)
            throw new InvalidOperationException("CLIENTE_JA_INATIVO: Cliente já havia saído do produto.");

        customer.Status = CustomerStatus.Inactive;
        customer.DeactivatedAt = DateTime.UtcNow;
        await _customerRepository.UpdateAsync(customer);

        return new ExitResult(
            customer.Id,
            customer.Name,
            false,
            customer.DeactivatedAt!.Value,
            "Adesão encerrada. Sua posição em custódia foi mantida."
        );
    }

    public async Task<UpdateMonthlyResult> UpdateMonthlyAsync(Guid customerId, decimal newValue, CancellationToken ct = default)
    {
        if (newValue < MinimumMonthlyContribution)
            throw new InvalidOperationException($"VALOR_MENSAL_INVALIDO: O valor mensal mínimo é de R$ {MinimumMonthlyContribution:F2}.");

        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO: Cliente não encontrado.");

        decimal anterior = customer.MonthlyContribution;
        customer.MonthlyContribution = newValue;
        await _customerRepository.UpdateAsync(customer);

        return new UpdateMonthlyResult(
            customer.Id,
            anterior,
            newValue,
            DateTime.UtcNow,
            "Valor mensal atualizado. O novo valor será considerado a partir da próxima data de compra."
        );
    }

    public async Task<PortfolioResult> GetPortfolioAsync(Guid customerId, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO: Cliente não encontrado.");

        var custodyItems = await _custodyRepository.GetByCustomerIdAsync(customerId);

        var assetItems = new List<PortfolioAssetItem>();
        decimal totalInvestido = 0m;
        decimal totalAtual = 0m;

        foreach (var item in custodyItems.Where(i => i.Quantity > 0))
        {
            decimal price = await _assetPriceRepository.GetLastClosingPriceAsync(item.Asset.Ticker) ?? item.AveragePrice;
            decimal valorAtual = item.Quantity * price;
            decimal valorInvestido = item.Quantity * item.AveragePrice;
            decimal pl = valorAtual - valorInvestido;
            decimal plPct = valorInvestido > 0 ? (pl / valorInvestido) * 100m : 0m;

            totalInvestido += valorInvestido;
            totalAtual += valorAtual;

            assetItems.Add(new PortfolioAssetItem(
                item.Asset.Ticker,
                item.Quantity,
                item.AveragePrice,
                price,
                valorAtual,
                pl,
                Math.Round(plPct, 2),
                0m // composicaoCarteira será calculada abaixo
            ));
        }

        // Calcular composição percentual de cada ativo
        var ativos = assetItems.Select(a => a with
        {
            ComposicaoCarteira = totalAtual > 0 ? Math.Round(a.ValorAtual / totalAtual * 100m, 2) : 0m
        }).ToList();

        decimal plTotal = totalAtual - totalInvestido;
        decimal rentabilidade = totalInvestido > 0 ? Math.Round((plTotal / totalInvestido) * 100m, 2) : 0m;
        string numeroConta = $"FLH-{customerId.ToString("N")[..6].ToUpper()}";

        return new PortfolioResult(
            customerId,
            customer.Name,
            numeroConta,
            DateTime.UtcNow,
            Math.Round(totalInvestido, 2),
            Math.Round(totalAtual, 2),
            Math.Round(plTotal, 2),
            rentabilidade,
            ativos
        );
    }

    public async Task<ProfitabilityResult> GetProfitabilityAsync(Guid customerId, CancellationToken ct = default)
    {
        var portfolio = await GetPortfolioAsync(customerId, ct);

        // Histórico de contribuições via EventStore
        var domainEvents = await _eventStore.GetEventsAsync(customerId, ct);
        var historico = domainEvents
            .OfType<DistributedContribution>()
            .Select(e => new ContribuicaoHistorico(
                e.OccurredOn,
                e.Ticker,
                e.Quantity,
                e.UnityPrice,
                e.TotalPrice))
            .OrderBy(h => h.Data)
            .ToList();

        return new ProfitabilityResult(
            portfolio.ClienteId,
            portfolio.Nome,
            portfolio.DataConsulta,
            portfolio.ValorTotalInvestido,
            portfolio.ValorAtualCarteira,
            portfolio.PlTotal,
            portfolio.RentabilidadePercentual,
            portfolio.Ativos,
            historico
        );
    }
}
