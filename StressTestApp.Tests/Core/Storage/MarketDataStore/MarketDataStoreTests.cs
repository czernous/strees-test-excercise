using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.Csv.Parser.Maps;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Result;
using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Tests.Core.Storage.MarketDataStore;

public class MarketDataStoreTests : IDisposable
{
    private readonly ICsvParser _mockParser;
    private readonly CsvPaths _csvPaths;
    private readonly Server.Core.Storage.MarketDataStore.MarketDataStore _store;

    public MarketDataStoreTests()
    {
        _mockParser = Substitute.For<ICsvParser>();
        _csvPaths = new CsvPaths
        {
            Portfolios = "portfolios.csv",
            Loans = "loans.csv",
            Ratings = "ratings.csv"
        };
        _store = new Server.Core.Storage.MarketDataStore.MarketDataStore(_mockParser, Options.Create(_csvPaths));
    }

    public void Dispose()
    {
        _store.Dispose();
    }

    private static async IAsyncEnumerable<T> ToDelayedAsyncEnumerable<T>(IReadOnlyList<T> source, TimeSpan delay)
    {
        await Task.Delay(delay);
        foreach (var item in source)
        {
            yield return item;
        }
    }

    [Fact]
    public async Task GetPortfoliosAsync_FirstCall_LoadsDataFromCsvLoader()
    {
        // Arrange
        var expectedPortfolios = new List<Portfolio>
        {
            new("P1", "Portfolio 1", "US", "USD"),
            new("P2", "Portfolio 2", "UK", "GBP")
        }.AsReadOnly();

        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Portfolio>, Error>.Ok(expectedPortfolios));

