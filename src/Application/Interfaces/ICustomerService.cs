namespace Application.Interfaces;

public record EnrollResult(
    Guid ClienteId,
    string Nome,
    string Cpf,
    string Email,
    decimal ValorMensal,
    bool Ativo,
    DateTime DataAdesao,
    string NumeroConta
);

public record ExitResult(
    Guid ClienteId,
    string Nome,
    bool Ativo,
    DateTime DataSaida,
    string Mensagem
);

public record UpdateMonthlyResult(
    Guid ClienteId,
    decimal ValorMensalAnterior,
    decimal ValorMensalNovo,
    DateTime DataAlteracao,
    string Mensagem
);

public record PortfolioAssetItem(
    string Ticker,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal CotacaoAtual,
    decimal ValorAtual,
    decimal Pl,
    decimal PlPercentual,
    decimal ComposicaoCarteira
);

public record PortfolioResult(
    Guid ClienteId,
    string Nome,
    string ContaGrafica,
    DateTime DataConsulta,
    decimal ValorTotalInvestido,
    decimal ValorAtualCarteira,
    decimal PlTotal,
    decimal RentabilidadePercentual,
    IReadOnlyList<PortfolioAssetItem> Ativos
);

public record ContribuicaoHistorico(
    DateTime Data,
    string Ticker,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal ValorTotal
);

public record ProfitabilityResult(
    Guid ClienteId,
    string Nome,
    DateTime DataConsulta,
    decimal ValorTotalInvestido,
    decimal ValorAtualCarteira,
    decimal PlTotal,
    decimal RentabilidadePercentual,
    IReadOnlyList<PortfolioAssetItem> Ativos,
    IReadOnlyList<ContribuicaoHistorico> Historico
);

public interface ICustomerService
{
    Task<EnrollResult> EnrollAsync(string name, string cpf, string email, decimal monthlyValue, CancellationToken ct = default);
    Task<ExitResult> ExitAsync(Guid customerId, CancellationToken ct = default);
    Task<UpdateMonthlyResult> UpdateMonthlyAsync(Guid customerId, decimal newValue, CancellationToken ct = default);
    Task<PortfolioResult> GetPortfolioAsync(Guid customerId, CancellationToken ct = default);
    Task<ProfitabilityResult> GetProfitabilityAsync(Guid customerId, CancellationToken ct = default);
}
