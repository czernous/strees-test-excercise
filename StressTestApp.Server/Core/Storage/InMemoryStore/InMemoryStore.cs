using StressTestApp.Server.Shared.Primitives.Result;
using StressTestApp.Server.Shared.Primitives.Errors;

namespace StressTestApp.Server.Core.Storage.InMemoryStore;

public interface IInMemoryStore
{
    ValueTask<Result<IReadOnlyList<T>, Error>> GetOrCacheAsync<T>(
        CancellationToken ct)
        where T : struct;
}
