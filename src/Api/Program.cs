using Api.BackgroundServices;
using Api.Endpoints;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Compra Programada API", Version = "v1" });
});

builder.Services.AddInfrastructure(builder.Configuration);

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

app.MapCustomerEndpoints();
app.MapAdminEndpoints();

app.Run();
