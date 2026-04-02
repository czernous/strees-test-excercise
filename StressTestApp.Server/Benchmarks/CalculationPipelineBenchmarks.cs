using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Data;
using StressTestApp.Server.Features.Calculations.Create;
using StressTestApp.Server.Infrastructure.CsvLoader;
using StressTestApp.Server.Infrastructure.CsvLoader.Configurations;
using StressTestApp.Server.Persistence.MarketDataStore;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CalculationPipelineBenchmarks
{
    private static readonly IReadOnlyDictionary<string, decimal> BaseHousePriceChanges =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["GB"] = -5.12m,
            ["US"] = -4.34m,
            ["FR"] = -3.87m,
            ["DE"] = -1.23m,
            ["SG"] = -5.50m,
            ["GR"] = -5.68m
        };

    private CsvPaths _paths = null!;
    private MarketDataStore _warmStore = null!;
    private int _requestSequence;

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
        _ = await _warmStore.GetPortfoliosAsync(CancellationToken.None);
        _ = await _warmStore.GetLoansAsync(CancellationToken.None);
        _ = await _warmStore.GetRatingsAsync(CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup() => _warmStore.Dispose();

    [Benchmark]
    public async Task<Guid> CreateCalculationColdAsync()
    {
        using var store = CreateStore();
        await using var db = await CreateDbContextAsync();

        var result = await CreateCalculationHandler.Handle(
            CreateRequest(),
            store,
            db,
            CancellationToken.None);

        return ExtractCalculationId(result.Result);
    }

    [Benchmark]
    public async Task<Guid> CreateCalculationWarmAsync()
    {
        await using var db = await CreateDbContextAsync();

        var result = await CreateCalculationHandler.Handle(
            CreateRequest(),
            _warmStore,
            db,
            CancellationToken.None);

        return ExtractCalculationId(result.Result);
    }

    private CreateCalculationRequest CreateRequest()
    {
        var sequence = Interlocked.Increment(ref _requestSequence);
        var offset = sequence / 10_000m;

        return new CreateCalculationRequest(
            BaseHousePriceChanges.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value - offset,
                StringComparer.OrdinalIgnoreCase));
    }

    private MarketDataStore CreateStore() =>
        new(new CsvLoader(), Options.Create(_paths));

    private static async Task<StressTestDbContext> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StressTestDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new StressTestDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    private static Guid ExtractCalculationId(IResult result) =>
        result switch
        {
            Created<CreateCalculationResponse> created => created.Value!.CalculationId,
            _ => throw new InvalidOperationException($"Expected Created result but received {result.GetType().Name}.")
        };
}
