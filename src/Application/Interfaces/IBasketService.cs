namespace Application.Interfaces;

public record BasketItemResult(string Ticker, decimal Percentual);

public record BasketResult(
    Guid CestaId,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    DateTime? DataDesativacao,
    IReadOnlyList<BasketItemResult> Itens
);

public record CreateBasketResult(
    Guid CestaId,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    IReadOnlyList<BasketItemResult> Itens,
    bool RebalanceamentoDisparado,
    string Mensagem
);

public interface IBasketService
{
    Task<CreateBasketResult> CreateOrUpdateAsync(string name, IReadOnlyList<(string Ticker, decimal Percentual)> itens, CancellationToken ct = default);
    Task<BasketResult?> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BasketResult>> GetHistoryAsync(CancellationToken ct = default);
}
