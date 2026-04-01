using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database;

namespace StressTestApp.Server.Features.Calculations.List;

public static class ListCalculationsHandler
{
    public static async Task<IResult> Handle(
        IStressTestDbContext db,
        ILogger<ListCalculationsResponse> logger,
        CancellationToken ct)
    {
        // We avoid fetching the 'Results' collection entirely.
        var data = await db.Calculations
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.CreatedAtUtc,
                c.DurationMs,
                c.PortfolioCount,
                c.LoanCount,
                c.TotalExpectedLoss,
                // Projecting just the key-value pairs for the dictionary
                Inputs = c.Inputs.Select(i => new { i.CountryCode, i.HousePriceChange }).ToList()
            })
            .ToListAsync(ct);

        // 2. In-Memory Processing
        // We order here because of SQLite's DateTimeOffset limitations.
        // We cap the list or use a reasonable limit if this table grows to 10k+ entries.
        var summaries = data
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

        logger.LogListRetrieved(summaries.Count);

        return Results.Ok(new ListCalculationsResponse(summaries));
    }
}