using StressTestApp.Server.Shared.Contracts;

namespace StressTestApp.Server.Shared.Models;

public record struct Loan(
    int Id,
    int PortId,
    decimal OriginalAmount,
    decimal OutstandingAmount,
    decimal CollateralValue,
    string CreditRating
) : IIntegrityContract
{
    public readonly bool IsValid => !string.IsNullOrWhiteSpace(CreditRating);
}
