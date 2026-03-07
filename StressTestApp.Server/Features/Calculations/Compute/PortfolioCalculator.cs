using StressTestApp.Server.Data.Models;

namespace StressTestApp.Server.Features.Calculations.Compute;

/// <summary>
/// Aggregates scenario results grouped by portfolio (single pass over loans).
/// </summary>
public static class PortfolioCalculator
{
    public static IReadOnlyList<PortfolioCalculationResult> Calculate(
        IReadOnlyList<Loan> loans,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Rating> ratings,
        IReadOnlyDictionary<string, decimal> housePriceChanges)
    {
        var portfolioById = portfolios.ToDictionary(
            p => int.Parse(p.Id));

        var pdByRating = ratings.ToDictionary(
            r => r.RatingValue,
            r => r.ProbabilityOfDefault / 100m,
            StringComparer.OrdinalIgnoreCase);

        var results = new Dictionary<int, PortfolioCalculationResult>();

        foreach (var loan in loans)
        {
            var portfolio = portfolioById[loan.PortId];

            housePriceChanges.TryGetValue(portfolio.Country, out var pctChange);

            var pd = pdByRating[loan.CreditRating];

            var (scenarioCollateral, expectedLoss) =
                LoanCalculator.Compute(
                    loan.CollateralValue,
                    loan.OriginalAmount,
                    loan.OutstandingAmount,
                    pctChange,
                    pd);

            if (!results.TryGetValue(loan.PortId, out var current))
            {
                current = new PortfolioCalculationResult(
                    PortfolioId: portfolio.Id,
                    PortfolioName: portfolio.Name,
                    Country: portfolio.Country,
                    Currency: portfolio.Ccy,
                    TotalOutstandingAmount: 0,
                    TotalCollateralValue: 0,
                    TotalScenarioCollateralValue: 0,
                    TotalExpectedLoss: 0,
                    LoanCount: 0);

                results.Add(loan.PortId, current);
            }

            results[loan.PortId] = current with
            {
                TotalOutstandingAmount = current.TotalOutstandingAmount + loan.OutstandingAmount,
                TotalCollateralValue = current.TotalCollateralValue + loan.CollateralValue,
                TotalScenarioCollateralValue = current.TotalScenarioCollateralValue + scenarioCollateral,
                TotalExpectedLoss = current.TotalExpectedLoss + expectedLoss,
                LoanCount = current.LoanCount + 1
            };
        }

        return [.. results.Values.OrderBy(x => x.PortfolioId)];
    }
}