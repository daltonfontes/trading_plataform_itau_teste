using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.BackgroundServices;

public class PurchaseEngineBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PurchaseEngineBackgroundService> _logger;

    public PurchaseEngineBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PurchaseEngineBackgroundService> logger)
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
                var now = DateTime.Today;

                using var scope = _scopeFactory.CreateScope();
                var cycleRepo = scope.ServiceProvider.GetRequiredService<IBuyCycleRepository>();
                var installment = await GetPendingInstallmentAsync(now, cycleRepo);

                if (installment is not null)
                {
                    _logger.LogInformation("Executing purchase cycle for {Date}, installment {Installment}", now, installment);

                    var purchaseEngine = scope.ServiceProvider.GetRequiredService<IPurchaseEngineService>();
                    await purchaseEngine.ExecuteAsync(now, installment.Value, stoppingToken);

                    _logger.LogInformation("Purchase cycle completed for {Date}", now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing purchase cycle");
            }

            // Check once per hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private static async Task<Installment?> GetPendingInstallmentAsync(DateTime today, IBuyCycleRepository cycleRepo)
    {
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return null;

        int day = today.Day;

        if (day >= 5 && day < 15)
        {
            bool alreadyDone = await cycleRepo.AlreadyExecutedAsync(today.Year, today.Month, Installment.Day5);
            if (!alreadyDone) return Installment.Day5;
        }
        else if (day >= 15 && day < 25)
        {
            bool alreadyDone = await cycleRepo.AlreadyExecutedAsync(today.Year, today.Month, Installment.Day15);
            if (!alreadyDone) return Installment.Day15;
        }
        else if (day >= 25)
        {
            bool alreadyDone = await cycleRepo.AlreadyExecutedAsync(today.Year, today.Month, Installment.Day25);
            if (!alreadyDone) return Installment.Day25;
        }

        return null;
    }
}
