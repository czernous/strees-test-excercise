using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Core.Storage.MarketDataStore;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Verification.TestSupport;

namespace StressTestApp.Server.Verification;

public sealed class MarketDataStoreVerificationTests
{
    private static CsvPaths BuildCsvPaths()
    {
        var csvDirectory = RepositoryPaths.FindDataCsvDirectory();

        return new CsvPaths
        {
            Portfolios = Path.Combine(csvDirectory, "portfolios.csv"),
            Loans = Path.Combine(csvDirectory, "loans.csv"),
            Ratings = Path.Combine(csvDirectory, "ratings.csv")
        };
    }

    private static ICsvParser CreateRealParser() =>
        new CsvParser(new FileLoader(), NullLogger<CsvParser>.Instance);

    [Fact]
    public async Task GetOrCacheAsync_WithRealData_PopulatesCacheAndCountrySet()
    {
        var paths = BuildCsvPaths();
        using var store = new MarketDataStore(CreateRealParser(), Options.Create(paths));

        var portfolios = await store.GetOrCacheAsync<Portfolio>(CancellationToken.None);
        var loans = await store.GetOrCacheAsync<Loan>(CancellationToken.None);
        var ratings = await store.GetOrCacheAsync<Rating>(CancellationToken.None);

        portfolios.IsSuccess.Should().BeTrue();
        loans.IsSuccess.Should().BeTrue();
        ratings.IsSuccess.Should().BeTrue();

        portfolios.Value.Should().NotBeEmpty();
        loans.Value.Should().NotBeEmpty();
        ratings.Value.Should().NotBeEmpty();
        store.AvailableCountries.Should().Contain(["GB", "US", "FR", "DE", "SG", "GR"]);
    }

    [Fact]
    public async Task GetOrCacheAsync_WarmCache_ReturnsSameReferenceWithoutReloading()
    {
        var paths = BuildCsvPaths();
        var countingParser = new CountingCsvParser(CreateRealParser());
        using var store = new MarketDataStore(countingParser, Options.Create(paths));

        var first = await store.GetOrCacheAsync<Loan>(CancellationToken.None);
        var second = await store.GetOrCacheAsync<Loan>(CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().BeSameAs(first.Value);
        countingParser.GetCallCount<Loan>().Should().Be(1);
    }

    [Fact]
    public async Task GetOrCacheAsync_ConcurrentColdLoads_LoadEachTypeOnceAndShareInstances()
    {
        var paths = BuildCsvPaths();
        var countingParser = new CountingCsvParser(CreateRealParser());
        using var store = new MarketDataStore(countingParser, Options.Create(paths));

        var portfolioTasks = Enumerable.Range(0, 12)
            .Select(_ => store.GetOrCacheAsync<Portfolio>(CancellationToken.None).AsTask());
        var loanTasks = Enumerable.Range(0, 12)
            .Select(_ => store.GetOrCacheAsync<Loan>(CancellationToken.None).AsTask());
        var ratingTasks = Enumerable.Range(0, 12)
            .Select(_ => store.GetOrCacheAsync<Rating>(CancellationToken.None).AsTask());

        var portfolioResults = await Task.WhenAll(portfolioTasks);
        var loanResults = await Task.WhenAll(loanTasks);
        var ratingResults = await Task.WhenAll(ratingTasks);

        portfolioResults.Should().OnlyContain(result => result.IsSuccess);
        loanResults.Should().OnlyContain(result => result.IsSuccess);
        ratingResults.Should().OnlyContain(result => result.IsSuccess);
        countingParser.GetCallCount<Portfolio>().Should().Be(1);
        countingParser.GetCallCount<Loan>().Should().Be(1);
        countingParser.GetCallCount<Rating>().Should().Be(1);

        portfolioResults.Should().OnlyContain(result => ReferenceEquals(result.Value, portfolioResults[0].Value));
        loanResults.Should().OnlyContain(result => ReferenceEquals(result.Value, loanResults[0].Value));
        ratingResults.Should().OnlyContain(result => ReferenceEquals(result.Value, ratingResults[0].Value));
    }

    [Fact]
    public async Task GetOrCacheAsync_TransientPortfolioFailure_DoesNotPoisonCacheAndRecoversOnRetry()
    {
        var paths = BuildCsvPaths();
        var flakyParser = new FlakyCsvParser(
            CreateRealParser(),
            static (type, attempt) => type == typeof(Portfolio) && attempt == 1
                ? Error.Create("Parser.Transient", "Synthetic transient parser failure.")
                : null);

        using var store = new MarketDataStore(flakyParser, Options.Create(paths));

        var firstAttempt = await store.GetOrCacheAsync<Portfolio>(CancellationToken.None);

        firstAttempt.IsSuccess.Should().BeFalse();
        store.AvailableCountries.Should().BeEmpty();

        var recoveryBatch = await Task.WhenAll(
            Enumerable.Range(0, 6)
                .Select(_ => store.GetOrCacheAsync<Portfolio>(CancellationToken.None).AsTask()));

        recoveryBatch.Should().OnlyContain(result => result.IsSuccess);
        recoveryBatch.Should().OnlyContain(result => ReferenceEquals(result.Value, recoveryBatch[0].Value));
        flakyParser.GetAttemptCount<Portfolio>().Should().Be(2);
        store.AvailableCountries.Should().NotBeEmpty();
    }
}

