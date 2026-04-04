# Benchmark Comparison

## Matching Benchmarks

| Benchmark | Candidate (master) Mean | Baseline (portfolio/original-bench) Mean | Mean Delta | Candidate (master) Allocated | Baseline (portfolio/original-bench) Allocated | Alloc Delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| StressTestApp.Server.Benchmarks.CalculationPipelineBenchmarks / CreateCalculationColdAsync | 62.36 ms | 194.78 ms | -67.98% | 7289.48 KB | 38.54 MB | -81.53% |
| StressTestApp.Server.Benchmarks.CalculationPipelineBenchmarks / CreateCalculationWarmAsync | 27.43 ms | 37.21 ms | -26.28% | 975.61 KB | 12.42 MB | -92.33% |
| StressTestApp.Server.Benchmarks.LoanCalculatorBenchmarks / ComputeBatch | 14.73 ms | 17.99 ms | -18.12% | 0 B | 0 B | n/a |
| StressTestApp.Server.Benchmarks.MarketDataStoreBenchmarks / ColdLoadLoansAsync | 31,645,260.12 ns | 106,902,195.73 ns | -70.4% | 6411793 B | 27204022 B | -76.43% |
| StressTestApp.Server.Benchmarks.MarketDataStoreBenchmarks / WarmCacheLoansAsync | 71.88 ns | 52.73 ns | +36.32% | 72 B | 336 B | -78.57% |
| StressTestApp.Server.Benchmarks.PortfolioCalculatorBenchmarks / CalculatePortfolioStress | 25.72 ms | 25.64 ms | +0.31% | 4.8 KB | 11.45 MB | -99.96% |

## Only In Candidate (master)

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParseLoansAsync | 32,447.64 μs | 6258.78 KB |
| StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParsePortfoliosAsync | 21.97 μs | 24.3 KB |
| StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParseRatingsAsync | 19.68 μs | 23.11 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / BufferedSepMaterializeAndValidateWithExactCapacityAsync | 27,029.6 μs | 8489.17 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / BufferedSepMaterializeListWithExactCapacityAsync | 26,595.8 μs | 8489.15 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / BufferedSepParseAsync | 2,512.8 μs | 6.02 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / DirectSepParseAsync | 2,521.8 μs | 21.07 KB |
| StressTestApp.Server.Benchmarks.IngestionIsolationBenchmarks / LoadOnlyAsync | 168.6 μs | 1.14 KB |

## Only In Baseline (portfolio/original-bench)

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| StressTestApp.Server.Benchmarks.CsvLoaderBenchmarks / LoadLoansAsync | 90,413.5 μs | 26569.36 KB |
| StressTestApp.Server.Benchmarks.CsvLoaderBenchmarks / LoadPortfoliosAsync | 634.1 μs | 90.9 KB |
| StressTestApp.Server.Benchmarks.CsvLoaderBenchmarks / LoadRatingsAsync | 616.5 μs | 83.19 KB |

