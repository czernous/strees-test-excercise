using StressTestApp.Server.Data.Models;

namespace StressTestApp.Server.Persistence.MarketDataStore.Interfaces;

public interface IMarketDataStore
{
    Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken ct);
    Task<IReadOnlyList<Rating>> GetRatingsAsync(CancellationToken ct);
    Task<IReadOnlyList<Loan>> GetLoansAsync(CancellationToken ct);
}
