using CsvHelper.Configuration;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

public interface ICsvParser
{
    Task<IReadOnlyList<T>> ParseAsync<T, TMap>(string filePath, CancellationToken cancellationToken = default)
        where TMap : ClassMap<T>;
}
