namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

/// <summary>
/// Stores the bound column indexes for a schema after header resolution has succeeded.
/// The fixed-width shape is deliberate for the current schemas.
/// If a future schema needs more columns, extend this map explicitly rather than
/// falling back to name-based lookups inside the hot row loop.
/// </summary>
internal readonly record struct ColumnMap(
    int C0,
    int C1 = -1,
    int C2 = -1,
    int C3 = -1,
    int C4 = -1,
    int C5 = -1);
