namespace StressTestApp.Server.Features.Calculations.Compute;

/// <summary>
/// Computes scenario metrics for a single loan (pure).
/// </summary>
public static class LoanCalculator
{
    /// <remarks>
    /// pctChange is interpreted as percent delta (e.g. -5.12 means -5.12%):
    /// ScenarioCollateral = Collateral * (1 + pctChange/100).
    ///
    /// RecoveryRate is clamped to [0..1] to avoid negative LGD/EL when collateral exceeds loan amount.
    /// </remarks>
    public static LoanCalculationResult Compute(
         int collateralValue,
         int originalLoanAmount,
         int outstandingAmount,
         decimal pctChange,
         decimal pd)
    {
        // pctChange like -5.12 means -5.12%
        var scenarioCollateral = collateralValue * (1m + (pctChange / 100m));

        var rr = originalLoanAmount == 0
            ? 0m
            : scenarioCollateral / originalLoanAmount;

        rr = Clamp(rr, 0m, 1m);

        var lgd = 1m - rr;
        var expectedLoss = pd * lgd * outstandingAmount;

        return new LoanCalculationResult(
            ScenarioCollateralValue: scenarioCollateral,
            ExpectedLoss: expectedLoss);
    }

    private static decimal Clamp(decimal v, decimal min, decimal max) =>
        v < min ? min : (v > max ? max : v);
}
