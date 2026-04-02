using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using StressTestApp.Server.Features.Calculations.Compute;

namespace StressTestApp.Server.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class LoanCalculatorBenchmarks
{
    private readonly (decimal Collateral, decimal Outstanding, decimal Change, decimal Pd)[] _inputs;

    public LoanCalculatorBenchmarks()
    {
        _inputs = Enumerable.Range(1, 100_000)
            .Select(index => (
                Collateral: 100_000m + (index % 5_000),
                Outstanding: 80_000m + (index % 4_000),
                Change: -10m + (index % 30) / 10m,
                Pd: (index % 10 + 1) / 100m))
            .ToArray();
    }

    [Benchmark]
    public decimal ComputeBatch()
    {
        var totalExpectedLoss = 0m;

        foreach (var input in _inputs)
        {
            totalExpectedLoss += LoanCalculator.Compute(
                input.Collateral,
                input.Outstanding,
                input.Change,
                input.Pd).ExpectedLoss;
        }

        return totalExpectedLoss;
    }
}
