using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Shared.Models;

/// <summary>
/// Represents a high-performance Loan entity for stress test calculations.
/// Uses explicit interface implementation to hide validation logic from the public API.
/// </summary>
public readonly record struct Loan(
    int Id,
    int PortId,
    decimal OriginalAmount,
    decimal OutstandingAmount,
    decimal CollateralValue,
    string CreditRating
) : IIntegrityContract
{
    /// <summary>
    /// Explicitly implements the integrity check. 
    /// This method is "invisible" unless the record is cast to <see cref="IIntegrityContract"/>.
    /// </summary>
    Error? IIntegrityContract.Validate()
    {
        if (Id <= 0)
            return Error.Validation("Loan Id must be greater than zero.");

        if (PortId <= 0)
            return Error.Validation("Portfolio Assignment (PortId) is required.");

        if (OriginalAmount < 0)
            return Error.Validation("Original Amount cannot be negative.");

        // Financial Integrity: Outstanding balance shouldn't technically exceed original + interest/fees 
        // (Simplified check for stress testing)
        if (OutstandingAmount < 0)
            return Error.Validation("Outstanding Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(CreditRating))
            return Error.Validation("Credit Rating is required for risk weighting.");

        return null;
    }
}
