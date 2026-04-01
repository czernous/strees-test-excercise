using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Features.Calculations.GetById;

public static class GetCalculationHandler
{
    public static async Task<Results<Ok<GetCalculationResponse>, NotFound>> Handle(
        Guid id,
        IStressTestDbContext db,
        ILogger<GetCalculationResponse> logger,
        CancellationToken ct)
    {
        // Include only what's necessary.
        var calculation = await db.Calculations
            .AsNoTracking()
            .Include(c => c.Inputs)
            .Include(c => c.Results)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (calculation is null)
        {
            logger.LogNotFound(id);
            return TypedResults.NotFound();
        }

        // 2. Mapping (Push this logic into a Mapper or the Entity itself)
        var response = new GetCalculationResponse(
            calculation.Id,
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
            ))]
        );

        return TypedResults.Ok(response);
    }
}