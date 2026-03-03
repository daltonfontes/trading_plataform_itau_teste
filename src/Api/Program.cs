using Api.BackgroundServices;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure (DB, repositories, Kafka, engines)
builder.Services.AddInfrastructure(builder.Configuration);

// Background engines
builder.Services.AddHostedService<PurchaseEngineBackgroundService>();
builder.Services.AddHostedService<RebalancingBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Debug endpoints for manual testing
app.MapPost("/api/debug/execute-purchase", async (
    string installmentStr,
    IPurchaseEngineService purchaseEngine,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<Installment>(installmentStr, out var installment))
        return Results.BadRequest("Invalid installment. Use Day5, Day15 or Day25.");

    await purchaseEngine.ExecuteAsync(DateTime.Today, installment, ct);
    return Results.Ok("Purchase cycle executed.");
});

app.MapPost("/api/debug/rebalance-deviation", async (
    IRebalancingEngineService rebalancingEngine,
    ITopFiveBasketRepository basketRepo,
    CancellationToken ct) =>
{
    var basket = await basketRepo.GetActiveAsync();
    if (basket is null) return Results.NotFound("No active basket found.");

    await rebalancingEngine.RebalanceOnDeviationAsync(basket, ct);
    return Results.Ok("Deviation rebalancing executed.");
});

app.Run();
