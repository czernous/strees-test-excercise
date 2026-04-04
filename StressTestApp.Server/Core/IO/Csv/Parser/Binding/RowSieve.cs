namespace StressTestApp.Server.Core.IO.Csv.Parser.Binding;

using StressTestApp.Server.Shared.Primitives.Errors;
using System.Globalization;

/// <summary>
/// Stack-only row reader that captures the first parse failure while keeping field reads linear.
/// </summary>
internal ref struct RowSieve
{
    private readonly Row _row;

    public Error? Error { get; private set; }
    public readonly bool HasError => Error is not null;

    public RowSieve(Row row) => _row = row;

    public int Int(int index, string name)
    {
        if (HasError)
        {
            return default;
        }

        var span = _row[index].Span.Trim(' ');
        if (span.IsEmpty)
        {
            Error = Missing(name);
            return default;
        }

        if (!int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            Error = TypeMismatch(name, "an integer");
            return default;
        }

        return value;
    }

    public decimal Decimal(int index, string name)
    {
        if (HasError)
        {
            return default;
        }

        var span = _row[index].Span.Trim(' ');
        if (span.IsEmpty)
        {
            Error = Missing(name);
            return default;
        }

        if (!decimal.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            Error = TypeMismatch(name, "a decimal");
            return default;
        }

        return value;
    }

    public string String(int index, string name)
    {
        if (HasError)
        {
            return string.Empty;
        }

        var value = _row[index].ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            Error = Missing(name);
            return string.Empty;
        }

        return value.Length > 0 && (value[0] == ' ' || value[^1] == ' ')
            ? value.Trim()
            : value;
    }

    private static Error Missing(string name) =>
        Shared.Primitives.Errors.Error.Validation("CSV.MissingValue", $"{name} cannot be empty.");

    private static Error TypeMismatch(string name, string expected) =>
        Shared.Primitives.Errors.Error.Validation("CSV.TypeMismatch", $"{name} must be {expected}.");
}
