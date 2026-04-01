namespace StressTestApp.Server.Features.Calculations;

public static partial class CalculationsLogger
{
    // Create (1000-1999)
    [LoggerMessage(1001, LogLevel.Information, "Calculation {Id} started for {PortfolioCount} portfolios.")]
    public static partial void LogStarted(this ILogger logger, Guid id, int portfolioCount);

    [LoggerMessage(1002, LogLevel.Information, "Calculation {Id} completed in {DurationMs}ms.")]
    public static partial void LogCompleted(this ILogger logger, Guid id, long durationMs);

    [LoggerMessage(1101, LogLevel.Warning, "Calculation failed: Duplicate input set detected.")]
    public static partial void LogDuplicateInput(this ILogger logger);

    // List (2000-2999)
    [LoggerMessage(2001, LogLevel.Information, "Retrieved {Count} calculation summaries.")]
    public static partial void LogListRetrieved(this ILogger logger, int count);

    // GetById (3000-3999)
    [LoggerMessage(3001, LogLevel.Warning, "Calculation {Id} not found.")]
    public static partial void LogNotFound(this ILogger logger, Guid id);

    // Critical/Data Errors (5000+)
    [LoggerMessage(5001, LogLevel.Error, "Data Integrity Violation: {Message}")]
    public static partial void LogIntegrityError(this ILogger logger, string message);
}