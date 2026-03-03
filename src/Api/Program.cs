using Api.BackgroundServices;
using Api.Endpoints;
using Infrastructure.Data;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Compra Programada API", Version = "v1" });
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddMySql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "mysql",
        tags: ["ready"]);

builder.Services.AddHostedService<PurchaseEngineBackgroundService>();
builder.Services.AddHostedService<RebalancingBackgroundService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseHttpMetrics();

app.MapCustomerEndpoints();
app.MapAdminEndpoints();

app.MapMetrics();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
