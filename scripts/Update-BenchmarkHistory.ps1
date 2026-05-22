<#
.SYNOPSIS
    Parses a BenchmarkDotNet GitHub-flavoured Markdown report and appends one summary row
    per benchmark to Documentation/BenchmarkHistory.md.

.DESCRIPTION
    Reads the newest '*-report-github.md' file found under the BenchmarkDotNet.Artifacts
    directory, extracts Mean and Maximum duration for two key benchmark groups:

        * Instrumentation  – any benchmark whose Method column contains "Preparemodules"
                             or "Instrument" (case-insensitive)
        * CreateReport     – any benchmark whose Method column contains "GetCoverage"
                             or "Report" (case-insensitive)

    The optional -BenchmarkFilter parameter lets you restrict which benchmark class rows
    are included (e.g. "InstrumentationOptionsBenchmarks").

    Appended table columns
    ──────────────────────
    | Date | Version | Runtime | BenchmarkClass | Method | Options |
    | InstrAvg (ms) | InstrMax (ms) | ReportAvg (ms) | ReportMax (ms) |
    | Allocated (MB) |

.PARAMETER ArtifactsRoot
    Root folder that contains the BenchmarkDotNet.Artifacts directory.
    Defaults to the current working directory.

.PARAMETER HistoryFile
    Path to the Markdown file that stores accumulated results.
    Defaults to 'Documentation/BenchmarkHistory.md' of the GitHub repository root.

.PARAMETER CoverletVersion
    Coverlet version string to record, e.g. "6.0.5".
    If omitted the script tries to read it from coverlet.core.csproj via the Version property.

.PARAMETER BenchmarkFilter
    Optional substring to restrict which benchmark Method rows are parsed
    (case-insensitive match against the full Method column value).

.EXAMPLE
    # Run after building and executing benchmarks
    pwsh scripts/Update-BenchmarkHistory.ps1 `
        -ArtifactsRoot "artifacts/bin/coverlet.core.benchmark.tests/release_net10.0" `
        -CoverletVersion "6.0.5"

.EXAMPLE
    # Only record InstrumentationOptionsBenchmarks rows
    pwsh scripts/Update-BenchmarkHistory.ps1 `
        -BenchmarkFilter "InstrumentationOptions"
#>
[CmdletBinding()]
param(
    [string] $ArtifactsRoot = (Get-Location).Path,
    [string] $HistoryFile   = (Join-Path (Split-Path $PSScriptRoot -Parent) 'Documentation/BenchmarkHistory.md'),
    [string] $CoverletVersion = '',
    [string] $BenchmarkFilter = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# 1. Locate the most recent BenchmarkDotNet GitHub markdown report
# ---------------------------------------------------------------------------
$artifactsDir = Join-Path $ArtifactsRoot 'BenchmarkDotNet.Artifacts' 'results'
if (-not (Test-Path $artifactsDir)) {
    Write-Error "BenchmarkDotNet results folder not found: $artifactsDir`nRun the benchmarks first."
}

$reportFile = Get-ChildItem -Path $artifactsDir -Filter '*-report-github.md' |
              Sort-Object LastWriteTime -Descending |
              Select-Object -First 1

if (-not $reportFile) {
    Write-Error "No '*-report-github.md' file found in $artifactsDir"
}

Write-Host "Parsing report: $($reportFile.FullName)"

# ---------------------------------------------------------------------------
# 2. Resolve coverlet version
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($CoverletVersion)) {
    # Walk upward from ArtifactsRoot to find coverlet.core.csproj
    $searchDir = $ArtifactsRoot
    $csproj    = $null
    while ($searchDir -and -not $csproj) {
        $csproj     = Get-ChildItem -Path $searchDir -Filter 'coverlet.core.csproj' -Recurse -ErrorAction SilentlyContinue |
                      Select-Object -First 1
        $searchDir  = Split-Path $searchDir -Parent
    }

    if ($csproj) {
        $xml = [xml](Get-Content $csproj.FullName)
        $CoverletVersion = $xml.Project.PropertyGroup.Version |
                           Where-Object { $_ } |
                           Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace($CoverletVersion)) {
        $CoverletVersion = 'unknown'
        Write-Warning "Could not determine coverlet version – recording 'unknown'."
    }
}

