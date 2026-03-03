namespace Application.Events;

public record IrDedoDuroEvent(
    Guid CustomerId,
    string Cpf,
    DateTime OperationDate,
    string Ticker,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalValue,
    decimal IrAmount,
    Guid BuyCycleId
);
