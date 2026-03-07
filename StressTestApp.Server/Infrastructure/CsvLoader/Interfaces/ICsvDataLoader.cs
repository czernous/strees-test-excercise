using CsvHelper.Configuration;

namespace StressTestApp.Server.Infrastructure.CsvLoader.Interfaces;

public interface ICsvDataLoader
{
    Task<IReadOnlyList<T>> LoadCsvAsync<T, TMap>(string filePath, CancellationToken cancellationToken = default)
        where TMap : ClassMap<T>;
}
