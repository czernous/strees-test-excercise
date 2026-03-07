namespace StressTestApp.Server.Data.Models
{
    public record Rating(
        string RatingValue,
        int ProbabilityOfDefault
    );
}
