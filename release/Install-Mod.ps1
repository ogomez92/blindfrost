<#
.SYNOPSIS
    Installs the Wildfrost Accessibility mod.

.DESCRIPTION
    Copies the contents of "put in game folder" into the Wildfrost
    installation folder.

    That is the whole installation: the mod enables itself automatically
    the first time the game starts, so you do not need sighted help to
    turn it on, and your save file is never touched.

.PARAMETER GamePath
    Path to the Wildfrost game directory. Default: standard Steam path.

.EXAMPLE
    .\Install-Mod.ps1
    .\Install-Mod.ps1 -GamePath "D:\Games\Wildfrost"
#>

param(
    [string]$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\Wildfrost"
)

$ErrorActionPreference = "Stop"

$gameSource = Join-Path $PSScriptRoot "put in game folder"

# --- Sanity checks ---------------------------------------------------------

if (-not (Test-Path $gameSource)) {
    Write-Error "Folder not found: $gameSource`nRun this script from the extracted release folder, next to 'put in game folder'."
}

if (-not (Test-Path (Join-Path $GamePath "Wildfrost.exe"))) {
    Write-Error "Wildfrost not found at: $GamePath`nIf the game is installed somewhere else, run:`n  .\Install-Mod.ps1 -GamePath `"D:\Path\To\Wildfrost`""
}

if (-not (Test-Path (Join-Path $GamePath "Modded"))) {
    Write-Error "No 'Modded' folder inside $GamePath.`nThe game's modded runtime is missing. Let Steam update the game, or verify the game files in Steam, then run this script again."
}

# --- Copy mod + Tolk DLLs into the game folder ------------------------------

Write-Host "Installing mod files into: $GamePath"
try {
    Copy-Item -Path (Join-Path $gameSource "*") -Destination $GamePath -Recurse -Force
} catch [System.UnauthorizedAccessException] {
    Write-Error "Access denied writing to $GamePath.`nRun this script again from an elevated (Run as administrator) PowerShell window."
}

$expected = @(
    (Join-Path $GamePath "Modded\Tolk.dll"),
    (Join-Path $GamePath "Modded\nvdaControllerClient64.dll"),
    (Join-Path $GamePath "Modded\Wildfrost_Data\StreamingAssets\Mods\WildfrostAccessibility\WildfrostAccessibility.dll")
)
foreach ($file in $expected) {
    if (-not (Test-Path $file)) {
        Write-Error "Expected file missing after copy: $file"
    }
}

# A leftover marker from an older installation would stop the fresh mod
# from enabling itself, so clear it.
$marker = Join-Path $GamePath "Modded\Wildfrost_Data\StreamingAssets\Mods\WildfrostAccessibility\autoenable.marker"
if (Test-Path $marker) {
    Remove-Item -Path $marker -Force
    Write-Host "Removed old autoenable.marker so the mod re-enables itself on the next start."
}

# --- Done -------------------------------------------------------------------

Write-Host ""
Write-Host "Installation complete."
Write-Host "IMPORTANT: launch the MODDED game - the Steam Play button runs the unmodded build by default."
Write-Host "Start this executable (a desktop shortcut to it helps):"
Write-Host "  $(Join-Path $GamePath 'Modded\Wildfrost.exe')"
Write-Host "The mod enables itself on the first start - your save is not touched."
Write-Host "You should hear: 'Wildfrost Accessibility loaded. Press F1 for help.'"
