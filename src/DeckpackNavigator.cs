using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Keyboard access to the game's inventory overlay (the Deckpack: deck and
    /// reserve cards, collected charms and crowns). P toggles it from any screen
    /// that has one (map, battle, events). While open: Up/Down switch groups,
    /// Left/Right move within a group, Enter on a charm or crown picks it up
    /// and drives the game's CardCharmDragHandler so arrows walk the cards that
    /// can take it and Enter attaches it; Enter on a card opens the game's own
    /// options menu (rename, take crown, move between deck and reserve);
    /// Escape backs out one level at a time.
    /// The overlay never changes the scene, so this runs inside every
    /// NavigableScreenHandler instead of being a screen handler itself.
    /// </summary>
    public static class DeckpackNavigator
    {
        private enum Group { Deck, Reserve, Charms, Crowns, Controls }

        private static bool _wasOpen;
        private static float _openTime;
        private static bool _openAnnounced;
        private static bool _hintSpoken;
        private static Group _group;

        // Scene objects, cached while the pack is open
        private static DeckDisplaySequence _sequence;
        private static CardCharmDragHandler _dragHandler;
        private static DeckSelectSequence _menu;

        // Charm/crown drag state. The display object is destroyed by the game
        // when the upgrade is successfully attached — that's how a drag that
        // ended outside our own key handling is classified.
        private static bool _wasDragging;
        private static UpgradeDisplay _dragDisplay;
        private static string _dragName;
        private static bool _pickupAnnounced;
        private static bool _endAnnounced;

        private static bool _menuWasOpen;

        /// <summary>True while the inventory overlay is open.</summary>
        public static bool IsOpen
        {
            get
            {
                try
                {
                    return MonoBehaviourSingleton<Deckpack>.instance != null && Deckpack.IsOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Forget everything on screen changes — no announcements.</summary>
        public static void Reset()
        {
            _wasOpen = false;
            _openAnnounced = false;
            _sequence = null;
            _dragHandler = null;
            _menu = null;
            _menuWasOpen = false;
            ResetDragState();
        }

        private static void ResetDragState()
        {
            _wasDragging = false;
            _dragDisplay = null;
            _dragName = null;
            _pickupAnnounced = false;
            _endAnnounced = false;
        }

        /// <summary>
        /// Called every frame from NavigableScreenHandler.OnUpdate. Returns true
        /// while the inventory is open — the screen's own input handling must
        /// stay out of the way then.
        /// </summary>
        public static bool RouteInput(NavigableScreenHandler owner)
        {
            // The pause menu overlays the inventory — its handler owns the keys then
            if (GameManager.paused)
                return false;

            bool open = IsOpen;
            if (open != _wasOpen)
            {
                _wasOpen = open;
                if (open) OnOpened();
                else OnClosed(owner);
            }
            if (!open)
                return false;

            // Overview after the game has placed its own initial focus,
            // so the focus announcement doesn't talk over it
            if (!_openAnnounced && Time.unscaledTime - _openTime >= 0.6f)
                AnnounceOverview();

            PollDragTransitions(owner);
            PollMenu(owner);

            // While the rename input has focus, every key belongs to it
            if (NavigationHelper.IsTextInputFocused())
                return true;

            HandleKeys(owner);
            return true;
        }

        private static void OnOpened()
        {
            DebugLogger.LogState("Deckpack", "closed", "open");
            _openTime = Time.unscaledTime;
            _openAnnounced = false;
            _group = Group.Deck;
            _sequence = null;
            _dragHandler = null;
            _menu = null;
            _menuWasOpen = false;
            ResetDragState();
        }

        private static void OnClosed(NavigableScreenHandler owner)
        {
            DebugLogger.LogState("Deckpack", "open", "closed");
            ResetDragState();
            ScreenReader.Say(Loc.Get("deckpack_closed"), interrupt: true);
            // The game often leaves focus in limbo here — put it back on a real
            // item (the first hand card in battle). Stay quiet through the close
            // transition; RequestRefocus announces the item it lands on.
            owner?.SuppressFocusFor(1.5f);
            owner?.RequestRefocus();
        }

        /// <summary>Inventory contents: deck, reserve, charm and crown counts.</summary>
        private static void AnnounceOverview()
        {
            _openAnnounced = true;

            var parts = new List<string> { Loc.Get("deckpack_open") };
            try
            {
                var inventory = References.PlayerData.inventory;
                int deck = inventory.deck?.Count ?? 0;
                int reserve = inventory.reserve?.Count ?? 0;
                int charms = 0, crowns = 0;
                if (inventory.upgrades != null)
                {
                    foreach (var upgrade in inventory.upgrades)
                    {
                        if (upgrade == null) continue;
                        if (upgrade.type == CardUpgradeData.Type.Charm) charms++;
                        else if (upgrade.type == CardUpgradeData.Type.Crown) crowns++;
                    }
                }

                var counts = new List<string> { Loc.Get("deckpack_part_deck", deck) };
                if (reserve > 0)
                    counts.Add(Loc.Get("deckpack_part_reserve", reserve));
                counts.Add(charms == 1
                    ? Loc.Get("deckpack_part_charm_one")
                    : Loc.Get("deckpack_part_charms", charms));
                if (crowns > 0)
                    counts.Add(crowns == 1
                        ? Loc.Get("deckpack_part_crown_one")
                        : Loc.Get("deckpack_part_crowns", crowns));
                parts.Add(string.Join(", ", counts) + ".");
            }
            catch
            {
                // No run inventory to summarize — the group navigation still works
            }

            if (!_hintSpoken)
            {
                _hintSpoken = true;
                parts.Add(Loc.Get("deckpack_hint"));
            }

            ScreenReader.Say(string.Join(" ", parts), interrupt: true);
        }

        // ---- Input ----------------------------------------------------------

        private static void HandleKeys(NavigableScreenHandler owner)
        {
            var dragHandler = GetDragHandler();
            bool dragging = dragHandler != null && dragHandler.IsDragging;
            var menu = ActiveMenu();

            // P closes outright, whatever state the pack is in
            if (Input.GetKeyDown(KeyCode.P))
            {
                DebugLogger.LogInput("Deckpack", "Toggle (P)");
                if (dragging)
                    CancelDrag(dragHandler, owner);
                CloseInventory();
                return;
            }

            // Escape backs out one level: drag, then card menu, then the pack
            if (NavigationHelper.IsBackPressed())
            {
                if (dragging)
                {
                    CancelDrag(dragHandler, owner);
                    return;
                }
                if (menu != null)
                {
                    CloseMenu(menu, owner);
                    return;
                }
                DebugLogger.LogInput("Deckpack", "Close (Escape)");
                CloseInventory();
                return;
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (dragging)
                {
                    ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
                    return;
                }
                owner.InspectFocusedCard();
                return;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                if (dragging) NavigateEligible(dragHandler, dir);
                else if (menu != null) NavigateMenu(menu, dir);
                else Navigate(dir);
                return;
            }

            if (NavigationHelper.IsConfirmPressed())
            {
                if (dragging) ApplyToFocused(dragHandler, owner);
                else if (menu != null) ActivateMenuButton(menu, owner);
                else Confirm(owner);
            }
        }

        // ---- Group navigation ------------------------------------------------

        /// <summary>Up/Down switch groups, Left/Right move within the group.</summary>
        private static void Navigate(NavDirection dir)
        {
            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                SwitchGroup(dir == NavDirection.Down);
                return;
            }

            var items = GetGroupItems(_group);
            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var next = NavigationHelper.NavigateLinear(
                items, navSystem?.currentNavigationItem, dir, vertical: false);
            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        /// <summary>Move to the next non-empty group and focus its first item.</summary>
        private static void SwitchGroup(bool forward)
        {
            const int groupCount = 5;
            for (int i = 0; i < groupCount; i++)
            {
                int next = ((int)_group + (forward ? i + 1 : -(i + 1)) + groupCount * 2) % groupCount;
                var candidate = (Group)next;
                var items = GetGroupItems(candidate);
                if (items.Count == 0) continue;

                _group = candidate;
                ScreenReader.Say(GroupName(candidate, items.Count), interrupt: true);
                NavigationHelper.FocusItem(items[0]);
                return;
            }

            ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
        }

        private static string GroupName(Group group, int count)
        {
            switch (group)
            {
                case Group.Deck:
                    return count == 1
                        ? Loc.Get("deckpack_group_deck_one")
                        : Loc.Get("deckpack_group_deck", count);
                case Group.Reserve:
                    return count == 1
                        ? Loc.Get("deckpack_group_reserve_one")
                        : Loc.Get("deckpack_group_reserve", count);
                case Group.Charms: return Loc.Get("deckpack_group_charms", count);
                case Group.Crowns: return Loc.Get("deckpack_group_crowns", count);
                default: return Loc.Get("deckpack_group_controls");
            }
        }

        private static List<UINavigationItem> GetGroupItems(Group group)
        {
            var items = new List<UINavigationItem>();
            var sequence = GetSequence();
            if (sequence == null) return items;

            switch (group)
            {
                case Group.Deck:
                    AddGroupCards(items, sequence.activeCardsGroup);
                    break;
                case Group.Reserve:
                    AddGroupCards(items, sequence.reserveCardsGroup);
                    break;
                case Group.Charms:
                    AddHolderItems(items, ReflectionUtil.GetField<UpgradeHolder>(sequence, "charmHolder"));
                    break;
                case Group.Crowns:
                    AddHolderItems(items, ReflectionUtil.GetField<UpgradeHolder>(sequence, "crownHolder"));
                    break;
                case Group.Controls:
                    var display = sequence.GetComponentInParent<DeckDisplay>()
                        ?? Object.FindObjectOfType<DeckDisplay>();
                    AddNavItem(items, display != null ? display.backButtonNavigationItem : null);
                    break;
            }
            return items;
        }

        /// <summary>Deck/reserve cards in the grid's own order.</summary>
        private static void AddGroupCards(List<UINavigationItem> items, DeckDisplayGroup group)
        {
            if (group == null || group.grids == null) return;
            foreach (var grid in group.grids)
            {
                if (grid == null) continue;
                foreach (Entity entity in grid)
                    AddNavItem(items, entity != null ? entity.uINavigationItem : null);
            }
        }

        /// <summary>Charms/crowns in their holder's order.</summary>
        private static void AddHolderItems(List<UINavigationItem> items, UpgradeHolder holder)
        {
            var list = ReflectionUtil.GetField<List<UpgradeDisplay>>(holder, "list");
            if (list == null) return;
            foreach (var upgrade in list)
                AddNavItem(items, upgrade != null ? upgrade.navigationItem : null);
        }

        private static void AddNavItem(List<UINavigationItem> items, UINavigationItem item)
        {
            if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                return;
            if (!item.enabled) return;
            if (items.Contains(item)) return;
            items.Add(item);
        }

        // ---- Enter ------------------------------------------------------------

        private static void Confirm(NavigableScreenHandler owner)
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            if (item == null) return;

            // Charm or crown: pick it up for assignment
            var upgrade = item.GetComponentInParent<UpgradeDisplay>();
            if (upgrade == null && item.clickHandler != null)
                upgrade = item.clickHandler.GetComponentInParent<UpgradeDisplay>();
            if (upgrade != null && upgrade.data != null)
            {
                PickUp(upgrade, owner);
                return;
            }

            // Deck/reserve card: open the game's options menu
            Entity entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            if (entity != null)
            {
                OpenCardMenu(entity);
                return;
            }

            // Anything else (the close button)
            NavigationHelper.ActivateCurrent();
        }

        // ---- Charm/crown assignment -------------------------------------------

        private static void PickUp(UpgradeDisplay upgrade, NavigableScreenHandler owner)
        {
            var interaction = upgrade.GetComponent<CardCharmInteraction>();
            var dragHandler = interaction != null && interaction.dragHandler != null
                ? interaction.dragHandler
                : GetDragHandler();
            if (dragHandler == null || dragHandler.IsDragging)
                return;

            // The game refuses charm drags mid-battle — its only feedback is a sound
            if (References.Battle != null && !References.Battle.ended
                && !ReflectionUtil.GetBoolField(dragHandler, "canDragMidBattle", true))
            {
                ScreenReader.Say(Loc.Get("deckpack_battle_blocked"), interrupt: true);
                return;
            }

            // If the game also reads this Enter as its Rewired Select while the
            // charm is hovered, CardCharmInteraction would start a second drag in
            // LateUpdate — disarm it for a frame so exactly one drag begins.
            if (interaction != null)
            {
                interaction.canDrag = false;
                CoroutineManager.Start(RestoreCanDrag(interaction));
            }

            string name = UpgradeName(upgrade.data);
            DebugLogger.LogInput("Deckpack", $"Pick up: {upgrade.data.name}");
            dragHandler.Drag(upgrade);

            var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
            int count = eligible?.Count ?? 0;
            if (count == 0)
            {
                // Don't leave the player inside a drag that can go nowhere
                _endAnnounced = true;
                _wasDragging = true; // the end transition must not re-announce
                dragHandler.CancelDrag();
                ScreenReader.Say(Loc.Get("deckpack_pickup_none", name), interrupt: true);
                return;
            }

            _dragDisplay = upgrade;
            _dragName = name;
            _pickupAnnounced = true;
            ScreenReader.Say(count == 1
                ? Loc.Get("deckpack_pickup_one", name)
                : Loc.Get("deckpack_pickup", name, count), interrupt: true);
            // The game focuses an eligible card on its own — the pickup
            // instructions matter more than that card's name right now
            owner?.SuppressFocusFor(1.5f);
        }

        private static System.Collections.IEnumerator RestoreCanDrag(CardCharmInteraction interaction)
        {
            yield return null;
            yield return null;
            if (interaction != null)
                interaction.canDrag = true;
        }

        /// <summary>All arrows walk the cards that can take the held upgrade.</summary>
        private static void NavigateEligible(CardCharmDragHandler dragHandler, NavDirection dir)
        {
            var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
            var items = new List<UINavigationItem>();
            if (eligible != null)
            {
                foreach (var entity in eligible)
                    AddNavItem(items, entity != null ? entity.uINavigationItem : null);
            }
            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

            bool forward = dir == NavDirection.Right || dir == NavDirection.Down;
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var next = NavigationHelper.NavigateLinear(
                items, navSystem?.currentNavigationItem,
                forward ? NavDirection.Right : NavDirection.Left, vertical: false);
            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        /// <summary>Enter while holding an upgrade: attach it to the focused card.</summary>
        private static void ApplyToFocused(CardCharmDragHandler dragHandler, NavigableScreenHandler owner)
        {
            var dragging = ReflectionUtil.GetField<UpgradeDisplay>(dragHandler, "dragging");
            var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
            Entity entity = FocusedEntity();

            if (dragging == null || entity == null || eligible == null || !eligible.Contains(entity))
            {
                ScreenReader.Say(
                    Loc.Get("deckpack_not_eligible", _dragName ?? Loc.Get("upgrade_charm")),
                    interrupt: true);
                return;
            }

            DebugLogger.LogInput("Deckpack",
                $"Assign {dragging.data?.name} -> {entity.data?.title ?? entity.name}");
            _endAnnounced = true;

            // Keyboard focus never establishes the game's hover — set it directly,
            // Release() assigns to whatever it believes is hovered
            ReflectionUtil.SetField(dragHandler, "hoverEntity", entity);
            dragHandler.Release(dragging);

            ScreenReader.Say(
                Loc.Get("deckpack_applying",
                    _dragName ?? UpgradeName(dragging.data),
                    entity.data?.title ?? ScreenHandler.CleanName(entity.name)),
                interrupt: true);
            owner?.SuppressFocusFor(2f);
            _group = Group.Charms;
        }

        private static void CancelDrag(CardCharmDragHandler dragHandler, NavigableScreenHandler owner)
        {
            string name = _dragName
                ?? UpgradeName(ReflectionUtil.GetField<UpgradeDisplay>(dragHandler, "dragging")?.data);
            DebugLogger.LogInput("Deckpack", "Cancel drag");
            _endAnnounced = true;
            dragHandler.CancelDrag();
            ScreenReader.Say(Loc.Get("deckpack_returned", name), interrupt: true);
            owner?.SuppressFocusFor(1.5f);
        }

        /// <summary>
        /// Watch the drag handler for pickups and drops that didn't come from our
        /// own key handling (mouse or the game's native Select), and voice them.
        /// </summary>
        private static void PollDragTransitions(NavigableScreenHandler owner)
        {
            var dragHandler = GetDragHandler();
            bool dragging = dragHandler != null && dragHandler.IsDragging;
            if (dragging == _wasDragging) return;
            _wasDragging = dragging;

            if (dragging)
            {
                if (_pickupAnnounced) return;
                var display = ReflectionUtil.GetField<UpgradeDisplay>(dragHandler, "dragging");
                _dragDisplay = display;
                _dragName = UpgradeName(display != null ? display.data : null);
                var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
                int count = eligible?.Count ?? 0;
                _pickupAnnounced = true;
                ScreenReader.Say(count == 1
                    ? Loc.Get("deckpack_pickup_one", _dragName)
                    : Loc.Get("deckpack_pickup", _dragName, count), interrupt: true);
                owner?.SuppressFocusFor(1.5f);
            }
            else
            {
                if (!_endAnnounced && _dragName != null)
                {
                    // The game destroys the display when the upgrade was attached;
                    // a surviving display means it went back to the holder.
                    // Recorded as an event: "which charm went where" is worth
                    // replaying with Ctrl+Up.
                    ScreenReader.SayEvent(_dragDisplay == null
                        ? Loc.Get("deckpack_applied", _dragName)
                        : Loc.Get("deckpack_returned", _dragName), interrupt: true);
                }
                ResetDragState();
            }
        }

        // ---- Card options menu (DeckSelectSequence) ----------------------------

        private static void OpenCardMenu(Entity entity)
        {
            CardController controller = null;
            try { controller = entity.display?.hover?.controller; }
            catch { /* display not wired */ }

            if (!(controller is CardControllerDeck) || !controller.enabled || !controller.canPress)
            {
                ScreenReader.Say(Loc.Get("deckpack_card_blocked"), interrupt: true);
                return;
            }

            DebugLogger.LogInput("Deckpack", $"Card menu: {entity.data?.title ?? entity.name}");

            // Keyboard focus never sets the controller's hover — drive its press
            // directly, the same way select-card screens are pressed
            controller.hoverEntity = entity;
            ReflectionUtil.SetField(controller, "pressEntity", entity);
            ReflectionUtil.InvokeMethod(controller, "Press");
            ReflectionUtil.SetField(controller, "pressEntity", null);
            // The menu-open announcement comes from PollMenu once it is visible
        }

        private static DeckSelectSequence ActiveMenu()
        {
            if (_menu == null)
                _menu = Object.FindObjectOfType<DeckSelectSequence>(true);
            return _menu != null && _menu.gameObject.activeInHierarchy ? _menu : null;
        }

        private static void PollMenu(NavigableScreenHandler owner)
        {
            var menu = ActiveMenu();
            bool open = menu != null;
            if (open == _menuWasOpen) return;
            _menuWasOpen = open;
            if (!open) return;

            var entity = ReflectionUtil.GetField<Entity>(menu, "entity");
            string title = entity?.data?.title ?? "";

            var labels = new List<string>();
            foreach (var item in GetMenuItems(menu))
            {
                string label = MenuLabel(menu, item, owner);
                if (!string.IsNullOrEmpty(label) && !labels.Contains(label))
                    labels.Add(label);
            }

            ScreenReader.Say(
                Loc.Get("deckpack_menu_open", title, string.Join(", ", labels)),
                interrupt: true);
            // The game moves focus onto the menu — the option list matters more
            owner?.SuppressFocusFor(2f);
        }

        private static void NavigateMenu(DeckSelectSequence menu, NavDirection dir)
        {
            var items = GetMenuItems(menu);
            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

            bool forward = dir == NavDirection.Right || dir == NavDirection.Down;
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var next = NavigationHelper.NavigateLinear(
                items, navSystem?.currentNavigationItem,
                forward ? NavDirection.Right : NavDirection.Left, vertical: false);
            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        private static void ActivateMenuButton(DeckSelectSequence menu, NavigableScreenHandler owner)
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            if (item == null) return;

            var entity = ReflectionUtil.GetField<Entity>(menu, "entity");
            string title = entity?.data?.title ?? "";

            // Voice the outcome of the buttons whose click gives no other feedback
            string outcome = null;
            if (IsMenuButton(menu, item, "moveDownButton"))
                outcome = Loc.Get("deckpack_moved_reserve", title);
            else if (IsMenuButton(menu, item, "moveUpButton"))
                outcome = Loc.Get("deckpack_moved_deck", title);
            else if (IsMenuButton(menu, item, "takeCrownButton"))
                outcome = Loc.Get("deckpack_crown_taken", title);

            DebugLogger.LogInput("Deckpack", $"Menu button: {item.gameObject.name}");
            NavigationHelper.ActivateCurrent();

            if (outcome != null)
            {
                // Outcome of a deck action — recorded for Ctrl+Up review
                ScreenReader.SayEvent(outcome, interrupt: true);
                owner?.SuppressFocusFor(1.5f);
            }
        }

        private static void CloseMenu(DeckSelectSequence menu, NavigableScreenHandler owner)
        {
            DebugLogger.LogInput("Deckpack", "Close card menu");
            menu.End();
            ScreenReader.Say(Loc.Get("deckpack_menu_closed"), interrupt: true);
            owner?.SuppressFocusFor(1.5f);
        }

        /// <summary>The menu's buttons, left to right — never the card it shows.</summary>
        private static List<UINavigationItem> GetMenuItems(DeckSelectSequence menu)
        {
            var items = new List<UINavigationItem>();
            foreach (var item in menu.GetComponentsInChildren<UINavigationItem>(false))
            {
                if (item == null || !item.isSelectable || !item.enabled
                    || !item.gameObject.activeInHierarchy)
                    continue;
                if (item.GetComponentInParent<Entity>() != null)
                    continue;
                items.Add(item);
            }
            items.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            return items;
        }

        /// <summary>
        /// Label a menu button by which serialized button object it belongs to —
        /// their visuals are icons, so text lookup alone is unreliable.
        /// </summary>
        internal static string MenuLabel(DeckSelectSequence menu, UINavigationItem item, ScreenHandler owner)
        {
            if (IsMenuButton(menu, item, "renameButton")) return Loc.Get("deckpack_option_rename");
            if (IsMenuButton(menu, item, "takeCrownButton")) return Loc.Get("deckpack_option_take_crown");
            if (IsMenuButton(menu, item, "moveDownButton")) return Loc.Get("deckpack_option_move_reserve");
            if (IsMenuButton(menu, item, "moveUpButton")) return Loc.Get("deckpack_option_move_deck");
            return owner != null ? owner.GetButtonText(item) : null;
        }

        private static bool IsMenuButton(DeckSelectSequence menu, UINavigationItem item, string fieldName)
        {
            var root = ReflectionUtil.GetField<GameObject>(menu, fieldName);
            if (root == null) return false;
            return IsUnder(item.gameObject, root)
                || (item.clickHandler != null && IsUnder(item.clickHandler, root));
        }

        private static bool IsUnder(GameObject obj, GameObject root)
        {
            return obj != null && root != null && obj.transform.IsChildOf(root.transform);
        }

        // ---- Descriptions -------------------------------------------------------

        /// <summary>
        /// Deckpack-specific item descriptions, hooked into ItemDescriber:
        /// menu buttons get their role names, and while an upgrade is held the
        /// eligible cards read as assignment targets (charm slots first).
        /// Returns null when the deckpack has nothing special to say.
        /// </summary>
        public static string DescribeItem(UINavigationItem item, ScreenHandler owner)
        {
            if (item == null || !IsOpen) return null;

            var menu = _menuWasOpen ? ActiveMenu() : null;
            if (menu != null
                && (item.GetComponentInParent<DeckSelectSequence>() != null
                    || (item.clickHandler != null
                        && item.clickHandler.GetComponentInParent<DeckSelectSequence>() != null))
                && item.GetComponentInParent<Entity>() == null)
            {
                return MenuLabel(menu, item, owner);
            }

            var dragHandler = _dragHandler;
            if (dragHandler != null && dragHandler.IsDragging)
            {
                Entity entity = item.GetComponentInParent<Entity>();
                if (entity == null && item.clickHandler != null)
                    entity = item.clickHandler.GetComponentInParent<Entity>();
                if (entity != null && entity.data != null)
                    return DescribeAssignTarget(entity);
            }

            return null;
        }

        /// <summary>"Snow Fox, 1 of 3 charm slots used, Charm: ..." — what matters when placing.</summary>
        private static string DescribeAssignTarget(Entity entity)
        {
            var parts = new List<string> { entity.data.title };

            // Slot usage — only meaningful for charms (crowns have their own slot)
            var dragData = _dragDisplay != null ? _dragDisplay.data : null;
            if (dragData == null || dragData.type == CardUpgradeData.Type.Charm)
            {
                try
                {
                    int total = entity.data.charmSlots;
                    try { total += entity.data.customData?.Get("extraCharmSlots", 0) ?? 0; }
                    catch { /* cards without custom data */ }
                    int used = 0;
                    foreach (var upgrade in entity.data.upgrades)
                    {
                        if (upgrade != null && upgrade.type == CardUpgradeData.Type.Charm
                            && upgrade.takeSlot)
                            used++;
                    }
                    parts.Add(Loc.Get("deckpack_target_slots", used, total));
                }
                catch { /* slots stay unspoken if the data is unreadable */ }
            }

            string upgrades = ItemDescriber.DescribeUpgrades(entity.data);
            if (!string.IsNullOrEmpty(upgrades))
                parts.Add(upgrades);

            return string.Join(", ", parts);
        }

        // ---- Open / close --------------------------------------------------------

        /// <summary>P pressed on a screen: open the inventory, or close it if open.</summary>
        public static void ToggleInventory()
        {
            if (IsOpen)
            {
                CloseInventory();
                return;
            }

            // Not from inside the pause menu, and not while holding a battle card
            if (GameManager.paused)
            {
                ScreenReader.Say(Loc.Get("deckpack_unavailable"), interrupt: true);
                return;
            }
            try
            {
                var battleController = Battle.instance?.playerCardController;
                if (battleController != null && battleController.dragging != null)
                {
                    ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
                    return;
                }
            }
            catch { /* no battle */ }

            var characterDisplay = FindCharacterDisplay();
            if (characterDisplay == null || characterDisplay.deckDisplay == null
                || characterDisplay.deckDisplay.displaySequence == null
                || MonoBehaviourSingleton<Deckpack>.instance == null)
            {
                ScreenReader.Say(Loc.Get("deckpack_unavailable"), interrupt: true);
                return;
            }

            // The game disables the backpack button during sequences — respect that
            var button = ReflectionUtil.GetField<UnityEngine.UI.Button>(
                MonoBehaviourSingleton<Deckpack>.instance, "button");
            if (button != null && (!button.interactable || !button.gameObject.activeInHierarchy))
            {
                ScreenReader.Say(Loc.Get("deckpack_blocked"), interrupt: true);
                return;
            }

            DebugLogger.LogInput("Deckpack", "Open");
            characterDisplay.OpenInventory();
            // "Inventory open" plus contents comes from the open transition
        }

        private static void CloseInventory()
        {
            var characterDisplay = FindCharacterDisplay();
            if (characterDisplay != null)
            {
                characterDisplay.CloseInventory();
            }
            else if (IsOpen)
            {
                Deckpack.Close();
                GetSequence()?.End();
            }
            // "Inventory closed" comes from the close transition
        }

        // ---- Scene lookups ---------------------------------------------------------

        private static CharacterDisplay FindCharacterDisplay()
        {
            try
            {
                if (References.Player != null && References.Player.entity != null
                    && References.Player.entity.display is CharacterDisplay display
                    && display.deckDisplay != null)
                    return display;
            }
            catch { /* no run */ }

            foreach (var display in Object.FindObjectsOfType<CharacterDisplay>())
            {
                if (display != null && display.deckDisplay != null)
                    return display;
            }
            return null;
        }

        private static DeckDisplaySequence GetSequence()
        {
            if (_sequence == null)
            {
                var characterDisplay = FindCharacterDisplay();
                _sequence = characterDisplay != null && characterDisplay.deckDisplay != null
                    ? characterDisplay.deckDisplay.displaySequence
                    : Object.FindObjectOfType<DeckDisplaySequence>(true);
            }
            return _sequence;
        }

        private static CardCharmDragHandler GetDragHandler()
        {
            if (_dragHandler == null)
            {
                _dragHandler = ReflectionUtil.GetField<CardCharmDragHandler>(GetSequence(), "charmDragHandler")
                    ?? Object.FindObjectOfType<CardCharmDragHandler>();
            }
            return _dragHandler;
        }

        private static Entity FocusedEntity()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            if (item == null) return null;
            Entity entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            return entity;
        }

        private static string UpgradeName(CardUpgradeData data)
        {
            if (data == null) return Loc.Get("upgrade_charm");
            string title;
            try { title = data.title; }
            catch { title = null; }
            return string.IsNullOrEmpty(title) ? ScreenHandler.CleanName(data.name) : title;
        }
    }
}
