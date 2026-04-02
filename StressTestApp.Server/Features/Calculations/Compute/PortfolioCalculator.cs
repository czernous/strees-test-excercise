using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Features.Calculations.Compute;

/// <summary>
/// Aggregates scenario results grouped by portfolio using a single-pass, 
/// memory-efficient aggregation strategy.
/// </summary>
public static class PortfolioCalculator
{
    /// <summary>
    /// Calculates portfolio-level metrics by iterating over loans and applying 
    /// scenario-specific risk parameters.
    /// </summary>
    public static IReadOnlyList<PortfolioCalculationResult> Calculate(
        IReadOnlyList<Loan> loans,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Rating> ratings,
        IReadOnlyDictionary<string, decimal> housePriceChanges)
    {
        // 1. Prepare Lookups access for the main loop)
        var portfolioMap = portfolios.ToDictionary(p => int.Parse(p.Id));

        var pdMap = ratings.ToDictionary(
            r => r.RatingValue,
            r => r.ProbabilityOfDefault / 100m,
            StringComparer.OrdinalIgnoreCase);

        // 2. Use a Dictionary of 'Accumulators' to avoid 'with' keyword overhead.
        // This ensures we only create one result object per portfolio, regardless of loan count.
        var accumulators = new Dictionary<int, PortfolioAccumulator>();

        foreach (var loan in loans)
        {
            // Relational checks: Ensure the loan points to existing metadata.
            if (!portfolioMap.TryGetValue(loan.PortId, out var portfolio)) continue;
            if (!pdMap.TryGetValue(loan.CreditRating, out var pd)) continue;

            // Get the stress delta for the country; default to 0 if not provided.
            housePriceChanges.TryGetValue(portfolio.Country, out var pctChange);

            // Compute the specific loan metrics (Pure Math).
            var calculation = LoanCalculator.Compute(
                loan.CollateralValue,
                loan.OutstandingAmount,
                pctChange,
                pd);

            // Update or Create the accumulator for this portfolio.
            if (!accumulators.TryGetValue(loan.PortId, out var acc))
            {
                acc = new PortfolioAccumulator(portfolio);
                accumulators[loan.PortId] = acc;
            }

            acc.Add(loan, calculation);
        }

        // 3. Transform accumulators into the final read-only result list.
        return [.. accumulators.Values
            .Select(a => a.ToResult())
            .OrderBy(x => x.PortfolioId)];
    }

    /// <summary>
    /// A private helper class to track running totals for a specific portfolio.
    /// Prevents excessive object allocations during the aggregation phase.
    /// </summary>
    private sealed class PortfolioAccumulator(Portfolio p)
    {
        private decimal _totalOutstanding;
        private decimal _totalCollateral;
        private decimal _totalScenarioCollateral;
        private decimal _totalExpectedLoss;
        private int _count;

        public void Add(Loan loan, LoanCalculationResult res)
        {
            _totalOutstanding += loan.OutstandingAmount;
            _totalCollateral += loan.CollateralValue;
            _totalScenarioCollateral += res.ScenarioCollateralValue;
            _totalExpectedLoss += res.ExpectedLoss;
            _count++;
        }

        public PortfolioCalculationResult ToResult() => new(
            PortfolioId: p.Id,
            PortfolioName: p.Name,
            Country: p.Country,
            Currency: p.Ccy,
            TotalOutstandingAmount: _totalOutstanding,
            TotalCollateralValue: _totalCollateral,
            TotalScenarioCollateralValue: _totalScenarioCollateral,
            TotalExpectedLoss: _totalExpectedLoss,
            LoanCount: _count);
    }
}