using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Core.IO.Csv.Parser.Errors;

/// <summary>
/// Specialized factory for mapping CSV parsing, schema mismatches, and 
/// data integrity violations to standardized <see cref="Error"/> primitives.
/// </summary>
public static class CsvParserError
{
    /// <summary>
    /// The CSV header row does not match the expected domain model schema or ClassMap.
    /// Use this when the column count is incorrect or required headers are missing.
    /// </summary>
    /// <param name="expected">A description or name of the expected schema/mapping.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.Validation.DataIntegrityViolation"/> code.</returns>
    public static Error InvalidSchema(string expected) =>
        Error.Create(ErrorCode.Validation.DataIntegrityViolation, $"CSV Header mismatch. Expected schema: {expected}");

    /// <summary>
    /// The source data is empty, contains only whitespace, or consists solely of a header row.
    /// Use this to prevent downstream processing of empty collections.
    /// </summary>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.Validation.DataIntegrityViolation"/> code.</returns>
    public static Error EmptySource() =>
        Error.Create(ErrorCode.Validation.DataIntegrityViolation, "The provided data source contains no records to parse.");

    /// <summary>
    /// A specific row or field failed to parse into the target type (e.g., failed <see cref="System.Buffers.Text.Utf8Parser"/>).
    /// Typically indicates a data type mismatch (e.g., 'ABC' in a decimal column).
    /// </summary>
    /// <param name="lineNumber">The 1-based index of the row where the failure occurred.</param>
    /// <param name="details">A description of the parsing failure or the specific field name.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.Validation.InvalidFormat"/> code.</returns>
    public static Error MalformedRow(int lineNumber, string details) =>
        Error.Create(ErrorCode.Validation.InvalidFormat, $"Line {lineNumber}: {details}");

    /// <summary>
    /// The file exists but the internal structure is corrupted (e.g., unclosed quotes or invalid line endings).
    /// </summary>
    /// <param name="reason">The specific reason for the corruption.</param>
    /// <returns>An <see cref="Error"/> with the <see cref="ErrorCode.Validation.DataIntegrityViolation"/> code.</returns>
    public static Error CorruptStructure(string reason) =>
        Error.Create(ErrorCode.Validation.DataIntegrityViolation, $"CSV structural corruption: {reason}");
}
