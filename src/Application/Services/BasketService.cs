using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Services;

public class BasketService : IBasketService
{
    private const int RequiredAssetCount = 5;
    private const decimal RequiredTotalPercentage = 100m;

    private readonly ITopFiveBasketRepository _basketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IRebalancingEngineService _rebalancingEngine;

    public BasketService(
        ITopFiveBasketRepository basketRepository,
        IAssetRepository assetRepository,
        IRebalancingEngineService rebalancingEngine)
    {
        _basketRepository = basketRepository;
        _assetRepository = assetRepository;
        _rebalancingEngine = rebalancingEngine;
    }

    public async Task<CreateBasketResult> CreateOrUpdateAsync(
        string name,
        IReadOnlyList<(string Ticker, decimal Percentual)> itens,
        CancellationToken ct = default)
    {
        if (itens.Count != RequiredAssetCount)
            throw new InvalidOperationException(
                $"QUANTIDADE_ATIVOS_INVALIDA: A cesta deve conter exatamente {RequiredAssetCount} ativos. Quantidade informada: {itens.Count}.");

        decimal totalPct = itens.Sum(i => i.Percentual);
        if (Math.Abs(totalPct - RequiredTotalPercentage) > 0.01m)
            throw new InvalidOperationException(
                $"PERCENTUAIS_INVALIDOS: A soma dos percentuais deve ser exatamente 100%. Soma atual: {totalPct}%.");

        if (itens.Any(i => i.Percentual <= 0))
            throw new InvalidOperationException("PERCENTUAIS_INVALIDOS: Cada percentual deve ser maior que 0%.");

        // Desativa cesta anterior
        var previous = await _basketRepository.GetActiveAsync();
        if (previous is not null)
        {
            previous.Status = BasketStatus.Inactive;
            previous.DeactivatedAt = DateTime.UtcNow;
            await _basketRepository.UpdateAsync(previous);
        }

        // Cria nova cesta com composições
        var newBasket = new TopFiveBasket
        {
            Id = Guid.NewGuid(),
            Name = name,
            ActiveSince = DateTime.UtcNow,
            Status = BasketStatus.Active
        };

        foreach (var (ticker, percentual) in itens)
        {
            var asset = await _assetRepository.GetOrCreateAsync(ticker);
            newBasket.Compositions.Add(new BasketComposition
            {
                Id = Guid.NewGuid(),
                TopFiveBasketId = newBasket.Id,
                AssetId = asset.Id,
                Asset = asset,
                Percentage = percentual
            });
        }

        await _basketRepository.AddAsync(newBasket);

        // Dispara rebalanceamento se havia cesta anterior
        bool rebalanceou = false;
        if (previous is not null)
        {
            await _rebalancingEngine.RebalanceOnBasketChangeAsync(newBasket, previous, ct);
            rebalanceou = true;
        }

        var resultItens = newBasket.Compositions
            .Select(c => new BasketItemResult(c.Asset.Ticker, c.Percentage))
            .ToList();

        string mensagem = rebalanceou
            ? "Cesta atualizada. Rebalanceamento disparado para os clientes ativos."
            : "Primeira cesta cadastrada com sucesso.";

        return new CreateBasketResult(
            newBasket.Id,
            newBasket.Name,
            true,
            newBasket.ActiveSince,
            resultItens,
            rebalanceou,
            mensagem
        );
    }

    public async Task<BasketResult?> GetActiveAsync(CancellationToken ct = default)
    {
        var basket = await _basketRepository.GetActiveAsync();
        return basket is null ? null : MapToResult(basket);
    }

    public async Task<IReadOnlyList<BasketResult>> GetHistoryAsync(CancellationToken ct = default)
    {
        var all = await _basketRepository.GetAllAsync();
        return all.Select(MapToResult).ToList();
    }

    private static BasketResult MapToResult(TopFiveBasket basket) =>
        new(
            basket.Id,
            basket.Name,
            basket.Status == BasketStatus.Active,
            basket.ActiveSince,
            basket.DeactivatedAt,
            basket.Compositions.Select(c => new BasketItemResult(c.Asset.Ticker, c.Percentage)).ToList()
        );
}
