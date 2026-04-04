namespace StressTestApp.Server.Core.IO.Csv.Parser.Schemas;

using StressTestApp.Server.Core.IO.Csv.Parser.Binding;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using nietras.SeparatedValues;

/// <summary>
/// Handwritten loan schema optimized for the hot ingestion path.
/// </summary>
internal static class LoanSchema
{
    public const string LoanId = "Loan_ID";
    public const string PortId = "Port_ID";
    public const string OriginalAmount = "OriginalLoanAmount";
    public const string OutstandingAmount = "OutstandingAmount";
    public const string CollateralValue = "CollateralValue";
    public const string CreditRating = "CreditRating";

    public static Result<ColumnMap, Error> Bind(SepReaderHeader header)
    {
        var sieve = new HeaderSieve(header);
        var map = new ColumnMap(
            sieve.Idx(LoanId),
            sieve.Idx(PortId),
            sieve.Idx(OriginalAmount),
            sieve.Idx(OutstandingAmount),
            sieve.Idx(CollateralValue),
            sieve.Idx(CreditRating));

        return sieve.HasError ? sieve.Error!.Value : map;
    }

    public static Result<Loan, Error> ParseBound(Row row, in ColumnMap columns)
    {
        var sieve = new RowSieve(row);
        var id = sieve.Int(columns.C0, LoanId);
        var parsedPortId = sieve.Int(columns.C1, PortId);
        var original = sieve.Decimal(columns.C2, OriginalAmount);
        var outstanding = sieve.Decimal(columns.C3, OutstandingAmount);
        var collateral = sieve.Decimal(columns.C4, CollateralValue);
        var rating = sieve.String(columns.C5, CreditRating);

        if (sieve.HasError)
            return sieve.Error!.Value;

        return new Loan(id, parsedPortId, original, outstanding, collateral, rating);
    }
}
