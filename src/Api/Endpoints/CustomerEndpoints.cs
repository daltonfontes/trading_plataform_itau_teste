using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes").WithTags("Clientes");

        group.MapPost("/adesao", EnrollAsync)
            .WithName("AdesaoCliente")
            .WithSummary("Aderir ao produto de compra programada")
            .Produces<EnrollResult>(201)
            .Produces<ProblemDetails>(400);

        group.MapPost("/{id:guid}/saida", ExitAsync)
            .WithName("SaidaCliente")
            .WithSummary("Solicitar saída do produto")
            .Produces<ExitResult>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(404);

        group.MapPut("/{id:guid}/valor-mensal", UpdateMonthlyAsync)
            .WithName("AlterarValorMensal")
            .WithSummary("Alterar valor de aporte mensal")
            .Produces<UpdateMonthlyResult>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(404);

        group.MapGet("/{id:guid}/carteira", GetPortfolioAsync)
            .WithName("ConsultarCarteira")
            .WithSummary("Consultar carteira e custódia do cliente")
            .Produces<PortfolioResult>(200)
            .Produces<ProblemDetails>(404);

        group.MapGet("/{id:guid}/rentabilidade", GetProfitabilityAsync)
            .WithName("ConsultarRentabilidade")
            .WithSummary("Consultar rentabilidade detalhada da carteira")
            .Produces<ProfitabilityResult>(200)
            .Produces<ProblemDetails>(404);

        return app;
    }

    private static async Task<IResult> EnrollAsync(
        [FromBody] EnrollRequest request,
        ICustomerService customerService,
        CancellationToken ct)
    {
        try
        {
            var result = await customerService.EnrollAsync(request.Nome, request.Cpf, request.Email, request.ValorMensal, ct);
            return Results.Created($"/api/clientes/{result.ClienteId}/carteira", result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("VALOR_MENSAL_INVALIDO"))
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 400);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CLIENTE_CPF_DUPLICADO"))
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 400);
        }
    }

    private static async Task<IResult> ExitAsync(
        Guid id,
        ICustomerService customerService,
        CancellationToken ct)
    {
        try
        {
            var result = await customerService.ExitAsync(id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 404);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 400);
        }
    }

    private static async Task<IResult> UpdateMonthlyAsync(
        Guid id,
        [FromBody] UpdateMonthlyRequest request,
        ICustomerService customerService,
        CancellationToken ct)
    {
        try
        {
            var result = await customerService.UpdateMonthlyAsync(id, request.NovoValorMensal, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 404);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 400);
        }
    }

    private static async Task<IResult> GetPortfolioAsync(
        Guid id,
        ICustomerService customerService,
        CancellationToken ct)
    {
        try
        {
            var result = await customerService.GetPortfolioAsync(id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 404);
        }
    }

    private static async Task<IResult> GetProfitabilityAsync(
        Guid id,
        ICustomerService customerService,
        CancellationToken ct)
    {
        try
        {
            var result = await customerService.GetProfitabilityAsync(id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.Problem(ex.Message.Split(": ")[1], statusCode: 404);
        }
    }
}

public record EnrollRequest(string Nome, string Cpf, string Email, decimal ValorMensal);
public record UpdateMonthlyRequest(decimal NovoValorMensal);
