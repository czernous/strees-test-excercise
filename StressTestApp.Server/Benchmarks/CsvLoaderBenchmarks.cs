using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Data.Models;
using StressTestApp.Server.Infrastructure.CsvLoader;
using StressTestApp.Server.Infrastructure.CsvLoader.Maps;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CsvLoaderBenchmarks
{
    private CsvLoader _loader = null!;
    private string _csvDirectory = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _loader = new CsvLoader();
        _csvDirectory = RepositoryPaths.FindServerDataCsvDirectory();
    }

    [Benchmark]
    public async Task<int> LoadLoansAsync() =>
        (await _loader.LoadCsvAsync<Loan, LoanMap>(Path.Combine(_csvDirectory, "loans.csv"))).Count;

    [Benchmark]
    public async Task<int> LoadPortfoliosAsync() =>
        (await _loader.LoadCsvAsync<Portfolio, PortfolioMap>(Path.Combine(_csvDirectory, "portfolios.csv"))).Count;

    [Benchmark]
    public async Task<int> LoadRatingsAsync() =>
        (await _loader.LoadCsvAsync<Rating, RatingMap>(Path.Combine(_csvDirectory, "ratings.csv"))).Count;
}
