<#
Removes unused `using` directives, letting the COMPILER decide rather than guessing.

Per file: try dropping all its usings at once (one build). If that fails, fall back to
dropping them one at a time, keeping only the removals that still compile.

Byte-preserving: reads and writes raw text, keeps each file's original newline style and
encoding, and restores the exact original bytes for any file it does not end up changing.
So only files with a real removal appear in `git status`.

A removal that compiles could in principle still shift extension-method or overload
resolution, so the caller MUST run Verify-Equivalence.ps1 afterwards — that compares the
decompiled assembly against the pre-refactor baseline and would catch any such shift.
#>
param(
    [string]$Root = (Join-Path (Split-Path $PSScriptRoot -Parent) "src")
)
$ErrorActionPreference = "Stop"
$proj = Join-Path $Root "WildfrostAccessibility.csproj"

function Build-Ok {
    $null = & dotnet build $proj -c Release --nologo -v q 2>&1
    return ($LASTEXITCODE -eq 0)
}
# Raw read/write: no encoding guessing, no newline rewriting.
function ReadRaw($p)     { return [System.IO.File]::ReadAllText($p) }
function WriteRaw($p, $t) { [System.IO.File]::WriteAllText($p, $t) }

Write-Host "Baseline build..." -ForegroundColor Cyan
if (-not (Build-Ok)) { Write-Host "Baseline build already FAILS - aborting." -ForegroundColor Red; exit 1 }

$files = Get-ChildItem $Root -Recurse -Filter *.cs |
         Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } | Sort-Object FullName

$totalRemoved = 0
$builds = 1
$report = @()

foreach ($f in $files) {
    $original = ReadRaw $f.FullName
    $nl = if ($original -match "`r`n") { "`r`n" } else { "`n" }

    # Split preserving nothing but the lines; we rejoin with the file's own newline.
    $lines = $original -split "`r?`n"
    $usings = @()
    foreach ($l in $lines) {
        if ($l -match '^\s*using\s+[A-Za-z_][\w.]*\s*;\s*$') { $usings += $l }
        elseif ($l -match '^\s*namespace\s') { break }
    }
    if ($usings.Count -eq 0) { continue }

    # Attempt 1: drop them all at once.
    $trial = ($lines | Where-Object { $usings -notcontains $_ }) -join $nl
    WriteRaw $f.FullName $trial
    $builds++
    if (Build-Ok) {
        $totalRemoved += $usings.Count
        $report += "  {0}: removed all {1} ({2})" -f $f.Name, $usings.Count, (($usings | ForEach-Object { $_.Trim() }) -join ", ")
        continue
    }

    # Attempt 2: one at a time.
    WriteRaw $f.FullName $original
    $currentLines = $lines
    $removed = @()
    foreach ($u in $usings) {
        $candidate = @($currentLines | Where-Object { $_ -ne $u })
        if ($candidate.Count -eq $currentLines.Count) { continue }
        WriteRaw $f.FullName (($candidate -join $nl))
        $builds++
        if (Build-Ok) { $currentLines = $candidate; $removed += $u.Trim() }
    }
    if ($removed.Count -gt 0) {
        WriteRaw $f.FullName (($currentLines -join $nl))
        $totalRemoved += $removed.Count
        $report += "  {0}: {1}" -f $f.Name, ($removed -join ", ")
    } else {
        WriteRaw $f.FullName $original    # exact original bytes back
    }
}

Write-Host "`nRemoved $totalRemoved unused using directive(s) across $($files.Count) files ($builds builds)." -ForegroundColor Green
$report | ForEach-Object { Write-Host $_ -ForegroundColor DarkGray }

Write-Host "`nFinal build check..." -ForegroundColor Cyan
if (Build-Ok) { Write-Host "Build OK." -ForegroundColor Green; exit 0 }
Write-Host "FINAL BUILD FAILED." -ForegroundColor Red; exit 1
