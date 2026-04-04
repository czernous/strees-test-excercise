namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;

/// <summary>
/// Parses a row using an already-bound column map for a specific contract type.
/// </summary>
internal delegate Result<T, Error> BoundParser<T>(Row row, in ColumnMap columns);
