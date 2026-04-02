namespace StressTestApp.Server.Shared.Primitives.Result;

/// <summary>
/// A lightweight discriminated union representing either a successful value of type
/// <typeparamref name="T"/> or a failure of type <typeparamref name="E"/>.
/// This type owns the core fluent API for in-memory result composition.
/// </summary>
/// <typeparam name="T">The type of the successful value.</typeparam>
/// <typeparam name="E">The type of the error, constrained to non-nullable types.</typeparam>
public readonly record struct Result<T, E> where E : notnull
{
    private readonly T? _value;
    private readonly E? _error;
    private readonly bool _isOk;

    /// <summary>
    /// Indicates whether the current result is successful.
    /// </summary>
    public bool IsSuccess => _isOk;

    /// <summary>
    /// Gets the successful value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed on a failed result.</exception>
    public T Value => _isOk ? _value! : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    /// <summary>
    /// Gets the error associated with a failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed on a successful result.</exception>
    public E Error => !_isOk ? _error! : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    private Result(T? v, bool ok, E? e) => (_value, _isOk, _error) = (v, ok, e);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T, E> Ok(T value) => new(value, true, default);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T, E> Fail(E error) => new(default, false, error);

    /// <summary>
    /// Resolves the current result into a final value.
    /// </summary>
    public TR Match<TR>(Func<T, TR> ok, Func<E, TR> err) =>
        _isOk ? ok(_value!) : err(_error!);

    /// <summary>
    /// Chains a synchronous operation that returns the next result.
    /// </summary>
    public Result<TR, E> Bind<TR>(Func<T, Result<TR, E>> next) =>
        _isOk ? next(_value!) : Result<TR, E>.Fail(_error!);

    /// <summary>
    /// Chains an asynchronous operation that returns the next result.
    /// </summary>
    public Task<Result<TR, E>> Bind<TR>(Func<T, Task<Result<TR, E>>> next) =>
        _isOk ? next(_value!) : Task.FromResult(Result<TR, E>.Fail(_error!));

    /// <summary>
    /// Maps a successful value while preserving failures.
    /// </summary>
    public Result<TR, E> Map<TR>(Func<T, TR> map) =>
        _isOk ? Result<TR, E>.Ok(map(_value!)) : Result<TR, E>.Fail(_error!);

    /// <summary>
    /// Validates a successful value against a predicate.
    /// </summary>
    public Result<T, E> Ensure(Func<T, bool> predicate, Func<T, E> onFailure) =>
        _isOk
            ? (predicate(_value!) ? this : Result<T, E>.Fail(onFailure(_value!)))
            : this;

    /// <summary>
    /// Executes a side effect on success and preserves the current result.
    /// </summary>
    public Result<T, E> Tap(Action<T> action)
    {
        if (_isOk)
        {
            action(_value!);
        }

        return this;
    }

    /// <summary>
    /// Executes an asynchronous side effect on success and preserves the current result.
    /// </summary>
    public async ValueTask<Result<T, E>> Tap(Func<T, ValueTask> action)
    {
        if (_isOk)
        {
            await action(_value!);
        }

        return this;
    }

    /// <summary>
    /// Executes a side effect on failure and preserves the current result.
    /// </summary>
    public Result<T, E> TapError(Action<E> action)
    {
        if (!_isOk)
        {
            action(_error!);
        }

        return this;
    }

    /// <summary>
    /// Executes an action regardless of state and preserves the current result.
    /// </summary>
    public Result<T, E> Finally<TState>(TState state, Action<TState> action)
    {
        action(state);
        return this;
    }

    public override string ToString() => _isOk
        ? $"Ok({(_value is null ? "null" : _value)})"
        : $"Fail({(_error is null ? "null" : _error)})";

    public static implicit operator Result<T, E>(T value) => Ok(value);
    public static implicit operator Result<T, E>(E error) => Fail(error);
}
