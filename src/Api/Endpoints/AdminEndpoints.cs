using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Administração");

        group.MapPost("/cesta", CreateBasketAsync)
            .WithName("CadastrarCesta")
            .WithSummary("Cadastrar ou alterar a cesta Top Five")
            .Produces<CreateBasketResult>(201)
            .Produces<ProblemDetails>(400);

        group.MapGet("/cesta/atual", GetActiveBasketAsync)
            .WithName("ConsultarCestaAtual")
            .WithSummary("Visualizar a cesta de recomendação vigente")
            .Produces<BasketResult>(200)
            .Produces<ProblemDetails>(404);

        group.MapGet("/cesta/historico", GetBasketHistoryAsync)
            .WithName("HistoricoCestas")
            .WithSummary("Retornar histórico de todas as cestas")
            .Produces<IReadOnlyList<BasketResult>>(200);

        return app;
    }

    private static async Task<IResult> CreateBasketAsync(
        [FromBody] CreateBasketRequest request,
        IBasketService basketService,
        CancellationToken ct)
    {
        try
        {
            var itens = request.Itens.Select(i => (i.Ticker, i.Percentual)).ToList();
            var result = await basketService.CreateOrUpdateAsync(request.Nome, itens, ct);
            return Results.Created($"/api/admin/cesta/atual", result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("QUANTIDADE_ATIVOS_INVALIDA"))
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 400);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("PERCENTUAIS_INVALIDOS"))
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 400);
        }
    }

    private static async Task<IResult> GetActiveBasketAsync(
        IBasketService basketService,
        CancellationToken ct)
    {
        var result = await basketService.GetActiveAsync(ct);
        return result is null
            ? Results.Problem("Nenhuma cesta ativa encontrada.", statusCode: 404)
            : Results.Ok(result);
    }

    private static async Task<IResult> GetBasketHistoryAsync(
        IBasketService basketService,
        CancellationToken ct)
    {
        var result = await basketService.GetHistoryAsync(ct);
        return Results.Ok(result);
    }
}

public record BasketItemRequest(string Ticker, decimal Percentual);
public record CreateBasketRequest(string Nome, List<BasketItemRequest> Itens);
