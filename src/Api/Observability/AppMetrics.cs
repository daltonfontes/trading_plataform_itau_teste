using Prometheus;

namespace Api.Observability;

public static class AppMetrics
{
    public static readonly Counter PurchaseCyclesTotal = Metrics
        .CreateCounter("trading_purchase_cycles_total", "Total de ciclos de compra executados com sucesso");

    public static readonly Counter PurchaseCycleErrorsTotal = Metrics
        .CreateCounter("trading_purchase_cycles_errors_total", "Total de erros em ciclos de compra");

    public static readonly Counter RebalancingChecksTotal = Metrics
        .CreateCounter("trading_rebalancing_checks_total", "Total de verificações de desvio de rebalanceamento executadas com sucesso");

    public static readonly Counter RebalancingErrorsTotal = Metrics
        .CreateCounter("trading_rebalancing_errors_total", "Total de erros nas verificações de rebalanceamento");
}
