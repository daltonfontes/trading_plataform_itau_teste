namespace Application.Events;

public record IrRebalancingEvent(
    Guid CustomerId,
    int Month,
    int Year,
    decimal TotalSales,
    decimal NetProfit,
    decimal IrAmount
);
