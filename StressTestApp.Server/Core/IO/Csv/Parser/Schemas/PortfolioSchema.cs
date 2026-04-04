namespace StressTestApp.Server.Core.IO.Csv.Parser.Schemas;

using StressTestApp.Server.Core.IO.Csv.Parser.Binding;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using nietras.SeparatedValues;

internal static class PortfolioSchema
{
    public const string PortId = "Port_ID";
    public const string PortName = "Port_Name";
    public const string PortCountry = "Port_Country";
    public const string PortCcy = "Port_CCY";

    public static Result<ColumnMap, Error> Bind(SepReaderHeader header)
    {
        if (!header.TryIndexOf(PortId, out var c0))
            return CsvParserError.InvalidSchema(PortId);

        if (!header.TryIndexOf(PortName, out var c1))
            return CsvParserError.InvalidSchema(PortName);

        if (!header.TryIndexOf(PortCountry, out var c2))
            return CsvParserError.InvalidSchema(PortCountry);

        if (!header.TryIndexOf(PortCcy, out var c3))
            return CsvParserError.InvalidSchema(PortCcy);

        return new ColumnMap(c0, c1, c2, c3);
    }

    public static Result<Portfolio, Error> ParseBound(Row row, in ColumnMap columns)
    {
        var id = row[columns.C0].ToString();
        if (string.IsNullOrWhiteSpace(id))
            return Error.Validation("CSV.MissingValue", $"{PortId} cannot be empty.");

        if (id.Length > 0 && (id[0] == ' ' || id[^1] == ' '))
            id = id.Trim();

        var name = row[columns.C1].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("CSV.MissingValue", $"{PortName} cannot be empty.");

        if (name.Length > 0 && (name[0] == ' ' || name[^1] == ' '))
            name = name.Trim();

        var country = row[columns.C2].ToString();
        if (string.IsNullOrWhiteSpace(country))
            return Error.Validation("CSV.MissingValue", $"{PortCountry} cannot be empty.");

        if (country.Length > 0 && (country[0] == ' ' || country[^1] == ' '))
            country = country.Trim();

        var ccy = row[columns.C3].ToString();
        if (string.IsNullOrWhiteSpace(ccy))
            return Error.Validation("CSV.MissingValue", $"{PortCcy} cannot be empty.");

        if (ccy.Length > 0 && (ccy[0] == ' ' || ccy[^1] == ' '))
            ccy = ccy.Trim();

        return new Portfolio(id, name, country, ccy);
    }
}
