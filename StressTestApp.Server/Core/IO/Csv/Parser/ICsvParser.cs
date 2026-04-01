using CsvHelper.Configuration;
using StressTestApp.Server.Shared.Contracts;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

public interface ICsvParser
{
    Task<IReadOnlyList<T>> ParseAsync<T, TMap>(string filePath, CancellationToken cancellationToken = default)
        where T : struct, IIntegrityContract
        where TMap : ClassMap<T>;
}
