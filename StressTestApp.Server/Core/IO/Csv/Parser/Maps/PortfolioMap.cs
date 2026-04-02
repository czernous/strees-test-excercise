using CsvHelper.Configuration;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Core.IO.Csv.Parser.Maps;

public sealed class PortfolioMap : ClassMap<Portfolio>
{
    public PortfolioMap()
    {
        Parameter(nameof(Portfolio.Id)).Name("Port_ID");
        Parameter(nameof(Portfolio.Name)).Name("Port_Name");
        Parameter(nameof(Portfolio.Country)).Name("Port_Country");
        Parameter(nameof(Portfolio.Ccy)).Name("Port_CCY");
    }
}
