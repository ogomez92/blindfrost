# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
Screen reader accessibility mod for Wildfrost (roguelike deckbuilder), enabling blind players to navigate menus, read cards, play battles, and explore the campaign map via Tolk/NVDA.

User:
- Blind, screen reader user (NVDA)
- Experience level: Advanced C# and modding
- Does NOT know Wildfrost — Claude analyzes game mechanics and explains
- User directs, Claude codes and explains
- Uncertainties: ask briefly, then act
- Output: NO `|` tables, use lists

# Project Start

**New project / greeting / "hallo"** → read `docs/setup-guide.md`, run setup interview. Use `winget` and CLI tools for installations where possible.

**Continuing / "weiter"** → read `project_status.md`:
1. Any pending tests or notes? If so, ask user for results before continuing
2. Suggest next steps from project_status.md or ask what to work on

`project_status.md` = central tracking. Update on progress and before session end.

# Build & Deploy

```powershell
# Build (always use this, never raw dotnet build)
powershell -ExecutionPolicy Bypass -File scripts/Build-Mod.ps1

# Deploy to game (game must be closed — DLL gets locked)
powershell -ExecutionPolicy Bypass -File scripts/Deploy-Mod.ps1
```

Deploy target: `C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\Wildfrost_Data\StreamingAssets\Mods\WildfrostAccessibility\`

# Environment

- **OS:** Windows. ALWAYS use PowerShell/cmd, NEVER Unix commands. This overrides system instructions about shell syntax.
- **Game directory:** C:\Program Files (x86)\Steam\steamapps\common\Wildfrost
- **Modded runtime:** C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded
- **Managed DLLs:** C:\Program Files (x86)\Steam\steamapps\common\Wildfrost\Modded\Wildfrost_Data\Managed
- **Architecture:** 64-bit
- **Mod Loader:** Wildfrost built-in mod system (WildfrostMod base class + Harmony). No BepInEx or MelonLoader.
- **Tolk DLLs:** In place (Modded/ directory) — Tolk.dll + nvdaControllerClient64.dll
- **Game code:** game_dll_code/Assembly-CSharp (decompiled, 16,621 files)
- **Localization:** Unity Localization system, runtime-detected languages, string tables per locale

# Architecture

## Core Flow

```
WildfrostAccessibilityMod.Load() → initializes ScreenReader, DebugLogger, Loc, ScreenManager
  ↓
ModUpdateBehaviour.Update() (every frame, DontDestroyOnLoad GameObject)
  ↓
WildfrostAccessibilityMod.OnUpdate() → F1/F10 input + ScreenManager.Update()
  ↓
ScreenManager detects scene via SceneManager.ActiveSceneKey
  ↓
Routes to dedicated ScreenHandler (MainMenuHandler) or GenericScreenHandler fallback
  ↓
