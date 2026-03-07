using FluentAssertions;
using StressTestApp.Server.Data.Models;
using StressTestApp.Server.Features.Calculations.Compute;

namespace StressTestApp.Tests.Features.Calculations;

public class PortfolioCalculatorTests
{
    [Fact]
    public void Calculate_WithSingleLoanAndPortfolio_AggregatesCorrectly()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "GB", "GBP")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 110_000, "AAA")
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1) // 1% PD
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m
        };

        // Expected:
        // Scenario Collateral = 110,000 * 0.95 = 104,500
        // RR = 104,500 / 100,000 = 1.045 (clamped to 1.0)
        // LGD = 0
        // EL = 0

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.PortfolioId.Should().Be("1");
        result.PortfolioName.Should().Be("Portfolio A");
        result.Country.Should().Be("GB");
        result.Currency.Should().Be("GBP");
        result.TotalOutstandingAmount.Should().Be(90_000m);
        result.TotalCollateralValue.Should().Be(110_000m);
        result.TotalScenarioCollateralValue.Should().Be(104_500m);
        result.TotalExpectedLoss.Should().Be(0m);
        result.LoanCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_WithMultipleLoansInSamePortfolio_SumsCorrectly()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "US", "USD")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 110_000, "AAA"),
            new(2, 1, 200_000, 180_000, 220_000, "AA"),
            new(3, 1, 150_000, 140_000, 160_000, "A")
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1),
            new("AA", 2),
            new("A", 3)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["US"] = -10m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.LoanCount.Should().Be(3);
        result.TotalOutstandingAmount.Should().Be(410_000m); // 90k + 180k + 140k
        result.TotalCollateralValue.Should().Be(490_000m); // 110k + 220k + 160k
        
        // Scenario collateral = 490,000 * 0.9 = 441,000
        result.TotalScenarioCollateralValue.Should().Be(441_000m);
        
        // Each loan's EL needs to be calculated individually and summed
        result.TotalExpectedLoss.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Calculate_WithMultiplePortfolios_GroupsCorrectly()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "UK Portfolio", "GB", "GBP"),
            new("2", "US Portfolio", "US", "USD")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 110_000, "AAA"), // UK
            new(2, 1, 150_000, 140_000, 160_000, "AA"), // UK
            new(3, 2, 200_000, 180_000, 220_000, "A"),  // US
            new(4, 2, 250_000, 230_000, 270_000, "BBB") // US
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1),
            new("AA", 2),
            new("A", 3),
            new("BBB", 5)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m,
            ["US"] = -10m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(2);
        
        var ukPortfolio = results.First(r => r.Country == "GB");
        ukPortfolio.LoanCount.Should().Be(2);
        ukPortfolio.TotalOutstandingAmount.Should().Be(230_000m); // 90k + 140k
        
        var usPortfolio = results.First(r => r.Country == "US");
        usPortfolio.LoanCount.Should().Be(2);
        usPortfolio.TotalOutstandingAmount.Should().Be(410_000m); // 180k + 230k
    }

    [Fact]
    public void Calculate_WithCountryNotInHousePriceChanges_UsesZeroChange()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "FR", "EUR")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 110_000, "AAA")
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m // FR not specified
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        
        // With 0% change, scenario collateral = original collateral
        result.TotalScenarioCollateralValue.Should().Be(110_000m);
    }

    [Fact]
    public void Calculate_WithNoLoans_ReturnsEmptyResults()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "GB", "GBP")
        }.AsReadOnly();

        var loans = new List<Loan>().AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_OrdersResultsByPortfolioId()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("3", "Portfolio C", "GB", "GBP"),
            new("1", "Portfolio A", "US", "USD"),
            new("2", "Portfolio B", "FR", "EUR")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 3, 100_000, 90_000, 110_000, "AAA"),
            new(2, 1, 100_000, 90_000, 110_000, "AAA"),
            new(3, 2, 100_000, 90_000, 110_000, "AAA")
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m,
            ["US"] = -5m,
            ["FR"] = -5m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(3);
        results[0].PortfolioId.Should().Be("1");
        results[1].PortfolioId.Should().Be("2");
        results[2].PortfolioId.Should().Be("3");
    }

    [Fact]
    public void Calculate_WithDifferentRatings_AppliesCorrectPD()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "GB", "GBP")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 50_000, "AAA"), // Low PD
            new(2, 1, 100_000, 90_000, 50_000, "CCC")  // High PD
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1),   // 1% PD
            new("CCC", 50)   // 50% PD
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -50m // Severe crash to ensure losses
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        
        // The CCC loan should contribute significantly more to expected loss
        result.TotalExpectedLoss.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Calculate_WithCaseInsensitiveRatingLookup_WorksCorrectly()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "GB", "GBP")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 110_000, "aaa"), // Lowercase
            new(2, 1, 100_000, 90_000, 110_000, "AAA")  // Uppercase
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m
        };

        // Act
        var act = () => PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        act.Should().NotThrow("Rating lookup should be case-insensitive");
    }

    [Theory]
    [InlineData(-5.12)]
    [InlineData(-4.34)]
    [InlineData(-3.87)]
    [InlineData(-1.23)]
    [InlineData(-5.5)]
    [InlineData(-5.68)]
    public void Calculate_WithExampleHousePriceChanges_ProducesValidResults(decimal pctChange)
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Test Portfolio", "GB", "GBP")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000, 90_000, 110_000, "AAA")
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1)
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = pctChange
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        
        // Invariants
        result.TotalExpectedLoss.Should().BeGreaterThanOrEqualTo(0m);
        result.TotalScenarioCollateralValue.Should().BeGreaterThanOrEqualTo(0m);
        result.TotalOutstandingAmount.Should().Be(90_000m);
        result.TotalCollateralValue.Should().Be(110_000m);
    }
}
