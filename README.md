# Wildfrost Accessibility Mod

Screen reader accessibility mod for Wildfrost, enabling blind players to navigate menus, read cards, play battles, and explore the campaign map via NVDA.

## Installation

### Requirements

- Wildfrost (Steam version)
- NVDA screen reader

### What is in the release zip

- `Install-Mod.ps1` - installer script that does all the steps below for you
- `put in game folder\` - the mod DLL plus the Tolk screen reader DLLs, already laid out in the game's own folder structure
- `readme.html` - this document as a web page

The mod enables itself automatically the first time the game starts after installation, so you do not need sighted help and your save file is never touched.

### Easy install: run the script (recommended)

1. Extract the zip somewhere.
2. Open PowerShell in the extracted folder and run:

       powershell -ExecutionPolicy Bypass -File .\Install-Mod.ps1

The script copies everything into place and tells you what it did. If the game is installed somewhere else, pass `-GamePath "D:\Path\To\Wildfrost"`.

If the script succeeds, skip to Step 2: Verify. The manual steps below do the same thing by hand.

### Step 1: Copy "put in game folder" into the game folder

Copy the **contents** of the `put in game folder` folder into your Wildfrost installation folder, merging with the existing `Modded` folder:

    C:\Program Files (x86)\Steam\steamapps\common\Wildfrost

After copying, these files must exist:

    C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\Tolk.dll
    C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\nvdaControllerClient64.dll
    C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\Wildfrost_Data\StreamingAssets\Mods\WildfrostAccessibility\WildfrostAccessibility.dll

If you install by hand instead: Tolk.dll and nvdaControllerClient64.dll go directly in the `Modded` folder, NOT in the WildfrostAccessibility mod folder. The game tries to load every DLL in a mod folder as a .NET assembly and crashes on native DLLs.

### Step 2: Verify

Launch Wildfrost. On the first start after installation the mod enables itself - no Mods menu, no sighted help, and your save file is not touched. You should hear "Wildfrost Accessibility loaded. Press F1 for help." from NVDA. Afterwards the mod auto-loads on every launch.

If you ever disable the mod in the game's Mods menu, it stays disabled. To make it enable itself once more, delete the file `autoenable.marker` next to `WildfrostAccessibility.dll` in the mod folder and start the game again.

## What the mod does

Every screen announces itself when it opens, and every focused element is described. Tutorial popups and help panels are read aloud, with drag-and-drop instructions rephrased for keyboard play.

- **Main menu** - up/down arrows move between buttons, Enter selects.
- **Town (base camp)** - arrow keys move between buildings. Each building is announced with its name and state. The Gate says what Enter will do: start a new journey, continue the one in progress, or begin the tutorial.
- **Continue journey screen** - announces your run in progress: start date, leader with health and attack, and the full deck list with duplicate counts. The Let's Go and Back buttons explain what they do.
- **Campaign map** - announces the zone, where you are, and which destinations you can travel to. Locations are announced with their name, category (battle, shop, boss...), and state (you are here, cleared, available, further ahead, not reachable). Battle locations include their wave count; details include the exact enemies per wave.
- **Battle** - announces turns, phases, and the redraw bell. Cards are read with stats, status effects (Snow, Frost...), and fully expanded keyword descriptions. Cards are played with Enter: pick up, choose a target with the arrows (only valid targets are reachable), Enter again to place.
- **Story events** - event titles, prompts, and story text ("Break the ice!", "Choose a new companion!"...) are read as they appear and whenever they change. The frozen-companion ice block announces each crack; offered cards are read in full. If the game refuses a choice (the tutorial requires inspecting a card first), the reason is spoken.
- **Deck piles** - draw and discard piles announce their card counts everywhere they appear.

## Controls

### Global

- F1: context help - explains the current screen and its keys
- F10: toggle debug mode
- Arrow keys: navigate
- Enter: activate / pick up / place

### Town

- I: describe the focused building

### Campaign map

- Left/Right: move along the journey path, location by location
- Up/Down: reach the deck piles and other controls
- Enter: travel to an available location
- M: read the whole map
- I: read details of the focused location, including enemies and rewards
- G: read your gold

### Story events

- Left/Right: move between the offered cards
- Enter: hit the ice block / choose the focused card
- I: inspect the focused card (the keyboard equivalent of right-click; the tutorial asks for this before letting you choose)
- Escape or I: close the inspect view

### Battle

- Up/Down: switch group - hand, your board, enemy board, bell and piles
- Left/Right: move within the group
- Enter: pick up a hand card / place it on the focused target
- H: read your hand
- B: read the full board with stats and status effects
- W: read incoming enemy waves
- R: read the redraw bell state
- T: read the turn and phase
- G: read your gold

## Supported Languages

The mod detects the game's language automatically.

- Core announcements: English, German, French, Spanish, Italian, Portuguese, Russian, Polish, Turkish, Japanese, Korean, Simplified Chinese, Traditional Chinese.
- Screen-specific announcements (Town, map, battle, continue screen): English, German, Spanish, French for now.

Anything not yet translated falls back to English.

## Building from Source

Requires .NET SDK 6.0+.

    scripts\Build-Mod.ps1
    scripts\Deploy-Mod.ps1 -BuildFirst

Build-Mod.ps1 also copies the built DLL into `release\put in game folder\...` so the release folder always contains the latest build.

## Releasing

Push a tag like `v1.0.0`. GitHub Actions converts this README to `release/readme.html` with pandoc, zips the `release` folder, and publishes it as a GitHub release.
