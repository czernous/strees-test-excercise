namespace StressTestApp.Server.Shared.Primitives.Result;

using StressTestApp.Server.Shared.Primitives.Errors;

/// <summary>
/// Factory and effect-boundary helpers for the <see cref="Result{T, E}"/> pattern.
/// Keep exception-to-error mapping here rather than inside handlers.
/// </summary>
public static class Result
{
    /// <summary>
    /// Wraps a raw value into a successful <see cref="Result{T, E}"/>.
    /// </summary>
    public static Result<T, Error> Success<T>(T value) => Result<T, Error>.Ok(value);

    /// <summary>
    /// Wraps a raw <see cref="Error"/> into a failed <see cref="Result{T, E}"/>.
    /// </summary>
    public static Result<T, Error> Failure<T>(Error error) => Result<T, Error>.Fail(error);

    /// <summary>
    /// Executes a synchronous effect and maps unexpected exceptions into an <see cref="Error"/>.
    /// </summary>
    public static Result<T, Error> Try<T>(
        Func<T> factory,
        Func<Exception, Error> onException)
    {
        try
        {
            return Success(factory());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return onException(ex);
        }
    }

    /// <summary>
    /// Executes a synchronous effect and maps unexpected exceptions to <see cref="Error.Unhandled(Exception)"/>.
    /// </summary>
    public static Result<T, Error> Try<T>(Func<T> factory) =>
        Try(factory, Error.Unhandled);

    /// <summary>
    /// Executes an asynchronous effect and maps unexpected exceptions into an <see cref="Error"/>.
    /// </summary>
    public static async Task<Result<T, Error>> TryAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        Func<Exception, Error> onException,
        CancellationToken ct)
    {
        try
        {
            return Success(await taskFactory(ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return onException(ex);
        }
    }

    /// <summary>
    /// Executes an asynchronous effect and maps unexpected exceptions to <see cref="Error.Unhandled(Exception)"/>.
    /// </summary>
    public static Task<Result<T, Error>> TryAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        CancellationToken ct) =>
        TryAsync(taskFactory, Error.Unhandled, ct);

    /// <summary>
    /// Executes an asynchronous effect that already returns a <see cref="Result{T, E}"/>
    /// and only maps unexpected exceptions into an <see cref="Error"/>.
    /// </summary>
    public static async Task<Result<T, Error>> TryAsync<T>(
        Func<CancellationToken, Task<Result<T, Error>>> taskFactory,
        Func<Exception, Error> onException,
        CancellationToken ct)
    {
        try
        {
            return await taskFactory(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return onException(ex);
        }
    }

    /// <summary>
    /// Executes an asynchronous effect that already returns a result and maps unexpected exceptions to <see cref="Error.Unhandled(Exception)"/>.
    /// </summary>
    public static Task<Result<T, Error>> TryAsync<T>(
        Func<CancellationToken, Task<Result<T, Error>>> taskFactory,
        CancellationToken ct) =>
        TryAsync(taskFactory, Error.Unhandled, ct);

    /// <summary>
    /// Combines three successful results into a single tuple result, failing fast on the first error.
    /// </summary>
    public static Result<(T1, T2, T3), Error> Combine<T1, T2, T3>(
        Result<T1, Error> r1,
        Result<T2, Error> r2,
        Result<T3, Error> r3)
    {
        if (!r1.IsSuccess) return r1.Error;
        if (!r2.IsSuccess) return r2.Error;
        if (!r3.IsSuccess) return r3.Error;

        return (r1.Value, r2.Value, r3.Value);
    }
}

/// <summary>
/// Async wrappers for <see cref="Result{T, E}"/> pipelines.
/// These methods intentionally delegate to the core instance API on <see cref="Result{T, E}"/>
/// so task-based handlers can stay fluent without duplicating business semantics.
/// </summary>
public static class ResultTaskExtensions
{
    extension<T, E>(Task<Result<T, E>> resultTask) where E : notnull
    {
        /// <summary>
        /// Chains an asynchronous operation onto a task-wrapped result.
        /// </summary>
        public async Task<Result<TR, E>> Bind<TR>(Func<T, Task<Result<TR, E>>> next)
        {
            var result = await resultTask;
            return await result.Bind(next);
        }

        /// <summary>
        /// Maps a successful task-wrapped value into another value while preserving failures.
        /// </summary>
        public async Task<Result<TR, E>> Map<TR>(Func<T, TR> map)
        {
            var result = await resultTask;
            return result.Map(map);
        }

        /// <summary>
        /// Validates a successful task-wrapped value against a predicate.
        /// </summary>
        public async Task<Result<T, E>> Ensure(Func<T, bool> predicate, Func<T, E> onFailure)
        {
            var result = await resultTask;
            return result.Ensure(predicate, onFailure);
        }

        /// <summary>
        /// Executes a side effect for a successful task-wrapped result.
        /// </summary>
        public async Task<Result<T, E>> Tap(Action<T> action)
        {
            var result = await resultTask;
            return result.Tap(action);
        }

        /// <summary>
        /// Executes a side effect for a failed task-wrapped result.
        /// </summary>
        public async Task<Result<T, E>> TapError(Action<E> action)
        {
            var result = await resultTask;
            return result.TapError(action);
        }

        /// <summary>
        /// Resolves a task-wrapped result into a final value.
        /// </summary>
        public async Task<TR> Match<TR>(Func<T, TR> onSuccess, Func<E, TR> onFailure)
        {
            var result = await resultTask;
            return result.Match(onSuccess, onFailure);
        }
    }
}
