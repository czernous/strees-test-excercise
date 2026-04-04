namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

/// <summary>
/// Stores the bound column indexes for a schema after header resolution has succeeded.
/// </summary>
internal readonly record struct ColumnMap(
    int C0,
    int C1 = -1,
    int C2 = -1,
    int C3 = -1,
    int C4 = -1,
    int C5 = -1);