        // Act
        var resultTask = await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);

        // Assert
        resultTask.IsSuccess.Should().BeTrue();
        resultTask.Value.Should().BeEquivalentTo(expectedPortfolios);
        await _mockParser
            .Received(1)
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPortfoliosAsync_SecondCall_ReturnsCachedData()
    {
        // Arrange
        var expectedPortfolios = new List<Portfolio>
        {
            new("P1", "Portfolio 1", "US", "USD")
        }.AsReadOnly();

        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Portfolio>, Error>.Ok(expectedPortfolios));

        // Act
        var firstResultTask = await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);
        var secondResultTask = await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);

        // Assert
        firstResultTask.IsSuccess.Should().BeTrue();
        secondResultTask.IsSuccess.Should().BeTrue();
        firstResultTask.Value.Should().BeEquivalentTo(expectedPortfolios);
        secondResultTask.Value.Should().BeEquivalentTo(expectedPortfolios);
        secondResultTask.Value.Should().BeSameAs(firstResultTask.Value, "Second call should return exact same cached instance");
        await _mockParser
                .Received(1)
                .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPortfoliosAsync_ConcurrentCalls_LoadsDataOnlyOnce()
    {
        // Arrange
        var expectedPortfolios = new List<Portfolio>
        {
            new("P1", "Portfolio 1", "US", "USD")
        }.AsReadOnly();

        var loadDelay = TimeSpan.FromMilliseconds(100);
        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Result<IReadOnlyList<Portfolio>, Error>>(
                Task.Delay(loadDelay).ContinueWith(_ => Result<IReadOnlyList<Portfolio>, Error>.Ok(expectedPortfolios))));

        // Act - Start 5 concurrent calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _store.GetOrCacheAsync<Portfolio>(CancellationToken.None).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.IsSuccess.Should().BeTrue();
            r.Value.Should().BeEquivalentTo(expectedPortfolios);
        });
        results.Should().OnlyContain(r => ReferenceEquals(r.Value, results[0].Value), "All should reference same cached instance");
        await _mockParser
                 .Received(1)
                 .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLoansAsync_FirstCall_LoadsDataFromCsvLoader()
    {
        // Arrange
        var expectedLoans = new List<Loan>
        {
            new(1, 101, 100000, 90000, 110000, "AAA"),
            new(2, 102, 200000, 180000, 220000, "AA")
        }.AsReadOnly();

        _mockParser
            .ParseAsync<Loan, LoanMap>(_csvPaths.Loans, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Loan>, Error>.Ok(expectedLoans));

        // Act
        var resultTask = await _store.GetOrCacheAsync<Loan>(CancellationToken.None);

        // Assert
        resultTask.IsSuccess.Should().BeTrue();
        resultTask.Value.Should().BeEquivalentTo(expectedLoans);
        await _mockParser
            .Received(1)
            .ParseAsync<Loan, LoanMap>(_csvPaths.Loans, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLoansAsync_SecondCall_ReturnsCachedData()
    {
        // Arrange
        var expectedLoans = new List<Loan>
        {
            new(1, 101, 100000, 90000, 110000, "AAA")
        }.AsReadOnly();

        _mockParser
            .ParseAsync<Loan, LoanMap>(_csvPaths.Loans, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Loan>, Error>.Ok(expectedLoans));

        // Act
        var firstResultTask = await _store.GetOrCacheAsync<Loan>(CancellationToken.None);
        var secondResultTask = await _store.GetOrCacheAsync<Loan>(CancellationToken.None);

        // Assert
        firstResultTask.IsSuccess.Should().BeTrue();
        secondResultTask.IsSuccess.Should().BeTrue();
        firstResultTask.Value.Should().BeEquivalentTo(expectedLoans);
        secondResultTask.Value.Should().BeEquivalentTo(expectedLoans);
        secondResultTask.Value.Should().BeSameAs(firstResultTask.Value);
        await _mockParser
            .Received(1)
            .ParseAsync<Loan, LoanMap>(_csvPaths.Loans, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRatingsAsync_FirstCall_LoadsDataFromCsvLoader()
    {
        // Arrange
        var expectedRatings = new List<Rating>
        {
            new("AAA", 1),
            new("AA", 2)
        }.AsReadOnly();

        _mockParser
            .ParseAsync<Rating, RatingMap>(_csvPaths.Ratings, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Rating>, Error>.Ok(expectedRatings));

        // Act
        var resultTask = await _store.GetOrCacheAsync<Rating>(CancellationToken.None);

        // Assert
        resultTask.IsSuccess.Should().BeTrue();
        resultTask.Value.Should().BeEquivalentTo(expectedRatings);
        await _mockParser
            .Received(1)
            .ParseAsync<Rating, RatingMap>(_csvPaths.Ratings, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRatingsAsync_SecondCall_ReturnsCachedData()
    {
        // Arrange
        var expectedRatings = new List<Rating>
        {
            new("AAA", 1)
        }.AsReadOnly();

        _mockParser
            .ParseAsync<Rating, RatingMap>(_csvPaths.Ratings, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Rating>, Error>.Ok(expectedRatings));

        // Act
        var firstResultTask = await _store.GetOrCacheAsync<Rating>(CancellationToken.None);
        var secondResultTask = await _store.GetOrCacheAsync<Rating>(CancellationToken.None);

        // Assert
        firstResultTask.IsSuccess.Should().BeTrue();
        secondResultTask.IsSuccess.Should().BeTrue();
        firstResultTask.Value.Should().BeEquivalentTo(expectedRatings);
        secondResultTask.Value.Should().BeEquivalentTo(expectedRatings);
        secondResultTask.Value.Should().BeSameAs(firstResultTask.Value);
        await _mockParser
            .Received(1)
            .ParseAsync<Rating, RatingMap>(_csvPaths.Ratings, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPortfoliosAsync_IndependentFromGetLoansAsync_DoesNotBlock()
    {
        // Arrange
        var portfolios = new List<Portfolio> { new("P1", "Portfolio 1", "US", "USD") }.AsReadOnly();
        var loans = new List<Loan> { new(1, 101, 100000, 90000, 110000, "AAA") }.AsReadOnly();

        var loansLoadDelay = TimeSpan.FromMilliseconds(200);

        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Portfolio>, Error>.Ok(portfolios));

        _mockParser
            .ParseAsync<Loan, LoanMap>(_csvPaths.Loans, Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Result<IReadOnlyList<Loan>, Error>>(
                Task.Delay(loansLoadDelay).ContinueWith(_ => Result<IReadOnlyList<Loan>, Error>.Ok((IReadOnlyList<Loan>)loans))));

        // Act
        var loansTask = _store.GetOrCacheAsync<Loan>(CancellationToken.None).AsTask();
        await Task.Delay(50); // Give loans task time to start loading
        var portfoliosTask = _store.GetOrCacheAsync<Portfolio>(CancellationToken.None).AsTask();

        // Assert - portfolios should complete before loans
        var portfolioResultTask = await portfoliosTask;
        portfolioResultTask.IsSuccess.Should().BeTrue();
        portfolioResultTask.Value.Should().BeEquivalentTo(portfolios);

        loansTask.IsCompleted.Should().BeFalse("Loans should still be loading");

        var loansResultTask = await loansTask;
        loansResultTask.IsSuccess.Should().BeTrue();
        loansResultTask.Value.Should().BeEquivalentTo(loans);
    }

    [Fact]
    public async Task GetPortfoliosAsync_LoaderThrowsException_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("File not found");
        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var act = async () => await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("File not found");
    }

    [Fact]
    public async Task GetPortfoliosAsync_LoaderThrowsException_DoesNotCacheFailure()
    {
        // Arrange
        var portfolios = new List<Portfolio> { new("P1", "Portfolio 1", "US", "USD") }.AsReadOnly();

        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(callInfo => throw new InvalidOperationException("First call fails"),
                     callInfo => Result<IReadOnlyList<Portfolio>, Error>.Ok(portfolios));

        // Act
        var firstCall = async () => await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);
        await firstCall.Should().ThrowAsync<InvalidOperationException>();

        var secondResult = await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().BeEquivalentTo(portfolios);
        await _mockParser
                   .Received(2)
                   .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPortfoliosAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var portfolios = new List<Portfolio> { new("P1", "Portfolio 1", "US", "USD") }.AsReadOnly();
        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Portfolio>, Error>.Ok(portfolios));

        // Act
        var act = async () => await _store.GetOrCacheAsync<Portfolio>(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetPortfoliosAsync_CancellationDuringLoad_DoesNotCachePartialData()
    {
        // Arrange
        var portfolios = new List<Portfolio> { new("P1", "Portfolio 1", "US", "USD") }.AsReadOnly();
        var cts = new CancellationTokenSource();

        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Result<IReadOnlyList<Portfolio>, Error>>(
                Task.Delay(100, cts.Token).ContinueWith(_ => 
                    Result<IReadOnlyList<Portfolio>, Error>.Ok((IReadOnlyList<Portfolio>)portfolios), cts.Token)));

        // Act
        var loadTask = _store.GetOrCacheAsync<Portfolio>(cts.Token).AsTask();
        await Task.Delay(20);
        cts.Cancel();

        var act = async () => await loadTask;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Reset mock for second call
        _mockParser.ClearReceivedCalls();
        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Portfolio>, Error>.Ok(portfolios));

        // Second call should reload
        var secondResult = await _store.GetOrCacheAsync<Portfolio>(CancellationToken.None);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().BeEquivalentTo(portfolios);
        await _mockParser
                  .Received(1)
                  .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Dispose_DisposesAllSemaphores()
    {
        // Arrange
        var store = new Server.Core.Storage.MarketDataStore.MarketDataStore(_mockParser, Options.Create(_csvPaths));

        // Act
        store.Dispose();

        // Assert - should not throw when called multiple times
        var act = () => store.Dispose();
        act.Should().NotThrow();
    }
}
