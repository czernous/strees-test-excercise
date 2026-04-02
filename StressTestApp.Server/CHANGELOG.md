# Changelog

## Unreleased

### Added
- Added a verification test project for real-data cache hydration, concurrent cold-load behaviour, transient load failure recovery, and deterministic calculator execution.
- Added a BenchmarkDotNet project covering loan-level math, portfolio aggregation, CSV parsing, and cold versus warm cache access.
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
