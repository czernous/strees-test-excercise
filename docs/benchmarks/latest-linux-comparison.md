# Latest Benchmark Reference

This file is the committed benchmark snapshot for the latest reviewed GitHub Actions comparison between the original take-home implementation and the current CsvHelper-based portfolio implementation.

Environment:
- benchmark style: BenchmarkDotNet `ShortRun`
- comparison model: original baseline vs current candidate
- runner: `ubuntu-latest`
- workflow: `.github/workflows/benchmark-compare.yml`

Refs used for this snapshot:
- original baseline: `portfolio/original-bench`
- current candidate: `master`
- workflow run date: `2026-04-02`

## Matching Benchmarks

| Benchmark | Candidate (master) Mean | Baseline (portfolio/original-bench) Mean | Mean Delta | Candidate (master) Allocated | Baseline (portfolio/original-bench) Allocated | Alloc Delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `CalculationPipelineBenchmarks.CreateCalculationColdAsync` | `173.68 ms` | `184.86 ms` | `-6.05%` | `29952.71 KB` | `38.53 MB` | `-24.08%` |
| `CalculationPipelineBenchmarks.CreateCalculationWarmAsync` | `33.49 ms` | `44.06 ms` | `-23.99%` | `998.94 KB` | `12.42 MB` | `-92.15%` |
| `LoanCalculatorBenchmarks.ComputeBatch` | `16.29 ms` | `16.12 ms` | `+1.05%` | `0 B` | `0 B` | `n/a` |
| `MarketDataStoreBenchmarks.ColdLoadLoansAsync` | `120,981,863.58 ns` | `110,028,014.20 ns` | `+9.96%` | `29376268 B` | `27204029 B` | `+7.98%` |
| `MarketDataStoreBenchmarks.WarmCacheLoansAsync` | `67.20 ns` | `57.41 ns` | `+17.05%` | `72 B` | `336 B` | `-78.57%` |
| `PortfolioCalculatorBenchmarks.CalculatePortfolioStress` | `28.68 ms` | `24.14 ms` | `+18.81%` | `4.77 KB` | `11.45 MB` | `-99.96%` |

## Interpretation

- The current implementation wins clearly on the end-to-end calculation path in both cold and warm modes.
- The current implementation dramatically lowers warm request allocation and portfolio aggregation allocation.
- Warm-cache access remains effectively free in both versions, with lower allocation in the current implementation.
- The original implementation is still slightly better on isolated cold loan ingestion latency and raw portfolio aggregation latency on this Linux runner.
- That remaining tradeoff is acceptable because the current implementation keeps stronger correctness, cache-admission validation, structured error handling, and observability while materially improving the actual request path.

## Candidate-Only Benchmarks

These are additional diagnostics that exist only in the current branch and are useful for internal analysis rather than direct branch-to-branch scoring.

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `CsvParserBenchmarks.ParseLoansAsync` | `112,729.5 μs` | `28683.14 KB` |
| `CsvParserBenchmarks.ParsePortfoliosAsync` | `617.3 μs` | `134.78 KB` |
| `CsvParserBenchmarks.ParseRatingsAsync` | `571.3 μs` | `127.13 KB` |
| `IngestionIsolationBenchmarks.BufferedMaterializeAndValidateWithExactCapacityAsync` | `102,979.5 μs` | `28682.34 KB` |
| `IngestionIsolationBenchmarks.BufferedMaterializeListWithExactCapacityAsync` | `103,360.9 μs` | `28682.17 KB` |
| `IngestionIsolationBenchmarks.BufferedMemoryStreamParseAsync` | `99,684.1 μs` | `22433.19 KB` |
| `IngestionIsolationBenchmarks.DirectCsvHelperParseAsync` | `92,036.3 μs` | `22431.01 KB` |
| `IngestionIsolationBenchmarks.LoadOnlyAsync` | `175.2 μs` | `1.14 KB` |
| `IngestionIsolationBenchmarks.ParserLikeMaterializeAndValidateWithExactCapacityAsync` | `122,807.7 μs` | `35713.8 KB` |
| `IngestionIsolationBenchmarks.ParserLikeMaterializeListWithExactCapacityAsync` | `117,235.7 μs` | `35713.39 KB` |
| `IngestionIsolationBenchmarks.ParserLikeRawMaterializeAndValidateWithExactCapacityAsync` | `103,462.2 μs` | `36893.65 KB` |
| `IngestionIsolationBenchmarks.ParserLikeRawMaterializeListWithExactCapacityAsync` | `103,444.0 μs` | `36893.65 KB` |

## Baseline-Only Benchmarks

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `CsvLoaderBenchmarks.LoadLoansAsync` | `94,931.5 μs` | `26568.71 KB` |
| `CsvLoaderBenchmarks.LoadPortfoliosAsync` | `656.2 μs` | `90.9 KB` |
| `CsvLoaderBenchmarks.LoadRatingsAsync` | `606.0 μs` | `83.23 KB` |

## Reproducibility

For repeatable branch-to-branch runs in the same Linux environment, use:
- `.github/workflows/benchmark-compare.yml`

Recommended refs:
- baseline: `portfolio/original-bench`
- candidate: `master`
