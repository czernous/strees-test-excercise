using FluentAssertions;
using StressTestApp.Server.Infrastructure.CsvLoader.Maps;
using StressTestApp.Server.Infrastructure.CsvLoader.Interfaces;
using StressTestApp.Server.Data.Models;

namespace StressTestApp.Tests.Infrastructure.CsvLoader;

public class CsvLoaderTests
{
    private readonly ICsvDataLoader _csvLoader;
    private readonly string _testDataPath;

    public CsvLoaderTests()
    {
        _csvLoader = new Server.Infrastructure.CsvLoader.CsvLoader();
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Csv");
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidLoanData_LoadsAllRecordsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_loans.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidLoanData_MapsHeadersToFieldsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_loans.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        var firstLoan = result[0];
        firstLoan.Id.Should().Be(1, "Loan_ID header should map to Id property");
        firstLoan.PortId.Should().Be(101, "Port_ID header should map to PortId property");
        firstLoan.OriginalAmount.Should().Be(100000, "OriginalLoanAmount header should map to OriginalAmount property");
        firstLoan.OutstandingAmount.Should().Be(90000, "OutstandingAmount header should map to OutstandingAmount property");
        firstLoan.CollateralValue.Should().Be(110000, "CollateralValue header should map to CollateralValue property");
        firstLoan.CreditRating.Should().Be("AAA", "CreditRating header should map to CreditRating property");
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidPortfolioData_LoadsAllRecordsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_portfolios.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidPortfolioData_MapsHeadersToFieldsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_portfolios.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Portfolio, PortfolioMap>(filePath);

        // Assert
        var firstPortfolio = result[0];
        firstPortfolio.Id.Should().Be("101", "Port_ID header should map to Id property");
        firstPortfolio.Name.Should().Be("Test Portfolio 1", "Port_Name header should map to Name property");
        firstPortfolio.Country.Should().Be("US", "Port_Country header should map to Country property");
        firstPortfolio.Ccy.Should().Be("USD", "Port_CCY header should map to Ccy property");
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidRatingData_LoadsAllRecordsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_ratings.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidRatingData_MapsHeadersToFieldsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_ratings.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Rating, RatingMap>(filePath);

        // Assert
        var firstRating = result[0];
        firstRating.RatingValue.Should().Be("AAA", "Rating header should map to RatingValue property");
        firstRating.ProbabilityOfDefault.Should().Be(1, "ProbablilityOfDefault header should map to ProbabilityOfDefault property");

        var lastRating = result[3];
        lastRating.RatingValue.Should().Be("BBB");
        lastRating.ProbabilityOfDefault.Should().Be(40);
    }

    [Fact]
    public async Task LoadCsvAsync_WithEmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "empty_loans.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadCsvAsync_WithIncorrectHeaders_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "invalid_headers.csv");

        // Act
        var act = async () => await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadCsvAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "nonexistent.csv");

        // Act
        var act = async () => await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"CSV file not found: {filePath}");
    }

    [Fact]
    public async Task LoadCsvAsync_WithWhitespaceInFields_TrimsValuesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "loans_with_whitespace.csv");

        // Act
        var result = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        result.Should().HaveCount(3);
        result[0].CreditRating.Should().Be("AAA", "leading and trailing whitespace should be trimmed");
        result[1].CreditRating.Should().Be("AA", "trailing whitespace should be trimmed");
        result[2].CreditRating.Should().Be("A", "leading whitespace should be trimmed");
    }

    [Fact]
    public async Task LoadCsvAsync_CalledMultipleTimes_ReturnsConsistentData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_loans.csv");

        // Act
        var result1 = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);
        var result2 = await _csvLoader.LoadCsvAsync<Loan, LoanMap>(filePath);

        // Assert
        result1.Should().HaveCount(3);
        result2.Should().HaveCount(3);
        result1.Should().BeEquivalentTo(result2);
    }
}
