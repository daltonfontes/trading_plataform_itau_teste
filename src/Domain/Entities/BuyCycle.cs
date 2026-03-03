using Domain.Enums;

namespace Domain.Entities;

public class BuyCycle
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Installment Installment { get; set; }
    public CycleStatus Status { get; set; } = CycleStatus.Pending;
    public decimal TotalValue { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
