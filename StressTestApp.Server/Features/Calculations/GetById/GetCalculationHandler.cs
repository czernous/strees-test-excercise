namespace StressTestApp.Server.Features.Calculations.GetById;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Extensions;
using StressTestApp.Server.Features.Calculations;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

public static class GetCalculationHandler
{
    public static async Task<Results<Ok<GetCalculationResponse>, NotFound<HttpError>, InternalServerError<HttpError>>> Handle(
        Guid id,
        IStressTestDbContext db,
        ILogger<GetCalculationResponse> logger,
        CancellationToken ct) =>
        await FetchCalculationAsync(id, db, logger, ct)
            .Match(
                response => TypedResults.Ok(response),
                err => err.ToGetByIdErrorResult<GetCalculationResponse>()
            );

    private static Task<Result<GetCalculationResponse, Error>> FetchCalculationAsync(
        Guid id,
        IStressTestDbContext db,
        ILogger<GetCalculationResponse> logger,
        CancellationToken ct) =>
        Result.TryAsync(
            async token => await db.Calculations
                .AsNoTracking()
                .Include(c => c.Results)
                .FirstOrDefaultAsync(c => c.Id == id, token),
            ex =>
            {
                logger.LogGetByIdFailed(id, ex);
                return Error.Create(
                    ErrorCode.Database.QueryFailed,
                    $"Failed to load calculation {id}.");
            },
            ct)
            .Ensure(
                calculation => calculation is not null,
                _ =>
                {
                    logger.LogNotFound(id);
                    return Error.Create(
                        ErrorCode.Database.NotFound,
                        $"Calculation {id} was not found.");
                })
            .Map(calculation => new GetCalculationResponse(
                calculation!.Id,
                calculation.CreatedAtUtc,
                calculation.DurationMs,
                calculation.PortfolioCount,
                calculation.LoanCount,
                calculation.TotalExpectedLoss,
                calculation.GetHousePriceChanges(),
                [.. calculation.Results.Select(r => new PortfolioCalculationResult(
                    r.PortfolioId,
                    r.PortfolioName,
                    r.Country,
                    r.Currency,
                    r.TotalOutstandingAmount,
                    r.TotalCollateralValue,
                    r.TotalScenarioCollateralValue,
                    r.TotalExpectedLoss,
                    r.LoanCount
                ))]));
}