# ---------------------------------------------------------------------------
# 3. Detect runtime from the report header block
# ---------------------------------------------------------------------------
$reportText = Get-Content $reportFile.FullName -Raw
$runtime    = 'unknown'
if ($reportText -match '(?m)^\s*\[Host\]\s*:\s*(.+)$') {
    $runtime = $Matches[1].Trim()
}

# ---------------------------------------------------------------------------
# 4. Parse table rows
# ---------------------------------------------------------------------------
# BenchmarkDotNet GitHub tables look like:
# | Type | Method | ... | Mean | Error | StdDev | ... | Allocated |
# |------|--------|-----|------|-------|--------|-----|-----------|
# | Foo  | Bar    | ... | 1.23 | ...   | ...    | ... | 456 KB    |

# Collect header and data lines (skip separator lines)
$tableLines = $reportText -split '\r?\n' |
              Where-Object { $_ -match '^\|' -and $_ -notmatch '^\|[-| :]+\|' }

if ($tableLines.Count -lt 2) {
    Write-Error "No table rows found in the report file."
}

# Parse header to find column indices
$headerLine  = $tableLines[0]
$headerCells = $headerLine -split '\|' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

function Get-ColIndex([string[]] $headers, [string] $name) {
    $idx = $headers | Select-String -Pattern "^$name$" -CaseSensitive:$false |
           ForEach-Object { $headers.IndexOf($_.Line) }
    return $idx
}

$colType      = [Array]::FindIndex($headerCells, [Predicate[string]]{ param($h) $h -match '^Type$' })
$colMethod    = [Array]::FindIndex($headerCells, [Predicate[string]]{ param($h) $h -match '^Method$' })
$colMean      = [Array]::FindIndex($headerCells, [Predicate[string]]{ param($h) $h -match '^Mean$' })
$colError     = [Array]::FindIndex($headerCells, [Predicate[string]]{ param($h) $h -match '^Error$' })
$colAlloc     = [Array]::FindIndex($headerCells, [Predicate[string]]{ param($h) $h -match '^Allocated$' })

# Params columns (e.g. SingleHit, SkipAutoProps, ReportFormat …)
$paramCols = @{}
for ($i = 0; $i -lt $headerCells.Count; $i++) {
    if ($i -notin @($colType, $colMethod, $colMean, $colError, $colAlloc) -and
        $headerCells[$i] -notmatch 'StdDev|Gen[012]|Median|Ratio|Baseline') {
        $paramCols[$headerCells[$i]] = $i
    }
}

# ---------------------------------------------------------------------------
# Helper: normalise a BDN duration string to milliseconds (double)
# ---------------------------------------------------------------------------
function ConvertToMs([string] $raw) {
    $raw = $raw.Trim() -replace ',', ''          # remove thousands separators

    if ($raw -match '([\d.]+)\s*ns$')  { return [double]$Matches[1] / 1e6 }
    if ($raw -match '([\d.]+)\s*μs$')  { return [double]$Matches[1] / 1e3 }
    if ($raw -match '([\d.]+)\s*us$')  { return [double]$Matches[1] / 1e3 }
    if ($raw -match '([\d.]+)\s*ms$')  { return [double]$Matches[1] }
    if ($raw -match '([\d.]+)\s*s$')   { return [double]$Matches[1] * 1e3 }
    return [double]::NaN
}

# ---------------------------------------------------------------------------
# Helper: normalise a BDN allocated string to MB (double)
# ---------------------------------------------------------------------------
function ConvertToMB([string] $raw) {
    $raw = $raw.Trim() -replace ',', ''

    if ($raw -match '([\d.]+)\s*B$')   { return [double]$Matches[1] / 1MB }
    if ($raw -match '([\d.]+)\s*KB$')  { return [double]$Matches[1] / 1KB }
    if ($raw -match '([\d.]+)\s*MB$')  { return [double]$Matches[1] }
    if ($raw -match '([\d.]+)\s*GB$')  { return [double]$Matches[1] * 1024 }
    return [double]::NaN
}

# ---------------------------------------------------------------------------
# 5. Build structured result rows
# ---------------------------------------------------------------------------
$dataLines = $tableLines | Select-Object -Skip 1   # skip header

