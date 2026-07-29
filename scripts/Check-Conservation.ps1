<#
Proves a file split only MOVED code: every non-scaffolding line of the original
file must appear, the same number of times, across the replacement files.

Usage:
  Check-Conservation.ps1 -OriginalRef "HEAD~1:src/Localization/Loc.cs" -New src/Localization/Loc*.cs
#>
param(
    [Parameter(Mandatory = $true)][string]$OriginalRef,
    [Parameter(Mandatory = $true)][string[]]$New
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

# Lines that are pure scaffolding and are expected to be duplicated per file.
function IsScaffolding($line) {
    $t = $line.Trim()
    if ($t -eq "") { return $true }
    if ($t -eq "{" -or $t -eq "}") { return $true }
    if ($t -like "using *;") { return $true }
    if ($t -like "namespace *") { return $true }
    if ($t -like "*class *") { return $true }        # class decls change (partial) + repeat
    if ($t -like "///*") { return $true }            # doc comments get re-authored per part
    if ($t -like "//*") { return $true }             # section banners get re-authored
    return $false
}

$origLines = (git show $OriginalRef) -split "`n" | ForEach-Object { $_.TrimEnd("`r") }
$newFiles = @()
foreach ($pattern in $New) { $newFiles += Get-ChildItem $pattern -File }
$newLines = @()
foreach ($f in $newFiles) { $newLines += (Get-Content $f.FullName) }

$o = $origLines | Where-Object { -not (IsScaffolding $_) } | ForEach-Object { $_.Trim() } | Sort-Object
$n = $newLines  | Where-Object { -not (IsScaffolding $_) } | ForEach-Object { $_.Trim() } | Sort-Object

$diff = Compare-Object $o $n
if ($diff) {
    Write-Host "CONSERVATION FAILED for $OriginalRef" -ForegroundColor Red
    Write-Host "  (<= only in original, => only in new files)" -ForegroundColor DarkGray
    $diff | Select-Object -First 60 | ForEach-Object {
        Write-Host ("  {0} {1}" -f $_.SideIndicator, $_.InputObject) -ForegroundColor Red
    }
    if ($diff.Count -gt 60) { Write-Host "  ... $($diff.Count - 60) more" -ForegroundColor Red }
    exit 1
}
Write-Host ("CONSERVATION OK: {0} code lines preserved across {1} file(s)." -f $o.Count, $newFiles.Count) -ForegroundColor Green
exit 0
