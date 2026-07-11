<#
.SYNOPSIS
    Deploys the built mod to the Wildfrost Mods directory.

.DESCRIPTION
    Copies the compiled DLL to the game's StreamingAssets/Mods directory
    so the game can discover and load it.

.PARAMETER Configuration
    Build configuration to deploy from: Debug or Release. Default: Release.

.PARAMETER GamePath
    Path to the Wildfrost game directory. Default: Standard Steam path.

.PARAMETER BuildFirst
    If specified, runs Build-Mod.ps1 before deploying.

.EXAMPLE
    .\Deploy-Mod.ps1
    .\Deploy-Mod.ps1 -BuildFirst
    .\Deploy-Mod.ps1 -GamePath "D:\Games\Wildfrost"
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\Wildfrost",

    [switch]$BuildFirst
)

$ErrorActionPreference = "Stop"
$ModName = "WildfrostAccessibility"
$ProjectDir = Join-Path $PSScriptRoot "..\src"

# Build first if requested
if ($BuildFirst) {
    & (Join-Path $PSScriptRoot "Build-Mod.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed. Deploy aborted."
        exit 1
    }
}

# Find the built DLL
$outputDir = Join-Path (Join-Path $ProjectDir "bin") $Configuration
$dll = Join-Path $outputDir "$ModName.dll"

if (-not (Test-Path $dll)) {
    # Search for it
    $found = Get-ChildItem -Path (Join-Path $ProjectDir "bin") -Recurse -Filter "$ModName.dll" | Select-Object -First 1
    if ($found) {
        $dll = $found.FullName
    } else {
        Write-Error "DLL not found. Run Build-Mod.ps1 first."
        exit 1
    }
}

# Determine mod destination
$modsDir = [System.IO.Path]::Combine($GamePath, "Modded", "Wildfrost_Data", "StreamingAssets", "Mods", $ModName)

if (-not (Test-Path $modsDir)) {
    New-Item -ItemType Directory -Path $modsDir -Force | Out-Null
    Write-Host "Created mod directory: $modsDir" -ForegroundColor Cyan
}

# Copy DLL
Copy-Item -Path $dll -Destination $modsDir -Force
Write-Host "Deployed: $dll" -ForegroundColor Green
Write-Host "      -> $modsDir" -ForegroundColor Green

# Verify
$deployed = Join-Path $modsDir "$ModName.dll"
if (Test-Path $deployed) {
    $info = Get-Item $deployed
    Write-Host "`nDeployment successful. Size: $($info.Length) bytes, Modified: $($info.LastWriteTime)" -ForegroundColor Green
} else {
    Write-Error "Deployment verification failed."
    exit 1
}
