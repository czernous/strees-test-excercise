param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactRoot,

    [string]$ReadmePath = "..\README.md",

    [string]$SnapshotPath = "..\docs\benchmarks\latest-linux-comparison.md",

    [string]$SnapshotDate = (Get-Date).ToString("yyyy-MM-dd")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$Path)

    $resolved = Join-Path $PSScriptRoot $Path
    return [System.IO.Path]::GetFullPath($resolved)
}

function Parse-MarkdownTable {
    param([string[]]$Lines)

    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $Lines) {
        if (-not $line.StartsWith("|")) {
            continue
        }

        if ($line -match '^\|\s*-') {
            continue
        }

        $cells = $line.Trim('|').Split('|') | ForEach-Object { $_.Trim() }
        $rows.Add($cells)
    }

    return $rows
}

function Get-SectionLines {
    param(
        [string[]]$Lines,
        [string]$Header
    )

    $start = $Lines.IndexOf($Header)
    if ($start -lt 0) {
        throw "Header '$Header' not found."
    }

    $section = [System.Collections.Generic.List[string]]::new()
    for ($i = $start + 1; $i -lt $Lines.Length; $i++) {
        $line = $Lines[$i]
        if ($line.StartsWith("## ")) {
            break
        }
        $section.Add($line)
    }

    return $section.ToArray()
}

function Get-ComparisonMap {
    param([string]$ComparisonPath)

    $lines = Get-Content $ComparisonPath
    $section = Get-SectionLines -Lines $lines -Header "## Matching Benchmarks"
    $table = Parse-MarkdownTable -Lines $section

    $map = @{}
    foreach ($row in $table | Select-Object -Skip 1) {
        $map[$row[0]] = [pscustomobject]@{
            CandidateMean = $row[1]
            BaselineMean = $row[2]
            CandidateAllocated = $row[4]
            BaselineAllocated = $row[5]
        }
    }

    return $map
}

