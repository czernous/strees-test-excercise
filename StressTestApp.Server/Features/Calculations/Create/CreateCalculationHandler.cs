namespace StressTestApp.Server.Features.Calculations.Create;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Core.Database.Entities;
using StressTestApp.Server.Core.Storage.MarketDataStore;
using StressTestApp.Server.Extensions;
using StressTestApp.Server.Features.Calculations;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Diagnostics;

public static class CreateCalculationHandler
{
    public static async Task<Results<Created<CreateCalculationResponse>,
        BadRequest<HttpError>,
        Conflict<HttpError>,
        InternalServerError<HttpError>>>
        Handle(
        CreateCalculationRequest request,
        IMarketDataStore marketData,
        IStressTestDbContext db,
        ILogger<CreateCalculationResponse> logger,
        CancellationToken ct)
    {
        logger.LogRequestReceived(request.HousePriceChanges?.Count ?? 0);

        return await ValidateRequest(request, logger)
            .Bind(_ => LoadMarketDataAsync(marketData, logger, ct))
            .Bind(data => ProcessRequestAsync(request, data, db, logger, ct))
            .Match(
                response => TypedResults.Created($"/calculations/{response.CalculationId}", response),
                err => err.ToCreateErrorResult<CreateCalculationResponse>()
            );
    }

    private static Result<bool, Error> ValidateRequest(
        CreateCalculationRequest request,
        ILogger logger)
    {
        if (request.HousePriceChanges is null || request.HousePriceChanges.Count == 0)
        {
            logger.LogValidationFailed("No house price changes were provided.");
            return Error.Create(
                ErrorCode.Validation.MissingRequired,
                "House price changes must be provided for at least one country.");
        }

        var invalidCountryCodes = request.HousePriceChanges.Keys
            .Where(code => string.IsNullOrWhiteSpace(code) || code.Length > 10)
            .ToList();

        if (invalidCountryCodes.Count > 0)
        {
            logger.LogValidationFailed($"Invalid country codes: {string.Join(", ", invalidCountryCodes)}");
            return Error.Create(
                ErrorCode.Validation.InvalidFormat,
                $"Invalid country codes: {string.Join(", ", invalidCountryCodes)}");
        }

        return true;
    }

    private static async Task<Result<MarketDataSnapshot, Error>> LoadMarketDataAsync(
        IMarketDataStore marketData,
        ILogger logger,
        CancellationToken ct)
    {
        var portfoliosTask = marketData.GetOrCacheAsync<Portfolio>(ct).AsTask();
        var loansTask = marketData.GetOrCacheAsync<Loan>(ct).AsTask();
        var ratingsTask = marketData.GetOrCacheAsync<Rating>(ct).AsTask();

        await Task.WhenAll(portfoliosTask, loansTask, ratingsTask);

        return Result.Combine(await portfoliosTask, await loansTask, await ratingsTask)
            .Bind(data => ValidateMarketData(data, marketData.AvailableCountries, logger))
            .TapError(err => logger.LogReferenceDataLoadFailed(err.Code, err.Message));
    }

    private static Task<Result<CreateCalculationResponse, Error>> ProcessRequestAsync(
        CreateCalculationRequest request,
        MarketDataSnapshot data,
        IStressTestDbContext db,
        ILogger<CreateCalculationResponse> logger,
        CancellationToken ct) =>
        ValidateCountries(request, data.AvailableCountries, logger)
            .Bind(_ => CheckForDuplicatesAsync(request, db, logger, ct))
            .Bind(_ => RunCalculationAsync(request, data, db, logger, ct));

    private static Result<MarketDataSnapshot, Error> ValidateMarketData(
        (IReadOnlyList<Portfolio> Portfolios, IReadOnlyList<Loan> Loans, IReadOnlyList<Rating> Ratings) data,
        IReadOnlySet<string> availableCountries,
        ILogger logger)
    {
        var (portfolios, loans, ratings) = data;

        if (portfolios.Count == 0 || loans.Count == 0 || ratings.Count == 0)
        {
            logger.LogIntegrityError("Reference market data is empty.");
            return Error.Create(
                ErrorCode.Validation.DataIntegrityViolation,
                "Required market data is not available. Please contact support.");
        }

        logger.LogReferenceDataLoaded(portfolios.Count, loans.Count, ratings.Count);
        return Result.Success(new MarketDataSnapshot(portfolios, loans, ratings, availableCountries));
    }

