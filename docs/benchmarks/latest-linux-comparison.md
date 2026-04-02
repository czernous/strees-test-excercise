# Latest Benchmark Reference

This file is the committed benchmark snapshot for the most recent reviewed comparison between the original take-home implementation and the current portfolio implementation.

Environment:
- benchmark style: BenchmarkDotNet `ShortRun`
- comparison model: original baseline vs current candidate
- machine used for this snapshot: local development machine
- note: the GitHub Actions workflow runs the same benchmark shape on `ubuntu-latest` to give a stable, branch-to-branch cloud reference

Refs used for this snapshot:
- original baseline: `portfolio/original-bench` from original submission commit lineage
- current candidate: `fix` / current polished backend

## Summary

| Benchmark | Current | Original | Direction |
| --- | ---: | ---: | --- |
| Loan batch, 100k computations | `12.24 ms` | `13.93 ms` | current faster |
| Portfolio aggregation | `20.98 ms` | `21.08 ms` | essentially equal latency |
| Portfolio aggregation allocation | `4.77 KB` | `11.45 MB` | current much lower allocation |
| Cold loan load / parse | `150.68 ms` | `144.11 ms` | original slightly faster |
| Cold loan load / parse allocation | `44.81 MB` | `27.27 MB` | original lower allocation |
| Warm cached loan read | `49.05 ns` / `72 B` | `46.53 ns` / `336 B` | latency equivalent, current lower allocation |
| Small file portfolios load / parse | `1.280 ms` | `1.414 ms` | current slightly faster |
| Small file ratings load / parse | `1.210 ms` | `1.326 ms` | current slightly faster |

## Interpretation

- The current implementation improves the actual compute path.
- The current implementation dramatically reduces portfolio aggregation allocations.
- Warm-cache behavior remains effectively free in both versions, with lower allocation in the current implementation.
- The original implementation is still slightly better on raw cold loan ingestion time and allocation.
- That tradeoff is acceptable for the current portfolio design because the new implementation adds stronger correctness, cache admission validation, structured error handling, and observability around the ingestion boundary.

## Raw Numbers

### Current

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `LoanCalculator.ComputeBatch` | `12.24 ms` | `-` |
| `PortfolioCalculator.CalculatePortfolioStress` | `20.98 ms` | `4.77 KB` |
| `CsvParser.ParseLoansAsync` | `147.70 ms` | `43.76 MB` |
| `CsvParser.ParsePortfoliosAsync` | `1.280 ms` | `135.71 KB` |
| `CsvParser.ParseRatingsAsync` | `1.210 ms` | `127.86 KB` |
| `MarketDataStore.ColdLoadLoansAsync` | `150.68 ms` | `44.81 MB` |
| `MarketDataStore.WarmCacheLoansAsync` | `49.05 ns` | `72 B` |

### Original

| Benchmark | Mean | Allocated |
| --- | ---: | ---: |
| `LoanCalculator.ComputeBatch` | `13.93 ms` | `-` |
| `PortfolioCalculator.CalculatePortfolioStress` | `21.08 ms` | `11.45 MB` |
| `CsvLoader.LoadLoansAsync` | `138.76 ms` | `26.63 MB` |
| `CsvLoader.LoadPortfoliosAsync` | `1.414 ms` | `90.79 KB` |
| `CsvLoader.LoadRatingsAsync` | `1.326 ms` | `83.09 KB` |
| `MarketDataStore.ColdLoadLoansAsync` | `144.11 ms` | `27.27 MB` |
| `MarketDataStore.WarmCacheLoansAsync` | `46.53 ns` | `336 B` |

## Reproducibility

For repeatable branch-to-branch runs in a stable Linux environment, use the GitHub Actions workflow:
- `.github/workflows/benchmark-compare.yml`

Recommended refs:
- baseline: `portfolio/original-bench`
- candidate: `master`
