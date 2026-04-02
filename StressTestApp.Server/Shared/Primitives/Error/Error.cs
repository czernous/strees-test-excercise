namespace StressTestApp.Server.Shared.Primitives.Errors;

/// <summary>
/// Represents a structured error with a code and message.
/// Used throughout the application for consistent error handling and reporting.
/// </summary>
/// <param name="Code">The error code identifying the type of error (e.g., "IO_001", "VAL_002").</param>
/// <param name="Message">A human-readable description of what went wrong.</param>
public readonly record struct Error(string Code, string Message)
{
    /// <summary>
    /// Creates an I/O error with a specific error code.
    /// </summary>
    /// <param name="code">The specific I/O error code from <see cref="ErrorCode.IO"/>.</param>
    /// <param name="message">A description of the I/O error.</param>
    /// <returns>An Error instance representing an I/O failure.</returns>
    public static Error IO(string code, string message) => new(code, message);

    /// <summary>
    /// Creates a validation error with a specific error code.
    /// </summary>
    /// <param name="code">The specific validation error code from <see cref="ErrorCode.Validation"/>.</param>
    /// <param name="message">A description of the validation failure.</param>
    /// <returns>An Error instance representing a validation failure.</returns>
    public static Error Validation(string code, string message) => new(code, message);

    /// <summary>
    /// Creates a conflict error (e.g., duplicate data) with a specific error code.
    /// </summary>
    /// <param name="code">The specific conflict error code (should contain 'Conflict' for HTTP mapping).</param>
    /// <param name="message">A description of why the conflict occurred.</param>
    /// <returns>An Error instance representing a state conflict.</returns>
    public static Error Conflict(string code, string message) => new(code, message);
    /// <summary>
    /// Creates a system error with a specific error code.
    /// </summary>
    /// <param name="code">The specific system error code from <see cref="ErrorCode.System"/>.</param>
    /// <param name="message">A description of the system error.</param>
    /// <returns>An Error instance representing a system failure.</returns>
    public static Error System(string code, string message) => new(code, message);

    /// <summary>
    /// Creates a generic I/O read error with default error code.
    /// </summary>
    /// <param name="message">A description of what failed to be read.</param>
    /// <returns>An Error with code <see cref="ErrorCode.IO.ReadError"/>.</returns>
    public static Error IO(string message) => new(ErrorCode.IO.ReadError, message);

    /// <summary>
    /// Creates a generic validation error with default error code.
    /// </summary>
    /// <param name="message">A description of what input was invalid.</param>
    /// <returns>An Error with code <see cref="ErrorCode.Validation.InvalidInput"/>.</returns>
    public static Error Validation(string message) => new(ErrorCode.Validation.InvalidInput, message);

    /// <summary>
    /// Creates an unexpected error for rare edge cases.
    /// Use when an error occurs that doesn't fit other categories or is not yet classified.
    /// </summary>
    /// <param name="message">Optional description of the unexpected condition. Defaults to "An unexpected error occurred."</param>
    /// <returns>An Error with code <see cref="ErrorCode.Unknown.UnexpectedError"/>.</returns>
    public static Error Unexpected(string message = "An unexpected error occurred.")
        => new(ErrorCode.Unknown.UnexpectedError, message);

    /// <summary>
    /// Creates an error from an unhandled exception.
    /// Use in global exception handlers to wrap exceptions into structured errors.
    /// </summary>
    /// <param name="ex">The unhandled exception that occurred.</param>
    /// <returns>An Error with code <see cref="ErrorCode.Unknown.UnhandledException"/> and the exception's message.</returns>
    public static Error Unhandled(Exception ex) =>
        new(ErrorCode.Unknown.UnhandledException, ex.Message);

    /// <summary>
    /// Creates a new <see cref="Error"/> with a specific, machine-readable code and a human-readable message.
    /// Use this as the primary factory when mapping errors directly from the <see cref="ErrorCode"/> registry.
    /// </summary>
    /// <param name="code">
    /// A unique, standardized identifier (e.g., <see cref="ErrorCode.IO.NotFound"/>). 
    /// This should be used by the Presentation layer to determine HTTP status codes or UI logic.
    /// </param>
    /// <param name="message">
    /// A descriptive explanation of the failure. Avoid including sensitive system details 
    /// in messages intended for public-facing APIs.
    /// </param>
    /// <returns>A validated <see cref="Error"/> record struct ready for the <see cref="Result{T, E}"/> pipeline.</returns>
    public static Error Create(string code, string message) => new(code, message);

    /// <summary>
    /// Implicitly converts an Error to its formatted string representation.
    /// Format: [CODE] Message (e.g., "[IO_001] File not found")
    /// </summary>
    /// <param name="error">The error to convert.</param>
    public static implicit operator string(Error error) => $"[{error.Code}] {error.Message}";
}