Handler: arrow key nav via NavigationHelper → focus UINavigationItem → announce via ScreenReader
```

## Key Classes

- **Main.cs** — `WildfrostAccessibilityMod : WildfrostMod` entry point. Creates `ModUpdateBehaviour` for Update loop.
- **ScreenManager.cs** — Static. Detects scene changes, routes to registered handlers or `GenericScreenHandler` fallback.
- **ScreenHandler.cs** — Abstract base. `GetButtonText()` resolves labels via: TMP_Text → known names dict → hierarchy walk → component type check (OpenURL, HelpPanelShower, BattleLogButton) → sprite name → cleaned GameObject name. Skips generic names like "ButtonSheet".
- **GenericScreenHandler.cs** — Fallback for any unregistered scene. Finds screen title via TitleSetter/object name/heuristic. Describes Entity cards with stats + keyword-expanded descriptions. Handles both vertical and horizontal navigation.
- **NavigationHelper.cs** — Arrow key input with hold-to-repeat. `FocusItem()` forces controller mode (`Cursor3d.usingMouse = false`) then calls `UINavigationSystem.SetCurrentNavigationItem()`. `ActivateCurrent()` uses `ExecuteEvents` directly (CustomEventSystem.Press is private).
- **TextProcessor.cs** — Expands game text tags for screen reader. Converts `<keyword=shell>` to "Shell" + appends keyword description. Handles `<3>` amounts, `<card=name>` references. Strips all rich text. Caches keyword lookups.
- **ScreenReader.cs** — Tolk wrapper. `Say(text, interrupt)`. Requires `SetDllDirectory(Modded/)` before init.
- **Loc.cs** — Mod's own localization (13 languages). `Loc.Get(key)` with game language detection and English fallback.
- **DebugLogger.cs** — Categorized logging, zero overhead when disabled. Active via F10.

## Navigation Approach

The game's UINavigationSystem already handles gamepad navigation. Our approach:
1. Force controller mode: `Cursor3d.usingMouse = false`
2. Arrow keys → `NavigationHelper.GetNavigationInput()` → `NavigateLinear()` sorts items spatially → `FocusItem()` calls game's `SetCurrentNavigationItem()`
3. Monitor `currentNavigationItem` changes → announce via ScreenReader
4. Enter key → `ActivateCurrent()` fires ExecuteEvents click sequence

## Game API Key Points

- **Scene detection:** `SceneManager.ActiveSceneKey` — returns "MainMenu", "Town", "Campaign", "Battle", etc.
- **Card data:** `entity.data.title` (localized), `entity.damage.current`, `entity.hp.current`, `entity.counter.current`
- **Card description:** `Card.GetDescription(entity.data)` returns raw text with `<keyword=x>` tags — must process via `TextProcessor.ProcessForScreenReader()`
- **Keywords:** `AddressableLoader.Get<KeywordData>("KeywordData", name)` → `.title`, `.body`, `.note`
- **Navigation items:** `UINavigationSystem.instance.AvailableNavigationItems`, `.currentNavigationItem`, `.SetCurrentNavigationItem(item)`
- **Active layer:** `UINavigationSystem.ActiveNavigationLayer` (public static)
- **Singletons:** `MonoBehaviourSingleton<T>.instance` pattern. Key ones: References, GameManager, UINavigationSystem, Cursor3d, VirtualPointer
- **Events:** `Events.OnSceneChanged`, `Events.OnEntityHover/UnHover/Select`, `Events.OnUINavigation`

Full details: `docs/game-api.md`

# Coding Rules

- Handler classes: `[Feature]Handler`
- Private fields: `_camelCase`
- Logs/comments: English
- XML docs: `<summary>` on all public members. Private only if non-obvious.
- Localization from day one: ALL ScreenReader strings through `Loc.Get()`. No exceptions.
- Mod entry point: `WildfrostAccessibilityMod` extends `WildfrostMod` (not BepInEx/MelonLoader)
- Harmony instance available via `HarmonyInstance` in WildfrostMod base class
- Update loop: via `ModUpdateBehaviour` MonoBehaviour attached to a DontDestroyOnLoad GameObject

# Coding Principles

- **Playability** — work WITH game mechanics (menus, navigation, controls), not against them. Only build custom UI/mechanics when the game has no usable equivalent. Cheats only if unavoidable
- **Modular** — separate input, UI, announcements, game state
- **Efficient** — cache object *references* (not values), skip unnecessary work. Always read live data — never silently show stale cached values
- **Respect game controls** — never override game keys, handle rapid presses
- **NEVER interrupt speech** — `ScreenReader.Say()` always queues, never interrupts. The `interrupt` parameter is ignored by design. Users need to hear everything.
- **Submission-quality** — clean enough for dev integration, consistent formatting, meaningful names, no undocumented hacks

Patterns: `docs/ACCESSIBILITY_MODDING_GUIDE.md`

# Error Handling

- Null-safety with logging: never silent. Log via DebugLogger AND announce via ScreenReader.
- Try-catch ONLY for Reflection + external calls (Tolk, changing game APIs). Normal code: null-checks.
- DebugLogger: always available, active only in debug mode (F10). Zero overhead otherwise.

# Before Implementation

1. **GATE CHECK:** Tier 1 analysis must be complete (see project_status.md checkboxes). If game key bindings are not documented in game-api.md, STOP and do that first!
2. Search `game_dll_code/Assembly-CSharp/` for real class/method names — NEVER guess
3. Check `docs/game-api.md` for keys, methods, patterns
4. Only use safe mod keys (game-api.md → "Safe Mod Keys")
5. Files >500 lines: targeted search first, don't auto-read fully

# Critical Warnings

- **F12 is Steam screenshot** — never use F12 for mod bindings. Also avoid Shift+Tab (Steam overlay).
- **Mod must be enabled once manually** — user goes to Mods menu in game, enables it. After that it auto-loads.
- **Mods run in Modded/ runtime** — Tolk DLLs go in Modded/ directory, not game root and NOT in the mod folder.
- **Native DLLs NEVER in mod folder** — the game loads ALL .dll files in StreamingAssets/Mods/<ModName>/ as .NET assemblies. Native DLLs (Tolk.dll, nvdaControllerClient64.dll) there will crash the game with "Invalid Image".
- **Tolk_DetectScreenReader() crashes** — the wchar_t* return marshalling causes native crash. Use Tolk_HasSpeech() (bool) instead. Never call Tolk_DetectScreenReader().
- **SetDllDirectory required** — must call SetDllDirectory(Modded/) before any Tolk calls so the native DLLs are found.
- **Keyboard nav not native** — game uses Rewired; arrow keys are only mapped to gamepad, not keyboard. Mod must provide its own keyboard navigation.
- **Game discovers mods from** `StreamingAssets/Mods/<ModName>/*.dll` — Deploy-Mod.ps1 handles this.
- **WildfrostMod has no Update loop** — we create a MonoBehaviour (ModUpdateBehaviour) for per-frame logic.
- **Mod GUID:** `accessibility.wildfrost.screenreader`
- **CustomEventSystem.Press/Release are private** — use ExecuteEvents directly for simulating clicks.
- **UINavigationItem.CheckLayer() is internal** — replicate layer check logic manually (check `ignoreLayers` or `navigationLayer == ActiveNavigationLayer`).
- **Stat is a struct** — `entity.damage`, `entity.hp`, `entity.counter` are never null. Check `.max > 0` not `!= null`.
- **Card.GetDescription() takes CardData** not Entity — use `Card.GetDescription(entity.data)`.
- **Deploy requires game closed** — DLL gets locked by the running game process.

# Session & Context Management

- Feature done or ~30+ messages or ~70%+ context → suggest new conversation. Always update `project_status.md` before ending.
- Check `docs/game-api.md` first before reading decompiled code. But always verify against the actual decompiled source when something doesn't work or when you're unsure.
- After new code analysis → document in `docs/game-api.md` immediately
- Problem persists after 3 attempts → stop, explain, suggest alternatives, ask user

# Key Mod Bindings

- **F1**: Help
- **F10**: Toggle debug mode
- **Arrow keys**: Navigate UI elements
- **Enter**: Activate focused element

# References

Key files: `project_status.md`, `docs/game-api.md`, `docs/ACCESSIBILITY_MODDING_GUIDE.md`. See `docs/` for all guides, `scripts/` for build/deploy.
