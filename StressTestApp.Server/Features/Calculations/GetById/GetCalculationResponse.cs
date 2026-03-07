using StressTestApp.Server.Features.Calculations.Compute;

namespace StressTestApp.Server.Features.Calculations.Get;

public sealed record GetCalculationResponse(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    long DurationMs,
    int PortfolioCount,
    int LoanCount,
    decimal TotalExpectedLoss,
    IReadOnlyDictionary<string, decimal> HousePriceChanges,
    IReadOnlyList<PortfolioCalculationResult> Results
);
