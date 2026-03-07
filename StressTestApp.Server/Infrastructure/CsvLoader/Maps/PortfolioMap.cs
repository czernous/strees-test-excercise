using CsvHelper.Configuration;
using StressTestApp.Server.Data.Models;

namespace StressTestApp.Server.Infrastructure.CsvLoader.Maps;

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
