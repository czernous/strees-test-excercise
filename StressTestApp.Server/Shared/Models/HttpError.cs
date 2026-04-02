using System.Net;

namespace StressTestApp.Server.Shared.Models;

/// <summary>
/// A standardized error response with support for additional metadata.
/// </summary>
public record HttpError(
    string Code,
    string Message,
    HttpStatusCode Status,
    IReadOnlyDictionary<string, string[]>? Errors = null);