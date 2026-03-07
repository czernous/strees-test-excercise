using Microsoft.AspNetCore.Http.HttpResults;
using StressTestApp.Server.Persistence.MarketDataStore.Interfaces;

namespace StressTestApp.Server.Features.Countries.List;

public static class ListCountriesHandler
{
    public static async Task<Results<Ok<ListCountriesResponse>, NotFound, ProblemHttpResult>> Handle(
        IMarketDataStore store,
        CancellationToken ct
    )
    {
        try
        {
            var portfolios = await store.GetPortfoliosAsync(ct);
            var countries = portfolios
                .Select(p => p.Country)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (countries.Count == 0)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(new ListCountriesResponse(countries));
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
