namespace StressTestApp.Server.Domain.Entities;

public record CalculationInput
{
    public required Guid Id { get; init; }
    public required Guid CalculationId { get; init; }
    public required string CountryCode { get; init; }
    public required decimal HousePriceChange { get; init; }
    public Calculation? Calculation { get; init; }

    private CalculationInput() { }

    public static CalculationInput Create(
        Guid calculationId,
        string countryCode,
        decimal housePriceChange)
    {
        return new CalculationInput
        {
            Id = Guid.CreateVersion7(),
            CalculationId = calculationId,
            CountryCode = countryCode,
            HousePriceChange = housePriceChange
        };
    }
}
