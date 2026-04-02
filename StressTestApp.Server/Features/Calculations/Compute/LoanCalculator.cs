namespace StressTestApp.Server.Features.Calculations.Compute;

/// <summary>
/// Computes scenario metrics for a single loan (pure).
/// Optimized for high-frequency execution within calculation loops.
/// </summary>
public static class LoanCalculator
{
    /// <summary>
    /// Computes the expected loss based on collateral fluctuations.
    /// </summary>
    /// <param name="collateralValue">The current market value of the collateral.</param>
    /// <param name="outstandingAmount">The Exposure at Default (EAD).</param>
    /// <param name="pctChange">The stress scenario delta (e.g., -15.0 for a 15% drop).</param>
    /// <param name="pd">Probability of Default as a decimal (e.g., 0.05 for 5%).</param>
    public static LoanCalculationResult Compute(
         decimal collateralValue,
         decimal outstandingAmount,
         decimal pctChange,
         decimal pd)
    {
        // 1. Calculate Stress Collateral: C * (1 + (-15 / 100))
        var scenarioCollateral = collateralValue * (1m + (pctChange / 100m));

        // 2. Recovery Rate (RR): What % of the DEBT is covered by the stressed collateral?
        // Use a small epsilon or check for zero to avoid DivisionByZero.
        var rr = outstandingAmount <= 0m
            ? 1m // If there is no debt, recovery is effectively 100% (or 0, depending on accounting policy)
            : Math.Clamp(scenarioCollateral / outstandingAmount, 0m, 1m);

        // 3. Loss Given Default (LGD): The portion of the debt we CANNOT recover.
        var lgd = 1m - rr;

        // 4. Expected Loss (EL): PD * LGD * EAD
        var expectedLoss = pd * lgd * outstandingAmount;

        return new LoanCalculationResult(
            ScenarioCollateralValue: scenarioCollateral,
            ExpectedLoss: expectedLoss);
    }
}