using Domain.Enums;

namespace Application.Interfaces;

public interface IPurchaseEngineService
{
    Task ExecuteAsync(DateTime date, Installment installment, CancellationToken ct = default);
}
