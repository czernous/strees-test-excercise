using CsvHelper.Configuration;
using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

/// <summary>
/// Defines a contract for high-performance, forward-only CSV parsing using memory spans.
/// </summary>
public interface ICsvParser
{
    /// <summary>
    /// Parses a raw byte buffer into a collection of domain models.
    /// </summary>
    /// <param name="data">The raw UTF-8 encoded byte data to parse.</param>
    /// <returns>
    /// A <see cref="Result{T, E}"/> containing the parsed records or a detailed error 
    /// if the schema or data integrity is compromised.
    /// </returns>
    ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T, TMap>(
         string filePath,
         CancellationToken ct = default)
         where T : struct, IIntegrityContract
         where TMap : ClassMap<T>;
}