$rows = foreach ($line in $dataLines) {
    $cells = $line -split '\|' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

    if ($cells.Count -lt ($colMean + 1)) { continue }

    $typeName  = if ($colType   -ge 0) { $cells[$colType]   } else { '' }
    $method    = if ($colMethod -ge 0) { $cells[$colMethod]  } else { '' }
    $meanRaw   = if ($colMean   -ge 0) { $cells[$colMean]    } else { '0' }
    $errorRaw  = if ($colError  -ge 0) { $cells[$colError]   } else { '0' }
    $allocRaw  = if ($colAlloc  -ge 0) { $cells[$colAlloc]   } else { '-' }

    # Apply optional filter
    if ($BenchmarkFilter -and ($typeName + $method) -notmatch $BenchmarkFilter) { continue }

    $meanMs  = ConvertToMs  $meanRaw
    $errorMs = ConvertToMs  $errorRaw
    $maxMs   = if ([double]::IsNaN($meanMs) -or [double]::IsNaN($errorMs)) { [double]::NaN } else { $meanMs + $errorMs }
    $allocMB = ConvertToMB  $allocRaw

    # Collect parameter values
    $optionParts = foreach ($kvp in $paramCols.GetEnumerator()) {
        $idx = $kvp.Value
        if ($idx -lt $cells.Count) { "$($kvp.Key)=$($cells[$idx])" }
    }
    $options = $optionParts -join ', '

    [PSCustomObject]@{
        Date          = (Get-Date -Format 'yyyy-MM-dd')
        Version       = $CoverletVersion
        Runtime       = $runtime
        BenchmarkClass = $typeName
        Method        = $method
        Options       = $options
        MeanMs        = [Math]::Round($meanMs,  3)
        MaxMs         = [Math]::Round($maxMs,   3)
        AllocMB       = [Math]::Round($allocMB, 4)
    }
}

if (-not $rows) {
    Write-Warning "No matching benchmark rows found.  Nothing to append."
    exit 0
}

# ---------------------------------------------------------------------------
# 6. Aggregate instrumentation vs report rows
# ---------------------------------------------------------------------------
$isInstr  = { param($r) $r.Method -imatch 'PrepareModules|Instrument' }
$isReport = { param($r) $r.Method -imatch 'GetCoverage|Report' }

# For reporting we group by (BenchmarkClass, Options)
$grouped = $rows | Group-Object { "$($_.BenchmarkClass)|$($_.Options)" }

# ---------------------------------------------------------------------------
# 7. Ensure HistoryFile and its markdown table exist
# ---------------------------------------------------------------------------
$tableHeader = @'
| Date | Version | Runtime | BenchmarkClass | Method | Options | Mean (ms) | Max (ms) | Allocated (MB) |
|------|---------|---------|----------------|--------|---------|----------:|---------:|---------------:|
'@

if (-not (Test-Path $HistoryFile)) {
    Write-Host "Creating new history file: $HistoryFile"
    @"
# Coverlet Benchmark History

This file is maintained automatically by `scripts/Update-BenchmarkHistory.ps1`.
Do not edit the table rows by hand – re-run the script after each benchmark run.

$tableHeader
"@ | Set-Content $HistoryFile -Encoding UTF8
}
else {
    $existing = Get-Content $HistoryFile -Raw
    if ($existing -notmatch [regex]::Escape('| Date | Version |')) {
        # Append the table header after the last line
        Add-Content $HistoryFile -Value "`n$tableHeader" -Encoding UTF8
    }
}

# ---------------------------------------------------------------------------
# 8. Append one row per benchmark entry
# ---------------------------------------------------------------------------
$appended = 0
foreach ($row in $rows) {
    $meanFmt  = if ([double]::IsNaN($row.MeanMs))  { '-' } else { $row.MeanMs.ToString('F3') }
    $maxFmt   = if ([double]::IsNaN($row.MaxMs))   { '-' } else { $row.MaxMs.ToString('F3')  }
    $allocFmt = if ([double]::IsNaN($row.AllocMB)) { '-' } else { $row.AllocMB.ToString('F4') }

    $mdRow = "| $($row.Date) | $($row.Version) | $($row.Runtime) | $($row.BenchmarkClass) | $($row.Method) | $($row.Options) | $meanFmt | $maxFmt | $allocFmt |"
    Add-Content $HistoryFile -Value $mdRow -Encoding UTF8
    $appended++
}

Write-Host "Appended $appended row(s) to $HistoryFile"
