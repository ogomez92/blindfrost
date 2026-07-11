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

**Phase:** Implementation — all main screens have dedicated handlers
**Currently working on:** In-game testing of Town/ContinueRun/Map/Battle handlers (built + deployed 2026-07-11)
**Blocked by:** Nothing — needs an in-game test pass

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

- **Screen Handler Framework** — ScreenHandler base class, ScreenManager (scene detection + routing, incl. overlay scenes like ContinueRun via `SceneManager.IsLoaded`), NavigationHelper (arrow keys, confirm, focus management)
- **NavigableScreenHandler** — shared base: screen announcement with retry, arrow nav, Enter activation, focus-change announcements, nav-layer change detection
- **ItemDescriber** — shared descriptions: card entities (stats + status effects + keyword-expanded text), battlefield slots, town buildings, card pockets (draw/discard pile + count), map nodes
- **MainMenuHandler** — announces "Main Menu", Up/Down navigation, Enter to activate
- **TownHandler (2026-07-11)** — announces Town; Gate says what it will do (start/continue run or tutorial, via `Campaign.CheckContinue`); buildings announced with localized name + state; I key reads building help (`BuildingType.helpKey`)
- **ContinueRunHandler (2026-07-11)** — announces run summary: start date, leader (name/hp/attack), full deck list with duplicate counts; Continue/Back buttons get purpose descriptions; handles missing-content runs
- **MapHandler (2026-07-11)** — scenes Campaign+MapNew; announces map, zone name, current location, available destinations; Left/Right walk nodes in path order (tier/positionIndex), Up/Down reach HUD; nodes announced with name (label TMP), category, state (here/cleared/available/ahead/unreachable) and wave count; M = full map overview, I = node details (per-wave enemy roster + rewards), G = gold
- **BattleHandler (2026-07-11)** — group navigation (Up/Down: hand → your board → enemy board → bell+piles; Left/Right within group); Enter picks up a hand card (reflection: pressEntity + Press()), arrows move over valid targets (game's NavigationStateCard), Enter places (Release()); bell via `RedrawBellSystem.Activate()`; announces turn/phase changes, bell rung; readout keys H hand, B full board, W waves (incl. per-wave units + boss waves), R bell state, T turn, G gold
- **Context-sensitive F1 help** — every handler overrides `GetHelpText()`
- **Generic handler** — curated localized scene names (`scene_*` keys) replace the removed font-size title heuristic (it had announced "Apply 10 Snow" in Town); falls back to TitleSetter/title-named objects, then scene name
- **Localization** — all new strings in en/de/es/fr; other 9 languages fall back to English until translated (existing 12 base keys still have all 13)

## Pending Tests

- **All new handlers (2026-07-11):** Launch game with NVDA, walk the same route as last session:
  1. Main menu → Play → Town: does it announce "Town, your base camp..." (NOT "Apply 10 Snow")?
  2. Focus Gate: does it say "Gate" + "Your tutorial journey is in progress. Press Enter to continue it"?
  3. Press I on a building: does it read the building's help text?
  4. Enter on Gate → ContinueRun overlay: does it announce leader + deck list + "Let's Go" hint? (This tests the new overlay-scene routing.)
  5. Continue → Map: announces zone/current location/destinations? Left/Right walk nodes with sensible names and states (no more "Discard Pocket / Battle / Battle")? M overview? I details with enemies?
  6. Enter on a battle node → Battle: battle announcement, group navigation, H/B/W/R/T/G readouts?
  7. **RISKIEST:** Enter on a hand card — does pickup work (reflection Press())? Then arrows to a slot, Enter to place (Release())? If it fails, check debug log for "[ReflectionUtil]" lines (F10 first).
  8. Draw/discard pockets: announced as "Draw pile, N cards"?

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

- 2026-07-11 (map feedback round): The map "arrows do nothing" report was navigation working but staying silent: with a single revealed node, left/right wraps to the same item and no announcement fired. Fixes:
  - Left/right onto the same node now says "This is the only revealed location right now."; empty map says "The map is not ready yet."
  - Up/down skips the always-empty battle draw/discard pockets rendered on the map HUD ("Draw pile, 0 cards" noise); if nothing remains it says "Nothing else on this screen."
  - `GetMapNodeName` reads ALL banner texts under the node (battle nodes have "Battle" + squad name from BattleData.nameRef) instead of the first only; numeric-only texts skipped. Category word is deduped against the name (no more "Battle, battle").
  - Standing on an uncleared node (run start) now appends "press Enter to enter" — verify Enter actually launches the battle from the current node (uses the node's clickHandler).
- 2026-07-11 (later): Battle feedback round from live testing:
  - **Counter fix:** board cards were described through `DescribeSlot` (entity is a child of its CardSlot, and the slot check ran first in `ItemDescriber.Describe`), which dropped counter/statuses/description. Cascade is now entity-first (parents-only lookup so real slot items still describe as slots).
  - **Slot positions are targeting-only now:** browsing reads the card itself; while holding a card, `DescribeTarget` reads "side, row N, slot N" + short occupant summary (name, health, acts-in, statuses).
  - **Combat narration:** BattleHandler subscribes to `Events.OnEntityPostHit` / `OnEntityKilled` / `OnStatusEffectApplied` — queued announcements "X hits Y for N", "X destroyed", "N snow applied to Y". Filtered to Play/Battle phases. Focus announcements are suppressed while the game auto-moves focus (`phase == Battle` or `!ActionQueue.Empty`) so narration isn't cut off by interrupt-on-focus.
  - **Hints once per session:** `ScreenHandler.HintOnce(key)` — battle_hint, map_hint, continue_hint, town_hint, and the pickup instructions ("arrows choose a target...") speak once per game launch; F1 keeps the full help.
  - Untested in game yet: narration volume during multi-hit turns may need tuning (e.g., collapse repeated hits), and status-application announcements may be chatty — verify live.
- 2026-07-11: Dedicated handlers implemented for Town, ContinueRun (overlay), Campaign map, Battle. Built + deployed. Game-code findings documented in docs/game-api.md sections 13-17.
- **First priority: run the Pending Tests route above.** The reflection-based card play in battle (Press/Release) is the least certain part.
- **Still generic (no dedicated handler yet):** CharacterSelect, Cards (collection), Mods, Settings/PauseMenu, Event scenes (map events), BossReward, BattleWin, CampaignEnd, shops. They get scene-name announcements + generic item descriptions now; dedicate handlers as testing shows need.
- **Translations pending:** new handler strings exist in en/de/es/fr only; ja/ko/zh-Hans/zh-Hant/it/pt/ru/pl/tr fall back to English.
- **Potential issue (unchanged):** `usingMouse = false` may conflict if the user moves the mouse; may need a Harmony patch on MouseInputSwitcher.
- Build note: run scripts directly in pwsh (`& scripts/Build-Mod.ps1`) — the `powershell -ExecutionPolicy Bypass` wrapper gets blocked in this environment.
