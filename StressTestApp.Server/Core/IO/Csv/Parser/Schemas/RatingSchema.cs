namespace StressTestApp.Server.Core.IO.Csv.Parser.Schemas;

using StressTestApp.Server.Core.IO.Csv.Parser.Binding;
using StressTestApp.Server.Core.IO.Csv.Parser.Errors;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Shared.Primitives.Errors;
using StressTestApp.Server.Shared.Primitives.Result;
using nietras.SeparatedValues;

internal static class RatingSchema
{
    public const string RatingValue = "Rating";
    public const string ProbabilityOfDefault = "ProbablilityOfDefault";

    public static Result<ColumnMap, Error> Bind(SepReaderHeader header)
    {
        var sieve = new HeaderSieve(header);
        var map = new ColumnMap(
            sieve.Idx(RatingValue),
            sieve.Idx(ProbabilityOfDefault));

        return sieve.HasError ? sieve.Error!.Value : map;
    }

    public static Result<Rating, Error> ParseBound(Row row, in ColumnMap columns)
    {
        var sieve = new RowSieve(row);
        var ratingValue = sieve.String(columns.C0, RatingValue);
        var defaultProbability = sieve.Int(columns.C1, ProbabilityOfDefault);

        if (sieve.HasError)
            return sieve.Error!.Value;

        return new Rating(ratingValue, defaultProbability);
    }
}
