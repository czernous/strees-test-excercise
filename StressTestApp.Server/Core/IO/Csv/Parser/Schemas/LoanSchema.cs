namespace StressTestApp.Server.Core.IO.Csv.Parser.Schemas;

using StressTestApp.Server.Core.IO.Csv.Parser.Binding;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using nietras.SeparatedValues;
using System.Globalization;

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
        if (!header.TryIndexOf(LoanId, out var c0))
            return CsvParserError.InvalidSchema(LoanId);

        if (!header.TryIndexOf(PortId, out var c1))
            return CsvParserError.InvalidSchema(PortId);

        if (!header.TryIndexOf(OriginalAmount, out var c2))
            return CsvParserError.InvalidSchema(OriginalAmount);

        if (!header.TryIndexOf(OutstandingAmount, out var c3))
            return CsvParserError.InvalidSchema(OutstandingAmount);

        if (!header.TryIndexOf(CollateralValue, out var c4))
            return CsvParserError.InvalidSchema(CollateralValue);

        if (!header.TryIndexOf(CreditRating, out var c5))
            return CsvParserError.InvalidSchema(CreditRating);

        return new ColumnMap(c0, c1, c2, c3, c4, c5);
    }

    public static Result<Loan, Error> ParseBound(Row row, in ColumnMap columns)
    {
        var loanId = row[columns.C0].Span.Trim(' ');
        if (loanId.IsEmpty)
            return Error.Validation("CSV.MissingValue", $"{LoanId} cannot be empty.");

        if (!int.TryParse(loanId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return Error.Validation("CSV.TypeMismatch", $"{LoanId} must be an integer.");

        var portId = row[columns.C1].Span.Trim(' ');
        if (portId.IsEmpty)
            return Error.Validation("CSV.MissingValue", $"{PortId} cannot be empty.");

        if (!int.TryParse(portId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPortId))
            return Error.Validation("CSV.TypeMismatch", $"{PortId} must be an integer.");

        var originalAmount = row[columns.C2].Span.Trim(' ');
        if (originalAmount.IsEmpty)
            return Error.Validation("CSV.MissingValue", $"{OriginalAmount} cannot be empty.");

        if (!decimal.TryParse(originalAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var original))
            return Error.Validation("CSV.TypeMismatch", $"{OriginalAmount} must be a decimal.");

        var outstandingAmount = row[columns.C3].Span.Trim(' ');
        if (outstandingAmount.IsEmpty)
            return Error.Validation("CSV.MissingValue", $"{OutstandingAmount} cannot be empty.");

        if (!decimal.TryParse(outstandingAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var outstanding))
            return Error.Validation("CSV.TypeMismatch", $"{OutstandingAmount} must be a decimal.");

        var collateralValue = row[columns.C4].Span.Trim(' ');
        if (collateralValue.IsEmpty)
            return Error.Validation("CSV.MissingValue", $"{CollateralValue} cannot be empty.");

        if (!decimal.TryParse(collateralValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var collateral))
            return Error.Validation("CSV.TypeMismatch", $"{CollateralValue} must be a decimal.");

        var rating = row[columns.C5].ToString();
        if (string.IsNullOrWhiteSpace(rating))
            return Error.Validation("CSV.MissingValue", $"{CreditRating} cannot be empty.");

        if (rating.Length > 0 && (rating[0] == ' ' || rating[^1] == ' '))
            rating = rating.Trim();

        return new Loan(id, parsedPortId, original, outstanding, collateral, rating);
    }
}
