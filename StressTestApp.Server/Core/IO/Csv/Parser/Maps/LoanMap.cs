using CsvHelper.Configuration;
using StressTestApp.Server.Core.IO.Csv.Parser.Converters;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Core.IO.Csv.Parser.Maps;

public sealed class LoanMap : ClassMap<Loan>
{
    public LoanMap()
    {
        Parameter(nameof(Loan.Id))
            .Name("Loan_ID");
        Parameter(nameof(Loan.PortId))
            .Name("Port_ID");
        Parameter(nameof(Loan.OriginalAmount))
            .Name("OriginalLoanAmount")
            .TypeConverter<DecimalConverter>();
        Parameter(nameof(Loan.OutstandingAmount))
            .Name("OutstandingAmount")
            .TypeConverter<DecimalConverter>();
        Parameter(nameof(Loan.CollateralValue))
            .Name("CollateralValue")
            .TypeConverter<DecimalConverter>();
        Parameter(nameof(Loan.CreditRating))
            .Name("CreditRating");
    }
}
