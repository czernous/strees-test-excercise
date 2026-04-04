namespace StressTestApp.Server.Core.IO.Csv.Parser.Schemas;

using StressTestApp.Server.Core.IO.Csv.Parser.Binding;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using nietras.SeparatedValues;
using System.Globalization;

internal static class RatingSchema
{
    public const string RatingValue = "Rating";
    public const string ProbabilityOfDefault = "ProbablilityOfDefault";

    public static Result<ColumnMap, Error> Bind(SepReaderHeader header)
    {
        if (!header.TryIndexOf(RatingValue, out var c0))
            return CsvParserError.InvalidSchema(RatingValue);

        if (!header.TryIndexOf(ProbabilityOfDefault, out var c1))
            return CsvParserError.InvalidSchema(ProbabilityOfDefault);

        return new ColumnMap(c0, c1);
    }

    public static Result<Rating, Error> ParseBound(Row row, in ColumnMap columns)
    {
        var ratingValue = row[columns.C0].ToString();
        if (string.IsNullOrWhiteSpace(ratingValue))
            return Error.Validation("CSV.MissingValue", $"{RatingValue} cannot be empty.");

        if (ratingValue.Length > 0 && (ratingValue[0] == ' ' || ratingValue[^1] == ' '))
            ratingValue = ratingValue.Trim();

        var probability = row[columns.C1].Span.Trim(' ');
        if (probability.IsEmpty)
            return Error.Validation("CSV.MissingValue", $"{ProbabilityOfDefault} cannot be empty.");

        if (!int.TryParse(probability, NumberStyles.Integer, CultureInfo.InvariantCulture, out var defaultProbability))
            return Error.Validation("CSV.TypeMismatch", $"{ProbabilityOfDefault} must be an integer.");

        return new Rating(ratingValue, defaultProbability);
    }
}
