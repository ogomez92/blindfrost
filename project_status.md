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
**Currently working on:** In-game testing of battle mechanics round 2 (unit move/swap/recall, triggers, pause menu/settings) — built + deployed 2026-07-13
**Blocked by:** Nothing — needs an in-game test pass

### Pending test: self-enable on first run (added 2026-07-13)

The mod now enables itself on the first game start after installation — no Mods menu needed, save file untouched. Constructor writes the GUID into `lastSavedMods` before `Bootstrap.ModsSetup` reads it, then drops `autoenable.marker` in the mod folder so a deliberate disable in the Mods menu sticks. The release package no longer ships a replacement Save.sav; Install-Mod.ps1 no longer touches AppData.

Test protocol (simulates a fresh player):
1. Close the game.
2. Delete `autoenable.marker` from the deployed mod folder (if present).
3. Disable the mod: temporarily rename `%USERPROFILE%\AppData\LocalLow\Deadpan Games\Wildfrost\Profiles\Default\Save.sav` to `Save.sav.test-backup` (fresh-save simulation; restore afterwards).
4. Launch the game. Expected: speech on the main menu without visiting the Mods menu, and `Log.log` contains "First run: mod self-enabled, will load this boot".
5. Close the game, restore the real Save.sav, delete the test save. Mod stays enabled there because the real save already lists the GUID.

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
- **Unit moving/swapping/recall (2026-07-13)** — Enter on an own board unit picks it up (same reflection Press path; game allows any owned entity); arrows walk the game's valid targets incl. the recall zone (discard pile — `CanPlayOn` whitelists it for recallable units); Enter places: free slot = move, occupied = swap/shove, recall zone = off the board. Free actions announced ("your turn continues"). Escape cancels a pickup via public `DragCancel()`. Success detected via ActionQueue (not `dragging`, which nulls either way)
- **Trigger narration (2026-07-13)** — `Events.OnEntityTrigger`: announces who acts and why — snowed units skipping their action, nullified triggers, Smackback retaliation ("X smacks back at Y"), Last Stand, reaction chains ("X is set off by Y"), plain "X acts" otherwise
- **Snow clarity (2026-07-13)** — units with a counter + snow stacks read "counter frozen by Snow" when browsing and targeting
- **Crowns & charms (2026-07-13)** — card descriptions include "Crowned, deploys at battle start" and charm names (`CardData.upgrades`); battle announcement warns when crowned cards in hand need placing before the fight
- **Combo gold (2026-07-13)** — `OnKillCombo`/`OnDropGold`: "Combo x2!" plus gold amounts earned in battle
- **PauseMenuHandler (2026-07-13)** — pause menu/settings/battle log/lore accessible: routed via `GameManager.paused` (menu lives in the always-loaded PauseScreen scene, invisible to scene-key routing — this was why settings were completely inaccessible). O key opens/closes it anywhere (guarded against text input focus). Up/Down move, Enter activates buttons/tabs (JournalTab Hover/Press/Release sequence), Left/Right adjust setting rows via the game's own `OnHorizontalOverride` gamepad path; dropdown values and slider percentages announced

