namespace StressTestApp.Server.Core.Storage.InMemoryStore;

public interface IInMemoryStore
{
    Task<IReadOnlyList<T>> GetOrCacheAsync<T>(
        CancellationToken ct)
        where T : class;
}
