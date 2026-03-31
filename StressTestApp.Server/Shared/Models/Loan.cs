namespace StressTestApp.Server.Shared.Models
{
    public record Loan(
        int Id,
        int PortId,
        decimal OriginalAmount,
        decimal OutstandingAmount,
        decimal CollateralValue,
        string CreditRating
    );
}
