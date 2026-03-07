using CsvHelper.Configuration;
using StressTestApp.Server.Data.Models;

namespace StressTestApp.Server.Infrastructure.CsvLoader.Maps;

public sealed class RatingMap : ClassMap<Rating>
{
    public RatingMap()
    {
        Parameter(nameof(Rating.RatingValue)).Name("Rating");
        Parameter(nameof(Rating.ProbabilityOfDefault)).Name("ProbablilityOfDefault");
    }
}
