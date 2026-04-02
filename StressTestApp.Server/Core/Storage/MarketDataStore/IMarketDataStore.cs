using StressTestApp.Server.Core.Storage.InMemoryStore;

namespace StressTestApp.Server.Core.Storage.MarketDataStore;

public interface IMarketDataStore : IInMemoryStore
{
    IReadOnlySet<string> AvailableCountries { get; }

}
