namespace StressTestApp.Server.Features.Calculations.Create;

public sealed record CreateCalculationResponse(
    Guid CalculationId,
    DateTimeOffset CreatedAtUtc,
    long DurationMs,
    IReadOnlyDictionary<string, decimal> HousePriceChanges,
    int PortfolioCount,
    int LoanCount,
    decimal TotalExpectedLoss);
