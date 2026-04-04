using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.FileLoader;

namespace StressTestApp.Tests.Core.IO.Csv.Parser;

public class CsvParserTests
{
    private readonly ICsvParser _csvParser;
    private readonly string _testDataPath;

    public CsvParserTests()
    {
        var fileLoader = new FileLoader();
        _csvParser = new CsvParser(fileLoader, NullLogger<CsvParser>.Instance);
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Csv");
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidLoanData_LoadsAllRecordsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_loans.csv");

        // Act
        var result = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidLoanData_MapsHeadersToFieldsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_loans.csv");

        // Act
        var result = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstLoan = result.Value[0];
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
        var result = await _csvParser.ParseAsync<Portfolio>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidPortfolioData_MapsHeadersToFieldsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_portfolios.csv");

        // Act
        var result = await _csvParser.ParseAsync<Portfolio>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstPortfolio = result.Value[0];
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
        var result = await _csvParser.ParseAsync<Rating>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(4);
    }

    [Fact]
    public async Task LoadCsvAsync_WithValidRatingData_MapsHeadersToFieldsCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_ratings.csv");

        // Act
        var result = await _csvParser.ParseAsync<Rating>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstRating = result.Value[0];
        firstRating.RatingValue.Should().Be("AAA", "Rating header should map to RatingValue property");
        firstRating.ProbabilityOfDefault.Should().Be(1, "ProbablilityOfDefault header should map to ProbabilityOfDefault property");

        var lastRating = result.Value[3];
        lastRating.RatingValue.Should().Be("BBB");
        lastRating.ProbabilityOfDefault.Should().Be(40);
    }

    [Fact]
    public async Task LoadCsvAsync_WithEmptyFile_ReturnsEmptyList()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "empty_loans.csv");

        // Act
        var result = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadCsvAsync_WithIncorrectHeaders_ReturnsFailureResult()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "invalid_headers.csv");

        // Act
        var result = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert - Should return failure Result with validation error
        result.IsSuccess.Should().BeFalse("Headers don't match expected schema");
        result.Error.Code.Should().StartWith("VAL_", "Should be a validation error");
    }

    [Fact]
    public async Task LoadCsvAsync_WithNonExistentFile_ReturnsFailureResult()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "nonexistent.csv");

        // Act
        var result = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert - Should return failure Result with IO error
        result.IsSuccess.Should().BeFalse("File doesn't exist");
        result.Error.Code.Should().StartWith("IO_", "Should be an IO error");
        result.Error.Message.Should().Contain("nonexistent.csv");
    }

    [Fact]
    public async Task LoadCsvAsync_WithWhitespaceInFields_TrimsValuesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "loans_with_whitespace.csv");

        // Act
        var result = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var loans = result.Value;
        loans.Should().HaveCount(3);
        loans[0].CreditRating.Should().Be("AAA", "leading and trailing whitespace should be normalized");
        loans[1].CreditRating.Should().Be("AA", "trailing whitespace should be normalized");
        loans[2].CreditRating.Should().Be("A", "leading whitespace should be normalized");
    }

    [Fact]
    public async Task LoadCsvAsync_CalledMultipleTimes_ReturnsConsistentData()
    {
        // Arrange
        var filePath = Path.Combine(_testDataPath, "valid_loans.csv");

        // Act
        var result1 = await _csvParser.ParseAsync<Loan>(filePath);
        var result2 = await _csvParser.ParseAsync<Loan>(filePath);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().HaveCount(3);
        result2.Value.Should().HaveCount(3);
        result1.Value.Should().BeEquivalentTo(result2.Value);
    }

    [Fact]
    public async Task LoadCsvAsync_WithEmptyRequiredFields_SkipsInvalidRows()
    {
        // Arrange - Create CSV with some empty fields
        var filePath = Path.Combine(_testDataPath, "portfolios_with_empty_fields.csv");
        var csvContent = """
            Port_ID,Port_Name,Port_Country,Port_CCY
            1,Valid Portfolio,US,USD
            2,Missing Country,,EUR
            3,,GB,GBP
            4,Another Valid,FR,EUR
            """;
        await File.WriteAllTextAsync(filePath, csvContent);

        // Act
        var result = await _csvParser.ParseAsync<Portfolio>(filePath);

        // Assert - Parser skips rows with ANY empty field
        result.IsSuccess.Should().BeTrue();
        var portfolios = result.Value;
        portfolios.Should().HaveCount(2, "Only rows without empty fields should be loaded");
        portfolios.Should().Contain(p => p.Id == "1" && p.Country == "US");
        portfolios.Should().Contain(p => p.Id == "4" && p.Country == "FR");
        portfolios.Should().AllSatisfy(p => ((IIntegrityContract)p).Validate().Should().BeNull());
    }

    [Fact]
    public async Task LoadCsvAsync_WithBlankRows_SkipsEmptyLines()
    {
        // Arrange - Create CSV with completely blank rows
        var filePath = Path.Combine(_testDataPath, "portfolios_with_blank_rows.csv");
        var csvContent = """
            Port_ID,Port_Name,Port_Country,Port_CCY
            1,Portfolio 1,US,USD

            2,Portfolio 2,GB,GBP

            3,Portfolio 3,FR,EUR
            """;
        await File.WriteAllTextAsync(filePath, csvContent);

        // Act
        var result = await _csvParser.ParseAsync<Portfolio>(filePath);

        // Assert - Only valid rows loaded, blank rows skipped
        result.IsSuccess.Should().BeTrue();
        var portfolios = result.Value;
        portfolios.Should().HaveCount(3);
        portfolios[0].Id.Should().Be("1");
        portfolios[1].Id.Should().Be("2");
        portfolios[2].Id.Should().Be("3");
    }

}






