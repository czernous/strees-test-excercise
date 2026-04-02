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

That separation is deliberate. In a larger system, file acquisition and parsing would likely live behind different services or queues, and the cache might be distributed rather than in-process. The current boundaries are designed so those responsibilities can evolve without rewriting the calculation pipeline.

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
- a committed benchmark reference snapshot in `docs/benchmarks/latest-linux-comparison.md`

### Current benchmark highlights

Measured on the local development machine with BenchmarkDotNet `ShortRun`:
- `LoanCalculator.ComputeBatch` over 100k loans: about `12.24 ms`, zero managed allocation
- `PortfolioCalculator.Calculate` on the full dataset: about `20.98 ms`, about `4.77 KB` allocated
- cold loan CSV parse: about `147.70 ms`
- cold loan cache load: about `150.68 ms`
- warm cached loan read: about `49.05 ns`

These numbers are not presented as universal truth; they are included to demonstrate actual measurement of the intended hot paths.

For the stable reviewer-facing snapshot, see:
- `docs/benchmarks/latest-linux-comparison.md`

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
