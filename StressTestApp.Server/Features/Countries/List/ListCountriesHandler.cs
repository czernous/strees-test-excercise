using Microsoft.AspNetCore.Http.HttpResults;
using StressTestApp.Server.Core.Storage.MarketDataStore;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Features.Countries.List;

public static class ListCountriesHandler
{
    public static async Task<Results<Ok<ListCountriesResponse>, NotFound, ProblemHttpResult>> Handle(
        IMarketDataStore store,
        ILogger<ListCountriesResponse> logger,
        CancellationToken ct
    )
    {
        try
        {
            var portfolios = await store.GetOrCacheAsync<Portfolio>(ct);

            var countries = store.AvailableCountries;

            if (countries.Count == 0)
            {
                logger.LogNoCountriesFound();
                return TypedResults.NotFound();
            }

            logger.LogCountriesRetrieved(countries.Count);
            return TypedResults.Ok(new ListCountriesResponse([.. countries]));
        }
        catch (Exception ex)
        {
            // Log the full stack trace for the developer, 
            // but return a generic message to the user.
            logger.LogCountryExtractionError(ex);

            return TypedResults.Problem(
                detail: "An error occurred while retrieving available countries.",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}