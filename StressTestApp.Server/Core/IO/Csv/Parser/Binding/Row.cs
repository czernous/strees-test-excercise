using nietras.SeparatedValues;

namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

/// <summary>
/// Short-lived facade over the current Sep row. Values must be parsed or copied immediately.
/// </summary>
internal readonly ref struct Row(SepReader.Row row)
{
    private readonly SepReader.Row _row = row;

    public int LineNumber => _row.LineNumberFrom;
    public int ColumnCount => _row.ColCount;

    public SepReader.Col this[int index] => _row[index];

    public SepReader.Col this[string name] => _row[name];
}