- **Full screen-coverage round (2026-07-16, from three-agent audit of game code vs mod):**
  - **Overlay routing:** Credits, CreditsEnd, CardFramesUnlocked, NewFrostGuardian, JournalNameHistory, Mods, DemoEnd added to `_overlayScenes` (they load as Temporary scenes that never change ActiveSceneKey — before this the PREVIOUS screen's handler stayed active and arrows drove invisible UI).
  - **New handlers:** BossRewardHandler (Enter opens the chest via `GainBlessingSequence2.StartOpen()`, arrows browse `BossRewardSelect` tokens, Enter takes one via its reflected `InputAction.Run()`); CardFramesUnlockedHandler (reads banner + card list, Enter = `End()`); TownUnlocksHandler (reads each `GainUnlockSequence` panel's title/description, Enter = `Close()`); FrostoscopeHandler (announces + reads boss roster, Enter = `End()`, second Enter force-unloads); JournalNameHandler (says add vs crossed-out); CreditsHandler (announce + Enter/Escape skips by unloading the scene — both loaders just wait for unload).
  - **Enter→Select simulation (SelectSimulator + Harmony postfix on `InputSystem.IsButtonPressed`):** several sequences poll Rewired "Select" which keyboard can never press (CombineCardSequence's continue prompt, FinalBossSequenceSystem's possession prompts) — hard dead-ends before. OverlayWatcher detects those two overlays (cinema bars active + object present/running), swallows arrows, and Enter arms the simulator (hover cleared first so the simulated Select can't click anything else).
  - **VisualNarrator:** `SpeechBubbleSystem.OnCreate` (ALL town/muncher/shop speech bubbles — real localized text, never read before); `Events.OnMinibossIntro`; Harmony prefixes on `WaveDeploySystem.Activate` (wave arrival), `CardAnimationClunkerBossChange.Routine` (boss transform), `FinalBossSequenceSystem.Flee/PossessLeader/BlockWisp` + `FrostEyeSystem.Create` postfix (player team only) for the shade cinematic, `CombineCardSystem.CombineSequence` (combine start).
  - **Dead-end fixes:** BattleWinHandler Enter forces `_sequence.End()` after 8s if the continue layout never appears; CampaignEndHandler gets pseudo-button navigation (collects Buttons under buttonsLayout/restartButton/scoresButton, first Enter selects, second activates) in case its end buttons are not nav items.
  - **Event feed support:** EventScreenHandler can now pick up cards with a plain CardController (the muncher!) — same reflection Press/Release as battle; Escape = DragCancel.
  - **Polish:** inspect-panel announcements now include the unit's greeting bubble text; character select announces the tribe stage and stage changes go through SayEvent; lore reads + deckpack outcomes recorded in the Events buffer; sprite-only card descriptions fall back to flavour text; screen announcements can no longer be stuck silent forever (12s cap speaks the handler name); GenericScreenHandler has help text.
  - New loc keys in en/de/es/fr (others fall back to English).

## Pending Tests

- **Screen-coverage round (2026-07-16, v-next):**
  1. **Boss reward:** beat a boss with rewards — "Boss reward selection" + prompt announced? Enter opens the chest ("It creaks open..."), ~2s later "The rewards are revealed..."? Arrows read "Option 1 of 3: ..." for each token, Enter takes one ("Taken: ...") and the remaining-count prompt updates?
  2. **Card frames:** after a run that earned frames, entering Town announces "Card frames unlocked!" + banner + card names, Enter continues?
  3. **Town unlocks:** with a pending unlock, each panel reads "Unlocked: <building>..." and Enter advances to the next / back to Town?
  4. **Frostoscope reveal:** after beating a final boss, the NewFrostGuardian scene announces itself and reads the next boss roster; Enter exits (second Enter force-unloads if stuck)?
  5. **Journal vignette:** starting a run (name written) and dying (name crossed out) both get a spoken line; screen passes on its own ~2s later?
  6. **Credits:** Main menu → Credits announces + Enter skips back to the menu (menu re-announces)? True-ending credits skippable the same way?
  7. **Combine:** collect a combo (e.g. the Sunlight/Moonlight items) — "Your cards swirl together..." at the start, bar title + prompt read at the end, Enter continues (this was a HARD STUCK before)?
  8. **Final boss shade:** on a boss kill, "A dark wisp... flees into the storm."? At the final boss: possession narration, prompts read, Enter advances the prompts?
  9. **Muncher:** arrow to a deck card, Enter picks it up ("X picked up" + hint), arrows to the muncher, Enter feeds ("X given") — and the muncher's speech bubbles are read? Escape cancels a pickup?
  10. **Speech bubbles:** town building greeters / shopkeeper lines read aloud as they appear, one at a time (not a burst)?
  11. **Battle narration:** miniboss arrival ("A powerful enemy slams onto the battlefield..."), wave deploy ("The wave bell tolls!"), boss transform line on phase change — and not spammed?
  12. **Character select (normal mode):** tribe stage announced ("Choose your tribe..."), then leaders on stage change, chosen unit's greeting read on the confirm panel?
  13. **Regression:** BattleWin continue, CampaignEnd summary + buttons, pause menu, map, shops still behave as before.

- **Character select round 2: dedicated handler (2026-07-13):**
  1. Town gate → "Choose your Leader": announced as "Choose your leader..." with browse hint?
  2. Arrow to a leader, Enter — hear "<Name> chosen. Press Enter to confirm, or Escape to put the card back."?
  3. Enter again — "Let's go! Starting the journey." and the tutorial/run actually starts?
  4. Escape instead — "Choice cancelled, back to browsing.", the card returns, arrows + Enter work again on all three leaders?
  5. Normal mode later: tribe flags still selectable, pet stage announced ("Choose your starting pet"), pet pick + confirm works end to end?
  6. Card reward screens / companion map events (pick 1 of 3) — Enter selects a card, the confirm panel announces the unit, Enter takes it into the deck (panel TakeCard), Escape puts it back? (Panel handling is now generic in NavigableScreenHandler, all screens.)

- **Stats page + bells + audit fixes (2026-07-13, v0.5.0):**
  1. **Stats page:** Journal → Stats tab: Up/Down reads each stat as "Time Played: 00:02:31", "Turns Taken: none" etc., left page first then right page, in visual order?
  2. **Redraw bell:** Focus the bell in the System group — reads "Redraw Bell" + effect text + charged/not-charged state (NOT just "Bell")?
  3. **Wave bell:** Focus the skull wave counter — "Wave bell. N enemies arriving in M turns"?
  4. **Last Stand:** Trigger a last stand (leader/miniboss survives death) — announced with dice-roll hint, Enter rolls, win/lose announced?
  5. **Spice/Frost attack:** A unit with Spice or Frost reads the boosted/reduced attack number (matching the shown icon)?
  6. **Injury:** Lose a companion in battle with injuries enabled — "X has been injured!"?

- **Battle mechanics round 2 + pause menu (2026-07-13):**
  1. **Settings (fix round 2):** Main menu → Settings (or O anywhere): "Game menu" announced, and Up/Down now walk the actual settings page — sub-tabs (Video etc.) AND rows with label + value? Left/Right on a row change the value and speak it? Enter on side tabs switches pages and announces? O closes. **Watch for double-activations** (a tab or button firing twice per Enter). If anything is still off: F10, open the menu, F9, arrow around, F9 again, close game, report — the log now contains a full navigation dump.
  2. **Move a unit:** In battle, arrow to one of your board units, Enter — "X picked up from the board" + move hint? Arrows offer own slots + recall zone only? Enter on a free slot: "X moved. Free action, your turn continues." — and the turn really doesn't advance?
  3. **Swap/shove:** Enter on an occupied own slot while holding a unit — units swap/shove, same free-action announcement?
  4. **Recall:** While holding a board unit, arrow to the discard pile — announces "Recall zone..." (+ game's recall keyword text)? Enter: "X recalled. Free action..."? Verify the unit lands in the discard pile.
  5. **Escape:** With a card picked up, Escape — "X put back", card returns, nothing played? (Watch for the game ALSO opening the pause menu on Escape — if so, we need to swallow/reorder.)
  6. **Triggers:** Let an enemy countdown reach zero — "X acts" before the hit narration? Attack a Smackback unit — "X smacks back at Y!"? Snow a unit until its trigger is skipped — "X is snowed and cannot act"?
  7. **Snow readout:** Focus a snowed unit — counter line ends with "counter frozen by Snow"?
  8. **Combo:** Kill 2+ enemies in one turn — "N gold. Combo x2!"?
  9. **Crowns/charms:** A crowned/charmed card reads "Crowned, deploys at battle start" / "Charm: ..."? Battle start with crowned cards in hand announces the deploy prompt, and placing them works before turn 1?
  10. Regression: hand-card pickup/placement, H/B/W/R/T/G readouts, map/town/continue flows from the 2026-07-11 list still fine?

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
- O: Open/close the game menu (settings, battle log, lore) — inactive while a text field is focused
- Game menu: Up/Down page items, Left/Right tabs (or setting values), T jump to tabs, Escape back one level, Tab/Shift+Tab step page
- Escape (battle, only while holding a card): cancel the pickup
- Battle readouts: H hand, B board, W waves, R bell, T turn, G gold

## Notes for Next Session

- 2026-07-13 (round 13, character select part 2 — the "Let's Go!" trap): Card press now works (Log.log 21:25: "PRESSING [Leader_Tutorial1]! :D") but the user was hard-stuck on the inspect/confirm panel for 40 s — no nav lines at all after the press. Cause: `SelectLeader.Select` opens `InspectNewUnitSequence`, whose `UINavigationLayer` becomes the active override layer; the panel's buttons (Let's Go! / X / rename) are apparently not UINavigationItems (no default-item search fired when the layer registered), the pressed card is the only item left on the layer (arrows wrap silently onto itself), the card's controller is now disabled (Enter dead), and Escape has no game-side keyboard path there. Fix: new dedicated `CharacterSelectHandler` (registered for scene "CharacterSelect"): Enter while the panel runs calls public `CharacterSelectScreen.Continue()` (= Let's Go: pet stage or start run), Escape calls `sequence.End()` + re-enables the stage's cardController (leader vs pet decided by `petSelection.running`); announces panel open ("<Name> chosen..."), cancel, pet stage start (rising edge of `petSelection.running`), and run start (`loadingToCampaign` flag). Focus announcements suppressed while the panel runs (SuppressFocusAnnouncements) and 1.5 s after cancel so panel messages aren't talked over. New Loc keys charselect_* + help_charselect in en/de/es/fr. Follow-up same day (round 14): panel handling generalized into NavigableScreenHandler — a 4 Hz poll (FindObjectOfType&lt;InspectNewUnitSequence&gt;, cached) detects any running inspect panel on ANY screen (companion map events, naked-gnome secret, pet choice). Enter → virtual ConfirmInspectPanel: default calls the panel's public TakeCard() when its cardSelector is wired (adds unit to deck, fires selectEvent → event routine completes); CharacterSelectHandler overrides it to CharacterSelectScreen.Continue() (its panel has no cardSelector — TakeCard would NRE). Escape → CancelInspectPanel: sequence.End() + re-enables every disabled CardControllerSelectCard in the scene (blanket is safe: only select-card screens have these panels, organizer/battle controllers untouched). Focus chatter suppressed while a panel runs and 1.5 s after cancel.
- 2026-07-13 (round 12, character select Enter): Enter on a leader card did nothing. Root cause (decompiled-code confirmed, matches round-10 EnterDiag finding that Enter is NOT Rewired "Select"): leader/pet/reward cards are world-space entities whose CardHover implements only pointer ENTER/EXIT — no down/click handlers — so `ActivateCurrent()`'s ExecuteEvents pointer path was a no-op; and the game only presses cards via `CardController.Update` polling Rewired "Select" against `hoverEntity`, which Enter never triggers. Fix: `NavigationHelper.TryPressSelectCard` — if the focused item resolves to an Entity whose `display.hover.controller` is a `CardControllerSelectCard`, set hoverEntity + pressEntity (reflection), invoke `Press()`, then `Release()` next frame (Release is what fires `pressEvent` → `SelectLeader.Select`). Scoped to CardControllerSelectCard on purpose: CardControllerCardOrganizer would start drags, battle cards already have their own path in BattleHandler. Guards: controller enabled + canPress, card not flipped, pressEntity still ours at release (no double-fire if a future binding makes Enter = Select).
- 2026-07-13 (round 11, stats/bells/coverage audit — v0.5.0):
  - **Stats page fix:** `OverallStatsDisplay` renders the whole page into a few big TMP blocks (`nameGroups`/`valueGroups`, lines split by `<br>`; centred locales inline values into the name block) — no per-row components, so the virtual-row scan found nothing. New `AddOverallStatRows` splits and re-pairs the blocks; "-" values read as "none". VirtualRow gained an `Order` tiebreak because List.Sort is unstable and all lines of a block share one anchor.
  - **Bell fix:** `RedrawBellSystem`/`WaveDeploySystem` nav items are serialized references, NOT hierarchy children — `GetComponentInParent` never found the systems, so bells read as "Bell" (log had 0 "Focused: Redraw"). `ItemDescriber.Describe` now identity-matches `item == RedrawBellSystem.nav` / `WaveDeploySystem.nav` first (Overflow variant also writes `WaveDeploySystem.nav`). Added `DescribeWaveBell(WaveDeploySystem)` for the standard wave system (only the Overflow variant existed).
  - **Full mechanics audit run (subagent, decompiled code vs mod).** Fixed from it: Last Stand phase announced + Enter rolls the dice (`LastStandSystem.Roll()`; Roll button is in NO nav layer — result watched via private `result` field, win/lose announced); attack readouts now include `tempDamage` (Spice/Frost/ongoing — game shows `damage + tempDamage`) via `ItemDescriber.GetShownAttack` in DescribeEntity/SummarizeEntity/DescribeSide; Token-type upgrades read on cards and standalone (were silently dropped / mislabeled "charm"); `Events.OnCardInjured` announced ("X has been injured!").
  - **Known gaps accepted for now (audit severity 3-4):** dynamic `effectBonus`/`effectFactor` not applied to spoken card-text numbers (permanent charm boosts ARE baked in); traits gained/silenced mid-battle not reflected (mod reads `CardData` traits, not `entity.traits`); reaction units without counters have no "reacts" cue; Frenzy re-triggers narrated generically; `entity.uses` not read; non-discard special pockets labeled "draw pile".
- 2026-07-13 (round 10, from live log 11:55): Round-9 lore code never ran — the log showed `navFocus=.../LorePageWildfrost(Clone)/Animator/Button`: lore pages ARE real focusable nav items, so they went through the CONTENT path (generic GetButtonText → "Lore") and the virtual-row dedupe skipped my LorePage rows. Fixes: GetItemDescription and Confirm now check FindInParents<LorePage> on content items — described as CleanName(gameObject.name) ("Lore Page Wildfrost") + locked/new + read hint; Enter runs OpenLorePage (Select + read story sorted top-to-bottom). **EnterDiag verdict: rewiredSelect=False — Enter is NOT the game's Select.** The old phantom clicks were Unity Submit (armed uGUI selection), now permanently cleared; the gameClicks branch in Confirm is effectively dead code but harmless. unitySelected=null in diag = the clearing works.
- 2026-07-13 (round 9: lore pages fixed): "lore lore lore" — each LorePage's ENTIRE story canvas is the button's face (scaled down; Select() just reparents/enlarges it), so text-joining read the same header from every page. Now: list entries are "Lore page N: <CleanName(pageData.name)>" + locked/new state (pageData is null while locked). Enter (virtual-row activate) calls Select() then reads ALL texts under page.canvas aloud (up to 40 blocks) + "Escape closes the page". Escape in GoBack() closes an open lore view first (LorePageManager.DisableFocusLayer, focusLayer field via reflection) before the normal back path. Added UnityEngine.UIModule reference to csproj (Canvas type). Story text blocks are sorted top-to-bottom (y desc, x asc tiebreak) before reading — uniform canvas scaling keeps relative positions valid mid-open-tween. Verify live: pageData asset names make decent titles; Escape close announces.
- 2026-07-13 (round 8: virtual rows generalized to all journal pages):
  - VirtualRow struct (Text, optional Activate action, Anchor transform). Sources: ChallengeEntry (text+progress), BattleLogEntry (log lines), StatDisplay (stats page), LorePage (activatable — Enter calls page.Select(); locked/new state announced; JournalPageData has no title so the row's own texts are used with "Lore page" fallback).
  - Dedupe: entries whose hierarchy contains a focusable content item are skipped (no double announcements). Rows sorted column-major with the same comparer as content.
  - Enter on a virtual row activates it or says "This entry is read-only." While standing on a virtual row the game hover is CLEARED (NavigationHelper.ClearHover) so a game-side Enter can't click the previously focused item; hover re-syncs when back on real items. _virtualIndex resets when the real focus moves, on page/layer change, and on menu enter.
  - Untested: lore reading view (after Enter on an unlocked page) — its content has no typed row component; if it reads nothing, add a generic paragraph-text source for the open view. Cards/Charms/EnemyCards pages assumed navigable as real card entities (ItemDescriber path) — verify.
- 2026-07-13 (round 7: settings values + challenges page):
  - Max FPS / Vsync values didn't read: the child-search missed their controls. `GetSettingValue` now reads the serialized private fields (`SettingOptions.dropdown`, `SettingSlider.slider`) via reflection first — the Setting component always references its control, wherever it sits in the prefab. Extra fallback: `DescribeRowTexts` joins all distinct TMP texts in the row ("Max FPS, 60"), used for both focus announcements and after adjusting.
  - Challenges page read nothing because `ChallengeEntry` rows have NO UINavigationItems at all (display-only even for gamepad; builder creates no nav items). New concept in PauseMenuHandler: **virtual rows** — Up/Down now walks focusable content first, then continues into read-only entries (active ChallengeEntry components, sorted top-to-bottom, described as text + progress). `_virtualIndex` tracks position; reset on enter/layer change. Pattern is reusable for other read-only pages (stats, lore) by extending GetVirtualRows.
  - Focus is never silent anymore: NavigableScreenHandler announces the cleaned GameObject name when a description comes back empty.
  - Note: the challenges list likely scrolls — virtual rows read regardless of scroll position, but visual scroll won't follow; revisit if the user wants parity. Stats/lore pages probably need their own virtual-row sources.
- 2026-07-13 (round 6: the phantom clicker — full log read of 09:55 session):
  - What WORKS now (log-confirmed): tab labels all correct (Menu/Settings/Challenges/Cards/Enemy Cards/Charms/Lore Pages/Stats Page), Left/Right walks tabs, Up/Down walks settings content (Back + Game Options/Visual Effects/Audio/Video), Escape (Input: Back) closed the menu.
  - What's BROKEN: every Enter triggers an invisible click on the WRONG object BEFORE our handler runs (log ordering proves it): "Loading Scene Mods" then our Confirm while focus was Settings; Credits loaded the same way 3 times across sessions; the journal closed on Enter twice with our handler never logging input (the click closed it, GameManager.paused flipped, ScreenManager unrouted before HandleInput ran).
  - Prime suspects, all now either neutralized or instrumented:
    1. Unity uGUI Submit: StandaloneInputModule fires Submit (Enter) at EventSystem.currentSelectedGameObject; any Button stays selected after a pointer-down (including from our own ActivateCurrent!) and gets invisibly re-clicked by every later Enter. Fix: `NavigationHelper.ClearUnitySelection()` — selection cleared after our clicks, in SyncHoverToFocus (per-frame in pause), guarded to never clear text-input focus.
    2. Mouse mode: `Cursor3d.usingMouse` starts true and returns on any physical mouse click; in mouse mode the game hovers/Select-clicks whatever is under the PHYSICAL cursor, which a blind user never aims. Fix: `EnsureControllerMode()` now enforced every frame from Main.OnUpdate (mouse effectively disabled while the mod runs).
    3. Rewired "Select" possibly mapped to Enter (game clicks its hover); hover is force-synced to our focus so this now clicks the RIGHT thing (worst case: benign double-activation of the same item).
  - **New diagnostic: with F10 debug on, EVERY Enter press logs an "EnterDiag" line**: rewiredSelect=?, rewiredBack=?, navFocus, gameHover, unitySelected. The next log names the phantom definitively — check those lines FIRST. If rewiredSelect=true → remove our manual clicks in pause (double-activation); if unitySelected shows a button at a phantom → selection-clearing needs to run more aggressively (every frame globally, not just pause).
- 2026-07-13 (pause menu fix round 5: T dead inside settings, wrong tab activated, no way out):
  - **T said "nothing to focus" inside a settings category** — sub-pages swap in their own navigation layer; the side tabs are off-layer there, so they are genuinely unreachable (the game's own gamepad flow requires Back first). Fix: Escape now triggers the game's back action — finds the `BackButtonGamePadController` for the active layer (public fields: uINavigationLayer, backButton, OnBackButtonOverride) and invokes it; falls back to PauseMenu.Close() so Escape always leads out. T with no tabs now says "No tabs reachable here. Press Escape to go back."
  - **"Enemy Cards" tab activated but Game Options opened** — stale-hover click variant 2: when the focused item has NO clickHandler, SyncHoverToFocus used to skip, leaving the game's hover on the previously hovered object; Enter (= game Select) clicked that. Fix: sync now UNHOVERS when the focused item has no clickHandler, so game Select can't click leftovers. Tab activation also always runs the manual deterministic Hover/Press/Release on the resolved JournalTab (double-select of the same tab is harmless) and logs "Select tab: <object name>" — next log shows announced-vs-clicked tab if labels are still mismatched.
  - F9 dump now flags "noClick" items and prints which JournalTab object an item resolves to (JournalTab:<name>).
- 2026-07-13 (pause menu fix round 4, tab-trap + screenshot): User got trapped cycling the tab strip with Up/Down — the journal's tabs are a vertical column on the book's left edge, so Y-sorted linear nav kept landing on them. Screenshot confirmed the layout: tab strip on left edge; LEFT page = settings category list (Game Options, Visual Effects, Audio, Video); RIGHT page = the selected category's value rows (Display Mode/Max FPS/Vsync, each with little arrow buttons). New navigation model in PauseMenuHandler:
  - Up/Down (and Tab/Shift+Tab): page CONTENT only, column-major order (left page top-to-bottom, then right page) via x-bucketed sort (bucket = world x / 2 — verify bucket size feels right in-game).
  - Left/Right: switch tabs — except on a setting row (overrideHorizontal), where they adjust the value.
  - T: jump to the tab strip (needed because a page of only setting rows has no Left/Right route out).
  - Hints/help updated in en/de/es/fr.
  - Note: the value rows shown in the screenshot suggest the rows exist alongside the category list (right page updates per selected category); the earlier dump didn't show them registered — if Up/Down still doesn't reach rows, F9 inside the settings page and check whether row nav items are on a different layer or unregistered.
- 2026-07-13 (pause menu fix round 3, after second live test with F9 dump): "Settings opened" then arrows dead. Dump showed:
  - Main menu buttons (Play/Credits/Mods/Settings/Exit, layer Canvas) stay REGISTERED behind the open journal; journal items (side tabs, Back, settings category sub-tabs Video/VFX/Audio/Game) are on layer "Menu". The actual setting VALUE rows were not registered at all — they presumably only appear after opening a category (Enter on Video etc.).
  - **Enter loaded the Credits scene again** (2nd time, both logs show "Loading Scene Credits" at the exact Enter press). Mechanism: Enter doubles as Rewired "Select"; the game's CustomEventSystem clicks its HOVERED object. `SetCurrentNavigationItem` only updates hover on forceHover layers — the journal's "Menu" layer isn't one, so the hover stayed stale on a main-menu button behind the book. Credits' own empty "Canvas" layer then became active → GetItems empty → arrows silent.
  - Fixes: (1) `NavigationHelper.SyncHoverToFocus()` — reflection into UINavigationSystem.eventSystem/CustomEventSystem.current, forces hover onto the focused item; called after every FocusItem AND every frame while the pause handler is active. Enter can no longer hit anything behind the menu. (2) PauseMenuHandler.Confirm is adaptive: if `InputSystem.IsSelectPressed()` is true on our Enter frame, the game performs the click itself and we only announce (prevents double-activation); manual click path kept for when Enter is not mapped to Select. (3) Empty-item navigation now says "Nothing to focus here" instead of silence (all handlers). (4) Tab/Shift+Tab navigate in the pause menu (user instinctively pressed Tab). (5) Tab labels read from the JournalTab's own children (fallback: object name, TabCards → "Cards") — several tabs previously all read the page title "Journal". (6) F9 dump now prints layer instance IDs (two different layers were both named "Canvas").
  - Watch next test: whether "(game click)" appears in the input log lines — that confirms Enter=Select mapping; if tabs then double-announce or double-toggle anywhere, remove the manual click path entirely. And check whether opening a settings category (Enter on Video) registers the value rows as nav items (F9 inside it if not).
- 2026-07-13 (pause menu fix round, after live test): Settings were still broken — log analysis (Log.log 07:38) showed:
  - `UINavigationSystem.Update()` clears any focused item that fails the active-layer check EVERY FRAME and re-focuses a default item. Our `GetItems()` filtered to children of the PauseMenu transform, but the settings rows are NOT under it — so we only ever offered the book's side tabs (which pass the layer check via ignoreLayers) while the game kept reassigning focus: the "stuck cursor". Fix: PauseMenuHandler now uses the base GetItems (= exactly the game's active-layer items) and never fights the game's focus.
  - Opening Settings from the main menu already selects the settings page (game default-focused "Video"); arrowing away to the side tabs and pressing Enter on "Journal" left the settings page. With row values/labels now reachable this shouldn't recur.
  - Tab descriptions mis-detected: JournalTab lookup searched children, so container items containing tabs read as "Journal, tab". Fix: parent-chain lookup only; same tightening for dropdown/slider value lookup (row's Setting component first, then own children — never whole panels).
  - **Unexplained + must watch next test:** at 07:38:43, the Enter press that selected our "Journal" tab ALSO made the game load the Credits scene (line "SceneManager → Loading Scene Credits" right before our "Select tab"). Suspicion: the game processes Enter/Return as Rewired "Select" against ITS current hover/focus (CustomEventSystem clicks the hovered clickHandler on Select), which had silently drifted back to the main menu's Credits button while our off-layer focuses were being cleared. Now that our focus always matches the game's, both handlers should act on the SAME item — but watch for double-activations (tabs toggling twice, buttons firing twice). If doubles appear: drop our ExecuteEvents click when the game's Select handles it.
  - **New diagnostic: F10 (debug mode) then F9 dumps the full navigation state** (active layer, current item, every registered nav item with path/layer/flags/components) to the mod log. If settings are still wrong, do: open menu → F9 → arrow around → F9 again → send log.
- 2026-07-13 (mechanics coverage round): Made the remaining core mechanics keyboard-accessible and narrated, based on a strategy-guide checklist (free unit movement/swapping, recall, snow tempo control, triggers like Smackback, crowns/charms, combo gold):
  - Board units can now be picked up with Enter exactly like hand cards (the game's own Press() allows any owned entity; our old `InHand()` check was the only blocker). The game's NavigationStateCard already restricts arrows to valid drop targets, including the recall zone.
  - Recall zone (discard pile) is described as such while dragging, appending the game's localized "recall" KeywordData body when available. NOTE: code shows recall sends the unit to the **discard pile** and no +5 heal exists in Release/CardRecall — the guide's "heals 5 HP" claim was NOT encoded; verify in-game before documenting player-facing behavior.
  - Free actions (board move/swap/recall) are announced as "your turn continues"; success is detected via ActionQueue because `dragging` nulls on both success and snap-back.
  - Triggers: OnEntityTrigger narration incl. snowed-skip (ActionProcessTrigger skips snowed entities), smackback, laststand, chains. May be chatty in dense turns — tune after live test.
  - **Settings inaccessibility root cause:** the pause menu lives in the persistent "PauseScreen" scene, so scene-key routing never fired. New PauseMenuHandler routes via `GameManager.paused`; O toggles the menu. Left/Right on setting rows invoke `UINavigationItem.OnHorizontalOverride` (the gamepad path). Unknowns to verify live: does the pause layer become ActiveNavigationLayer (else the hierarchy fallback kicks in); does Escape natively toggle the pause menu on keyboard (Rewired "Back" mapping is in data assets, not code).
  - Untested edge: scrolling in the settings list if rows overflow the view; battle log tab content is only generically readable.

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
