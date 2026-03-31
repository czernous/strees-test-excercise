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
        CancellationToken ct)
    {
        // Fetch data first, then build dictionaries in memory
        // SQLite/EF Core can't translate ToDictionary in projections
        var result = await db.Calculations
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.CreatedAtUtc,
                c.DurationMs,
                c.PortfolioCount,
                c.LoanCount,
                c.TotalExpectedLoss,
                Inputs = c.Inputs.Select(i => new { i.CountryCode, i.HousePriceChange }).ToList(),
                Results = c.Results.Select(r => new
                {
                    r.PortfolioId,
                    r.PortfolioName,
                    r.Country,
                    r.Currency,
                    r.TotalOutstandingAmount,
                    r.TotalCollateralValue,
                    r.TotalScenarioCollateralValue,
                    r.TotalExpectedLoss,
                    r.LoanCount
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetCalculationResponse(
            result.Id,
            result.CreatedAtUtc,
            result.DurationMs,
            result.PortfolioCount,
            result.LoanCount,
            result.TotalExpectedLoss,
            result.Inputs.ToDictionary(i => i.CountryCode, i => i.HousePriceChange),
            result.Results.Select(r => new PortfolioCalculationResult(
                r.PortfolioId,
                r.PortfolioName,
                r.Country,
                r.Currency,
                r.TotalOutstandingAmount,
                r.TotalCollateralValue,
                r.TotalScenarioCollateralValue,
                r.TotalExpectedLoss,
                r.LoanCount
            )).ToList()
        );

        return TypedResults.Ok(response);
    }
}
