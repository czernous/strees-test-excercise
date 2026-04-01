namespace StressTestApp.Server.Features.Countries;

public static partial class CountriesLogger
{
    [LoggerMessage(6001, LogLevel.Information, "Retrieved {Count} unique countries from portfolio cache.")]
    public static partial void LogCountriesRetrieved(this ILogger logger, int count);

    [LoggerMessage(6002, LogLevel.Warning, "No countries found in the current portfolio data.")]
    public static partial void LogNoCountriesFound(this ILogger logger);

    [LoggerMessage(6003, LogLevel.Error, "Critical failure while extracting countries from store.")]
    public static partial void LogCountryExtractionError(this ILogger logger, Exception ex);
}