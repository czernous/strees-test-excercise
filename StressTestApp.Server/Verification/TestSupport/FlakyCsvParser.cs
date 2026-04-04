using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Collections.Concurrent;

namespace StressTestApp.Server.Verification.TestSupport;

internal sealed class FlakyCsvParser(
    ICsvParser innerParser,
    Func<Type, int, Error?> transientErrorFactory) : ICsvParser
{
    private readonly ConcurrentDictionary<Type, int> _attempts = new();

    public int GetAttemptCount<T>() where T : struct =>
        _attempts.TryGetValue(typeof(T), out var count) ? count : 0;

    public async ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T>(
        string filePath,
        CancellationToken ct = default)
        where T : struct, IIntegrityContract
    {
        var attempt = _attempts.AddOrUpdate(typeof(T), 1, (_, count) => count + 1);
        var transientError = transientErrorFactory(typeof(T), attempt);

        if (transientError is Error error)
        {
            return Result.Failure<IReadOnlyList<T>>(error);
        }

        return await innerParser.ParseAsync<T>(filePath, ct);
    }
}

