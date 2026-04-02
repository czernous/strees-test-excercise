namespace StressTestApp.Server.Features.Countries.List;

using Microsoft.AspNetCore.Http.HttpResults;
using StressTestApp.Server.Core.Storage.MarketDataStore;
using StressTestApp.Server.Extensions;
using StressTestApp.Server.Features.Countries;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

public static class ListCountriesHandler
{
    public static async Task<Results<Ok<ListCountriesResponse>, InternalServerError<HttpError>>> Handle(
        IMarketDataStore store,
        ILogger<ListCountriesResponse> logger,
        CancellationToken ct) =>
        (await GetCountriesAsync(store, logger, ct))
            .Match<Results<Ok<ListCountriesResponse>, InternalServerError<HttpError>>>(
                ToOk,
                ToInternalServerError);

    private static Task<Result<IReadOnlySet<string>, Error>> GetCountriesAsync(
        IMarketDataStore store,
        ILogger<ListCountriesResponse> logger,
        CancellationToken ct) =>
        store.GetOrCacheAsync<Portfolio>(ct).AsTask()
            .TapError(err => logger.LogCountryLoadFailed(err.Code, err.Message))
            .Map(_ => store.AvailableCountries)
            .Ensure(
                countries => countries.Count > 0,
                _ => CreateNoCountriesError(logger))
            .Tap(countries => logger.LogCountriesRetrieved(countries.Count));

    private static Results<Ok<ListCountriesResponse>, InternalServerError<HttpError>> ToOk(IReadOnlySet<string> countries) =>
        TypedResults.Ok(new ListCountriesResponse([.. countries]));

    private static Results<Ok<ListCountriesResponse>, InternalServerError<HttpError>> ToInternalServerError(Error err) =>
        err.ToListErrorResult<ListCountriesResponse>();

    private static Error CreateNoCountriesError(ILogger logger)
    {
        logger.LogNoCountriesFound();
        return Error.Create(
            ErrorCode.Validation.DataIntegrityViolation,
            "No supported countries are available in the portfolio data.");
    }
}
