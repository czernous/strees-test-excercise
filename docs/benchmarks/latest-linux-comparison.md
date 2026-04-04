# Latest Benchmark Reference

This file is the committed benchmark snapshot for the latest reviewed GitHub Actions comparison between the original take-home implementation and the current handwritten `Sep` implementation.

Environment:
- benchmark style: BenchmarkDotNet `ShortRun`
- comparison model: original baseline vs current candidate
- runner: `ubuntu-latest`
- workflow: `.github/workflows/benchmark-compare.yml`

Refs used for this snapshot:
- original baseline: `portfolio/original-bench`
- current candidate: `master`
- workflow run date: `2026-04-04`

## Matching Benchmarks

| Benchmark | Candidate (master) Mean | Baseline (portfolio/original-bench) Mean | Mean Delta | Candidate (master) Allocated | Baseline (portfolio/original-bench) Allocated | Alloc Delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| StressTestApp.Server.Benchmarks.CalculationPipelineBenchmarks / CreateCalculationColdAsync | 61.24 ms | 192.78 ms | -68.23% | 7287.04 KB | 38.54 MB | -81.54% |
| StressTestApp.Server.Benchmarks.CalculationPipelineBenchmarks / CreateCalculationWarmAsync | 27.41 ms | 34.09 ms | -19.6% | 975.72 KB | 12.42 MB | -92.33% |
| StressTestApp.Server.Benchmarks.LoanCalculatorBenchmarks / ComputeBatch | 14.69 ms | 14.59 ms | +0.69% | 0 B | 0 B | n/a |
| StressTestApp.Server.Benchmarks.MarketDataStoreBenchmarks / ColdLoadLoansAsync | 29,692,610.77 ns | 108,852,928.53 ns | -72.72% | 6412354 B | 27202795 B | -76.43% |
| StressTestApp.Server.Benchmarks.MarketDataStoreBenchmarks / WarmCacheLoansAsync | 67.32 ns | 60.95 ns | +10.45% | 72 B | 336 B | -78.57% |
| StressTestApp.Server.Benchmarks.PortfolioCalculatorBenchmarks / CalculatePortfolioStress | 25.40 ms | 22.88 ms | +11.01% | 4.8 KB | 11.45 MB | -99.96% |

## Candidate-Only Benchmarks

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParseLoansAsync | 29,680.77 μs | 6259.6 KB |
| StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParsePortfoliosAsync | 21.92 μs | 24.3 KB |
| StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParseRatingsAsync | 18.30 μs | 23.11 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / BufferedSepMaterializeAndValidateWithExactCapacityAsync | 27,419.7 μs | 8489.04 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / BufferedSepMaterializeListWithExactCapacityAsync | 26,027.4 μs | 8489.16 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / BufferedSepParseAsync | 2,421.6 μs | 6.02 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / DirectSepParseAsync | 2,537.7 μs | 21.07 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / LoadOnlyAsync | 154.6 μs | 1.14 KB |

## Baseline-Only Benchmarks

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| StressTestApp.Server.Benchmarks.CsvLoaderBenchmarks / LoadLoansAsync | 97,772.1 μs | 26568.25 KB |
| StressTestApp.Server.Benchmarks.CsvLoaderBenchmarks / LoadPortfoliosAsync | 631.6 μs | 90.9 KB |
| StressTestApp.Server.Benchmarks.CsvLoaderBenchmarks / LoadRatingsAsync | 601.2 μs | 83.19 KB |
