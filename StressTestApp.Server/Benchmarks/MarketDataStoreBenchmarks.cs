using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Options;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Infrastructure.CsvLoader;
using StressTestApp.Server.Infrastructure.CsvLoader.Configurations;
using StressTestApp.Server.Persistence.MarketDataStore;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class MarketDataStoreBenchmarks
{
    private CsvPaths _paths = null!;
    private MarketDataStore _warmStore = null!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        var csvDirectory = RepositoryPaths.FindServerDataCsvDirectory();
        _paths = new CsvPaths
        {
            Portfolios = Path.Combine(csvDirectory, "portfolios.csv"),
            Loans = Path.Combine(csvDirectory, "loans.csv"),
            Ratings = Path.Combine(csvDirectory, "ratings.csv")
        };

        _warmStore = CreateStore();
        _ = await _warmStore.GetLoansAsync(CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup() => _warmStore.Dispose();

    [Benchmark]
    public async Task<int> ColdLoadLoansAsync()
    {
        using var store = CreateStore();
        var result = await store.GetLoansAsync(CancellationToken.None);
        return result.Count;
    }

    [Benchmark]
    public async Task<int> WarmCacheLoansAsync()
    {
        var result = await _warmStore.GetLoansAsync(CancellationToken.None);
        return result.Count;
    }

    private MarketDataStore CreateStore() =>
        new(new CsvLoader(), Options.Create(_paths));
}
