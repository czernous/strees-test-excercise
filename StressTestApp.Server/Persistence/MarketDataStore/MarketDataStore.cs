using Microsoft.Extensions.Options;
using StressTestApp.Server.Data.Models;
using StressTestApp.Server.Infrastructure.CsvLoader.Configurations;
using StressTestApp.Server.Infrastructure.CsvLoader.Interfaces;
using StressTestApp.Server.Infrastructure.CsvLoader.Maps;
using StressTestApp.Server.Persistence.MarketDataStore.Interfaces;

namespace StressTestApp.Server.Persistence.MarketDataStore;

public sealed class MarketDataStore(ICsvDataLoader fileLoader, IOptions<CsvPaths> filePathsOptions) : IMarketDataStore, IDisposable
{
    private readonly CsvPaths _filePaths = filePathsOptions.Value;
    private readonly SemaphoreSlim _portfolioGate = new(1, 1);
    private readonly SemaphoreSlim _loanGate = new(1, 1);
    private readonly SemaphoreSlim _ratingGate = new(1, 1);

    private IReadOnlyList<Portfolio>? _portfolios;
    private IReadOnlyList<Loan>? _loans;
    private IReadOnlyList<Rating>? _ratings;

    private static async Task<T> GetOrLoadAsync<T>(
        Func<T?> getter,
        Action<T> setter,
        SemaphoreSlim gate,
        Func<CancellationToken, Task<T>> loader,
        CancellationToken ct)
        where T : class
    {
        var cached = getter();
        if (cached is not null)
            return cached;

        await gate.WaitAsync(ct);
        try
        {
            cached = getter();
            if (cached is not null)
                return cached;

            var loaded = await loader(ct);
            setter(loaded);
            return loaded;
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken ct) =>
        GetOrLoadAsync(
            () => _portfolios,
            loaded => _portfolios = loaded,
            _portfolioGate,
            token => fileLoader.LoadCsvAsync<Portfolio, PortfolioMap>(_filePaths.Portfolios, token),
            ct);

    public Task<IReadOnlyList<Loan>> GetLoansAsync(CancellationToken ct) =>
        GetOrLoadAsync(
            () => _loans,
            loaded => _loans = loaded,
            _loanGate,
            token => fileLoader.LoadCsvAsync<Loan, LoanMap>(_filePaths.Loans, token),
            ct);

    public Task<IReadOnlyList<Rating>> GetRatingsAsync(CancellationToken ct) =>
        GetOrLoadAsync(
            () => _ratings,
            loaded => _ratings = loaded,
            _ratingGate,
            token => fileLoader.LoadCsvAsync<Rating, RatingMap>(_filePaths.Ratings, token),
            ct);
    public void Dispose()
    {
        _portfolioGate.Dispose();
        _loanGate.Dispose();
        _ratingGate.Dispose();
    }
}