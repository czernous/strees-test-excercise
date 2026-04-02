namespace StressTestApp.Server.Features.Calculations.List;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Extensions;
using StressTestApp.Server.Features.Calculations;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

public static class ListCalculationsHandler
{
    private const int MaxRecentCalculations = 100;

    public static async Task<Results<Ok<ListCalculationsResponse>, InternalServerError<HttpError>>> Handle(
        IStressTestDbContext db,
        ILogger<ListCalculationsResponse> logger,
        CancellationToken ct) =>
        (await FetchCalculationsAsync(db, logger, ct))
            .Match<Results<Ok<ListCalculationsResponse>, InternalServerError<HttpError>>>(
                ToOk,
                ToInternalServerError);

    private static Task<Result<ListCalculationsResponse, Error>> FetchCalculationsAsync(
        IStressTestDbContext db,
        ILogger<ListCalculationsResponse> logger,
        CancellationToken ct) =>
        Result.TryAsync(
            async token => await db.Calculations
                .AsNoTracking()
                .Select(c => new CalculationProjection(
                    c.Id,
                    c.CreatedAtUtc,
                    c.DurationMs,
                    c.PortfolioCount,
                    c.LoanCount,
                    c.TotalExpectedLoss,
                    c.Inputs.Select(i => new InputProjection(i.CountryCode, i.HousePriceChange))))
                .Take(MaxRecentCalculations)
                .ToListAsync(token),
            ex =>
                {
                    logger.LogListFailed(ex);
                    return Error.Create(
                        ErrorCode.Database.QueryFailed,
                        "Failed to list calculations.");
                },
            ct)
            .Map(MapResponse)
            .Tap(response => logger.LogListRetrieved(response.Calculations.Count));

    private static ListCalculationsResponse MapResponse(IReadOnlyList<CalculationProjection> data) =>
        new(
            [.. data
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new CalculationSummary(
                    c.Id,
                    c.CreatedAtUtc,
                    c.DurationMs,
                    c.PortfolioCount,
                    c.LoanCount,
                    c.TotalExpectedLoss,
                    c.Inputs.ToDictionary(i => i.CountryCode, i => i.HousePriceChange)
                ))]);

    private static Results<Ok<ListCalculationsResponse>, InternalServerError<HttpError>> ToOk(ListCalculationsResponse response) =>
        TypedResults.Ok(response);

    private static Results<Ok<ListCalculationsResponse>, InternalServerError<HttpError>> ToInternalServerError(Error err) =>
        err.ToListErrorResult<ListCalculationsResponse>();

    private sealed record CalculationProjection(
        Guid Id,
        DateTimeOffset CreatedAtUtc,
        long DurationMs,
        int PortfolioCount,
        int LoanCount,
        decimal TotalExpectedLoss,
        IEnumerable<InputProjection> Inputs);

    private sealed record InputProjection(
        string CountryCode,
        decimal HousePriceChange);
}
