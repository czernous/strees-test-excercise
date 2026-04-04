using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Features.Calculations.Compute;
using StressTestApp.Server.Shared.Models;
using StressTestApp.Server.Verification.TestSupport;

namespace StressTestApp.Server.Verification;

public sealed class PortfolioCalculatorConcurrencyTests
{
    private readonly ICsvParser _parser =
        new CsvParser(new FileLoader(), NullLogger<CsvParser>.Instance);

    [Fact]
    public async Task Calculate_WhenRunConcurrently_ProducesStableDeterministicResults()
    {
        var csvDirectory = RepositoryPaths.FindDataCsvDirectory();

        var portfolios = (await _parser.ParseAsync<Portfolio>(Path.Combine(csvDirectory, "portfolios.csv"))).Value;
        var loans = (await _parser.ParseAsync<Loan>(Path.Combine(csvDirectory, "loans.csv"))).Value;
        var ratings = (await _parser.ParseAsync<Rating>(Path.Combine(csvDirectory, "ratings.csv"))).Value;

        var housePriceChanges = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["GB"] = -5.12m,
            ["US"] = -4.34m,
            ["FR"] = -3.87m,
            ["DE"] = -1.23m,
            ["SG"] = -5.5m,
            ["GR"] = -5.68m
        };

        var baseline = PortfolioCalculator.Calculate(loans, portfolios, ratings, housePriceChanges);

        var concurrentRuns = await Task.WhenAll(
            Enumerable.Range(0, Environment.ProcessorCount * 2)
                .Select(_ => Task.Run(() => PortfolioCalculator.Calculate(loans, portfolios, ratings, housePriceChanges))));

        concurrentRuns.Should().OnlyContain(result => result.Count == baseline.Count);

        foreach (var run in concurrentRuns)
        {
            run.Should().BeEquivalentTo(baseline);
        }
    }
}


