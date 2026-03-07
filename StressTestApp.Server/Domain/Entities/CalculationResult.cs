using StressTestApp.Server.Features.Calculations.Compute;

namespace StressTestApp.Server.Domain.Entities;

/// <summary>
/// Persisted version of PortfolioCalculationResult
/// </summary>
public record CalculationResult
{
    public required Guid Id { get; init; }
    public required Guid CalculationId { get; init; }
    public required string PortfolioId { get; init; }
    public required string PortfolioName { get; init; }
    public required string Country { get; init; }
    public required string Currency { get; init; }
    public required decimal TotalOutstandingAmount { get; init; }
    public required decimal TotalCollateralValue { get; init; }
    public required decimal TotalScenarioCollateralValue { get; init; }
    public required decimal TotalExpectedLoss { get; init; }
    public required int LoanCount { get; init; }

    // Navigation property
    public Calculation? Calculation { get; init; }

    private CalculationResult() { }

    public static CalculationResult Create(Guid calculationId, PortfolioCalculationResult result)
    {
        return new CalculationResult
        {
            Id = Guid.CreateVersion7(),
            CalculationId = calculationId,
            PortfolioId = result.PortfolioId,
            PortfolioName = result.PortfolioName,
            Country = result.Country,
            Currency = result.Currency,
            TotalOutstandingAmount = result.TotalOutstandingAmount,
            TotalCollateralValue = result.TotalCollateralValue,
            TotalScenarioCollateralValue = result.TotalScenarioCollateralValue,
            TotalExpectedLoss = result.TotalExpectedLoss,
            LoanCount = result.LoanCount
        };
    }

    public PortfolioCalculationResult ToPortfolioResult()
    {
        return new PortfolioCalculationResult(
            PortfolioId,
            PortfolioName,
            Country,
            Currency,
            TotalOutstandingAmount,
            TotalCollateralValue,
            TotalScenarioCollateralValue,
            TotalExpectedLoss,
            LoanCount
        );
    }
}
