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
        CancellationToken cancellationToken)
    {
        var requestPath = httpContext.Request.Path.Value ?? "/";

        logger.LogSystemCrash(
            exception,
            requestPath,
            httpContext.TraceIdentifier);

        var error = Error.Unhandled(exception);

        var response = new HttpError(
            error.Code,
            "A critical system error occurred. Please contact support.",
            HttpStatusCode.InternalServerError);

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
