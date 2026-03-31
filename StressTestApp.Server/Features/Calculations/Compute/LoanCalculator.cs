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
    /// RecoveryRate = ScenarioCollateral / OutstandingAmount (clamped to [0..1]).
    /// This ensures RR reflects what can be recovered from the current debt, not the original loan.
    /// LGD = 1 - RR. EL = PD * LGD * OutstandingAmount.
    /// </remarks>
    public static LoanCalculationResult Compute(
         decimal collateralValue,
         decimal outstandingAmount,
         decimal pctChange,
         decimal pd)
    {
        // pctChange like -5.12 means -5.12%
        var scenarioCollateral = collateralValue * (1m + (pctChange / 100m));

        var rr = outstandingAmount == 0
            ? 0m
            : scenarioCollateral / outstandingAmount;

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
