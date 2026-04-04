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
        var sieve = new HeaderSieve(header);
        var map = new ColumnMap(
            sieve.Idx(PortId),
            sieve.Idx(PortName),
            sieve.Idx(PortCountry),
            sieve.Idx(PortCcy));

        return sieve.HasError ? sieve.Error!.Value : map;
    }

    public static Result<Portfolio, Error> ParseBound(Row row, in ColumnMap columns)
    {
        var sieve = new RowSieve(row);
        var id = sieve.String(columns.C0, PortId);
        var name = sieve.String(columns.C1, PortName);
        var country = sieve.String(columns.C2, PortCountry);
        var ccy = sieve.String(columns.C3, PortCcy);

        if (sieve.HasError)
            return sieve.Error!.Value;

        return new Portfolio(id, name, country, ccy);
    }
}
