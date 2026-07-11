<#
.SYNOPSIS
    Installs the Wildfrost Accessibility mod.

.DESCRIPTION
    Copies the contents of "put in game folder" into the Wildfrost
    installation folder, and the contents of "put in AppData LocalLow"
    into %USERPROFILE%\AppData\LocalLow.

    The AppData part is a fresh save file with the mod already enabled,
    so you do not need sighted help to turn the mod on. If you already
    have a Wildfrost save, it is backed up first, or left untouched if
    you pass -KeepSave.

.PARAMETER GamePath
    Path to the Wildfrost game directory. Default: standard Steam path.

.PARAMETER KeepSave
    Do not touch the existing save. You will then have to enable the mod
    once manually in the game's Mods menu (needs mouse or gamepad).

.PARAMETER OverwriteSave
    Replace an existing save without asking. A backup is still made.

.EXAMPLE
    .\Install-Mod.ps1
    .\Install-Mod.ps1 -GamePath "D:\Games\Wildfrost"
    .\Install-Mod.ps1 -KeepSave
#>

param(
    [string]$GamePath = "C:\Program Files (x86)\Steam\steamapps\common\Wildfrost",
    [switch]$KeepSave,
    [switch]$OverwriteSave
)

$ErrorActionPreference = "Stop"

$gameSource     = Join-Path $PSScriptRoot "put in game folder"
$localLowSource = Join-Path $PSScriptRoot "put in AppData LocalLow"
$localLow       = Join-Path $env:USERPROFILE "AppData\LocalLow"

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

# --- Step 1: mod + Tolk DLLs into the game folder --------------------------

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
Write-Host "Game files installed."

# --- Step 2: pre-enabled save into AppData\LocalLow -------------------------

$saveDest = Join-Path $localLow "Deadpan Games\Wildfrost\Profiles\Default\Save.sav"
$installSave = $true

if ($KeepSave) {
    $installSave = $false
    Write-Host "Skipping the save file (-KeepSave)."
} elseif (Test-Path $saveDest) {
    if (-not $OverwriteSave) {
        Write-Host ""
        Write-Host "You already have a Wildfrost save. Replacing it enables the mod without"
        Write-Host "needing the Mods menu, but your current progress is LOST (a backup copy"
        Write-Host "is kept next to it)."
        $answer = Read-Host "Replace your save with the mod-enabled one? (y/n)"
        if ($answer -notmatch '^[yY]') {
            $installSave = $false
            Write-Host "Keeping your existing save."
        }
    }
    if ($installSave) {
        $backup = "$saveDest.backup-" + (Get-Date -Format "yyyyMMdd-HHmmss")
        Copy-Item -Path $saveDest -Destination $backup -Force
        Write-Host "Existing save backed up to: $backup"
    }
}

if ($installSave) {
    Copy-Item -Path (Join-Path $localLowSource "*") -Destination $localLow -Recurse -Force
    Write-Host "Save with the mod pre-enabled installed to: $saveDest"
}

# --- Done -------------------------------------------------------------------

Write-Host ""
Write-Host "Installation complete."
if ($installSave) {
    Write-Host "Launch Wildfrost. You should hear: 'Wildfrost Accessibility loaded. Press F1 for help.'"
} else {
    Write-Host "Because your save was kept, enable the mod once manually:"
    Write-Host "  1. Launch Wildfrost"
    Write-Host "  2. On the main menu, use a gamepad or mouse to open 'Mods'"
    Write-Host "  3. Click 'Wildfrost Accessibility' to enable it, then go back"
    Write-Host "After that, launch the game and you should hear: 'Wildfrost Accessibility loaded. Press F1 for help.'"
}
