using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.BackgroundServices;

public class RebalancingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RebalancingBackgroundService> _logger;

    public RebalancingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RebalancingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.Today.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var rebalancingEngine = scope.ServiceProvider.GetRequiredService<IRebalancingEngineService>();
                    var basketRepo        = scope.ServiceProvider.GetRequiredService<ITopFiveBasketRepository>();

                    var basket = await basketRepo.GetActiveAsync();
                    if (basket is not null)
                    {
                        _logger.LogInformation("Running deviation rebalancing check");
                        await rebalancingEngine.RebalanceOnDeviationAsync(basket, stoppingToken);
                        _logger.LogInformation("Deviation rebalancing check completed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running rebalancing deviation check");
            }

            // Check once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
