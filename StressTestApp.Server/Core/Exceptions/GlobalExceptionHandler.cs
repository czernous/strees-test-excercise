namespace StressTestApp.Server.Core.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using System.Net;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        // 1. Zero-Allocation Logging via Source Generator
        logger.LogSystemCrash(
            exception,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        // 2. Map to Shared Error Primitive
        var error = Error.Unhandled(exception);

        // 3. Use the Shared HttpError Record
        var response = new HttpError(
            error.Code,
            "A critical system error occurred. Please contact support.",
            HttpStatusCode.InternalServerError);

        // 4. Terminal Write
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, ct);

        return true;
    }
}