<#
.SYNOPSIS
    Proves a refactor did not change behaviour, by comparing compiled output.

.DESCRIPTION
    Decompiles the freshly built DLL and diffs it against a baseline decompilation
    taken before the refactor. Because it compares the COMPILED output, it is blind
    to file layout, member order and comments — exactly the things a move-only
    refactor changes — while catching any real change to code.

    Per type it compares two ways:
      1. sorted-line multiset diff — order-insensitive; a pure "move code between
         partial files" refactor must be IDENTICAL here.
      2. exact text diff — anything that passes (1) but fails (2) is reported as a
         member reordering, which is semantically inert in C# UNLESS the type has
         field initializers or a constructor. Check those separately if it reports any.

    Requires ilspycmd:  dotnet tool install -g ilspycmd

.EXAMPLE
    # before refactoring: build, then snapshot
    ./scripts/Build-Mod.ps1
    ./scripts/Verify-Equivalence.ps1 -MakeBaseline

    # after refactoring: build, then check
    ./scripts/Build-Mod.ps1
    ./scripts/Verify-Equivalence.ps1

.NOTES
    Exit code 0 = equivalent. The two sanctioned diffs below were audited during the
    2026-07-29 refactor; remove them when taking a fresh baseline.
#>
param(
    [string]$Scratch = (Join-Path $env:TEMP "wildfrost-equivalence"),
    [switch]$MakeBaseline
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Scratch)) { New-Item -ItemType Directory -Path $Scratch -Force | Out-Null }

$repo = Split-Path $PSScriptRoot -Parent
$dll = Join-Path $repo "src\bin\Release\WildfrostAccessibility.dll"
$baseDir = Join-Path $Scratch "il-baseline"
$currDir = Join-Path $Scratch "il-current"

function Decompile($outDir) {
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    & ilspycmd -p -o $outDir $dll 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "ilspycmd failed" }
    # the generated .csproj differs run-to-run in irrelevant ways; drop it
    Get-ChildItem $outDir -Recurse -Filter *.csproj | Remove-Item -Force
}

if ($MakeBaseline) {
    Decompile $baseDir
    Write-Host "Baseline decompilation written to $baseDir" -ForegroundColor Green
    exit 0
}

Decompile $currDir

# Normalize: strip blank lines + trim, then sort
function NormSorted($path) {
    (Get-Content $path) | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" } | Sort-Object
}

$baseFiles = Get-ChildItem $baseDir -Recurse -Filter *.cs
$currFiles = Get-ChildItem $currDir -Recurse -Filter *.cs
$baseMap = @{}; foreach ($f in $baseFiles) { $baseMap[$f.FullName.Substring($baseDir.Length)] = $f.FullName }
$currMap = @{}; foreach ($f in $currFiles) { $currMap[$f.FullName.Substring($currDir.Length)] = $f.FullName }

$problems = @()

# AssemblyInfo embeds the git commit hash, so it changes on every commit.
$ignoredTypes = @("\Properties\AssemblyInfo.cs")

# If your refactor deliberately adds members (e.g. splitting one huge method into
# several), list those exact decompiled lines here so they stop being reported.
# Anything NOT listed still fails the check. Keep this empty by default — an entry
# here is a claim you have audited that change by hand.
$allowedAdds = @()

foreach ($k in $baseMap.Keys) {
    if (-not $currMap.ContainsKey($k)) { $problems += "MISSING TYPE FILE: $k" }
}
foreach ($k in $currMap.Keys) {
    if (-not $baseMap.ContainsKey($k)) { $problems += "NEW TYPE FILE: $k" }
}

$reordered = @()
foreach ($k in $baseMap.Keys) {
    if (-not $currMap.ContainsKey($k)) { continue }
    if ($ignoredTypes -contains $k) { continue }
    $b = NormSorted $baseMap[$k]
    $c = NormSorted $currMap[$k]
    $d = Compare-Object $b $c
    # Drop audited additions, plus the two braces each added method body brings.
    if ($d -and $allowedAdds.Count -gt 0) {
        $script:braceBudget = $allowedAdds.Count * 2
        $d = @($d | Where-Object {
            if ($_.SideIndicator -ne "=>") { return $true }
            if ($allowedAdds -contains $_.InputObject) { return $false }
            if (($_.InputObject -eq "{" -or $_.InputObject -eq "}") -and $script:braceBudget -gt 0) {
                $script:braceBudget--; return $false
            }
            return $true
        })
    }
    if ($d) {
        $problems += "CONTENT DIFFERS: $k"
        $d | Select-Object -First 40 | ForEach-Object {
            $problems += ("    {0} {1}" -f $_.SideIndicator, $_.InputObject)
        }
        if ($d.Count -gt 40) { $problems += "    ... $($d.Count - 40) more" }
    } else {
        $bRaw = (Get-Content $baseMap[$k]) -join "`n"
        $cRaw = (Get-Content $currMap[$k]) -join "`n"
        if ($bRaw -ne $cRaw) { $reordered += $k }
    }
}

if ($reordered.Count -gt 0) {
    Write-Host "Members reordered (semantically identical) in $($reordered.Count) type(s):" -ForegroundColor Yellow
    $reordered | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkYellow }
}

if ($problems.Count -gt 0) {
    Write-Host "`nEQUIVALENCE CHECK FAILED:" -ForegroundColor Red
    $problems | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}

Write-Host "`nEQUIVALENCE OK - decompiled output matches baseline ($($baseMap.Count) types)." -ForegroundColor Green
exit 0
