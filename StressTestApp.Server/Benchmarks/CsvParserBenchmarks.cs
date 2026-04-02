using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using StressTestApp.Server.Benchmarks.TestSupport;
using StressTestApp.Server.Core.IO.Csv.Parser;
using StressTestApp.Server.Core.IO.Csv.Parser.Maps;
using StressTestApp.Server.Core.IO.FileLoader;
using StressTestApp.Server.Shared.Models;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CsvParserBenchmarks
{
    private CsvParser _parser = null!;
    private string _csvDirectory = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new CsvParser(new FileLoader(), NullLogger<CsvParser>.Instance);
        _csvDirectory = RepositoryPaths.FindDataCsvDirectory();
    }

    [Benchmark]
    public async Task<int> ParseLoansAsync() =>
        (await _parser.ParseAsync<Loan, LoanMap>(Path.Combine(_csvDirectory, "loans.csv"))).Value.Count;

    [Benchmark]
    public async Task<int> ParsePortfoliosAsync() =>
        (await _parser.ParseAsync<Portfolio, PortfolioMap>(Path.Combine(_csvDirectory, "portfolios.csv"))).Value.Count;

    [Benchmark]
    public async Task<int> ParseRatingsAsync() =>
        (await _parser.ParseAsync<Rating, RatingMap>(Path.Combine(_csvDirectory, "ratings.csv"))).Value.Count;
}

