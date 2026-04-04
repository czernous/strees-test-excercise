namespace StressTestApp.Server.Core.Storage.MarketDataStore;

using Microsoft.Extensions.Options;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using System.Collections.Concurrent;

public sealed class MarketDataStore(
    ICsvParser csvParser,
    IOptions<CsvPaths> filePathsOptions) : IMarketDataStore, IDisposable
{
    private readonly CsvPaths _filePaths = filePathsOptions.Value;
    private readonly ConcurrentDictionary<Type, object> _cache = new();
    private readonly ConcurrentDictionary<Type, SemaphoreSlim> _locks = new();

    // Using a simple HashSet for updates, but exposing as IReadOnlySet
    private HashSet<string> _availableCountries = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AvailableCountries => _availableCountries;

    public async ValueTask<Result<IReadOnlyList<T>, Error>> GetOrCacheAsync<T>(CancellationToken ct)
         where T : struct
    {
        var type = typeof(T);

        if (_cache.TryGetValue(type, out var cached))
        {
            return Result.Success((IReadOnlyList<T>)cached);
        }

        var semaphore = _locks.GetOrAdd(type, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            if (_cache.TryGetValue(type, out cached))
            {
                return Result.Success((IReadOnlyList<T>)cached);
            }

            // LoadByTypeAsync now returns Result<IReadOnlyList<T>, Error>
            var result = await LoadByTypeAsync<T>(ct);

            if (!result.IsSuccess) return result;

            var data = result.Value;

            if (typeof(T) == typeof(Portfolio))
            {
                UpdateCountryCache((IReadOnlyList<Portfolio>)data);
            }

            _cache[type] = data;
            return Result.Success(data);
        }
        finally
        {
            semaphore.Release();
        }
    }
    private async ValueTask<Result<IReadOnlyList<T>, Error>> LoadByTypeAsync<T>(CancellationToken ct)
        where T : struct
    {
        return typeof(T) switch
        {
            Type t when t == typeof(Portfolio) => await ParseAs<T, Portfolio>(_filePaths.Portfolios, ct),
            Type t when t == typeof(Loan) => await ParseAs<T, Loan>(_filePaths.Loans, ct),
            Type t when t == typeof(Rating) => await ParseAs<T, Rating>(_filePaths.Ratings, ct),
            _ => Error.Validation("Store.UnsupportedType", $"No mapping for {typeof(T).Name}")
        };
    }

    // Helper to bridge the generic T with the specific Parser types safely
    private async Task<Result<IReadOnlyList<T>, Error>> ParseAs<T, TActual>(string path, CancellationToken ct)
        where T : struct
        where TActual : struct, IIntegrityContract
    {
        var result = await csvParser.ParseAsync<TActual>(path, ct);

        return result.IsSuccess
            ? Result.Success((IReadOnlyList<T>)(object)result.Value)
            : result.Error;
    }

    private void UpdateCountryCache(IReadOnlyList<Portfolio> portfolios)
    {
        var countries = new HashSet<string>(portfolios.Count / 10, StringComparer.OrdinalIgnoreCase);
        foreach (var p in portfolios) countries.Add(p.Country);
        _availableCountries = countries;
    }

    public void Dispose()
    {
        foreach (var lockItem in _locks.Values) lockItem.Dispose();
        _locks.Clear();
    }
}
