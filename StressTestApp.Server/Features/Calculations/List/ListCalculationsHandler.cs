using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Data;

namespace StressTestApp.Server.Features.Calculations.List;

public static class ListCalculationsHandler
{
    public static async Task<IResult> Handle(
        StressTestDbContext db,
        CancellationToken ct)
    {
        // SQLite doesn't support ordering by DateTimeOffset in SQL
        // Fetch data and order in memory (acceptable for calculation history)
        var calculations = await db.Calculations
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.CreatedAtUtc,
                c.DurationMs,
                c.PortfolioCount,
                c.LoanCount,
                c.TotalExpectedLoss,
                Inputs = c.Inputs.Select(i => new { i.CountryCode, i.HousePriceChange }).ToList()
            })
            .ToListAsync(ct);

        var summaries = calculations
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CalculationSummary(
                c.Id,
                c.CreatedAtUtc,
                c.DurationMs,
                c.PortfolioCount,
                c.LoanCount,
                c.TotalExpectedLoss,
                c.Inputs.ToDictionary(i => i.CountryCode, i => i.HousePriceChange)
            ))
            .ToList();

        var response = new ListCalculationsResponse(summaries);

        return Results.Ok(response);
    }
}
