using Microsoft.Extensions.Options;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Configurations;
using StressTestApp.Server.Core.IO.Csv.Parser.Maps;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Models;
using System.Collections.Concurrent;

namespace StressTestApp.Server.Core.Storage.MarketDataStore;

public sealed class MarketDataStore(
    ICsvParser csvParser,
    IOptions<CsvPaths> filePathsOptions) : IMarketDataStore, IDisposable
{
    private readonly CsvPaths _filePaths = filePathsOptions.Value;
    // Cache using Type as key
    private readonly ConcurrentDictionary<Type, object> _cache = new();
    // Granular locks per Type
    private readonly ConcurrentDictionary<Type, SemaphoreSlim> _locks = new();
    private HashSet<string> _availableCountries = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AvailableCountries => _availableCountries;

    public async ValueTask<IReadOnlyList<T>> GetOrCacheAsync<T>(CancellationToken ct) 
        where T : struct, IIntegrityContract
    {
        var type = typeof(T);

        if (_cache.TryGetValue(type, out var cached))
        {
            return (IReadOnlyList<T>)cached;
        }

        var semaphore = _locks.GetOrAdd(type, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check pattern
            if (_cache.TryGetValue(type, out cached))
            {
                return (IReadOnlyList<T>)cached;
            }

            var loaded = await LoadByTypeAsync<T>(ct);
            if (typeof(T) == typeof(Portfolio))
            {
                var portfolios = (IReadOnlyList<Portfolio>)loaded;
                var countries = new HashSet<string>(portfolios.Count / 10, StringComparer.OrdinalIgnoreCase);
                foreach (var portfolio in portfolios)
                {
                    countries.Add(portfolio.Country);
                }
                _availableCountries = countries;
            }

            _cache[type] = loaded;

            return loaded;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private Task<IReadOnlyList<T>> LoadByTypeAsync<T>(CancellationToken ct) 
        where T :struct, IIntegrityContract
    {
        // Keep the explicit mapping logic here to maintain Correctness/Maps
        return typeof(T) switch
        {
            Type t when t == typeof(Portfolio) => (Task<IReadOnlyList<T>>)(object)csvParser.ParseAsync<Portfolio, PortfolioMap>(_filePaths.Portfolios, ct),
            Type t when t == typeof(Loan) => (Task<IReadOnlyList<T>>)(object)csvParser.ParseAsync<Loan, LoanMap>(_filePaths.Loans, ct),
            Type t when t == typeof(Rating) => (Task<IReadOnlyList<T>>)(object)csvParser.ParseAsync<Rating, RatingMap>(_filePaths.Ratings, ct),
            _ => throw new NotSupportedException($"No parser mapping for {typeof(T).Name}")
        };
    }

    public void Dispose()
    {
        foreach (var lockItem in _locks.Values)
        {
            lockItem.Dispose();
        }
    }
}