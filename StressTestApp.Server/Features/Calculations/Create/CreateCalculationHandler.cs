using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Core.Database.Entities;
using StressTestApp.Server.Core.Storage.MarketDataStore;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Features.Calculations.Create;

public static class CreateCalculationHandler
{
    public static async Task<Results<Created<CreateCalculationResponse>, ProblemHttpResult>> Handle(
        CreateCalculationRequest request,
        IMarketDataStore marketData,
        IStressTestDbContext db,
        ILogger<CreateCalculationRequest> logger,
        CancellationToken ct)
    {
        // 1. Initial Request Validation (Fail Fast)
        if (request.HousePriceChanges is null || request.HousePriceChanges.Count == 0)
        {
            return TypedResults.Problem(
                detail: "House price changes must be provided for at least one country.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error"
            );
        }

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

        // 2. Warm the Cache & Fetch Data
        // This ensures marketData.AvailableCountries is populated.
        var portfolios = await marketData.GetOrCacheAsync<Portfolio>(ct);
        var loans = await marketData.GetOrCacheAsync<Loan>(ct);
        var ratings = await marketData.GetOrCacheAsync<Rating>(ct);

        if (portfolios.Count == 0 || loans.Count == 0 || ratings.Count == 0)
        {
            logger.LogIntegrityError("Market data CSVs are empty or missing");
            return TypedResults.Problem(
                detail: "Required market data is not available. Please contact support.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Data Unavailable"
            );
        }


        // 3. Country Validation (O(1) lookup using Store Metadata)
        var unknownCountries = request.HousePriceChanges.Keys
            .Where(code => !marketData.AvailableCountries.Contains(code))
            .ToList();

        if (unknownCountries.Count > 0)
        {
            return TypedResults.Problem(
                detail: $"Unknown countries: {string.Join(", ", unknownCountries)}. " +
                        $"Available countries: {string.Join(", ", marketData.AvailableCountries.OrderBy(c => c))}",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error"
            );
        }

        // 4. Duplicate Check (Database-side)
        var potentialDuplicates = await db.Calculations
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.CreatedAtUtc,
                InputCount = c.Inputs.Count,
                Inputs = c.Inputs
                    .Select(i => new { i.CountryCode, i.HousePriceChange })
                    .ToList()
            })
            .Where(x => x.InputCount == request.HousePriceChanges.Count)
            .ToListAsync(ct);

        var duplicate = potentialDuplicates.FirstOrDefault(c =>
            c.Inputs.All(i =>
                request.HousePriceChanges.TryGetValue(i.CountryCode, out var requestedValue) &&
                Math.Abs(i.HousePriceChange - requestedValue) < 0.001m));

        if (duplicate is not null)
        {
            logger.LogDuplicateInput();
            return TypedResults.Problem(
                detail: $"A calculation with identical inputs already exists (ID: {duplicate.Id}, " +
                        $"Created: {duplicate.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}). " +
                        "Please modify at least one input value to run a new calculation.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate Calculation"
            );
        }

        // 5. Execution
        logger.LogStarted(Guid.NewGuid(), portfolios.Count);
        var sw = Stopwatch.StartNew();

        var results = PortfolioCalculator.Calculate(
            loans,
            portfolios,
            ratings,
            request.HousePriceChanges);

        sw.Stop();

        // 6. Persistence
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

        logger.LogCompleted(calculation.Id, sw.ElapsedMilliseconds);

        return TypedResults.Created(
            $"/calculations/{calculation.Id}", 
            new CreateCalculationResponse(
                calculation.Id,
                calculation.CreatedAtUtc,
                calculation.DurationMs,
                calculation.GetHousePriceChanges(),
                calculation.PortfolioCount,
                calculation.LoanCount,
                calculation.TotalExpectedLoss
            ));
    }
}