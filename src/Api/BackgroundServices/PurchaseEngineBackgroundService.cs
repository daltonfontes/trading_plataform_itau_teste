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

                if (TryGetInstallment(now, out Installment installment))
                {
                    _logger.LogInformation("Executing purchase cycle for {Date}, installment {Installment}", now, installment);

                    using var scope = _scopeFactory.CreateScope();
                    var purchaseEngine = scope.ServiceProvider.GetRequiredService<IPurchaseEngineService>();
                    await purchaseEngine.ExecuteAsync(now, installment, stoppingToken);

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

    private bool TryGetInstallment(DateTime today, out Installment installment)
    {
        installment = default;

        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        // We need IBuyCycleRepository to check if already executed.
        // Using a scope here to avoid holding a scoped service in singleton.
        using var scope = _scopeFactory.CreateScope();
        var cycleRepo = scope.ServiceProvider.GetRequiredService<IBuyCycleRepository>();

        int day = today.Day;

        if (day >= 5 && day < 15)
        {
            bool alreadyDone = cycleRepo.AlreadyExecutedAsync(today.Year, today.Month, Installment.Day5).GetAwaiter().GetResult();
            if (!alreadyDone) { installment = Installment.Day5; return true; }
        }
        else if (day >= 15 && day < 25)
        {
            bool alreadyDone = cycleRepo.AlreadyExecutedAsync(today.Year, today.Month, Installment.Day15).GetAwaiter().GetResult();
            if (!alreadyDone) { installment = Installment.Day15; return true; }
        }
        else if (day >= 25)
        {
            bool alreadyDone = cycleRepo.AlreadyExecutedAsync(today.Year, today.Month, Installment.Day25).GetAwaiter().GetResult();
            if (!alreadyDone) { installment = Installment.Day25; return true; }
        }

        return false;
    }
}
