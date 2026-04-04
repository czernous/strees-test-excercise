using StressTestApp.Server.Shared.Contracts;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

namespace StressTestApp.Server.Core.IO.Csv.Parser;

/// <summary>
/// Defines the ingestion parsing boundary for reference-data files.
/// </summary>
public interface ICsvParser
{
    /// <summary>
    /// Parses a CSV file into validated domain models ready for cache admission.
    /// </summary>
    /// <param name="filePath">The physical path to the CSV source file.</param>
    /// <returns>
    /// A <see cref="Result{T, E}"/> containing the parsed records or a detailed error
    /// if file loading, CSV structure, or record integrity fails.
    /// </returns>
    ValueTask<Result<IReadOnlyList<T>, Error>> ParseAsync<T>(
         string filePath,
         CancellationToken ct = default)
         where T : struct, IIntegrityContract;
}
