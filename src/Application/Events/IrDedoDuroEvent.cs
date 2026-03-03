namespace Application.Events;

public record IrDedoDuroEvent(
    Guid CustomerId,
    DateTime OperationDate,
    string Ticker,
    decimal Quantity,
    decimal TotalValue,
    decimal IrAmount,
    Guid BuyCycleId
);
