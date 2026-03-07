using FluentAssertions;
using StressTestApp.Server.Features.Calculations.Compute;

namespace StressTestApp.Tests.Features.Calculations;

public class LoanCalculatorTests
{
    [Fact]
    public void Compute_SimpleCase_CalculatesCorrectly()
    {
        // Arrange - Simple numbers we can verify by hand
        int collateralValue = 100_000;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = -10m; // -10%
        decimal pd = 0.05m; // 5%

        // Manual calculation:
        // Scenario Collateral = 100,000 * (1 + (-10/100)) = 90,000
        // RR = 90,000 / 100,000 = 0.9
        // LGD = 1 - 0.9 = 0.1
        // EL = 0.05 * 0.1 * 90,000 = 450

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ScenarioCollateralValue.Should().Be(90_000m);
        result.ExpectedLoss.Should().Be(450m);
    }

    [Fact]
    public void Compute_NegativeHousePriceChange_DecreasesCollateralValue()
    {
        // Arrange
        int collateralValue = 200_000;
        int originalLoanAmount = 150_000;
        int outstandingAmount = 140_000;
        decimal pctChange = -5.12m;
        decimal pd = 0.02m;

        // Scenario Collateral = 200,000 * (1 + (-5.12/100)) = 200,000 * 0.9488 = 189,760
        // RR = 189,760 / 150,000 = 1.265... (clamped to 1.0)
        // LGD = 1 - 1.0 = 0
        // EL = 0.02 * 0 * 140,000 = 0

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ScenarioCollateralValue.Should().Be(189_760m);
        result.ExpectedLoss.Should().Be(0m); // Collateral still covers loan
    }

    [Fact]
    public void Compute_PositiveHousePriceChange_IncreasesCollateralValue()
    {
        // Arrange
        int collateralValue = 100_000;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = 10m; // +10%
        decimal pd = 0.05m;

        // Scenario Collateral = 100,000 * 1.10 = 110,000
        // RR = 110,000 / 100,000 = 1.1 (clamped to 1.0)
        // LGD = 0
        // EL = 0

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ScenarioCollateralValue.Should().Be(110_000m);
        result.ExpectedLoss.Should().Be(0m); // Fully covered
    }

    [Fact]
    public void Compute_ZeroCollateral_ProducesMaximumLoss()
    {
        // Arrange
        int collateralValue = 0;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = -10m;
        decimal pd = 0.05m;

        // Scenario Collateral = 0
        // RR = 0
        // LGD = 1.0
        // EL = 0.05 * 1.0 * 90,000 = 4,500

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ScenarioCollateralValue.Should().Be(0m);
        result.ExpectedLoss.Should().Be(4_500m);
    }

    [Fact]
    public void Compute_ZeroOriginalLoanAmount_ReturnsZeroRecoveryRate()
    {
        // Arrange
        int collateralValue = 100_000;
        int originalLoanAmount = 0; // Edge case - should not crash
        int outstandingAmount = 90_000;
        decimal pctChange = -10m;
        decimal pd = 0.05m;

        // Should handle division by zero gracefully
        // RR = 0 (special case in code)
        // LGD = 1.0
        // EL = 0.05 * 1.0 * 90,000 = 4,500

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ExpectedLoss.Should().Be(4_500m);
    }

    [Fact]
    public void Compute_ZeroOutstandingAmount_ProducesZeroExpectedLoss()
    {
        // Arrange
        int collateralValue = 100_000;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 0;
        decimal pctChange = -50m;
        decimal pd = 0.1m;

        // EL = PD * LGD * 0 = 0

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ExpectedLoss.Should().Be(0m);
    }

    [Fact]
    public void Compute_ZeroProbabilityOfDefault_ProducesZeroExpectedLoss()
    {
        // Arrange
        int collateralValue = 50_000;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = -20m;
        decimal pd = 0m; // AAA rating, zero default probability

        // EL = 0 * LGD * OutstandingAmount = 0

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ExpectedLoss.Should().Be(0m);
    }

