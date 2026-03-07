using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Data;
using StressTestApp.Server.Domain.Entities;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Persistence.MarketDataStore.Interfaces;

namespace StressTestApp.Server.Features.Calculations.Create;

public static class CreateCalculationHandler
{
    public static async Task<Results<Ok<CreateCalculationResponse>, ProblemHttpResult>> Handle(
        CreateCalculationRequest request,
        IMarketDataStore marketData,
        StressTestDbContext db,
        CancellationToken ct)
    {
        try
        {
            // Validation: Check if house price changes are provided
            if (request.HousePriceChanges == null || request.HousePriceChanges.Count == 0)
            {
                return TypedResults.Problem(
                    detail: "House price changes must be provided for at least one country.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error"
                );
            }

            // Validation: Check for valid country codes (2-3 letter uppercase)
            var invalidCountryCodes = request.HousePriceChanges.Keys
                .Where(code => string.IsNullOrWhiteSpace(code) || code.Length > 10)
                .ToList();

            if (invalidCountryCodes.Count > 0)
            {
                return TypedResults.Problem(
                    detail: $"Invalid country codes: {string.Join(", ", invalidCountryCodes)}",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error"
                );
            }

            var sw = Stopwatch.StartNew();

            var portfolios = await marketData.GetPortfoliosAsync(ct);
            var loans = await marketData.GetLoansAsync(ct);
            var ratings = await marketData.GetRatingsAsync(ct);

            if (portfolios.Count == 0 || loans.Count == 0 || ratings.Count == 0)
            {
                return TypedResults.Problem(
                    detail: "Required market data is not available. Please contact support.",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Data Unavailable"
                );
            }

            var availableCountries = portfolios
                .Select(p => p.Country)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unknownCountries = request.HousePriceChanges.Keys
                .Where(code => !availableCountries.Contains(code))
                .ToList();

            if (unknownCountries.Count > 0)
            {
                return TypedResults.Problem(
                    detail: $"Unknown countries: {string.Join(", ", unknownCountries)}. " +
                            $"Available countries: {string.Join(", ", availableCountries.OrderBy(c => c))}",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation Error"
                );
            }
            // Validation: Check for duplicate calculation with same inputs
            var existingCalculations = await db.Calculations
                .Where(c => c.Inputs.Count == request.HousePriceChanges.Count)
                .Include(c => c.Inputs)
                .Select(c => new
                {
                    c.Id,
                    c.CreatedAtUtc,
                    Inputs = c.Inputs.Select(i => new { i.CountryCode, i.HousePriceChange }).ToList()
                })
                .ToListAsync(ct);

            var duplicateCalculation = existingCalculations.FirstOrDefault(c =>
            {
                var inputDict = c.Inputs.ToDictionary(i => i.CountryCode, i => i.HousePriceChange);
                return inputDict.Count == request.HousePriceChanges.Count &&
                       inputDict.All(kvp =>
                           request.HousePriceChanges.TryGetValue(kvp.Key, out var value) &&
                           Math.Abs(kvp.Value - value) < 0.001m);
            });

            if (duplicateCalculation != null)
            {
                return TypedResults.Problem(
                    detail: $"A calculation with identical inputs already exists (ID: {duplicateCalculation.Id}, " +
                            $"Created: {duplicateCalculation.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}). " +
                            "Please modify at least one input value to run a new calculation.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Duplicate Calculation"
                );
            }
            var results = PortfolioCalculator.Calculate(
                loans,
                portfolios,
                ratings,
                request.HousePriceChanges);

            sw.Stop();

            var calculation = Calculation.Create(
                durationMs: sw.ElapsedMilliseconds,
                housePriceChanges: request.HousePriceChanges,
                portfolioCount: results.Count,
                loanCount: loans.Count,
                totalExpectedLoss: results.Sum(r => r.TotalExpectedLoss),
                calculationResults: results
            );

            db.Calculations.Add(calculation);
            await db.SaveChangesAsync(ct);

            var response = new CreateCalculationResponse(
                calculation.Id,
                calculation.CreatedAtUtc,
                calculation.DurationMs,
                calculation.GetHousePriceChanges(),
                calculation.PortfolioCount,
                calculation.LoanCount,
                calculation.TotalExpectedLoss
            );

            return TypedResults.Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.Problem(
                detail: $"Missing reference data: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Data Integrity Error"
            );
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Calculation Error"
            );
        }
    }
}
