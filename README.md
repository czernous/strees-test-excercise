# Stress Test Application

A full-stack stress testing application for loan portfolios. It started as a take-home exercise and was then revisited as a portfolio project with a stronger production-quality focus: correctness first, then explicit failure handling, safe reference-data ingestion, observability, and measured hot-path performance.

## What It Does

The application lets you:
- define house price shock scenarios by country
- calculate portfolio-level stressed collateral and expected loss
- persist and review historical calculations
- inspect available market-data countries

## Tech Stack

- Backend: ASP.NET Core 10, Carter minimal APIs, Entity Framework Core, SQLite
- Frontend: React 19, TypeScript, Vite, Tailwind CSS v4, DaisyUI
- Validation and control flow: custom `Result<T, Error>` and typed HTTP results
- Verification: xUnit, FluentAssertions, BenchmarkDotNet

## Why The Backend Looks Like This

This codebase is intentionally shaped around production concerns even though the current deployment is small.

### 1. File IO, Parsing, and Cache Admission Are Separate

Reference data currently lives in CSV files, but the backend treats three concerns separately:
- file loading: get bytes from disk
- parsing: map bytes into typed records
- integrity validation: reject malformed records before they enter the cache

That separation is deliberate. In a larger system, file acquisition and parsing would likely live behind different services or queues, and the cache might be distributed rather than in-process. The current boundaries are designed so those responsibilities can evolve without rewriting the calculation pipeline. The broader goal is to keep one reusable ingestion path that can be applied to multiple reference-data source types while preserving shared integrity validation and cache-admission rules. The implementation can still specialize underneath that boundary when one source shape clearly dominates the cost profile.

### 2. Cached Reference Data Is Treated As Trusted Once Admitted

The application does not want corrupted market data in cache. Each record type implements `IIntegrityContract`, and records are validated during ingestion before being admitted into the market-data store. Once cached, handlers can treat reference data as trusted input rather than re-validating it on every request.

### 3. Request Paths Are Explicit About Failure

Expected failures such as invalid input, duplicate requests, missing data, parser errors, and persistence issues are returned through `Result<T, Error>` and mapped to typed HTTP responses. The goal is to keep handlers thin and predictable:
- pure/domain composition in `Bind`, `Map`, and `Ensure`
- side-effect boundaries in `Try` and `TryAsync`
- logging in `Tap` and `TapError`
- HTTP translation only at the endpoint boundary

### 4. Hot Paths Are Optimised Where The Cost Is Real

The backend avoids repeated CSV reads by caching parsed market data in memory behind per-type locks. The calculation path uses single-pass aggregation and allocation-light loan math. Some ingestion and parsing code is more deliberate than a simple take-home needs because the portfolio goal here was to show how the design would scale into a more realistic stress or risk processing environment.

## Architecture

The backend uses a pragmatic vertical-slice layout for use cases:
- `Features/Calculations/Create`
- `Features/Calculations/GetById`
- `Features/Calculations/List`
- `Features/Countries/List`

Cross-cutting technical concerns live outside slices where pure VSA becomes awkward inside a single project:
- `Core/IO` for file loading and CSV parsing
- `Core/Storage` for in-memory market data caching
- `Core/Database` for EF Core persistence
- `Shared/Primitives` for `Result` and `Error`
- `Shared/Models` for shared domain/reference models

This is intentionally a hybrid rather than dogmatic VSA. In a larger solution, these boundaries would likely move into separate class libraries or services.

## Data Pipeline

The current calculation pipeline is:
1. request validation
2. market-data load from cache, or one-time CSV ingestion on cold start
3. pre-cache integrity validation of parsed records
4. country validation against cached portfolio metadata
5. pure loan and portfolio stress calculation
6. duplicate detection and persistence
7. typed success or structured error response

Cold-load behaviour is concurrency-safe. If multiple requests need the same uncached reference data at once, only one load occurs per type and the rest reuse the same cached instance.

## Performance And Verification

The repository contains three layers of evidence:
- unit and integration tests in `StressTestApp.Tests`
- concurrency and cache verification tests in `StressTestApp.Server/Verification`
- hot-path benchmarks in `StressTestApp.Server/Benchmarks`
- a committed benchmark reference snapshot in [docs/benchmarks/latest-linux-comparison.md](docs/benchmarks/latest-linux-comparison.md)

### Current benchmark highlights

Latest GitHub Actions Linux snapshot as of `2026-04-04`, comparing:
- original submission
- best tuned `CsvHelper` branch
- final handwritten `Sep` parser on `master`

