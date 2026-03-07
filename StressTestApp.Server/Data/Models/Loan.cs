namespace StressTestApp.Server.Data.Models
{
    public record Loan(
        int Id,
        int PortId,
        int OriginalAmount,
        int OutstandingAmount,
        int CollateralValue,
        string CreditRating
    );
}