function Get-ReadmeTable {
    param(
        [hashtable]$ComparisonMap
    )

    $rows = @(
        @("`LoanCalculator.ComputeBatch`", "StressTestApp.Server.Benchmarks.LoanCalculatorBenchmarks / ComputeBatch"),
        @("`PortfolioCalculator.CalculatePortfolioStress`", "StressTestApp.Server.Benchmarks.PortfolioCalculatorBenchmarks / CalculatePortfolioStress"),
        @("`CsvParser.ParseLoansAsync`", "StressTestApp.Server.Benchmarks.CsvParserBenchmarks / ParseLoansAsync"),
        @("`MarketDataStore.ColdLoadLoansAsync`", "StressTestApp.Server.Benchmarks.MarketDataStoreBenchmarks / ColdLoadLoansAsync"),
        @("`MarketDataStore.WarmCacheLoansAsync`", "StressTestApp.Server.Benchmarks.MarketDataStoreBenchmarks / WarmCacheLoansAsync"),
        @("`CalculationPipeline.CreateCalculationWarmAsync`", "StressTestApp.Server.Benchmarks.CalculationPipelineBenchmarks / CreateCalculationWarmAsync"),
        @("`CalculationPipeline.CreateCalculationColdAsync`", "StressTestApp.Server.Benchmarks.CalculationPipelineBenchmarks / CreateCalculationColdAsync")
    )

    $csvHelperStatic = @{
        "`LoanCalculator.ComputeBatch`" = "12.27 ms`, `0 B"
        "`PortfolioCalculator.CalculatePortfolioStress`" = "21.25 ms`, `4.77 KB"
        "`CsvParser.ParseLoansAsync`" = "126.302 ms`, `28683.45 KB"
        "`MarketDataStore.ColdLoadLoansAsync`" = "124,724,383.33 ns`, `29375240 B"
        "`MarketDataStore.WarmCacheLoansAsync`" = "47.16 ns`, `72 B"
        "`CalculationPipeline.CreateCalculationWarmAsync`" = "26.19 ms`, `1000.35 KB"
        "`CalculationPipeline.CreateCalculationColdAsync`" = "178.28 ms`, `31330.8 KB"
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("| Benchmark | Original | Best CsvHelper | Final Handwritten Sep |")
    $lines.Add("|---|---:|---:|---:|")

    foreach ($row in $rows) {
        $label = $row[0]
        $comparisonKey = $row[1]
        $comparison = $ComparisonMap[$comparisonKey]

        if ($null -eq $comparison) {
            throw "Comparison row '$comparisonKey' not found."
        }

        $original = if ($comparison.BaselineAllocated -eq "0 B") {
            "$($comparison.BaselineMean)`, `0 B"
        }
        else {
            "$($comparison.BaselineMean)`, `$($comparison.BaselineAllocated)"
        }

        $candidate = if ($comparison.CandidateAllocated -eq "0 B") {
            "$($comparison.CandidateMean)`, `0 B"
        }
        else {
            "$($comparison.CandidateMean)`, `$($comparison.CandidateAllocated)"
        }

        if ($label -eq "`CsvParser.ParseLoansAsync`") {
            $original = "n/a"
        }

        if ($label -like "`CalculationPipeline.*" -and $label -ne "`CalculationPipeline.CreateCalculationColdAsync`" -and $label -ne "`CalculationPipeline.CreateCalculationWarmAsync`") {
            $original = "n/a"
        }

        if ($label -eq "`CalculationPipeline.CreateCalculationWarmAsync`" -or $label -eq "`CalculationPipeline.CreateCalculationColdAsync`") {
            $original = "$($comparison.BaselineMean)`, `$($comparison.BaselineAllocated)"
        }

        $lines.Add("| $label | ``$original`` | ``$($csvHelperStatic[$label])`` | ``$candidate`` |")
    }

    return $lines
}

function Update-Readme {
    param(
        [string]$Path,
        [string]$Date,
        [string[]]$TableLines
    )

    $content = Get-Content $Path -Raw

    $content = [regex]::Replace(
        $content,
        'Latest GitHub Actions Linux snapshot as of `\d{4}-\d{2}-\d{2}`',
        "Latest GitHub Actions Linux snapshot as of ``$Date``")

    $pattern = '(?s)(<!-- benchmark-table:start -->).*?(<!-- benchmark-table:end -->)'
    $replacement = '$1' + [Environment]::NewLine + ($TableLines -join [Environment]::NewLine) + [Environment]::NewLine + '$2'

    if (-not [regex]::IsMatch($content, $pattern)) {
        throw "README benchmark markers not found."
    }

    $content = [regex]::Replace($content, $pattern, $replacement)
    Set-Content -Path $Path -Value $content
}

$artifactRootPath = Resolve-RepoPath $ArtifactRoot
$readmePath = Resolve-RepoPath $ReadmePath
$snapshotPath = Resolve-RepoPath $SnapshotPath

$comparisonPath = Join-Path $artifactRootPath "benchmark-comparison\comparison.md"
if (-not (Test-Path $comparisonPath)) {
    throw "Comparison artifact not found at $comparisonPath"
}

$comparisonMap = Get-ComparisonMap -ComparisonPath $comparisonPath
$tableLines = Get-ReadmeTable -ComparisonMap $comparisonMap

Copy-Item -LiteralPath $comparisonPath -Destination $snapshotPath -Force

$snapshotContent = Get-Content $snapshotPath -Raw
$snapshotContent = $snapshotContent -replace 'workflow run date: `\d{4}-\d{2}-\d{2}`', "workflow run date: ``$SnapshotDate``"
$snapshotContent = $snapshotContent -replace 'current handwritten `Sep` implementation', 'current handwritten `Sep` implementation'
Set-Content -Path $snapshotPath -Value $snapshotContent

Update-Readme -Path $readmePath -Date $SnapshotDate -TableLines $tableLines
