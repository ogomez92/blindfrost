using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Key routing while the inventory overlay is open, and group navigation:
    /// P/Escape/I, moving between deck, reserve, charms, crowns and controls,
    /// and the Enter dispatch onto an upgrade, a card or a plain button.
    /// </summary>
    public static partial class DeckpackNavigator
    {
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

    }
}
