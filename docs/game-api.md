# Wildfrost - Game API Documentation

## Overview

- **Game:** Wildfrost
- **Genre:** Roguelike deckbuilder
- **Engine:** Unity (IL2CPP main, Mono modded runtime)
- **Runtime:** net472
- **Architecture:** 64-bit
- **Mod System:** Built-in WildfrostMod + Harmony (no BepInEx/MelonLoader)
- **Localization:** Unity Localization with string tables per locale
- **Input Library:** Rewired

---

## 1. Singleton Access Points

### MonoBehaviourSingleton<T> Pattern

Access via `MonoBehaviourSingleton<T>.instance` (auto-set in Awake):

- `References` — **HUB SINGLETON** — central data access point
- `GameManager` — game ready/paused state, busy task tracking
- `UINavigationSystem` — UI navigation controller
- `CardManager` — card management/pooling
- `Cursor3d` — 3D cursor position, `usingMouse` flag
- `VirtualPointer` — visual gamepad selection indicator
- `Console` — debug console
- `Config` — configuration
- `Transition` — scene transition effects
- `ActionQueue` — queued game actions
- `FloatingTextManager` — floating text effects
- `Names` — name generation
- `Town` — town scene controller
- `Deckpack` — deck management
- `CustomCursor` — cursor rendering
- `Curves` — animation curves
- `StringReference` — string references

### Static Instance Pattern

- `Battle.instance` — current battle (public static)
- `Campaign.instance` — current campaign (public static)
- `InputSystem.instance` — input handler (private static)
- `SceneManager.ActiveSceneKey` — current scene name (static property)
- `SaveSystem.instance` — save/load (private static)
- `SfxSystem.instance` — sound effects (private static)
- `EventManager.instance` — event management (public static)
- `RewiredControllerManager.instance` — controller manager (public static)
- `ScreenSystem.instance` — screen management (private static)
- `PromptSystem.instance` — prompt dialogs (private static)
- `HelpPanelSystem.instance` — help panels (lazy FindObjectOfType)
- `ControllerButtonSystem.instance` — controller input styles (private static)
- `StatusEffectSystem` — static methods + activeEffects list
- `MetaprogressionSystem` — static data dictionary
- `Settings` — static utility class for persistent settings

### References.cs — The Hub

Access: `MonoBehaviourSingleton<References>.instance`

Key static properties:
- `References.PlayerData` — current player save data
- `References.Player` — current player character entity
- `References.Map` — current MapNew display
- `References.Campaign` — current Campaign instance
- `References.Battle` — current Battle instance
- `References.Classes` — available character classes
- `References.Areas` — available areas/regions
- `References.LeaderData` — leader card from deck
- `References.GetCurrentArea()` — player's current area

### Events.cs — Global Event Hub

Static delegates for all major game events:
- `Events.OnSceneLoaded` / `OnSceneChanged` / `OnSceneUnload` — scene transitions
- `Events.OnUINavigation` / `OnUINavigationReset` — UI navigation
- `Events.OnEntityHover` / `OnEntityUnHover` / `OnEntitySelect` / `OnEntityFocus` — entity interaction
- `Events.InvokeBattlePhaseStart(phase)` — battle phase changes
- `Events.InvokeCampaignDeleted()` — campaign deletion
- `Events.InvokeButtonHover(type)` / `InvokeButtonPress(type)` — button interaction

---

## 2. Game Key Bindings (DO NOT override in mod!)

**CRITICAL: These keys are used by the game. The mod must NOT use them!**

### Rewired Actions (9 core + 4 debug)

- **"Select"** — primary confirm/click (gamepad A, mouse left)
- **"Back"** — cancel/back (gamepad B)
- **"Move Horizontal"** — left/right axis (D-pad/stick, arrows on keyboard in gamepad mode)
- **"Move Vertical"** — up/down axis (D-pad/stick, arrows on keyboard in gamepad mode)
- **"Inspect"** — open card details (gamepad Y)
- **"Options"** — open charms/options menu
- **"Scroll Vertical"** — scroll axis (triggers/mousewheel)
- **"Backpack"** — debug only
- **"Redraw Bell"** — debug only
- **"Up"** / **"Down"** — debug menu activation only
- **"Snap"** — card organizer debug tool

### Direct KeyCode Usage (Game)

