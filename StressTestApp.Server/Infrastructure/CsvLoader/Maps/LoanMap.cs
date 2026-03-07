using CsvHelper.Configuration;
using StressTestApp.Server.Data.Models;

namespace StressTestApp.Server.Infrastructure.CsvLoader.Maps;

public sealed class LoanMap : ClassMap<Loan>
{
    public LoanMap()
    {
        Parameter(nameof(Loan.Id)).Name("Loan_ID");
        Parameter(nameof(Loan.PortId)).Name("Port_ID");
        Parameter(nameof(Loan.OriginalAmount)).Name("OriginalLoanAmount");
        Parameter(nameof(Loan.OutstandingAmount)).Name("OutstandingAmount");
        Parameter(nameof(Loan.CollateralValue)).Name("CollateralValue");
        Parameter(nameof(Loan.CreditRating)).Name("CreditRating");
    }
}
