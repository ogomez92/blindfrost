# Wildfrost Accessibility Mod

Screen reader accessibility mod for Wildfrost, enabling blind players to navigate menus, read cards, play battles, and explore the campaign map via NVDA.

## Installation

### Requirements

- Wildfrost (Steam version)
- NVDA screen reader

### Files in release/

- WildfrostAccessibility.dll - the mod
- Tolk.dll - screen reader bridge
- nvdaControllerClient64.dll - NVDA integration

### Step 1: Copy Tolk DLLs

Copy these two files to your Wildfrost **Modded** directory (NOT the mod folder):

    Tolk.dll -> C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\
    nvdaControllerClient64.dll -> C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\

IMPORTANT: Do NOT put these in the WildfrostAccessibility mod folder. The game tries to load all DLLs in mod folders as .NET assemblies and will crash on native DLLs.

### Step 2: Copy the mod DLL

Create the mod folder and copy ONLY the mod DLL:

    WildfrostAccessibility.dll -> C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\Wildfrost_Data\StreamingAssets\Mods\WildfrostAccessibility\

If the WildfrostAccessibility folder does not exist, create it.

### Step 3: Enable the mod (first time only)

1. Launch Wildfrost (it will use the Modded runtime automatically)
2. On the main menu, use a gamepad or mouse to navigate to "Mods"
3. Click on "Wildfrost Accessibility" to enable it
4. Go back to the main menu

After this one-time step, the mod auto-loads on every future launch.

### Step 4: Verify

You should hear "Wildfrost Accessibility loaded. Press F1 for help." from NVDA.

## Controls

- F1: Help
- F10: Toggle debug mode

## Supported Languages

The mod detects the game's language automatically. Currently translated:
English, German, French, Spanish, Italian, Portuguese, Russian, Polish, Turkish, Japanese, Korean, Simplified Chinese, Traditional Chinese.

Other languages fall back to English.

## Building from Source

Requires .NET SDK 6.0+.

    scripts\Build-Mod.ps1
    scripts\Deploy-Mod.ps1 -BuildFirst
