using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Shared.Models;

/// <summary>
/// Represents a Credit Rating and its associated risk parameters.
/// Uses explicit implementation to keep validation logic internal to the parsing pipeline.
/// </summary>
public readonly record struct Rating(
    string RatingValue,
    int ProbabilityOfDefault
) : IIntegrityContract
{
    /// <summary>
    /// Validates the rating data. 
    /// Ensures numerical risk parameters are within a valid logical range [0-100].
    /// </summary>
    Error? IIntegrityContract.Validate()
    {
        if (string.IsNullOrWhiteSpace(RatingValue))
            return Error.Validation("Rating Value (e.g., 'AAA') is required.");

        // Risk Validation: PD cannot be negative. 
        // We assume an integer representation of percentage (0-100).
        if (ProbabilityOfDefault < 0 || ProbabilityOfDefault > 100)
            return Error.Validation($"Probability of Default ({ProbabilityOfDefault}) must be between 0 and 100.");

        return null; 
    }
}