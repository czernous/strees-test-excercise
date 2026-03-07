namespace StressTestApp.Server.Features.Calculations.List;

public sealed record ListCalculationsResponse(
    IReadOnlyList<CalculationSummary> Calculations
);

public sealed record CalculationSummary(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    long DurationMs,
    int PortfolioCount,
    int LoanCount,
    decimal TotalExpectedLoss,
    IReadOnlyDictionary<string, decimal> HousePriceChanges
);