<!-- benchmark-table:start -->
| Benchmark | Original | Best CsvHelper | Final Handwritten Sep |
|---|---:|---:|---:|
| `LoanCalculator.ComputeBatch` | `17.99 ms`, `0 B` | `12.27 ms`, `0 B` | `14.73 ms`, `0 B` |
| `PortfolioCalculator.CalculatePortfolioStress` | `25.64 ms`, `11.45 MB` | `21.25 ms`, `4.77 KB` | `25.72 ms`, `4.8 KB` |
| `CsvParser.ParseLoansAsync` | n/a | `126.302 ms`, `28683.45 KB` | `32.45 ms`, `6258.78 KB` |
| `MarketDataStore.ColdLoadLoansAsync` | `106,902,195.73 ns`, `27204022 B` | `124,724,383.33 ns`, `29375240 B` | `31,645,260.12 ns`, `6411793 B` |
| `MarketDataStore.WarmCacheLoansAsync` | `52.73 ns`, `336 B` | `47.16 ns`, `72 B` | `71.88 ns`, `72 B` |
| `CalculationPipeline.CreateCalculationWarmAsync` | `37.21 ms`, `12.42 MB` | `26.19 ms`, `1000.35 KB` | `27.43 ms`, `975.61 KB` |
| CalculationPipeline.CreateCalculationColdAsync | 194.78 ms, 38.54 MB | 178.28 ms, 31330.8 KB | 62.36 ms, 7289.48 KB |
<!-- benchmark-table:end -->

The main result is still on the cold path. The final handwritten `Sep` parser is materially faster and materially leaner than both the original submission and the best `CsvHelper` branch, while keeping warm-path allocation flat and request latency competitive.

### CSV parser experiment notes

The parser ended up on a handwritten `Sep` path, not on `CsvHelper` and not on source generation.

What held up under measurement:
- buffered file loading stayed separate from parsing
- column indexes are bound once per file
- numeric fields are parsed once, directly from spans
- string creation stays on `SepToString.PoolPerCol(maximumStringLength: 128)`
- quote parsing is disabled
- surrounding spaces are trimmed only when a final string is materialized

What did not hold up:
- stream-first `CsvHelper` spikes
- callback-heavy `CsvHelper` parsing
- a custom decimal converter
- FP-style parsing pipelines
- parser-local memoization/canonicalization
- source-generated schemas and generated registry for the runtime path

The source-generation experiment was useful as a maintainability spike, but it regressed the tuned handwritten path. The project keeps the handwritten schemas because they are still readable and they preserve the best cold-path numbers.

For the stable reviewer-facing snapshot, see:
- [docs/benchmarks/latest-linux-comparison.md](docs/benchmarks/latest-linux-comparison.md)
- [docs/csv-parser-case-study.md](docs/csv-parser-case-study.md)

For reproducible branch-to-branch reruns in CI, use:
- `.github/workflows/benchmark-compare.yml`

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)

## Getting Started

### Run the backend

```bash
cd StressTestApp.Server
dotnet run
```

Backend URLs:
- HTTPS: `https://localhost:7044`
- HTTP: `http://localhost:5101`

### Run the frontend

```bash
cd stresstestapp.client
npm install
npm run dev
```

The frontend development server runs on `https://localhost:59564`.

## API Endpoints

### Calculations
- `POST /api/calculations`
- `GET /api/calculations`
- `GET /api/calculations/{id}`

### Countries
- `GET /api/countries`

### Health
- `GET /health`

## Running Tests

Existing backend test suite:

```bash
cd StressTestApp.Tests
dotnet test
```

Additional verification suite:

```bash
cd StressTestApp.Server
dotnet test Verification\StressTestApp.Verification.csproj
```

## Running Benchmarks

```bash
cd StressTestApp.Server
dotnet run -c Release --project Benchmarks\StressTestApp.Benchmarks.csproj -- --filter *
```

BenchmarkDotNet reports are emitted under `StressTestApp.Server/BenchmarkDotNet.Artifacts/results/`.

## Troubleshooting

### Database reset

```powershell
Remove-Item Data\Db\stresstest.db*
```

The database will be recreated on the next backend start.

### HTTPS certificate issues

```bash
dotnet dev-certs https --trust
```

## Project Notes

This is a portfolio exercise project. Some boundaries are intentionally more production-oriented than the current single-node deployment strictly requires because the goal was to demonstrate engineering judgment around trusted data ingestion, failure isolation, concurrency safety, and measurable performance.
















