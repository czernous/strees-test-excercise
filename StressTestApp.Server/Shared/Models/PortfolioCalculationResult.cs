namespace StressTestApp.Server.Shared.Models;

/// <summary>
/// Output row grouped by portfolio.
/// </summary>
public sealed record PortfolioCalculationResult(
    string PortfolioId,
    string PortfolioName,
    string Country,
    string Currency,
    decimal TotalOutstandingAmount,
    decimal TotalCollateralValue,
    decimal TotalScenarioCollateralValue,
    decimal TotalExpectedLoss,
    int LoanCount);