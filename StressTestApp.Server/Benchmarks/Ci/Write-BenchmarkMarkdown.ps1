param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDir,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$Title = "Benchmark Summary",

    [string]$CompareToResultsDir,

    [string]$CurrentLabel = "Current",

    [string]$BaselineLabel = "Baseline"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-TimeToNanoseconds {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parts = ($Value -replace ",", "").Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Count -lt 2) {
        return $null
    }

    $number = [double]::Parse($parts[0], [System.Globalization.CultureInfo]::InvariantCulture)
    $unit = $parts[1]

    switch ($unit) {
        "ns" { return $number }
        "us" { return $number * 1000 }
        "ms" { return $number * 1000000 }
        "s"  { return $number * 1000000000 }
        default { return $null }
    }
}

function Convert-AllocatedToBytes {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "-") {
        return $null
    }

    $parts = ($Value -replace ",", "").Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Count -lt 2) {
        return $null
    }

    $number = [double]::Parse($parts[0], [System.Globalization.CultureInfo]::InvariantCulture)
    $unit = $parts[1]

    switch ($unit) {
        "B"  { return $number }
        "KB" { return $number * 1KB }
        "MB" { return $number * 1MB }
        "GB" { return $number * 1GB }
        default { return $null }
    }
}

function Read-BenchmarkRows {
    param([string]$Directory)

    $rows = [System.Collections.Generic.List[object]]::new()
    Get-ChildItem -Path $Directory -Filter "*-report.csv" | Sort-Object Name | ForEach-Object {
        $csvRows = Import-Csv $_.FullName
        $benchmark = $_.BaseName -replace '-report$', ''

        foreach ($row in $csvRows) {
            $rows.Add([pscustomobject]@{
                Benchmark      = $benchmark
                Method         = $row.Method
                Mean           = $row.Mean
                MeanNs         = Convert-TimeToNanoseconds $row.Mean
                Allocated      = $row.Allocated
                AllocatedBytes = Convert-AllocatedToBytes $row.Allocated
            })
        }
    }

    return $rows.ToArray()
}

function Format-PercentDelta {
    param(
        [double]$Current,
        [double]$Baseline
    )

    if ($Baseline -eq 0) {
        return "n/a"
    }

    $delta = (($Current - $Baseline) / $Baseline) * 100
    return "{0:+0.##;-0.##;0}%" -f $delta
}

function Write-SummaryMarkdown {
    param(
        [object[]]$Rows,
        [string]$Path,
        [string]$Heading
    )

    $lines = @(
        "# $Heading",
        "",
        "| Benchmark | Mean | Allocated |",
        "| --- | ---: | ---: |"
    )

    foreach ($row in $Rows | Sort-Object Benchmark, Method) {
        $lines += "| $($row.Benchmark) / $($row.Method) | $($row.Mean) | $($row.Allocated) |"
    }

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Set-Content -Path $Path -Value $lines
}

function Write-ComparisonMarkdown {
    param(
        [object[]]$CurrentRows,
        [object[]]$BaselineRows,
        [string]$Path,
        [string]$Heading,
        [string]$LeftLabel,
        [string]$RightLabel
    )

    $baselineByMethod = @{}
    foreach ($row in $BaselineRows) {
        $baselineByMethod["$($row.Benchmark)|$($row.Method)"] = $row
    }

    $lines = @(
        "# $Heading",
        "",
        "| Benchmark | $LeftLabel Mean | $RightLabel Mean | Mean Delta | $LeftLabel Allocated | $RightLabel Allocated | Alloc Delta |",
        "| --- | ---: | ---: | ---: | ---: | ---: | ---: |"
    )

    foreach ($row in $CurrentRows | Sort-Object Benchmark, Method) {
        $comparisonKey = "$($row.Benchmark)|$($row.Method)"
        $baseline = $baselineByMethod[$comparisonKey]
        if ($null -eq $baseline) {
            $lines += "| $($row.Benchmark) / $($row.Method) | $($row.Mean) | n/a | n/a | $($row.Allocated) | n/a | n/a |"
            continue
        }

        $meanDelta = if ($null -eq $row.MeanNs -or $null -eq $baseline.MeanNs) {
            "n/a"
        }
        else {
            Format-PercentDelta -Current $row.MeanNs -Baseline $baseline.MeanNs
        }

        $allocDelta = if ($null -eq $row.AllocatedBytes -or $null -eq $baseline.AllocatedBytes) {
            "n/a"
        }
        else {
            Format-PercentDelta -Current $row.AllocatedBytes -Baseline $baseline.AllocatedBytes
        }

        $lines += "| $($row.Benchmark) / $($row.Method) | $($row.Mean) | $($baseline.Mean) | $meanDelta | $($row.Allocated) | $($baseline.Allocated) | $allocDelta |"
    }

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Set-Content -Path $Path -Value $lines
}

$currentRows = Read-BenchmarkRows -Directory $ResultsDir

if ([string]::IsNullOrWhiteSpace($CompareToResultsDir)) {
    Write-SummaryMarkdown -Rows $currentRows -Path $OutputPath -Heading $Title
    exit 0
}

$baselineRows = Read-BenchmarkRows -Directory $CompareToResultsDir
Write-ComparisonMarkdown `
    -CurrentRows $currentRows `
    -BaselineRows $baselineRows `
    -Path $OutputPath `
    -Heading $Title `
    -LeftLabel $CurrentLabel `
    -RightLabel $BaselineLabel