    [Fact]
    public void Compute_Minus100PercentChange_ProducesZeroCollateral()
    {
        // Arrange
        int collateralValue = 100_000;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = -100m; // Complete market collapse
        decimal pd = 0.05m;

        // Scenario Collateral = 100,000 * (1 + (-100/100)) = 0
        // RR = 0
        // LGD = 1.0
        // EL = 0.05 * 1.0 * 90,000 = 4,500

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ScenarioCollateralValue.Should().Be(0m);
        result.ExpectedLoss.Should().Be(4_500m);
    }

    [Fact]
    public void Compute_CollateralExceedsLoan_ClampsRecoveryRateToOne()
    {
        // Arrange
        int collateralValue = 200_000; // Double the loan value
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = 0m;
        decimal pd = 0.05m;

        // RR = 200,000 / 100,000 = 2.0 (should be clamped to 1.0)
        // LGD = 0
        // EL = 0

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        result.ExpectedLoss.Should().Be(0m); // Fully covered
    }

    [Theory]
    [InlineData(-5.12, 100_000, 100_000, 90_000, 0.05)]
    [InlineData(-4.34, 150_000, 120_000, 110_000, 0.02)]
    [InlineData(-3.87, 200_000, 180_000, 170_000, 0.03)]
    [InlineData(-1.23, 250_000, 200_000, 190_000, 0.01)]
    [InlineData(-5.5, 80_000, 90_000, 85_000, 0.04)]
    [InlineData(-5.68, 300_000, 280_000, 275_000, 0.06)]
    public void Compute_ExampleScenarios_ProducesValidResults(
        decimal pctChange, int collateral, int originalLoan, int outstanding, decimal pd)
    {
        // Act
        var result = LoanCalculator.Compute(
            collateral, originalLoan, outstanding, pctChange, pd);

        // Assert - Invariants that should always hold
        result.ExpectedLoss.Should().BeGreaterThanOrEqualTo(0m, "Expected loss cannot be negative");
        result.ScenarioCollateralValue.Should().BeGreaterThanOrEqualTo(0m, "Collateral cannot be negative");

        // Expected loss should not exceed outstanding amount
        result.ExpectedLoss.Should().BeLessThanOrEqualTo(outstanding, 
            "Expected loss cannot exceed outstanding amount");
    }

    [Fact]
    public void Compute_LargeValues_HandlesWithoutOverflow()
    {
        // Arrange - Test with very large but realistic values
        int collateralValue = 10_000_000; // $10M
        int originalLoanAmount = 8_000_000; // $8M
        int outstandingAmount = 7_500_000; // $7.5M
        decimal pctChange = -30m; // Severe crash to ensure losses
        decimal pd = 0.08m;

        // Scenario collateral = 10M * 0.7 = 7M (less than loan)

        // Act
        var act = () => LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        act.Should().NotThrow("Should handle large values without overflow");
        var result = act();
        result.ExpectedLoss.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Compute_PercentageChangeAsPercent_CalculatesCorrectly()
    {
        // Arrange - Verify the interpretation of pctChange
        // The formula should be: Collateral * (1 + pctChange/100)
        // NOT: Collateral * (1 + pctChange)
        
        int collateralValue = 100_000;
        int originalLoanAmount = 100_000;
        int outstandingAmount = 90_000;
        decimal pctChange = -5m; // -5% (not -0.05)
        decimal pd = 0.05m;

        // Act
        var result = LoanCalculator.Compute(
            collateralValue, originalLoanAmount, outstandingAmount, pctChange, pd);

        // Assert
        // -5% means multiply by 0.95, so: 100,000 * 0.95 = 95,000
        result.ScenarioCollateralValue.Should().Be(95_000m,
            "pctChange should be interpreted as percentage points, not a decimal multiplier");
    }
}
