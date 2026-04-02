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

        return new Calculation
        {
            Id = id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DurationMs = durationMs,
            PortfolioCount = portfolioCount,
            LoanCount = loanCount,
            TotalExpectedLoss = totalExpectedLoss,
            Inputs = [.. housePriceChanges.Select(kvp => CalculationInput.Create(id, kvp.Key, kvp.Value))],
            Results = [.. calculationResults.Select(r => CalculationResult.Create(id, r))]
        };
    }

    public IReadOnlyDictionary<string, decimal> GetHousePriceChanges() =>
        Inputs.ToDictionary(i => i.CountryCode, i => i.HousePriceChange);

    public IReadOnlyList<PortfolioCalculationResult> GetCalculationResults() =>
        [.. Results.Select(r => r.ToPortfolioResult())];
}