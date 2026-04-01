using StressTestApp.Server.Shared.Contracts;

namespace StressTestApp.Server.Shared.Models;

public record struct Rating(
    string RatingValue,
    int ProbabilityOfDefault
) : IIntegrityContract
{
    public readonly bool IsValid => !string.IsNullOrWhiteSpace(RatingValue);
}
