using Domain.Enums;

namespace Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public decimal MonthlyContribution { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}
