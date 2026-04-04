# CSV Parser Case Study

This project started with a simple goal: revisit the original take-home submission and push the backend toward a more production-grade shape without hand-waving about performance.

The parser work became a small case study because the first intuition was wrong. The obvious explanation for the cold-path regression was "extra architecture", but benchmarking showed that was not the real cost.

## Starting Point

The original implementation:
- loaded reference data from CSV
- materialized it into typed records
- cached it in memory
- used it to drive cold and warm calculation requests

That was functionally fine, but the portfolio rewrite introduced stronger boundaries:
- file loading separate from parsing
- validation before cache admission
- explicit `Result<T, Error>` flow
- concurrency-safe cache hydration

Those changes improved correctness and clarity, but they also created a good question:
- did the stronger architecture actually make the hot path worse?

## What Was Benchmarked

The benchmarking was split into layers on purpose.

### End-to-end request path

These were the reviewer-facing benchmarks:
- `CalculationPipeline.CreateCalculationColdAsync`
- `CalculationPipeline.CreateCalculationWarmAsync`
- `MarketDataStore.ColdLoadLoansAsync`
- `MarketDataStore.WarmCacheLoansAsync`
- `LoanCalculator.ComputeBatch`
- `PortfolioCalculator.CalculatePortfolioStress`

### Parser-specific path

These isolated parser cost more directly:
- `CsvParser.ParseLoansAsync`
- `CsvParser.ParsePortfoliosAsync`
- `CsvParser.ParseRatingsAsync`

### Isolation benchmarks

These answered narrower questions before changing production code:
- file load only
- direct parser only
- parser plus materialization
- parser plus materialization plus validation
- alternative parser spikes

That split mattered. It stopped the work from treating every regression as a parser problem when some regressions only showed up in the full cold request path.

## What Turned Out To Be Wrong

Several ideas sounded good and lost under measurement.

### 1. "The abstraction is the problem"

It was not.

Isolation benchmarks showed the buffered file-load boundary was effectively free compared to parsing. The real costs were deeper inside the parser/materialization path.

### 2. Stream-first `CsvHelper` would be better

It was not.

Both of these regressed the cold path:
- `GetRecordsAsync<T>()` over a file stream
- `GetRecords<T>()` over a file stream

The code looked closer to a "proper streaming architecture", but the benchmarks were worse than the buffered path.

### 3. A custom decimal converter would help

It did not.

The custom converter added code and weakened malformed-field behavior, but the measured path was slower and heavier than the default converter path.

### 4. FP-style parsing would clean things up without real cost

It did not.

Two separate spikes were tried:
- `Result`-based pipeline composition
- parser-local state/combinator experiments

Both looked elegant in isolation, but both paid too much in:
- wrapper churn
- tuple/combinator overhead
- extra allocation

The cold path got worse.

### 5. Source-generated runtime schemas/registry would be cleaner with no downside

Not here.

Source generation helped as a maintainability experiment, but the generated runtime shape regressed the tuned handwritten parser. It was removed from the live path.

### 6. Generalizing hot `ParseBound` helpers would be "close enough"

Also not here.

Even relatively small helper-based cleanup in the handwritten schemas pushed the cold path the wrong way. That was enough to reject it.

## What Actually Mattered

The winning parser shape was much more direct than the failed spikes.

### 1. Bind column indexes once

Name-based lookup in the row loop was too expensive on the hot loan file.

Binding once per file and then reading by index was a real win.

### 2. Parse numeric fields once

The expensive anti-pattern was:
- `TryParse(...)` in guards
- `Parse(...)` again in the success path

Parsing once into locals mattered more than expression style.

### 3. Keep file loading buffered

The buffered loader was not the problem. Keeping parsing over a buffered in-memory source was better than forcing stream-first semantics that looked cleaner on paper.

### 4. Use `Sep` close to its intended model

`Sep` works best when the code stays near its low-level row/column API:
- bind once
- parse immediately
- copy only final values

Trying to build richer abstractions on top of `Row`/`Col` mostly fought the library.

### 5. Let `Sep` handle string creation efficiently

The best string path used:
- `SepToString.PoolPerCol(maximumStringLength: 128)`

That beat the attempts to outsmart `ToString()` ourselves.

### 6. Treat quotes and global trimming as format decisions, not parser defaults

The best result on the known dataset came from:
- quote parsing disabled
- no global trim pass
- trim only when a final string is actually materialized

That kept the parser strict and cheap, while still letting the final domain strings be normalized when needed.

## Final Runtime Shape

The final live parser kept:
- handwritten `Sep` schemas
- handwritten schema registry
- buffered file loading
- one-time column binding
- one-pass numeric parsing
- validation before cache admission

That was the best balance of:
- readability
- explicitness
- measured performance

## Final Outcome

Against the original submission on the GitHub Linux runner:
- `CreateCalculationColdAsync`: `61.24 ms` vs `192.78 ms`
- `ColdLoadLoansAsync`: `29,692,610.77 ns` vs `108,852,928.53 ns`
- `CalculatePortfolioStress` allocation: `4.8 KB` vs `11.45 MB`

Against the best tuned `CsvHelper` branch, the handwritten `Sep` path also won clearly on the cold parser/load pipeline.

## Main Lessons

1. Measure the real path before blaming architecture.
2. Split benchmarks by layer so the wrong abstraction does not get blamed for a deeper cost.
3. In hot parsing code, "cleaner" abstractions often lose unless they preserve the exact low-level work shape.
4. Source generation is useful for boilerplate, but it does not automatically preserve a hand-tuned runtime path.
5. For this project, the handwritten parser was the right end state because it stayed readable enough while preserving the best numbers.
