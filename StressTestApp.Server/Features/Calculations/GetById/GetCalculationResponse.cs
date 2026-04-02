using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Features.Calculations.GetById;

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