    private static Result<bool, Error> ValidateCountries(
        CreateCalculationRequest request,
        IReadOnlySet<string> availableCountries,
        ILogger logger)
    {
        // Reuse the cache-admission country index instead of rebuilding the set on every request.
        var unknownCountries = request.HousePriceChanges.Keys
            .Where(code => !availableCountries.Contains(code))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unknownCountries.Count == 0)
        {
            return true;
        }

        logger.LogUnknownCountries(string.Join(", ", unknownCountries));
        return Error.Create(
            ErrorCode.Validation.InvalidInput,
            $"Unknown countries: {string.Join(", ", unknownCountries)}. Available countries: {string.Join(", ", availableCountries.OrderBy(c => c))}");
    }

    private static async Task<Result<bool, Error>> CheckForDuplicatesAsync(
        CreateCalculationRequest request,
        IStressTestDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var requestedInputs = new Dictionary<string, decimal>(
            request.HousePriceChanges,
            StringComparer.OrdinalIgnoreCase);

        var existingCalculationsResult = await Result.TryAsync(
            async token => await db.Calculations
                .AsNoTracking()
                .Where(c => c.Inputs.Count == request.HousePriceChanges.Count)
                .Include(c => c.Inputs)
                .Select(c => new
                {
                    c.Id,
                    c.CreatedAtUtc,
                    Inputs = c.Inputs
                        .Select(i => new { i.CountryCode, i.HousePriceChange })
                        .ToList()
                })
                .ToListAsync(token),
            ex =>
            {
                logger.LogDuplicateCheckFailed(ex);
                return Error.Create(
                    ErrorCode.Database.QueryFailed,
                    "Failed to query existing calculations.");
            },
            ct);

        if (!existingCalculationsResult.IsSuccess)
        {
            return existingCalculationsResult.Error;
        }

        var duplicate = existingCalculationsResult.Value.FirstOrDefault(c =>
        {
            var inputDict = c.Inputs.ToDictionary(
                i => i.CountryCode,
                i => i.HousePriceChange,
                StringComparer.OrdinalIgnoreCase);

            return inputDict.Count == requestedInputs.Count &&
                   inputDict.All(kvp =>
                       requestedInputs.TryGetValue(kvp.Key, out var value) &&
                       Math.Abs(kvp.Value - value) < 0.001m);
        });

        if (duplicate is null)
        {
            return true;
        }

        logger.LogDuplicateInput();
        return Error.Create(
            ErrorCode.Validation.DuplicateEntry,
            $"A calculation with identical inputs already exists (ID: {duplicate.Id}, Created: {duplicate.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}). Please modify at least one input value to run a new calculation.");
    }

    private static async Task<Result<CreateCalculationResponse, Error>> RunCalculationAsync(
        CreateCalculationRequest request,
        MarketDataSnapshot marketData,
        IStressTestDbContext db,
        ILogger<CreateCalculationResponse> logger,
        CancellationToken ct)
    {
        var (portfolios, loans, ratings, _) = marketData;

        var sw = Stopwatch.StartNew();
        var operationId = Guid.CreateVersion7();
        logger.LogStarted(operationId);

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
            calculationResults: results);

        var persistenceResult = await Result.TryAsync(
            async token =>
            {
                db.Calculations.Add(calculation);
                await db.SaveChangesAsync(token);
                return calculation;
            },
            ex =>
            {
                logger.LogPersistenceFailed(ex);
                return ex is DbUpdateException
                    ? Error.Create(ErrorCode.Database.IntegrityViolation, "Failed to persist calculation results.")
                    : Error.Create(ErrorCode.Database.QueryFailed, "Failed to save calculation results.");
            },
            ct);

        if (!persistenceResult.IsSuccess)
        {
            return persistenceResult.Error;
        }

        logger.LogCompleted(calculation.Id, sw.ElapsedMilliseconds);

        return Result.Success(new CreateCalculationResponse(
            calculation.Id,
            calculation.CreatedAtUtc,
            calculation.DurationMs,
            // The request payload already has the exact shape the response needs.
            request.HousePriceChanges,
            calculation.PortfolioCount,
            calculation.LoanCount,
            calculation.TotalExpectedLoss));
    }

    private readonly record struct MarketDataSnapshot(
        IReadOnlyList<Portfolio> Portfolios,
        IReadOnlyList<Loan> Loans,
        IReadOnlyList<Rating> Ratings,
        IReadOnlySet<string> AvailableCountries);
}
