using CsvHelper.Configuration;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Collections.Concurrent;

namespace StressTestApp.Server.Verification.TestSupport;

internal sealed class CountingCsvParser(ICsvParser innerParser) : ICsvParser
{
    private readonly ConcurrentDictionary<Type, int> _counts = new();

    public int GetCallCount<T>() where T : struct =>
        _counts.TryGetValue(typeof(T), out var count) ? count : 0;

    public async ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T, TMap>(
        string filePath,
        CancellationToken ct = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>
    {
        _counts.AddOrUpdate(typeof(T), 1, (_, count) => count + 1);
        return await innerParser.ParseAsync<T, TMap>(filePath, ct);
    }
}