- **BackQuote (`)** — toggle console
- **F12** — toggle console (ALSO Steam screenshot!)
- **Tab** — console auto-complete
- **F1-F8** — console utility keys
- **Escape** — exit console
- **UpArrow/DownArrow** — console command history
- **Return/Enter** — console execute
- **Ctrl+D** — duplicate card (CardOrganizer debug)
- **Delete** — delete duplicate card (CardOrganizer debug)
- **LeftArrow/RightArrow** — prev/next card (CardViewer)
- **Alt+Return** — toggle fullscreen
- **Space / Mouse2** — debug menu hold combo

### Hold/Repeat Settings

- `holdDirectionStartTime = 0.1f` — initial delay before repeat
- `holdDirectionFlowTime = 0.05f` — repeat interval
- `navigationControllerDeadzone = 0.05f` — stick deadzone

---

## 3. Safe Mod Keys

Keys confirmed NOT used by the game — safe for mod bindings:

- **F1**: Help (mod) — game uses only in console context
- **F9**: Available
- **F10**: Debug mode toggle (mod)
- **F11**: Available
- **Letters (A-Z)**: Available when NOT in console (console checks for BackQuote toggle)
- **Number keys**: Available when NOT in console
- **Ctrl+letter combos**: Most available (Ctrl+D used in CardOrganizer debug only)

**NEVER use:**
- F12 (Steam screenshot + console)
- Shift+Tab (Steam overlay)
- Escape (console, pause menu)
- Arrow keys (CardViewer, console history, gamepad nav)
- Enter/Return (console, fullscreen toggle with Alt)
- Space (debug menu hold combo)
- BackQuote (console toggle)

---

## 4. UI Navigation System

### Architecture

- **UINavigationSystem** (singleton) — master navigation controller
- **UINavigationItem** — individual selectable/hoverable UI element
- **UINavigationLayer** — groups of selectable elements (menus, dialogs)
- **UINavigationHistory** — tracks navigation across layers/items
- **CustomEventSystem** — bridges gamepad/keyboard input to Unity EventSystem

### How Focus Works

Selection via `UINavigationSystem.instance.SetCurrentNavigationItem(item)`:
1. Unhovers previous item via `eventSystem.Unhover()`
2. Hovers new item via `eventSystem.Hover()`
3. Adds to history

Key properties:
- `UINavigationSystem.instance.currentNavigationItem` — currently focused item
- `UINavigationSystem.instance.AvailableNavigationItems` — all registered items
- `Cursor3d.instance.usingMouse` — true = mouse mode (disables nav), false = gamepad mode

### Selection Priority

```
Mega = 10, Highest = 2, High = 1, Medium = 0, Low = -1, Lowest = -2
```

### Navigation Algorithm

Direction-based search: score = distance * 1.0 + angle * 1.0, pick lowest. Max 45-degree angle from direction. Grid threshold = 1.5 units. Looping supported via `canLoop = true`.

### Override System

Items can override navigation for specific directions:
- `overrideInputs = true` + `inputLeft/Right/Up/Down` = hard redirect
- `overrideHorizontal/Vertical = true` → invokes callback with +/-1.0

### Input Flow

```
Gamepad stick/dpad → UINavigationSystem.CheckForNavigation()
→ GetSelectable(direction) → SetCurrentNavigationItem(new)
→ eventSystem.Hover() → Events.InvokeUINavigation()

Select button → eventSystem.Press(clickHandler) → pointerDown/Click events
```

### Mouse vs Gamepad Switching

`InputSwitcher` detects input method each frame:
- Mouse click → MouseInputSwitcher: `Cursor3d.usingMouse = true`, hides VirtualPointer
- Gamepad axis → ControllerInputSwitcher: `Cursor3d.usingMouse = false`, shows VirtualPointer
- Touch → TouchInputSwitcher: similar to mouse

When mouse mode active, UINavigationSystem clears selection (`SetCurrentNavigationItem(null)`).

### Navigation States

Stack-based via `NavigationState.Start(state)` / `BackToPreviousState()`:

- **NavigationStateIdle** — no restrictions, all items selectable
- **NavigationStateWait** — hides pointer, optionally disables input
- **NavigationStateBattle** — disables non-battle items
- **NavigationStateCard** — target selection mode, boosts valid targets to Mega
- **NavigationStateMuncher** — muncher interaction
- **NavigationStateAssignUpgrade** — charm assignment mode

### Interception Points for Modding

```csharp
Events.OnUINavigation += MyCallback;
Events.OnUINavigationReset += MyCallback;
Events.OnEntityHover += (entity) => { };
Events.OnEntityUnHover += (entity) => { };
Events.OnEntitySelect += (entity) => { };

UINavigationSystem.instance.SetCurrentNavigationItem(item); // Force selection
NavigationState.Start(myState); // Custom state
```

---

## 5. Scene/Screen System

### SceneManager

- `SceneManager.ActiveSceneKey` — current active scene identifier (string)
- `SceneManager.ActiveSceneName` — current active scene name
- `SceneManager.Load(sceneKey, SceneType, onComplete)` — load scene
- `SceneManager.Unload(sceneKey)` — unload scene
- `SceneManager.IsLoaded(sceneKey)` — check if loaded

### Scene Types

- `Persistent` (0) — never unloaded: Camera, Global, Input, Systems, Saving, PauseScreen
- `Active` (1) — only one at a time: MainMenu, Campaign, Battle, Town, etc.
- `Temporary` (2) — unloaded when new Active loads

### Known Scene Names

- **"MainMenu"** — main menu screen
- **"CharacterSelect"** — character selection
- **"ContinueRun"** — continue existing run
- **"Town"** — town/hub screen
- **"Cards"** — card collection/upgrades
- **"Campaign"** — world map display
- **"Battle"** — battle screen
- **"Mods"** — mod manager screen
- **"PauseScreen"** — pause menu (persistent, overlays all)
- **"Credits"** — credits (temporary)
- **"TownUnlocks"** — town unlock dialogs (temporary)
- Persistent: "Camera", "Global", "Input", "Systems", "Saving"

### How to Detect Current Screen

```csharp
// Method 1: Scene name
string screen = SceneManager.ActiveSceneKey;

// Method 2: Check references
if (References.Battle != null) { /* In battle */ }
if (References.Campaign != null) { /* In campaign */ }
if (References.Map != null) { /* On map screen */ }

// Method 3: Scene events
Events.OnSceneChanged += () => { string scene = SceneManager.ActiveSceneKey; };
```

### Bootstrap Sequence

1. Load persistent: Camera, Global
2. Then: Input, Systems, Saving, PauseScreen
3. Start scene: "MainMenu" via `Transition.To()`

---

## 6. Main Menu

### Buttons

- **playButton** (GameObject) — `MainMenu.Play()` → tutorial or Town
- **continueButton** (GameObject) — continue existing run
- **modsButton** (GameObject) — mod manager screen

Additional (from Menu base / PauseMenu):
- **Settings** — `MainMenu.Settings()` → PauseMenu overlay
- **Credits** — `MainMenu.Credits()` → temporary Credits scene
- **Quit** — `Menu.ExitGame()` → `GameManager.Quit()`

### Button Components

Each button has:
- `ButtonAnimator` — visual states (hover/unhover/press/release, colors, tweens)
- `UINavigationItem` — controller navigation
- `EventPointerClick` / `EventPointerDown` — click handlers with UnityEvent

### Button Visual States

```csharp
ButtonAnimator.Hover()    // Focus: highlight color + tween
ButtonAnimator.UnHover()  // Unfocus: normal color
ButtonAnimator.Press()    // Click: press tween
ButtonAnimator.Release()  // Release: release tween
buttonAnimator.interactable = false; // Grays out + disables nav
```

---

## 7. Card System

### CardData (ScriptableObject — card definition)

Key fields:
- `title` (property) — localized card name (from `titleKey.GetLocalizedString()`)
- `titleFallback` — fallback if titleKey empty
- `textKey` (LocalizedString) — card ability text
- `flavourKey` (LocalizedString) — flavour text
- `damage` (int) — attack value
- `hp` (int) — health value
- `counter` (int) — turn counter before acting
- `uses` (int) — uses for items
- `attackEffects` (StatusEffectStacks[]) — effects on attack
- `startWithEffects` (StatusEffectStacks[]) — passive effects
- `traits` (List<TraitStacks>) — keywords/traits
- `upgrades` (List<CardUpgradeData>) — charms applied
- `cardType` (CardType) — Friendly, Enemy, Item, etc.
- `playType` (Card.PlayType) — None, Play (item), Place (ally)
- `targetMode` (TargetMode) — how card targets
- `needsTarget` (bool) — requires a target

### Entity (Card instance in play)

Key runtime properties:
- `entity.data` — CardData reference
- `entity.damage.current` / `.max` — current/max attack
- `entity.hp.current` / `.max` — current/max health
- `entity.counter.current` / `.max` — turn counter
- `entity.uses.current` / `.max` — uses
- `entity.statusEffects` — List<StatusEffectData> applied effects
- `entity.traits` — List<TraitStacks> keywords
- `entity.alive` — is alive
- `entity.inPlay` — is spawned in battle
- `entity.owner` — Player or Enemy character
- `entity.containers` — current containers (hand/board/deck)
- `entity.silenced` — effects suppressed
- `entity.InHand()` — in hand container
- `entity.GetAllies()` / `entity.GetAllEnemies()` — allied/enemy cards

### Card Display (Card.cs MonoBehaviour)

- `card.titleText` (TMP) — card name
- `card.descText` (TMP) — card description
- `Card.GetDescription(entity)` — full formatted description with effects/traits/bonuses

### Enumerating Cards

```csharp
// Hand
foreach (Entity card in player.handContainer) { }

// Board (all cards for character)
List<Entity> board = Battle.GetCardsOnBoard(player);

// Board (specific slot)
CardSlotLane row = Battle.instance.GetRow(player, 0); // row 0 or 1
Entity card = row.slots[0].GetTop(); // column 0, 1, or 2

// All player cards (hand + board + deck + discard + reserve)
List<Entity> all = Battle.GetCards(player);

// Check location
bool inHand = card.InHand();
bool onBoard = Battle.IsOnBoard(card);
```

### Status Effects (StatusEffectData)

- `count` — stack count
- `visible` — shown in UI
- `offensive` — negative effect
- `GetAmount()` — amount accounting for bonuses/factor/silence
- `GetDesc(amount, silenced)` — localized description
- Apply: `StatusEffectSystem.Apply(entity, applier, effectData, count)`

### Traits (TraitData + KeywordData)

```csharp
foreach (Entity.TraitStacks trait in entity.traits)
{
    string name = trait.data.keyword.title; // localized keyword name
    int count = trait.count;
    bool silenced = trait.silenced;
}
```

---

## 8. Battle System

### Battle Phases

```csharp
public enum Phase { None=0, Init=1, Play=2, Battle=3, End=4, LastStand=5 }
```

- `Battle.instance.phase` — current phase
- `Battle.instance.player` / `.enemy` — Character references
- `Battle.instance.winner` — battle victor
- `Battle.instance.turnCount` — current turn

### Turn Flow

1. UpdateBoard (arrange both sides)
2. UpdateContainer (position hand)
3. **Phase.Play** — player selects cards
4. WaitForTurnEnd — wait for player input
5. **Phase.Battle** — units attack
6. CheckUnitsTakeTurns — counter-based attacks
7. ProcessUnits (enemy then player) — attacks execute
8. ProcessTurnEnd — end-of-turn effects

### Board Layout

2 rows x 3 columns per character = 6 slots each. Access via:
```csharp
Battle.instance.GetRow(character, rowIndex) // 0 or 1
row.slots[columnIndex] // 0, 1, or 2
```

### Character Containers

```csharp
character.handContainer   // cards in hand
character.drawContainer   // deck
character.discardContainer // discard pile
character.reserveContainer // reserve
```

---

## 9. Localization System

### Architecture

Unity Localization with Harmony patches in `LocalizationHelper.cs`.

### Reading Localized Strings

```csharp
// Card name (auto-localized)
string name = cardData.title;

// Current language
string langCode = LocalizationSettings.SelectedLocale.Identifier.Code;

// Listen for language changes
LocalizationSettings.Instance.OnSelectedLocaleChanged += (Locale newLocale) => { };
```

### String Tables

- **"Cards"** — card titles, descriptions, flavour
- **"Card Text"** — status effect descriptions
- **"Tooltips"** — keyword titles and descriptions
- **"UI Text"** — UI labels, unlock descriptions
- **"Challenges"** — challenge titles/text/rewards

### Mod String API (Builder pattern)

```csharp
// Add custom localized strings
StringTable table = LocalizationHelper.GetCollection("Cards", new LocaleIdentifier(SystemLanguage.English));
table.SetString("mymod.key", "My Value");
```

### Language Persistence

```csharp
Settings.Save("Language", locale.Identifier.Code);
string saved = Settings.Load("Language", "");
```

---

## 10. Input System Details

### InputSystem.cs

Static wrapper around `Rewired.Player`:
- `InputSystem.mainPlayer` — Rewired Player instance
- `InputSystem.Enabled` — gates on `!Transition.Running`
- `InputSystem.IsSelectPressed/Held/Released()` — Select action shortcuts
- `InputSystem.IsButtonPressed(string action, bool positive)` — any action check
- `InputSystem.GetAxis(string action)` — analog axis value
- `InputSystem.CheckLongHold()` — hold-to-repeat for navigation

### RewiredHotKeyController

Wrapper for UI button hotkeys:
- `HotKeyString` — Rewired action name
- `hasKeyboardInput` + `keyboardAction` — keyboard fallback KeyCode
- Auto-detects mouse vs controller mode

---

## 11. Harmony Patch Points

### Recommended Patches

- **Scene transitions**: Postfix on `Transition.To()` or subscribe to `Events.OnSceneChanged`
- **UI navigation**: Subscribe to `Events.OnUINavigation`, `Events.OnEntityHover/UnHover`
- **Battle phase**: Subscribe to `Events.InvokeBattlePhaseStart`
- **Button hover/press**: `Events.InvokeButtonHover`, `Events.InvokeButtonPress`

### Pattern: Hooking Entity Hover

```csharp
Events.OnEntityHover += (Entity entity) =>
{
    string name = entity.data.title;
    int hp = entity.hp.current;
    ScreenReader.Say($"{name}, {hp} HP");
};
```

---

## 12. Known Issues / Problems

- Game uses IL2CPP for main exe but Mono for Modded runtime
- Tolk DLLs must be in Modded/ directory (not game root or mod folder)
- Tolk_DetectScreenReader() crashes — use Tolk_HasSpeech() instead
- SetDllDirectory(Modded/) required before Tolk calls
- Arrow keys used by CardViewer and console — mod navigation must be careful
- Mouse mode disables gamepad navigation entirely
- Native DLLs in mod folder crash game ("Invalid Image")

---

## 13. Town Scene

- **No `Gate` class.** The gate is a `Building` prefab ("Gate(Clone)") + `BuildingInteraction` (pointer handler, calls `Town.SelectBuilding(building)`) + `UINavigationItem`.
- `Building`: `type` (BuildingType), `built`, `buildStarted`, `canSelect`, `HasUncheckedUnlocks`, private `onSelect` UnityEvent (wired in scene YAML).
- `BuildingType`: `titleKey` (LocalizedString, name), `helpKey` (LocalizedString, packed `title|body|note` — split on `'|'`), `helpEmoteType`.
- **Gate action logic** (`Menu.StartGameOrContinue`): tutorial run in progress → ContinueRun; `tutorialProgress <= 1` → tutorial offer; `Campaign.CheckContinue(GameModeNormal)` → ContinueRun; else `StartGame` (new run).
- `Campaign.CheckContinue(gameMode)`: save exists + result == None + "data" exists.
- Buildings: TribeHut, PetHut, InventorHut, IcebreakerHut, ChallengeShrine, Balloon (daily, `BuildingBalloon`), TownHall.
- Town tutorial uses `HelpPanelShower` → `HelpPanelSystem.Show(key)` — **text is set before activation** (safe to read on activation). One-shot via `tutorialTownDone`.
- **"Apply 10 Snow" trap:** inactive tooltip/prompt widgets (Tooltip, CardTooltip, HelpPanelSystem, Prompt, CardInspector) retain stale TMP text — never scrape scene TMP text heuristically for titles.

## 14. Overlay Scenes (Temporary, do NOT change ActiveSceneKey)

`SceneType.Temporary` scenes load on top of the active scene; `SceneManager.ActiveSceneKey` stays unchanged. Detect via `SceneManager.IsLoaded(key)`:
- **"ContinueRun"** (over Town), **"BossReward"**, **"BattleWin"**, **"CampaignEnd"** (over Battle), **"TownUnlocks"**, **"Credits"**.

## 15. ContinueRun Screen

- Controller: `ContinueScreen` (all fields private serialized → reflection): `cardContainer` (CardContainer with the deck entities), `titleText`, `dateText` (start date, set by code), `progressText` (never assigned), `continueButton` ("Let's Go!"), `backButton` (active if `GameMode.canGoBack`), `missingDataDisplay`, `giveUpHelpShower`.
- Deck loads async in `Start()` — wait until `cardContainer.Count > 0`.
- **Leader** = deck card with `data.cardType.miniboss == true` (same rule as `References.LeaderData`).
- Buttons: `Continue()` → `menu.GoTo("Campaign")`; `Close()` → unload scene; `PromptGiveUp()` → confirm panel → deletes campaign.

## 16. Campaign Map (scenes "Campaign" + "MapNew")

- Data model: `Campaign.instance.nodes` (`List<CampaignNode>`, id == index). Visual: `References.Map` (MapNew); `References.Map.FindNode(campaignNode)` → `MapNode`.
- Player position: `Campaign.FindCharacterNode(References.Player)`.
- `CampaignNode`: `id, tier, positionIndex, areaIndex, position, connections (List<Connection>{otherId}), revealed, cleared, glow, finalNode, characters, data (Dictionary<string,object>), type (CampaignNodeType)`. `GetDesc()` = reward tooltip text.
- `CampaignNodeType`: `letter` ("battle", "area1"...), `zoneName` (plain string, NOT localized), flags `isBattle, isBoss, mustClear, canSkip, interactable, startRevealed`. Category via subclass name (`CampaignNodeTypeShop` etc.).
- **Valid move** = target is current node or in `current.connections`, AND (current.cleared or !current.type.mustClear). `MapNode.reachable` (public) = BFS-reachable.
- **Node display name:** authored in prefab — read TMP under `MapNodeLabel` (private `label` field; `GetComponentInChildren<MapNodeLabel>(true)`). Battle nodes: `node.data["battle"]` → `AddressableLoader.Get<BattleData>("BattleData", name)` → `nameRef` (LocalizedString), `waveCounter`.
- **Enemy roster:** `node.data.GetSaveCollection<BattleWaveManager.WaveData>("waves")` → per wave `.Count` + `.PeekCardData(j)`.
- **Zone name:** on non-interactable label nodes (`type.letter.StartsWith("area")`, match `areaIndex`) — read their MapNode's TMP text.
- Events: `Events.OnMapNodeHover/UnHover/Select/Reveal(MapNode)`, `Events.PostBattle(CampaignNode)`.
- Map HUD comes from persistent "UI" scene: `CharacterDisplay` (goldDisplay, deckDisplay, handOverlay). Gold: `References.Player.data.inventory.gold.Value` (SafeInt — reference DeadSafe.dll!).

## 17. Battle Interaction (playing cards)

- Controller: `Battle.instance.playerCardController` (public field, runtime type `CardControllerBattle`).
- Public fields: `hoverEntity`, `dragging`, `hoverContainer`, `hoverSlot`. Protected: `pressEntity`, `Press()`, `Release()` (reflection needed).
- **Pickup flow:** focus card nav item (sets hoverEntity via event system Hover cascade) → set `pressEntity` = entity → invoke `Press()` → `dragging != null` = picked up, game enters `NavigationStateCard` (only valid targets/slots selectable).
- **Place flow:** focus target (sets hoverSlot/hoverEntity) → invoke `Release()` → dispatches by `playType` (Place → ActionMove; Play → ActionTrigger/Against) + `ActionEndTurn`.
- Cards in hand: `battle.player.handContainer` (iterate for entities, `entity.uINavigationItem`).
- Board: `Battle.instance.GetRow(character, 0|1)` returns `CardContainer` (actually `CardSlotLane`, `.slots` List<CardSlot>, `slot.GetTop()`).
- **Redraw bell:** `RedrawBellSystem` (FindObjectOfType) — `counter` (Stat), `IsCharged` (counter.current <= 0 = free ring), `interactable` (Play phase only), `Activate()` public, `RedrawBellSystem.nav` static.
- **Waves:** `References.Battle.enemy.GetComponent<BattleWaveManager>().list` — `Wave { counter, units (List<CardData>), isBossWave, spawned }`. HUD: `WaveDeploySystem` (private `counter` int = turns to next deploy; `WaveDeploySystem.nav` static). No player-facing "skip wave" — early deploy is automatic when enemy board empty.
- **No End Turn button** — turn ends via card play / recall / bell (each enqueues `ActionEndTurn`). Programmatic: `ActionQueue.Add(new ActionEndTurn(Battle.instance.player))`.
- Events: `Events.OnBattlePhaseStart(Battle.Phase)`, `OnBattleTurnStart/End(int)`, `OnRedrawBellHit(RedrawBellSystem)`, `OnBattleStart/End`, `OnBattleWin`.
- **Status effects:** `entity.statusEffects` (List<StatusEffectData>) — `visible`, `count`, `GetAmount()`, name via `AddressableLoader.Get<KeywordData>("KeywordData", effect.keyword).title`. Attack payload: `entity.attackEffects` (StatusEffectStacks: data + count).
- Post-win flow: optional "BossReward" → "BattleWin" (both Temporary); lose/end → "CampaignEnd".

## 18. Moving, Swapping, Recalling Units (free actions)

- **Board units are draggable like hand cards**: `CardControllerBattle.Press()` only requires `pressEntity.owner == owner` — the same reflection path (set `pressEntity`, invoke `Press()`) works for units on the board.
- **Valid drop targets while dragging** come from `NavigationStateCard.Begin()`: it disables every nav item except containers where `entity.CanPlayOn(container)` is true. For a `PlayType.Place` card that means own board slots (empty = move, occupied = swap/shove via `ShoveSystem`).
- **Recall**: `Entity.CanPlayOn(CardContainer)` explicitly returns true for `owner.discardContainer` when `entity.CanRecall()` (extension in `EntityExt`: `data.cardType.canRecall && blockRecall <= 0 && Battle.IsOnBoard(entity)`). So the discard pile nav item (`container.nav`) stays enabled as a drop target. `Release()` over it queues `ActionMove(entity, discardContainer)` + `ActionEndTurn` and fires `Events.InvokeDiscard`. `Sequences.CardRecall` confirms the unit goes to the **discard pile** (not hand) — no heal found in code.
- **Free actions**: `Character.freeAction = true` is set when the released unit came from the board (move, swap, recall) — `Battle` then skips consuming the turn in its end-turn check. Ringing the bell also sets it (`RedrawBellSystem`).
- **Release outcome detection**: every successful release queues actions; an invalid drop leaves `ActionQueue` empty and the card snaps back (`dragging` is nulled either way by `DragEnd()`, so check the queue, not `dragging`).
- **Cancel pickup**: `CardController.DragCancel()` is public; the `CardControllerBattle` override tweens the card home and restores the navigation state. Game maps it to Rewired "Back" (`CancelCardDragSystem`).
- **"Use on hand" anchor**: `CardControllerBattle.useOnHandAnchor` (public UINavigationItem) is the gamepad drop zone for no-target plays; disabled while the held card `NeedsTarget`.

## 19. Triggers (reactions, Smackback, Last Stand)

- All card activations run through `ActionProcessTrigger.Run()` which fires `Events.OnEntityTrigger(ref Trigger)` before processing and `OnEntityTriggered` after.
- `Trigger` fields: `entity` (who acts), `triggeredBy`, `targets`, `type` (string), `nullified`.
- Known `type` values: `"basic"` (default, incl. counter-reaching-zero and played cards), `"smackback"` (`StatusEffectTriggerAgainstAttackerWhenHit`; `triggeredBy` = the attacker being retaliated against), `"laststand"` (`LastStandSystem`), `"eat"` (`StatusEffectInstantEatSomething`).
- **Snow skips triggers**: `ActionProcessTrigger` does not process when `trigger.entity.IsSnowed` (public: `Entity.IsSnowed => SnowAmount() > 0`). Snow blocks counter reduction by consuming `hit.counterReduction` (`StatusEffectSnow`).
- Played-from-hand cards arrive with `triggeredBy = owner.entity` (the leader) — compare against `entity.owner?.entity` to distinguish real reaction chains.

## 20. Gold / Kill Combos

- `KillComboSystem`: kills of enemy units in the same turn count up; at `count >= 2` each further kill fires `Events.InvokeDropGold(amount, "Combo", References.Player, pos)` then `Events.InvokeKillCombo(count)`. Combo counter resets on turn end.
- `Events.OnDropGold(int amount, string source, Character owner, Vector3 position)` — all battle gold drops.

## 21. Crowns & Charms

- `CardData.upgrades` (`List<CardUpgradeData>`), `CardUpgradeData.type`: enum `Type { ..., Charm = 1, Crown = 3 }`, localized name via `.title` (property wrapping `titleKey`).
- Convenience: `CardData.HasCrown`, `GetCrown()`, `crownSlots`.
- Crowned cards deploy pre-battle: `Battle.WaitForChampionsToDeploy` enables the card controller before turn 1 while crowned cards are in hand — the normal pickup/place flow works during this window.

## 22. Pause Menu / Settings (scene "PauseScreen", persistent)

- Loaded at bootstrap (`Bootstrap.thenLoad`) and **never changes ActiveSceneKey**; `SceneManager.IsLoaded("PauseScreen")` is always true. Detect it being open via `GameManager.paused` (public static, set in `PauseMenu.OnEnable/OnDisable`; `Time.timeScale` is 0 while open — use unscaled time).
- `PauseMenu : Menu` (public `Open/Close/Toggle`, `Settings()`, `BattleLog()`, `OpenLorePages()`); find via `Object.FindObjectOfType<PauseMenu>(true)`. `Open()` respects a static `blocked` counter. The main menu's settings button calls the same instance.
- Tabs: `JournalTab` with public `Hover/Press/Release/Select`; `Release()` fires `Select()` only while hovered.
- Setting rows: `SettingOptions : Setting<int>` (TMP_Dropdown; public `Increase/Decrease/Add(float)`), `SettingSlider : Setting<float>` (UI Slider). Persistence via `SetSettingInt/SetSettingFloat` components (`Set(value)` saves; `OnEnable` loads).
- **Gamepad value adjustment** goes through `UINavigationItem.overrideHorizontal` + `OnHorizontalOverride` (`UnityEvent<float>`) — invoke with plus or minus 1f to change a focused row's value exactly like the game does.
- **Stats page** ("Overall Statistics"): `OverallStatsDisplay` (OnEnable → Populate) writes the whole page into a few large TMP blocks — private `nameGroups[]` / `valueGroups[]`, lines separated by `<br>`, blank `<br>` lines as group separators; centred locales (zh/ko/ja) inline the value into the name block and leave `valueGroups` empty. Per-row `StatDisplay` components are used by other stat panels (end of run), NOT this page.

## 22b. Battle bells & Last Stand

- **Bell nav items are serialized references, not children**: `RedrawBellSystem.nav` and `WaveDeploySystem.nav` (statics, assigned from private `navigationItem` fields in OnEnable/Awake) do NOT live under their system components — `GetComponentInParent` from the nav item finds nothing. Identify by identity comparison against the static fields. `WaveDeploySystemOverflow` also assigns `WaveDeploySystem.nav`.
- **Standard wave bell** `WaveDeploySystem`: private `waveManager` (BattleWaveManager, `list` public), `currentWave`, `counter`. The Overflow variant has its own `waves`/popup fields (already documented behavior in ItemDescriber).
- **Last Stand** (`Battle.Phase.LastStand`, `LastStandSystem`): stacks an ActionSequence that shows a modal (`background`/`button` private GameObjects) and blocks on `WaitUntil(diceRolled)`; public `Roll()` sets it. The Roll button belongs to NO navigation layer — unreachable via nav items. Dice outcome lands in private int `result` (-1 pending / 0 player wins / 1 enemy wins), reset to -1 after the kill sequence; statics `subject`/`attacker`/`previousPhase` on the class. Attack stat shown on cards is `entity.damage + entity.tempDamage` (SafeInt, temporary Spice/Frost/ongoing modifiers) — `EntityDisplay.cs:95`.
- **Injuries**: `Events.OnCardInjured` (`UnityAction<CardData>`) fires when a companion gains an injury (`InjurySystem`); persistent on `CardData.injuries`.
- **Card upgrade types**: `CardUpgradeData.Type` = None/Charm/Token/Crown — Token is a real, displayed category (own holder on the card).

## 23. Change History

- 2026-03-29: Initial creation with structure overview
- 2026-03-29: Complete Tier 1 analysis — input system, UI navigation, singletons, scenes, cards, battle, localization
- 2026-07-11: Town, ContinueRun, overlay scenes, campaign map, battle interaction, status effects (sections 13-17)
- 2026-07-13: Unit moving/swapping/recall, trigger system, kill combos, crowns/charms, pause menu and settings (sections 18-22)
- 2026-07-13: Stats page rendering, bell nav-item identity, Last Stand system, tempDamage, injuries, Token upgrades (sections 22, 22b)
