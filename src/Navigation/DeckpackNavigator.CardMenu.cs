using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The game's own card options menu (DeckSelectSequence) — opening it,
    /// navigating and activating its buttons, labelling them — plus the
    /// deckpack-specific item descriptions hooked into ItemDescriber.
    /// </summary>
    public static partial class DeckpackNavigator
    {
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

    }
}
