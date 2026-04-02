namespace StressTestApp.Server.Core.Exceptions;

using Microsoft.Extensions.Logging;

public static partial class ExceptionLogger
{
    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Critical,
        Message = "System Crash: Unhandled exception at {Path}. TraceId: {TraceId}")]
    public static partial void LogSystemCrash(
        this ILogger logger,
        Exception ex,
        string path,
        string traceId);
}