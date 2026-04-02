using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Core.Storage.MarketDataStore;
using StressTestApp.Server.Shared.Models;

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
        var csvDirectory = RepositoryPaths.FindDataCsvDirectory();
        _paths = new CsvPaths
        {
            Portfolios = Path.Combine(csvDirectory, "portfolios.csv"),
            Loans = Path.Combine(csvDirectory, "loans.csv"),
            Ratings = Path.Combine(csvDirectory, "ratings.csv")
        };

        _warmStore = CreateStore();
        _ = await _warmStore.GetOrCacheAsync<Loan>(CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _warmStore.Dispose();
    }

    [Benchmark]
    public async Task<int> ColdLoadLoansAsync()
    {
        using var store = CreateStore();
        var result = await store.GetOrCacheAsync<Loan>(CancellationToken.None);
        return result.Value.Count;
    }

    [Benchmark]
    public async Task<int> WarmCacheLoansAsync()
    {
        var result = await _warmStore.GetOrCacheAsync<Loan>(CancellationToken.None);
        return result.Value.Count;
    }

    private MarketDataStore CreateStore() =>
        new(
            new CsvParser(new FileLoader(), NullLogger<CsvParser>.Instance),
            Options.Create(_paths));
}

