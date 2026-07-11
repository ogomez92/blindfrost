# Project Status: WildfrostAccessibility

## Project Info

- **Game:** Wildfrost
- **Engine:** Unity (IL2CPP backend, Mono modded runtime)
- **Architecture:** 64-bit
- **Mod Loader:** Wildfrost built-in mod system (WildfrostMod + Harmony)
- **Runtime:** net4.7.2
- **Game directory:** C:\Program Files (x86)\Steam\steamapps\common\Wildfrost
- **Modded runtime:** C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded
- **Managed DLLs:** C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\Wildfrost_Data\Managed
- **Game code (decompiled):** T:\code\mods\wildfrost\game_dll_code\Assembly-CSharp
- **User experience level:** Advanced (C#, modding)
- **User game familiarity:** Not at all — Claude analyzes game mechanics and explains

## Setup Progress

- [x] Experience level determined
- [x] Game name and path confirmed
- [x] Game familiarity assessed (not at all — Claude explains mechanics)
- [x] Game directory auto-check completed
- [x] Mod loader identified (Wildfrost built-in — no BepInEx/MelonLoader needed)
- [x] Tolk DLLs in place (copied from Monster Train)
- [x] .NET SDK available (6.0 + 9.0)
- [x] Game code available in game_dll_code/
- [x] Multilingual support decided (yes — all game languages, detected at runtime)
- [x] Project directory set up (csproj, Main.cs, ScreenReader.cs, DebugLogger.cs, Loc.cs)
- [x] CLAUDE.md updated with project-specific values
- [x] First build successful (17,920 bytes)
- [x] Deployed to game Mods directory
- [x] "Mod loaded" announcement working in game (confirmed 2026-03-29)

## Current Phase

**Phase:** Framework → Implementation
**Currently working on:** Screen handler framework + MainMenu accessibility
**Blocked by:** Nothing — Tier 1 analysis complete, ready for implementation

## Codebase Analysis Progress

### GATE: Tier 1 MUST be complete before Phase 2 (Framework)!

- [x] 1.1 Structure overview (namespaces, singletons) — documented in game-api.md (2026-03-29)
- [x] 1.2 Input system — ALL game key bindings documented in game-api.md "Game Key Bindings" (2026-03-29)
- [x] 1.2 Input system — Safe mod keys identified and listed in game-api.md "Safe Mod Keys" (2026-03-29)
- [x] 1.3 UI system (base classes, text access patterns, Reflection needed?) — documented (2026-03-29)
- [x] 1.4 State management decision — documented in "Architecture Decisions" below (2026-03-29)
- [x] 1.5 Localization: game's language system analyzed (2026-03-29)

### GATE: Relevant Tier 2 items MUST be done before implementing each feature!

- [ ] 1.6 Game mechanics (analyzed as needed per feature)
- [ ] 1.7 Status/feedback systems
- [ ] 1.8 Event system / Harmony patch points
- [ ] 1.9 Results documented in `docs/game-api.md`
- [ ] 1.10 Tutorial analysis (when relevant)

## Game Key Bindings (Original)

### Rewired Actions (gamepad/controller)
- Select — confirm/click
- Back — cancel/back
- Move Horizontal/Vertical — D-pad/stick navigation
- Inspect — card details
- Options — charms/options
- Scroll Vertical — scroll UI
- Backpack, Redraw Bell, Up, Down, Snap — debug only

### Direct KeyCode Usage
- BackQuote / F12 — console toggle
- Escape — exit console
- Tab — console auto-complete
- Ctrl+D — debug card duplicate
- Alt+Return — fullscreen toggle
- LeftArrow/RightArrow — CardViewer navigation
- Space — debug menu combo

## Implemented Features

- **Screen Handler Framework** — ScreenHandler base class, ScreenManager (scene detection + routing), NavigationHelper (arrow keys, confirm, focus management)
- **MainMenuHandler** — announces "Main Menu" on entry, arrow key Up/Down navigation between buttons, Enter to activate, announces focused button text via screen reader

## Pending Tests

- **MainMenuHandler (2026-03-29):** Launch game, check:
  1. Does NVDA say "Main Menu" after the title screen loads?
  2. Do Up/Down arrow keys move between menu buttons?
  3. Does NVDA announce each button name (Play, Continue, Mods, etc.) when focused?
  4. Does Enter activate the focused button?
  5. Does F1 still work?
  - If buttons aren't announced: enable debug mode (F10), check Unity console for handler logs
  - If arrow keys don't work: the game may be in mouse mode — try pressing a gamepad button first, or check if EnsureControllerMode() runs

## Known Issues

- Wildfrost uses its own mod system, NOT BepInEx or MelonLoader.
- Game has both IL2CPP (main exe) and Mono (Modded/ folder) runtimes — mods run in the Modded runtime.
- Native DLLs (Tolk, nvdaControllerClient64) MUST go in Modded/ dir, NOT in mod folder — game tries to load all DLLs in mod folder as .NET assemblies.
- Tolk_DetectScreenReader() causes native crash — use Tolk_HasSpeech() instead.
- SetDllDirectory(Modded/) must be called before any Tolk functions.
- Game keyboard nav is gamepad-only (Rewired) — mod must provide keyboard navigation.

## Architecture Decisions

- Using Wildfrost's built-in WildfrostMod base class for mod entry point
- Harmony 0Harmony.dll already included in game for patching
- Mod GUID format: creatorName.wildfrost.modName
- **Screen detection:** Use `SceneManager.ActiveSceneKey` + `Events.OnSceneChanged` to detect current screen
- **Navigation approach:** Hook into existing UINavigationSystem — it already handles gamepad nav. We intercept hover/focus events to announce via screen reader, and provide keyboard arrow key input that feeds into the existing system.
- **No Reflection needed:** Card data (title, hp, damage, effects) all accessible via public properties. UI text via TMP components. Localization via public `.title` properties.
- **State management:** ScreenHandler base class per scene, ScreenManager routes Update/input to active handler based on `SceneManager.ActiveSceneKey`

## Key Bindings (Mod)

- F1: Help
- F10: Toggle debug mode

## Notes for Next Session

- Tier 1 analysis complete (2026-03-29) — all findings in docs/game-api.md
- Framework built: ScreenHandler, ScreenManager, NavigationHelper
- MainMenuHandler implemented — needs in-game testing
- **Next screens to implement:** Town, CharacterSelect, ContinueRun, Campaign (map), Battle, PauseMenu, Mods, Settings
- **Key insight:** Game's UINavigationSystem already does gamepad nav. Our approach: force controller mode via `Cursor3d.usingMouse = false`, then arrow keys feed into existing nav system. We hook `currentNavigationItem` changes to announce via screen reader.
- **Potential issue:** Setting `usingMouse = false` may conflict if user moves mouse. May need Harmony patch on MouseInputSwitcher to prevent re-switching. Test first.
