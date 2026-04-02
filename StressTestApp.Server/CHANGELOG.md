# Changelog

## Unreleased

### Added
- Added a verification test project for real-data cache hydration, concurrent cold-load behaviour, transient load failure recovery, and deterministic calculator execution.
- Added a BenchmarkDotNet project covering loan-level math, portfolio aggregation, CSV parsing, and cold versus warm cache access.
- Added ingestion-isolation benchmarks to separate file-read, parse, materialization, and validation costs before changing the parser implementation.
- Added source-generated logging for calculation, country, parser, and global exception paths.
- Added structured `Error` and `Result` primitives with operation-specific HTTP error mapping.

### Changed
- Corrected expected-loss calculation to use outstanding amount as the recovery-rate denominator.
- Switched financial values in the domain calculation path to `decimal`.
- Split file loading from CSV parsing so IO failures and data-shape failures are handled independently.
- Validated CSV records before cache admission through `IIntegrityContract` so malformed reference data is not cached.
- Standardised handler flows around `Result` composition, explicit effect boundaries, and typed error responses.
- Improved market-data caching so first-use loads are concurrency-safe and subsequent requests stay in-memory.
- Updated the server project to exclude benchmark and verification sources from the web app build.
- Documented the CSV parser experiments and kept the current implementation on the generic buffered CsvHelper path while the cold-ingestion regression is isolated.
- Removed expensive parser callbacks and corrected hot-file list sizing after isolation benchmarks showed both were inflating cold-path allocation.

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
- The original loader used `GetRecordsAsync<T>()`, but still materialised the full dataset into a list, so it was not truly end-to-end streaming.
- The current buffered parser is not the same implementation as the original loader. It keeps the stronger ingestion boundary, including explicit file-read buffering, integrity validation before cache admission, and structured parser/load error handling.
- A true stream-based `CsvHelper.GetRecordsAsync<T>()` spike restored incremental parsing semantics, but materially regressed cold-load and cold-request latency.
- A hybrid stream-based `CsvHelper.GetRecords<T>()` spike recovered parser-level speed, but still underperformed the buffered implementation in the end-to-end cold calculation pipeline.
- Isolation benchmarks showed the loader abstraction itself was not the issue: buffered loading was cheap, and the dominant cost sat inside CsvHelper on the hot loan file.
- The parser now relies on CsvHelper's default blank-line handling instead of expensive per-row callbacks, and the production loan map no longer uses a custom decimal converter.
- The current investigation is focused on explaining that regression within the existing CsvHelper-based ingestion path before considering different parser implementations.
- Naming should continue to follow behaviour. The current implementation uses buffered file loading, so `FileLoader.LoadAsync` remains accurate; if ingestion moves to stream-based parsing, the loader and parser names should move with it.





