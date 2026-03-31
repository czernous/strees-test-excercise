using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.Csv.Parser.Maps;
using StressTestApp.Server.Shared.Models;

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
            .Returns(expectedPortfolios);

        // Act
        var result = await _store.GetPortfoliosAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedPortfolios);
        await _mockParser.
            Received(1)
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
            .Returns(expectedPortfolios);

        // Act
        var firstResult = await _store.GetPortfoliosAsync(CancellationToken.None);
        var secondResult = await _store.GetPortfoliosAsync(CancellationToken.None);

        // Assert
        firstResult.Should().BeSameAs(expectedPortfolios);
        secondResult.Should().BeSameAs(expectedPortfolios);
        secondResult.Should().BeSameAs(firstResult);
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
            .Returns(callInfo =>
            {
                return Task.Delay(loadDelay).ContinueWith(_ => (IReadOnlyList<Portfolio>)expectedPortfolios);
            });

        // Act - Start 5 concurrent calls
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _store.GetPortfoliosAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeSameAs(expectedPortfolios));
        results.Should().OnlyContain(r => ReferenceEquals(r, expectedPortfolios));
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
            .Returns(expectedLoans);

        // Act
        var result = await _store.GetLoansAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedLoans);
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
            .Returns(expectedLoans);

        // Act
        var firstResult = await _store.GetLoansAsync(CancellationToken.None);
        var secondResult = await _store.GetLoansAsync(CancellationToken.None);

        // Assert
        firstResult.Should().BeSameAs(expectedLoans);
        secondResult.Should().BeSameAs(expectedLoans);
        secondResult.Should().BeSameAs(firstResult);
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
            .Returns(expectedRatings);

        // Act
        var result = await _store.GetRatingsAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedRatings);
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
            .Returns(expectedRatings);

        // Act
        var firstResult = await _store.GetRatingsAsync(CancellationToken.None);
        var secondResult = await _store.GetRatingsAsync(CancellationToken.None);

        // Assert
        firstResult.Should().BeSameAs(expectedRatings);
        secondResult.Should().BeSameAs(expectedRatings);
        secondResult.Should().BeSameAs(firstResult);
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
            .Returns(portfolios);

        _mockParser
            .ParseAsync<Loan, LoanMap>(_csvPaths.Loans, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                return Task.Delay(loansLoadDelay).ContinueWith(_ => (IReadOnlyList<Loan>)loans);
            });

        // Act
        var loansTask = _store.GetLoansAsync(CancellationToken.None);
        await Task.Delay(50); // Give loans task time to start loading
        var portfoliosTask = _store.GetPortfoliosAsync(CancellationToken.None);

        // Assert - portfolios should complete before loans
        var portfolioResult = await portfoliosTask;
        portfolioResult.Should().BeSameAs(portfolios);

        loansTask.IsCompleted.Should().BeFalse("Loans should still be loading");

        var loansResult = await loansTask;
        loansResult.Should().BeSameAs(loans);
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
        var act = async () => await _store.GetPortfoliosAsync(CancellationToken.None);

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
                     callInfo => Task.FromResult<IReadOnlyList<Portfolio>>(portfolios));

        // Act
        var firstCall = async () => await _store.GetPortfoliosAsync(CancellationToken.None);
        await firstCall.Should().ThrowAsync<InvalidOperationException>();

        var secondResult = await _store.GetPortfoliosAsync(CancellationToken.None);

        // Assert
        secondResult.Should().BeSameAs(portfolios);
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
            .Returns(portfolios);

        // Act
        var act = async () => await _store.GetPortfoliosAsync(cts.Token);

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
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                return Task.Delay(100, token).ContinueWith(_ => (IReadOnlyList<Portfolio>)portfolios, token);
            });

        // Act
        var loadTask = _store.GetPortfoliosAsync(cts.Token);
        await Task.Delay(20);
        cts.Cancel();

        var act = async () => await loadTask;
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Reset mock for second call
        _mockParser.ClearReceivedCalls();
        _mockParser
            .ParseAsync<Portfolio, PortfolioMap>(_csvPaths.Portfolios, Arg.Any<CancellationToken>())
            .Returns(portfolios);

        // Second call should reload
        var secondResult = await _store.GetPortfoliosAsync(CancellationToken.None);

        // Assert
        secondResult.Should().BeSameAs(portfolios);
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
