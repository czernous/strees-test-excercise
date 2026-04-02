namespace StressTestApp.Server.Features.Calculations;

public static partial class CalculationsLogger
{
    // Create (1000-1999)
    [LoggerMessage(1001, LogLevel.Information, "Calculation request received for {CountryCount} country deltas.")]
    public static partial void LogRequestReceived(this ILogger logger, int countryCount);

    [LoggerMessage(1002, LogLevel.Warning, "Calculation request rejected: {Reason}")]
    public static partial void LogValidationFailed(this ILogger logger, string reason);

    [LoggerMessage(1003, LogLevel.Information, "Loaded market data for calculation. Portfolios={PortfolioCount}, Loans={LoanCount}, Ratings={RatingCount}.")]
    public static partial void LogReferenceDataLoaded(this ILogger logger, int portfolioCount, int loanCount, int ratingCount);

    [LoggerMessage(1004, LogLevel.Warning, "Calculation request rejected due to unsupported countries: {Countries}")]
    public static partial void LogUnknownCountries(this ILogger logger, string countries);

    [LoggerMessage(1005, LogLevel.Warning, "Calculation request rejected because an identical input set already exists.")]
    public static partial void LogDuplicateInput(this ILogger logger);

    [LoggerMessage(1006, LogLevel.Information, "Calculation {Id} started.")]
    public static partial void LogStarted(this ILogger logger, Guid id);

    [LoggerMessage(1007, LogLevel.Information, "Calculation {Id} completed in {DurationMs}ms.")]
    public static partial void LogCompleted(this ILogger logger, Guid id, long durationMs);

    [LoggerMessage(1008, LogLevel.Error, "Failed while loading reference market data. Code={Code} Message={Message}")]
    public static partial void LogReferenceDataLoadFailed(this ILogger logger, string code, string message);

    [LoggerMessage(1009, LogLevel.Error, "Failed while querying existing calculations for duplicate detection.")]
    public static partial void LogDuplicateCheckFailed(this ILogger logger, Exception ex);

    [LoggerMessage(1010, LogLevel.Error, "Failed while persisting calculation results.")]
    public static partial void LogPersistenceFailed(this ILogger logger, Exception ex);

    // List (2000-2999)
    [LoggerMessage(2001, LogLevel.Information, "Retrieved {Count} calculation summaries.")]
    public static partial void LogListRetrieved(this ILogger logger, int count);

    [LoggerMessage(2002, LogLevel.Error, "Failed while listing calculation summaries.")]
    public static partial void LogListFailed(this ILogger logger, Exception ex);

    // GetById (3000-3999)
    [LoggerMessage(3001, LogLevel.Warning, "Calculation {Id} not found.")]
    public static partial void LogNotFound(this ILogger logger, Guid id);

    [LoggerMessage(3002, LogLevel.Error, "Failed while loading calculation {Id}.")]
    public static partial void LogGetByIdFailed(this ILogger logger, Guid id, Exception ex);

    // Critical/Data Errors (5000+)
    [LoggerMessage(5001, LogLevel.Error, "Data Integrity Violation: {Message}")]
    public static partial void LogIntegrityError(this ILogger logger, string message);
}
