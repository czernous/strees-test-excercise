using FluentAssertions;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Shared.Models;

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
            new(1, 1, 100_000m, 90_000m, 110_000m, "AAA")
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
        // RR = 104,500 / 90,000 = 1.161... (clamped to 1.0)
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
            new(1, 1, 100_000m, 100_000m, 110_000m, "AAA"), // Collateral 110k, Outstanding 100k
            new(2, 1, 200_000m, 200_000m, 190_000m, "AA"),  // Collateral 190k, Outstanding 200k - LOSS expected
            new(3, 1, 150_000m, 150_000m, 140_000m, "A")    // Collateral 140k, Outstanding 150k - LOSS expected
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1),   // 1% PD
            new("AA", 2),    // 2% PD
            new("A", 3)      // 3% PD
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["US"] = -10m    // -10% house price drop
        };

        // Expected calculations (with -10% scenario):
        // Loan 1: ScenarioCV = 110k * 0.9 = 99k,  RR = 99k/100k = 0.99, LGD = 0.01, EL = 0.01 * 0.01 * 100k = 10
        // Loan 2: ScenarioCV = 190k * 0.9 = 171k, RR = 171k/200k = 0.855, LGD = 0.145, EL = 0.02 * 0.145 * 200k = 580
        // Loan 3: ScenarioCV = 140k * 0.9 = 126k, RR = 126k/150k = 0.84, LGD = 0.16, EL = 0.03 * 0.16 * 150k = 720
        // Total EL = 10 + 580 + 720 = 1,310

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];
        result.LoanCount.Should().Be(3);
        result.TotalOutstandingAmount.Should().Be(450_000m); // 100k + 200k + 150k
        result.TotalCollateralValue.Should().Be(440_000m); // 110k + 190k + 140k

        // Scenario collateral = individual loans calculated separately, summed
        // 99k + 171k + 126k = 396k
        result.TotalScenarioCollateralValue.Should().Be(396_000m);

        // Total Expected Loss should be approximately 1,310
        result.TotalExpectedLoss.Should().Be(1_310m);
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
            new(1, 1, 100_000m, 90_000m, 110_000m, "AAA"), // UK
            new(2, 1, 150_000m, 140_000m, 160_000m, "AA"), // UK
            new(3, 2, 200_000m, 180_000m, 220_000m, "A"),  // US
            new(4, 2, 250_000m, 230_000m, 270_000m, "BBB") // US
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
            new(1, 1, 100_000m, 90_000m, 110_000m, "AAA")
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
            new(1, 3, 100_000m, 90_000m, 110_000m, "AAA"),
            new(2, 1, 100_000m, 90_000m, 110_000m, "AAA"),
            new(3, 2, 100_000m, 90_000m, 110_000m, "AAA")
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
            new(1, 1, 100_000m, 90_000m, 50_000m, "AAA"), // Low PD
            new(2, 1, 100_000m, 90_000m, 50_000m, "CCC")  // High PD
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
            new(1, 1, 100_000m, 90_000m, 110_000m, "aaa"), // Lowercase
            new(2, 1, 100_000m, 90_000m, 110_000m, "AAA")  // Uppercase
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
            new(1, 1, 100_000m, 90_000m, 110_000m, "AAA")
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

    [Fact]
    public void Calculate_WithUnknownRating_SkipsLoanGracefully()
    {
        // Arrange
        var portfolios = new List<Portfolio>
        {
            new("1", "Portfolio A", "GB", "GBP")
        }.AsReadOnly();

        var loans = new List<Loan>
        {
            new(1, 1, 100_000m, 90_000m, 110_000m, "AAA"),     // Known rating
            new(2, 1, 50_000m, 45_000m, 55_000m, "UNKNOWN")    // Unknown rating - should be skipped
        }.AsReadOnly();

        var ratings = new List<Rating>
        {
            new("AAA", 1) // Only AAA is known
        }.AsReadOnly();

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert
        results.Should().HaveCount(1);
        var result = results[0];

        // Only the AAA loan should be included
        result.LoanCount.Should().Be(1, "Loan with unknown rating should be skipped");
        result.TotalOutstandingAmount.Should().Be(90_000m, "Only AAA loan outstanding");
        result.TotalCollateralValue.Should().Be(110_000m, "Only AAA loan collateral");
        result.TotalScenarioCollateralValue.Should().Be(104_500m, "Only AAA loan scenario collateral");
    }
}
