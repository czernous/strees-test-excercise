namespace StressTestApp.Server.Shared.Primitives.Errors;

/// <summary>
/// Global error codes for the application. These can be used across different modules to standardize error handling. 
/// </summary>
public static class ErrorCode
{
    /// <summary>
    /// Provides constants for common I/O error codes.
    /// </summary>
    public static class IO
    {
        /// <summary>
        /// The requested file or resource was not found on the file system.
        /// Use when a file path is valid but the file does not exist.
        /// </summary>
        public const string NotFound = "IO_001";

        /// <summary>
        /// The file or resource is locked by another process and cannot be accessed.
        /// Use when a file operation fails due to exclusive lock held by another process.
        /// </summary>
        public const string Locked = "IO_002";

        /// <summary>
        /// Access to the file or directory is denied due to insufficient permissions.
        /// Use when the application lacks read/write permissions for the resource.
        /// </summary>
        public const string AccessDenied = "IO_003";

        /// <summary>
        /// An error occurred while reading from a file or stream.
        /// Use for general read failures, corrupted data, or unexpected EOF conditions.
        /// </summary>
        public const string ReadError = "IO_004";

        /// <summary>
        /// An error occurred while writing to a file or stream.
        /// Use for disk full, quota exceeded, or other write operation failures.
        /// </summary>
        public const string WriteError = "IO_005";
    }

    /// <summary>
    /// Provides constants for validation error codes.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// User input failed validation rules.
        /// Use when request data is malformed, contains invalid values, or violates business rules.
        /// </summary>
        public const string InvalidInput = "VAL_001";

        /// <summary>
        /// A required field or parameter is missing from the request.
        /// Use when mandatory data is null, empty, or not provided.
        /// </summary>
        public const string MissingRequired = "VAL_002";

        /// <summary>
        /// A numeric or date value is outside the acceptable range.
        /// Use when values exceed min/max bounds or violate range constraints.
        /// </summary>
        public const string OutOfRange = "VAL_003";

        /// <summary>
        /// Data format does not match expected pattern (e.g., invalid email, phone number).
        /// Use when string values fail regex or format validation.
        /// </summary>
        public const string InvalidFormat = "VAL_004";

        /// <summary>
        /// A unique constraint would be violated (e.g., duplicate ID, email).
        /// Use when creating or updating would result in duplicate data.
        /// </summary>
        public const string DuplicateEntry = "VAL_005";

        /// <summary> 
        /// CRITICAL: The data source is structurally compromised. 
        /// Use when when for example CSV headers are missing, columns are shifted, or the file is empty.
        /// </summary>
        public const string DataIntegrityViolation = "VAL_006";
    }

    /// <summary>
    /// Provides constants for database error codes.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// The requested record was not found in the database.
        /// Use when a lookup by primary key or unique key returns no row.
        /// </summary>
        public const string NotFound = "DB_001";

        /// <summary>
        /// Unable to establish a connection to the database.
        /// Use when database is unreachable, credentials are invalid, or network is down.
        /// </summary>
        public const string ConnectionFailed = "DB_002";

        /// <summary>
        /// Database query execution failed.
        /// Use when SQL syntax is invalid, table/column doesn't exist, or query timeout.
        /// </summary>
        public const string QueryFailed = "DB_003";

        /// <summary>
        /// Database operation exceeded the configured timeout period.
        /// Use when long-running queries or transactions fail to complete in time.
        /// </summary>
        public const string Timeout = "DB_004";

        /// <summary>
        /// Concurrent modification conflict detected (optimistic concurrency failure).
        /// Use when another transaction modified the same data, causing version mismatch.
        /// </summary>
        public const string Conflict = "DB_005";

        /// <summary>
        /// Foreign key, unique constraint, or check constraint violation.
        /// Use when database-level integrity rules prevent the operation.
        /// </summary>
        public const string IntegrityViolation = "DB_006";
    }

    /// <summary>
    /// Provides constants for configuration error codes.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Required configuration setting is not found in appsettings.json or environment variables.
        /// Use when application fails to start due to missing config.
        /// </summary>
        public const string MissingConfiguration = "CFG_001";

        /// <summary>
        /// Configuration value is present but has invalid format or out-of-range value.
        /// Use when config validation fails (e.g., invalid connection string, negative timeout).
        /// </summary>
        public const string InvalidConfiguration = "CFG_002";

        /// <summary>
        /// Application or module failed to initialize with the provided configuration.
        /// Use when startup validation or dependency injection setup fails.
        /// </summary>
        public const string InitializationFailed = "CFG_003";
    }

    /// <summary>
    /// Provides constants for external service error codes.
    /// </summary>
    public static class External
    {
        /// <summary>
        /// External service is unreachable or returned 5xx status code.
        /// Use when downstream API, database, or message queue is unavailable.
        /// </summary>
        public const string ServiceUnavailable = "EXT_001";

        /// <summary>
        /// External service call exceeded the configured timeout.
        /// Use when HTTP request or RPC call takes too long to respond.
        /// </summary>
        public const string Timeout = "EXT_002";

        /// <summary>
        /// External service returned malformed or unexpected response.
        /// Use when response schema doesn't match contract or contains invalid data.
        /// </summary>
        public const string InvalidResponse = "EXT_003";

        /// <summary>
        /// External service rate limit or quota exceeded.
        /// Use when API returns 429 Too Many Requests or throttling error.
        /// </summary>
        public const string RateLimitExceeded = "EXT_004";
    }

    /// <summary>
    /// Provides constants for system and infrastructure error codes.
    /// </summary>
    public static class System
    {
        /// <summary>
        /// Application ran out of available memory.
        /// Use when allocation fails or GC cannot free enough memory.
        /// </summary>
        public const string OutOfMemory = "SYS_001";

        /// <summary>
        /// System-level operation exceeded its timeout period.
        /// Use for thread pool starvation, deadlock detection, or async operation timeout.
        /// </summary>
        public const string Timeout = "SYS_002";

        /// <summary>
        /// Operation was cancelled via CancellationToken.
        /// Use when user cancels a request or background task is stopped gracefully.
        /// </summary>
        public const string CancellationRequested = "SYS_003";

        /// <summary>
        /// System resource exhausted (e.g., file handles, threads, connections).
        /// Use when resource pool is depleted or OS limits are reached.
        /// </summary>
        public const string ResourceExhausted = "SYS_004";
    }

    /// <summary>
    /// Provides constants for unknown or unhandled error codes.
    /// </summary>
    public static class Unknown
    {
        /// <summary>
        /// An unexpected error occurred that doesn't fit other categories.
        /// Use as a fallback for rare edge cases or bugs not yet categorized.
        /// </summary>
        public const string UnexpectedError = "UNK_001";

        /// <summary>
        /// An unhandled exception propagated to the global exception handler.
        /// Use when an exception bypasses all specific handlers and reaches top-level.
        /// This typically indicates a programming error or missing error handling.
        /// </summary>
        public const string UnhandledException = "UNK_002";
    }
}
