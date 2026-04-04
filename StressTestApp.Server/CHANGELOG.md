# Changelog

## Unreleased

### Added
- Added a verification test project for real-data cache hydration, concurrent cold-load behaviour, transient load failure recovery, and deterministic calculator execution.
- Added a BenchmarkDotNet project covering loan-level math, portfolio aggregation, CSV parsing, and cold versus warm cache access.
- Added ingestion-isolation benchmarks to separate file-read, parse, materialization, and validation costs before changing the parser implementation.
- Added source-generated logging for calculation, country, parser, and global exception paths.
- Added structured `Error` and `Result` primitives with operation-specific HTTP error mapping.
- Added centralized package management and shared analyzer defaults at the solution root.

### Changed
- Corrected expected-loss calculation to use outstanding amount as the recovery-rate denominator.
- Switched financial values in the domain calculation path to `decimal`.
- Split file loading from CSV parsing so IO failures and data-shape failures are handled independently.
- Validated CSV records before cache admission through `IIntegrityContract` so malformed reference data is not cached.
- Standardised handler flows around `Result` composition, explicit effect boundaries, and typed error responses.
- Improved market-data caching so first-use loads are concurrency-safe and subsequent requests stay in-memory.
- Updated the server project to exclude benchmark and verification sources from the web app build.
- Replaced the live CSV ingestion path with a handwritten `Sep` parser that binds column indexes once and parses directly from spans.
- Tightened `Sep` reader configuration for the known data shape: pooled string creation, quote parsing disabled, and no global trim pass.
- Kept schema parsing handwritten after benchmarking showed the source-generated runtime path regressed the tuned handwritten implementation.

### Fixed
- Fixed partial-read handling in the pooled file loader.
- Fixed duplicate calculation detection so it compares actual inputs again instead of only input counts.
- Fixed cold-start country validation so requests no longer validate against an empty country cache.
- Fixed cancellation handling in CSV parsing so request cancellation is not misreported as corrupt input.
- Fixed `Result<T, E>.ToString()` so formatting does not throw by touching the inactive branch.

### Verification
- `dotnet test ..\StressTestApp.Tests\StressTestApp.Tests.csproj`
- `dotnet test Verification\StressTestApp.Verification.csproj`
- `dotnet run -c Release --project Benchmarks\StressTestApp.Benchmarks.csproj -- --filter *`

### Notes
- The separate ingestion path is intentional. In production, reference data often arrives from multiple file or feed types, so the goal is to keep one reusable ingestion boundary for source access, parsing, integrity validation, and safe cache admission.
- The original loader used `GetRecordsAsync<T>()`, but still materialised the full dataset into a list, so it was never truly streaming end to end.
- `CsvHelper` was benchmarked hard before being replaced. The big costs turned out to be mapped materialization, callback-heavy row handling, and avoidable hot-path overhead rather than the file-loader boundary itself.
- The winning parser shape is still simple: buffered file load, bind indexes once, parse numeric fields once, materialize strings only when needed, validate before cache admission.
- Several follow-up ideas were measured and rejected for the runtime path: FP-style parsing, string canonicalization, source-generated schemas/registry, and extra parser abstraction in the hot `ParseBound` methods.
- Final handwritten `Sep` snapshot on the development machine:
  - `CsvParser.ParseLoansAsync`: `33.84 ms`, `6258.71 KB`
  - `MarketDataStore.ColdLoadLoansAsync`: `29.20 ms`, `6412680 B`
  - `CalculationPipeline.CreateCalculationColdAsync`: `60.15 ms`, `7312.22 KB`
  - `CalculationPipeline.CreateCalculationWarmAsync`: `24.27 ms`, `979.93 KB`





