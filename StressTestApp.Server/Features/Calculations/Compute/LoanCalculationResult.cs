namespace StressTestApp.Server.Features.Calculations.Compute;

/// <summary>
/// Result of scenario computation for a single loan.
/// </summary>
public readonly record struct LoanCalculationResult(
    decimal ScenarioCollateralValue,
    decimal ExpectedLoss);
