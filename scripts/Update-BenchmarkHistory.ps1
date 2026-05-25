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
    [string] $BenchmarkFilter = '',
    [switch] $Force   # Allow writing a version older than the latest already in the history file
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

# Known BDN infrastructure / diagnoser columns that must never appear in the Options column.
# Only [Params]-declared columns (user benchmark parameters) are kept.
$bdnInfraColumns = @(
    'Job', 'Toolchain', 'Runtime', 'WarmupCount', 'IterationCount', 'InvocationCount',
    'UnrollFactor', 'StdDev', 'Error', 'Median', 'Ratio', 'RatioSD', 'Baseline',
    'Gen0', 'Gen1', 'Gen2', 'Lock Contentions', 'Completed Work Items', 'Exceptions'
)

# Params columns (e.g. SingleHit, SkipAutoProps, ReportFormat …)
# Exclude all BDN infrastructure columns; keep only user-declared [Params] values.
$paramCols = @{}
for ($i = 0; $i -lt $headerCells.Count; $i++) {
    $h = $headerCells[$i]
    if ($i -notin @($colType, $colMethod, $colMean, $colError, $colAlloc) -and
        $bdnInfraColumns -notcontains $h) {
        $paramCols[$h] = $i
    }
}

# ---------------------------------------------------------------------------
# Helper: parse a BDN number string (dot decimal, no thousands separator)
# ---------------------------------------------------------------------------
function ParseNumber([string] $raw) {
    return [double]::Parse($raw.Trim(), [cultureinfo]::InvariantCulture)
}

# ---------------------------------------------------------------------------
# Helper: convert a BDN duration string to milliseconds (double)
# ---------------------------------------------------------------------------
function ConvertToMs([string] $raw) {
    $raw = $raw.Trim()

    if ($raw -match '([\d.]+)\s*ns$')  { return (ParseNumber $Matches[1]) / 1e6 }
    if ($raw -match '([\d.]+)\s*μs$')  { return (ParseNumber $Matches[1]) / 1e3 }
    if ($raw -match '([\d.]+)\s*us$')  { return (ParseNumber $Matches[1]) / 1e3 }
    if ($raw -match '([\d.]+)\s*ms$')  { return (ParseNumber $Matches[1]) }
    if ($raw -match '([\d.]+)\s*s$')   { return (ParseNumber $Matches[1]) * 1e3 }
    return [double]::NaN
}

# ---------------------------------------------------------------------------
# Helper: convert a BDN allocated string to MB (double)
# ---------------------------------------------------------------------------
function ConvertToMB([string] $raw) {
    $raw = $raw.Trim()

    if ($raw -match '([\d.]+)\s*B$')   { return (ParseNumber $Matches[1]) / 1MB }
    if ($raw -match '([\d.]+)\s*KB$')  { return (ParseNumber $Matches[1]) / 1KB }
    if ($raw -match '([\d.]+)\s*MB$')  { return (ParseNumber $Matches[1]) }
    if ($raw -match '([\d.]+)\s*GB$')  { return (ParseNumber $Matches[1]) * 1024 }
    return [double]::NaN
}

# ---------------------------------------------------------------------------
# 5. Build structured result rows
# ---------------------------------------------------------------------------
$dataLines = $tableLines | Select-Object -Skip 1   # skip header

