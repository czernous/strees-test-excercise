using StressTestApp.Server.Shared.Contracts;

namespace StressTestApp.Server.Core.Storage.InMemoryStore;

public interface IInMemoryStore
{
    ValueTask<IReadOnlyList<T>> GetOrCacheAsync<T>(
        CancellationToken ct)
        where T : struct, IIntegrityContract;

}
