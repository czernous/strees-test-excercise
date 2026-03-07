namespace StressTestApp.Server.Features.Calculations.Create;

public sealed record CreateCalculationRequest(
    IReadOnlyDictionary<string, decimal> HousePriceChanges);
