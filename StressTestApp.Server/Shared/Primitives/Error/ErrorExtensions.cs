namespace StressTestApp.Server.Extensions;

using Microsoft.AspNetCore.Http.HttpResults;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using System.Net;

public static class ErrorExtensions
{
    extension(Error err)
    {
        public Results<Created<T>, BadRequest<HttpError>, Conflict<HttpError>, InternalServerError<HttpError>>
            ToCreateErrorResult<T>()
        {
            return GetStatusCode(err) switch
            {
                HttpStatusCode.BadRequest => TypedResults.BadRequest(err.ToModel(HttpStatusCode.BadRequest)),
                HttpStatusCode.Conflict => TypedResults.Conflict(err.ToModel(HttpStatusCode.Conflict)),
                _ => TypedResults.InternalServerError(err.ToModel(HttpStatusCode.InternalServerError))
            };
        }
    }

    extension(Error err)
    {
        public Results<Ok<T>, NotFound<HttpError>, InternalServerError<HttpError>>
            ToGetByIdErrorResult<T>()
        {
            return GetStatusCode(err) switch
            {
                HttpStatusCode.NotFound
                    => TypedResults.NotFound(err.ToModel(HttpStatusCode.NotFound)),
                _ => TypedResults.InternalServerError(err.ToModel(HttpStatusCode.InternalServerError))
            };
        }
    }

    extension(Error err)
    {
        public Results<Ok<T>, InternalServerError<HttpError>>
            ToListErrorResult<T>()
        {
            return GetStatusCode(err) switch
            {
                _ => TypedResults.InternalServerError(err.ToModel(HttpStatusCode.InternalServerError))
            };
        }
    }

    /// <summary>
    /// Internal Mapping Helper
    /// </summary>
    extension(Error err)
    {
        private HttpError ToModel(HttpStatusCode status)
            => new(err.Code, err.Message, status);
    }

    private static HttpStatusCode GetStatusCode(Error err) =>
        err.Code switch
        {
            ErrorCode.Validation.InvalidInput => HttpStatusCode.BadRequest,
            ErrorCode.Validation.MissingRequired => HttpStatusCode.BadRequest,
            ErrorCode.Validation.OutOfRange => HttpStatusCode.BadRequest,
            ErrorCode.Validation.InvalidFormat => HttpStatusCode.BadRequest,
            ErrorCode.Validation.DuplicateEntry => HttpStatusCode.Conflict,
            ErrorCode.Database.Conflict => HttpStatusCode.Conflict,
            ErrorCode.Database.IntegrityViolation => HttpStatusCode.Conflict,
            ErrorCode.Database.NotFound => HttpStatusCode.NotFound,
            ErrorCode.IO.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };
}
