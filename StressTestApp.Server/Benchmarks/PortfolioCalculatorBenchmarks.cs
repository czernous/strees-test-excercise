using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Shared.Models;

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
        var parser = new CsvParser(new FileLoader(), NullLogger<CsvParser>.Instance);
        var csvDirectory = RepositoryPaths.FindDataCsvDirectory();

        _portfolios = (await parser.ParseAsync<Portfolio>(Path.Combine(csvDirectory, "portfolios.csv"))).Value;
        _loans = (await parser.ParseAsync<Loan>(Path.Combine(csvDirectory, "loans.csv"))).Value;
        _ratings = (await parser.ParseAsync<Rating>(Path.Combine(csvDirectory, "ratings.csv"))).Value;
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