$rows = foreach ($line in $dataLines) {
    $cells = $line -split '\|' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

    if ($cells.Count -lt ($colMean + 1)) { continue }

    # Strip BDN GitHub-markdown bold (**text**) and common HTML entities from every cell
    $cells = $cells | ForEach-Object {
        $_ -replace '\*\*([^*]*)\*\*', '$1' `
           -replace "&#39;", "'" `
           -replace '&amp;', '&' `
           -replace '&lt;',  '<' `
           -replace '&gt;',  '>'
    }

    $typeName  = if ($colType   -ge 0) { $cells[$colType]   } else { '' }
    $method    = if ($colMethod -ge 0) { $cells[$colMethod]  } else { '' }
    $meanRaw   = if ($colMean   -ge 0) { $cells[$colMean]    } else { '0' }
    $errorRaw  = if ($colError  -ge 0) { $cells[$colError]   } else { '0' }
    $allocRaw  = if ($colAlloc  -ge 0) { $cells[$colAlloc]   } else { '-' }

    # Skip rows that BDN could not measure (out-of-process jobs, exceptions during benchmark)
    if ($meanRaw -match '^[-?]$') { continue }

    # Apply optional filter
    if ($BenchmarkFilter -and ($typeName + $method) -notmatch $BenchmarkFilter) { continue }

    $meanMs  = ConvertToMs  $meanRaw
    $errorMs = ConvertToMs  $errorRaw
    $maxMs   = if ([double]::IsNaN($meanMs) -or [double]::IsNaN($errorMs)) { [double]::NaN } else { $meanMs + $errorMs }
    $allocMB = ConvertToMB  $allocRaw

    # Collect parameter values
    $optionParts = foreach ($kvp in $paramCols.GetEnumerator() | Sort-Object Key) {
        $idx = $kvp.Value
        if ($idx -lt $cells.Count -and $cells[$idx] -notmatch '^[?-]$') {
            "$($kvp.Key)=$($cells[$idx])"
        }
    }
    $options = ($optionParts | Where-Object { $_ }) -join ', '

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
# Helper: compare two coverlet version strings.
# Format: major.minor.patch  or  major.minor.patch-prelabel+buildN
# Release > pre-release ("6.0.0" > "6.0.0-p1").
# Pre-release labels compared lexically by suffix after the last '-' then by
# any trailing integer (so "p1" > "p0", "rc2" > "rc1").
# Returns  1 if $a > $b,  -1 if $a < $b,  0 if equal.
# ---------------------------------------------------------------------------
function Compare-CoverletVersion([string] $a, [string] $b) {
    # Split off pre-release tag
    $splitVersion = {
        param([string] $v)
        # Ignore SemVer build metadata (e.g. "+build.123") when ordering versions.
        $v = $v -replace '\+.*$', ''
        if ($v -match '^(\d+\.\d+\.\d+)(?:-(.+))?$') {
            [pscustomobject]@{ Core = [version]$Matches[1]; Pre = $Matches[2] }
        } else {
            [pscustomobject]@{ Core = [version]'0.0.0'; Pre = $v }
        }
    }

    $va = & $splitVersion $a
    $vb = & $splitVersion $b

    $cmp = $va.Core.CompareTo($vb.Core)
    if ($cmp -ne 0) { return $cmp }

    # Same core: release beats pre-release
    if (-not $va.Pre -and $vb.Pre)  { return  1 }   # a is release, b is pre
    if ($va.Pre  -and -not $vb.Pre) { return -1 }   # a is pre, b is release
    if (-not $va.Pre -and -not $vb.Pre) { return 0 }

    # Both pre-release: compare label text, then trailing integer
    $labelCmp = [string]::Compare($va.Pre, $vb.Pre, [System.StringComparison]::OrdinalIgnoreCase)
    if ($labelCmp -ne 0) {
        # Try to extract and compare trailing integers for labels like "p0", "p1", "rc2"
        $numA = if ($va.Pre -match '(\d+)$') { [int]$Matches[1] } else { $null }
        $numB = if ($vb.Pre -match '(\d+)$') { [int]$Matches[1] } else { $null }
        $prefA = if ($va.Pre -match '^(\D+)') { $Matches[1] } else { '' }
        $prefB = if ($vb.Pre -match '^(\D+)') { $Matches[1] } else { '' }
        if ($prefA -eq $prefB -and $null -ne $numA -and $null -ne $numB) {
            return $numA.CompareTo($numB)
        }
        return $labelCmp
    }
    return 0
}

# ---------------------------------------------------------------------------
# Helper: extract the deduplication key from an existing markdown table row.
# Key = Version | BenchmarkClass | Method | Options  (columns 2, 4, 5, 6).
# Returns $null for non-data lines (headers, separators, prose).
# ---------------------------------------------------------------------------
function Get-RowKey([string] $line) {
    if ($line -notmatch '^\|' -or $line -match '^\|[-| :]+\|') { return $null }
    # Split on '|' and trim but keep empty cells so positions stay fixed.
    # A row  '| a | b | c |'  becomes  ['', 'a', 'b', 'c', '']
    # Column layout (1-based pipe index):
    #   [1]=Date  [2]=Version  [3]=Runtime  [4]=BenchmarkClass  [5]=Method  [6]=Options …
    $cells = $line -split '\|' | ForEach-Object { $_.Trim() }
    if ($cells.Count -lt 8 -or $cells[1] -eq 'Date') { return $null }
    return "$($cells[2])|$($cells[4])|$($cells[5])|$($cells[6])"
}

# ---------------------------------------------------------------------------
# 7. Ensure HistoryFile and its markdown table exist
# ---------------------------------------------------------------------------
$tableHeaderLines = @(
    '| Date | Version | Runtime | BenchmarkClass | Method | Options | Mean (ms) | Max (ms) | Allocated (MB) |',
    '|------|---------|---------|----------------|--------|---------|----------:|---------:|---------------:|'
)

if (-not (Test-Path $HistoryFile)) {
    Write-Host "Creating new history file: $HistoryFile"
    $init = @(
        '# Coverlet Benchmark History',
        '',
        'This file is maintained automatically by `scripts/Update-BenchmarkHistory.ps1`.',
        'Do not edit the table rows by hand – re-run the script after each benchmark run.',
        ''
    ) + $tableHeaderLines
    $init | Set-Content $HistoryFile -Encoding UTF8
}

# ---------------------------------------------------------------------------
# 8. Read history into memory, upsert rows by dedup key, write back once
# ---------------------------------------------------------------------------
$fileLines = [System.Collections.Generic.List[string]](Get-Content $HistoryFile -Encoding UTF8)

# ---------------------------------------------------------------------------
# Guard: reject an update if $CoverletVersion is older than the latest version
# already recorded, unless the caller passed -Force.
# ---------------------------------------------------------------------------
$latestRecorded = $fileLines |
    Where-Object { $_ -match '^\|' -and $_ -notmatch '^\|[-| :]+\|' } |
    ForEach-Object {
        $c = $_ -split '\|' | ForEach-Object { $_.Trim() }
        if ($c.Count -ge 3 -and $c[1] -ne 'Date' -and $c[2] -ne '') { $c[2] }
    } |
    Where-Object { $_ } |
    Select-Object -Unique |
    ForEach-Object -Begin   { $best = $null } `
                   -Process {
                       if ($null -eq $best -or (Compare-CoverletVersion $_ $best) -gt 0) { $best = $_ }
                   } `
                   -End     { $best }

if ($latestRecorded -and -not $Force) {
    $cmp = Compare-CoverletVersion $CoverletVersion $latestRecorded
    if ($cmp -lt 0) {
        Write-Error (@"
Version '$CoverletVersion' is older than the latest version already in the history file ('$latestRecorded').
Re-run with -Force if you intentionally want to record an older version.
"@)
    }
    if ($cmp -eq 0) {
        Write-Host "Version '$CoverletVersion' matches the latest recorded version '$latestRecorded' – rows will be updated in-place."
    }
    if ($cmp -gt 0) {
        Write-Host "New version '$CoverletVersion' is newer than '$latestRecorded' – new rows will be appended."
    }
}
$hasHeader = $fileLines | Where-Object { $_ -match [regex]::Escape('| Date | Version |') }
if (-not $hasHeader) {
    $fileLines.Add('')
    foreach ($h in $tableHeaderLines) { $fileLines.Add($h) }
}

# Build an index: dedup key -> line index within $fileLines
$keyIndex = @{}
for ($i = 0; $i -lt $fileLines.Count; $i++) {
    $k = Get-RowKey $fileLines[$i]
    if ($k) { $keyIndex[$k] = $i }
}

$replaced = 0
$appended = 0

foreach ($row in $rows) {
    $ic       = [cultureinfo]::InvariantCulture
    $meanFmt  = if ([double]::IsNaN($row.MeanMs))  { '-' } else { $row.MeanMs.ToString('F3', $ic)  }
    $maxFmt   = if ([double]::IsNaN($row.MaxMs))   { '-' } else { $row.MaxMs.ToString('F3', $ic)   }
    $allocFmt = if ([double]::IsNaN($row.AllocMB)) { '-' } else { $row.AllocMB.ToString('F4', $ic) }

    $mdRow = "| $($row.Date) | $($row.Version) | $($row.Runtime) | $($row.BenchmarkClass) | $($row.Method) | $($row.Options) | $meanFmt | $maxFmt | $allocFmt |"
    # Key must match Get-RowKey: cells[2]=Version, cells[4]=BenchmarkClass, cells[5]=Method, cells[6]=Options
    $key   = "$($row.Version)|$($row.BenchmarkClass)|$($row.Method)|$($row.Options)"

    if ($keyIndex.ContainsKey($key)) {
        # Replace the existing row in-place
        $fileLines[$keyIndex[$key]] = $mdRow
        $replaced++
    } else {
        # New combination – append and record its future index for this session
        $keyIndex[$key] = $fileLines.Count
        $fileLines.Add($mdRow)
        $appended++
    }
}

# Write the updated file back in a single operation (preserve trailing newline)
[System.IO.File]::WriteAllLines(
    (Resolve-Path $HistoryFile).Path,
    $fileLines,
    [System.Text.UTF8Encoding]::new($false)   # UTF-8 without BOM
)

if ($replaced -gt 0) { Write-Host "Replaced $replaced existing row(s) in $HistoryFile" }
if ($appended -gt 0) { Write-Host "Appended $appended new row(s) to $HistoryFile"      }
if ($replaced -eq 0 -and $appended -eq 0) { Write-Host 'No rows written.' }
