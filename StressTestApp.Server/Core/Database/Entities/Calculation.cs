using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Core.Database.Entities;

public record Calculation
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required long DurationMs { get; init; }
    public required int PortfolioCount { get; init; }
    public required int LoanCount { get; init; }
    public required decimal TotalExpectedLoss { get; init; }
    public required IReadOnlyCollection<CalculationInput> Inputs { get; init; }
    public required IReadOnlyCollection<CalculationResult> Results { get; init; }

    private Calculation() { }

    public static Calculation Create(
        long durationMs,
        IReadOnlyDictionary<string, decimal> housePriceChanges,
        int portfolioCount,
        int loanCount,
        decimal totalExpectedLoss,
        IReadOnlyList<PortfolioCalculationResult> calculationResults)
    {
        var id = Guid.CreateVersion7();
        // Materialize directly into exact-size arrays to avoid extra iterator and builder churn.
        var inputs = new CalculationInput[housePriceChanges.Count];
        var inputIndex = 0;

        foreach (var (countryCode, housePriceChange) in housePriceChanges)
        {
            inputs[inputIndex++] = CalculationInput.Create(id, countryCode, housePriceChange);
        }

        var results = new CalculationResult[calculationResults.Count];
        for (var i = 0; i < calculationResults.Count; i++)
        {
            results[i] = CalculationResult.Create(id, calculationResults[i]);
        }

        return new Calculation
        {
            Id = id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DurationMs = durationMs,
            PortfolioCount = portfolioCount,
            LoanCount = loanCount,
            TotalExpectedLoss = totalExpectedLoss,
            Inputs = inputs,
            Results = results
        };
    }

    public IReadOnlyDictionary<string, decimal> GetHousePriceChanges() =>
        Inputs.ToDictionary(i => i.CountryCode, i => i.HousePriceChange);

    public IReadOnlyList<PortfolioCalculationResult> GetCalculationResults() =>
        [.. Results.Select(r => r.ToPortfolioResult())];
}
