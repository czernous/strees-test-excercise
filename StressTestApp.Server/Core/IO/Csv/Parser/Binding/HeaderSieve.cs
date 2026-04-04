namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

using nietras.SeparatedValues;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Shared.Primitives.Errors;

/// <summary>
/// Captures the first missing-header failure while keeping schema binding compact.
/// </summary>
internal ref struct HeaderSieve(SepReaderHeader header)
{
    private readonly SepReaderHeader _header = header;

    public Error? Error { get; private set; }
    public readonly bool HasError => Error is not null;

    public int Idx(string name)
    {
        if (HasError)
        {
            return -1;
        }

        if (_header.TryIndexOf(name, out var index))
        {
            return index;
        }

        Error = CsvParserError.InvalidSchema(name);
        return -1;
    }
}
