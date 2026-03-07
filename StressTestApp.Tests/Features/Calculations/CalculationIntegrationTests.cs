using FluentAssertions;
using StressTestApp.Server.Infrastructure.CsvLoader;
using StressTestApp.Server.Infrastructure.CsvLoader.Maps;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Data.Models;

namespace StressTestApp.Tests.Features.Calculations;

public class CalculationIntegrationTests
{
    private readonly string _testDataPath;
    private readonly CsvLoader _csvLoader;

    public CalculationIntegrationTests()
    {
        _csvLoader = new CsvLoader();
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Csv");
    }

    [Fact]
    public async Task Calculate_WithRealCsvData_ProducesReasonableResults()
    {
        // Arrange - Load real CSV data
        var portfoliosPath = Path.Combine(_testDataPath, "valid_portfolios.csv");
        var loansPath = Path.Combine(_testDataPath, "valid_loans.csv");
        var ratingsPath = Path.Combine(_testDataPath, "valid_ratings.csv");

        var portfolios = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(portfoliosPath);
        var loans = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(loansPath);
        var ratings = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(ratingsPath);

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5.12m,
            ["US"] = -4.34m,
            ["FR"] = -3.87m,
            ["DE"] = -1.23m,
            ["SG"] = -5.5m,
            ["GR"] = -5.68m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert - Sanity checks
        results.Should().NotBeEmpty("Should have at least one portfolio with loans");
        
        results.Should().AllSatisfy(r =>
        {
            r.PortfolioId.Should().NotBeNullOrEmpty();
            r.PortfolioName.Should().NotBeNullOrEmpty();
            r.Country.Should().NotBeNullOrEmpty();
            r.Currency.Should().NotBeNullOrEmpty();
            r.LoanCount.Should().BeGreaterThan(0, "Each portfolio should have loans");
            r.TotalOutstandingAmount.Should().BeGreaterThan(0m);
            r.TotalCollateralValue.Should().BeGreaterThan(0m);
            r.TotalScenarioCollateralValue.Should().BeGreaterThanOrEqualTo(0m);
            r.TotalExpectedLoss.Should().BeGreaterThanOrEqualTo(0m);
        });

        // Total expected loss across all portfolios should be positive (given negative price changes)
        var totalExpectedLoss = results.Sum(r => r.TotalExpectedLoss);
        totalExpectedLoss.Should().BeGreaterThanOrEqualTo(0m);

        // Scenario collateral should be less than original collateral (due to negative changes)
        results.Should().AllSatisfy(r =>
        {
            if (housePriceChanges.ContainsKey(r.Country))
            {
                var change = housePriceChanges[r.Country];
                if (change < 0)
                {
                    r.TotalScenarioCollateralValue.Should().BeLessThan(r.TotalCollateralValue,
                        $"Portfolio in {r.Country} with {change}% change should have reduced collateral");
                }
            }
        });

        // Number of portfolios in results should match number of portfolios with loans
        var portfolioIdsWithLoans = loans.Select(l => l.PortId).Distinct().Count();
        results.Should().HaveCount(portfolioIdsWithLoans);
    }

    [Fact]
    public async Task Calculate_WithRealCsvData_AggregatesLoansCorrectly()
    {
        // Arrange
        var portfoliosPath = Path.Combine(_testDataPath, "valid_portfolios.csv");
        var loansPath = Path.Combine(_testDataPath, "valid_loans.csv");
        var ratingsPath = Path.Combine(_testDataPath, "valid_ratings.csv");

        var portfolios = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(portfoliosPath);
        var loans = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(loansPath);
        var ratings = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(ratingsPath);

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5m
        };

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert - Verify aggregation correctness
        foreach (var result in results)
        {
            // Get all loans for this portfolio
            var portfolioLoans = loans.Where(l => l.PortId.ToString() == result.PortfolioId).ToList();
            
            // Verify loan count
            result.LoanCount.Should().Be(portfolioLoans.Count);
            
            // Verify sum of outstanding amounts
            var expectedOutstanding = portfolioLoans.Sum(l => l.OutstandingAmount);
            result.TotalOutstandingAmount.Should().Be(expectedOutstanding);
            
            // Verify sum of collateral values
            var expectedCollateral = portfolioLoans.Sum(l => l.CollateralValue);
            result.TotalCollateralValue.Should().Be(expectedCollateral);
        }
    }

    [Fact]
    public async Task Calculate_WithMultipleRuns_ProducesConsistentResults()
    {
        // Arrange
        var portfoliosPath = Path.Combine(_testDataPath, "valid_portfolios.csv");
        var loansPath = Path.Combine(_testDataPath, "valid_loans.csv");
        var ratingsPath = Path.Combine(_testDataPath, "valid_ratings.csv");

        var portfolios = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(portfoliosPath);
        var loans = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(loansPath);
        var ratings = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(ratingsPath);

        var housePriceChanges = new Dictionary<string, decimal>
        {
            ["GB"] = -5.12m,
            ["US"] = -4.34m
        };

        // Act - Run calculation twice
        var results1 = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);
        
        var results2 = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert - Results should be identical
        results1.Should().HaveCount(results2.Count);
        
        for (int i = 0; i < results1.Count; i++)
        {
            results1[i].PortfolioId.Should().Be(results2[i].PortfolioId);
            results1[i].TotalExpectedLoss.Should().Be(results2[i].TotalExpectedLoss);
            results1[i].TotalScenarioCollateralValue.Should().Be(results2[i].TotalScenarioCollateralValue);
        }
    }

    [Fact]
    public async Task Calculate_WithAllCountries_CoversAllPortfolios()
    {
        // Arrange
        var portfoliosPath = Path.Combine(_testDataPath, "valid_portfolios.csv");
        var loansPath = Path.Combine(_testDataPath, "valid_loans.csv");
        var ratingsPath = Path.Combine(_testDataPath, "valid_ratings.csv");

        var portfolios = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(portfoliosPath);
        var loans = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(loansPath);
        var ratings = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(ratingsPath);

        // Get all unique countries from portfolios
        var allCountries = portfolios.Select(p => p.Country).Distinct().ToList();

        // Create house price changes for all countries
        var housePriceChanges = allCountries.ToDictionary(
            country => country,
            country => -5m); // Apply -5% to all

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert - All results should have scenario collateral calculated
        results.Should().AllSatisfy(r =>
        {
            // With -5% change, scenario collateral should be 95% of original
            var expectedScenario = r.TotalCollateralValue * 0.95m;
            r.TotalScenarioCollateralValue.Should().BeApproximately(expectedScenario, 0.01m);
        });
    }

    [Fact]
    public async Task Calculate_WithZeroHousePriceChanges_ProducesMinimalLoss()
    {
        // Arrange
        var portfoliosPath = Path.Combine(_testDataPath, "valid_portfolios.csv");
        var loansPath = Path.Combine(_testDataPath, "valid_loans.csv");
        var ratingsPath = Path.Combine(_testDataPath, "valid_ratings.csv");

        var portfolios = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(portfoliosPath);
        var loans = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(loansPath);
        var ratings = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(ratingsPath);

        var allCountries = portfolios.Select(p => p.Country).Distinct().ToList();
        var housePriceChanges = allCountries.ToDictionary(country => country, _ => 0m);

        // Act
        var results = PortfolioCalculator.Calculate(
            loans, portfolios, ratings, housePriceChanges);

        // Assert - With no price change, scenario collateral should equal original collateral
        results.Should().AllSatisfy(r =>
        {
            r.TotalScenarioCollateralValue.Should().Be(r.TotalCollateralValue);
        });
    }
}
