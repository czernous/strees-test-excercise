using Microsoft.Extensions.Options;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.Csv.Parser.Maps;
using StressTestApp.Server.Core.Storage.InMemoryStore;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Core.Storage.MarketDataStore;

public sealed class MarketDataStore(ICsvParser csvParser, IOptions<CsvPaths> filePathsOptions) : IInMemoryStore, IDisposable
{
    private readonly CsvPaths _filePaths = filePathsOptions.Value;
    private readonly ICsvParser _parser = csvParser;
    private readonly SemaphoreSlim _portfolioGate = new(1, 1);
    private readonly SemaphoreSlim _loanGate = new(1, 1);
    private readonly SemaphoreSlim _ratingGate = new(1, 1);

    private IReadOnlyList<Portfolio>? _portfolios;
    private IReadOnlyList<Loan>? _loans;
    private IReadOnlyList<Rating>? _ratings;

    private static async Task<IReadOnlyList<T>> GetOrCacheAsync<T>(
        Func<IReadOnlyList<T>?> getter,
        Action<IReadOnlyList<T>> setter,
        SemaphoreSlim gate,
        Func<CancellationToken, Task<IReadOnlyList<T>>> loader,
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
    public Task<IReadOnlyList<T>> GetOrCacheAsync<T>(CancellationToken ct) where T : class
    {
        return typeof(T) switch
        {
            Type t when t == typeof(Portfolio) => (Task<IReadOnlyList<T>>)(object)GetPortfoliosAsync(ct),
            Type t when t == typeof(Loan) => (Task<IReadOnlyList<T>>)(object)GetLoansAsync(ct),
            Type t when t == typeof(Rating) => (Task<IReadOnlyList<T>>)(object)GetRatingsAsync(ct),
            _ => throw new NotSupportedException($"Type {typeof(T).Name} is not supported by this store.")
        };
    }

    public Task<IReadOnlyList<Portfolio>> GetPortfoliosAsync(CancellationToken ct) =>
        GetOrCacheAsync(
            () => _portfolios,
            loaded => _portfolios = loaded,
            _portfolioGate,
            token => _parser.ParseAsync<Portfolio, PortfolioMap>(_filePaths.Portfolios, token),
            ct);

    public Task<IReadOnlyList<Loan>> GetLoansAsync(CancellationToken ct) =>
        GetOrCacheAsync(
            () => _loans,
            loaded => _loans = loaded,
            _loanGate,
            token => _parser.ParseAsync<Loan, LoanMap>(_filePaths.Loans, token),
            ct);

    public Task<IReadOnlyList<Rating>> GetRatingsAsync(CancellationToken ct) =>
        GetOrCacheAsync(
            () => _ratings,
            loaded => _ratings = loaded,
            _ratingGate,
            token => _parser.ParseAsync<Rating, RatingMap>(_filePaths.Ratings, token),
            ct);
    public void Dispose()
    {
        _portfolioGate.Dispose();
        _loanGate.Dispose();
        _ratingGate.Dispose();
    }
}