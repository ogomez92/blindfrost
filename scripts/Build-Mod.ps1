<#
.SYNOPSIS
    Builds the Wildfrost Accessibility mod.

.DESCRIPTION
    Compiles the mod project and reports build results.
    Uses dotnet build targeting net472.

.PARAMETER Configuration
    Build configuration: Debug or Release. Default: Release.

.EXAMPLE
    .\Build-Mod.ps1
    .\Build-Mod.ps1 -Configuration Debug
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ProjectDir = Join-Path $PSScriptRoot "..\src"
$ProjectFile = Join-Path $ProjectDir "WildfrostAccessibility.csproj"

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

Write-Host "Building WildfrostAccessibility ($Configuration)..." -ForegroundColor Cyan

$buildOutput = & dotnet build $ProjectFile -c $Configuration 2>&1
$exitCode = $LASTEXITCODE

$buildOutput | ForEach-Object { Write-Host $_ }

if ($exitCode -ne 0) {
    Write-Host "`nBuild FAILED." -ForegroundColor Red
    exit $exitCode
}

$outputDir = Join-Path (Join-Path $ProjectDir "bin") $Configuration
$dll = Join-Path $outputDir "WildfrostAccessibility.dll"

if (Test-Path $dll) {
    Write-Host "`nBuild successful: $dll" -ForegroundColor Green
} else {
    Write-Host "`nBuild completed but DLL not found at expected path." -ForegroundColor Yellow
    # Try to find it
    $found = Get-ChildItem -Path (Join-Path $ProjectDir "bin") -Recurse -Filter "WildfrostAccessibility.dll" | Select-Object -First 1
    if ($found) {
        Write-Host "Found DLL at: $($found.FullName)" -ForegroundColor Green
    }
}
