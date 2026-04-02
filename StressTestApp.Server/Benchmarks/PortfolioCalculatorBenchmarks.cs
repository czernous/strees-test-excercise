using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Data.Models;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Infrastructure.CsvLoader;
using StressTestApp.Server.Infrastructure.CsvLoader.Maps;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PortfolioCalculatorBenchmarks
{
    private IReadOnlyList<Loan> _loans = [];
    private IReadOnlyList<Portfolio> _portfolios = [];
    private IReadOnlyList<Rating> _ratings = [];
    private IReadOnlyDictionary<string, decimal> _housePriceChanges = new Dictionary<string, decimal>();

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var loader = new CsvLoader();
        var csvDirectory = RepositoryPaths.FindServerDataCsvDirectory();

        _portfolios = await loader.LoadCsvAsync<Portfolio, PortfolioMap>(Path.Combine(csvDirectory, "portfolios.csv"));
        _loans = await loader.LoadCsvAsync<Loan, LoanMap>(Path.Combine(csvDirectory, "loans.csv"));
        _ratings = await loader.LoadCsvAsync<Rating, RatingMap>(Path.Combine(csvDirectory, "ratings.csv"));
        _housePriceChanges = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["GB"] = -5.12m,
            ["US"] = -4.34m,
            ["FR"] = -3.87m,
            ["DE"] = -1.23m,
            ["SG"] = -5.50m,
            ["GR"] = -5.68m
        };
    }

    [Benchmark]
    public IReadOnlyList<PortfolioCalculationResult> CalculatePortfolioStress() =>
        PortfolioCalculator.Calculate(_loans, _portfolios, _ratings, _housePriceChanges);
}
