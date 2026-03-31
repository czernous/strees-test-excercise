using CsvHelper.Configuration;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Core.IO.Csv.Parser.Maps;

public sealed class RatingMap : ClassMap<Rating>
{
    public RatingMap()
    {
        Parameter(nameof(Rating.RatingValue)).Name("Rating");
        Parameter(nameof(Rating.ProbabilityOfDefault)).Name("ProbablilityOfDefault");
    }
}
