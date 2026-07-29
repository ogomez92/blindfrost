using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Focus movement over the four groups — hand, your board, the enemy board,
    /// the system items: the arrow dispatcher, switching between groups, the
    /// focus an overlay hands back, and collecting each group's navigation items.
    /// </summary>
    public partial class BattleHandler
    {
        /// <summary>
        /// While holding a card: the valid targets form a grid — Up/Down move
        /// between rows (staying in the same column), Left/Right move along the
        /// row, and nothing wraps: the edge announces itself instead of jumping
        /// to the far side. Otherwise: Up/Down switch groups, Left/Right move
        /// within the current group.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            if (IsTargeting())
            {
                NavigateTargeting(dir);
                return;
            }

            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                SwitchGroup(dir == NavDirection.Down);
                return;
            }

            var items = GetGroupItems(_group);
            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_group_empty", GetGroupName(_group)));
                return;
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var next = NavigationHelper.NavigateLinear(
                items, navSystem?.currentNavigationItem, dir, vertical: false);
            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        /// <summary>
        /// After an overlay (the inventory) closes, land focus on the first hand
        /// card rather than wherever the game left it. Falls back to any group
        /// with cards if the hand is empty.
        /// </summary>
        protected override UINavigationItem DefaultFocusItem()
        {
            var hand = GetGroupItems(Group.Hand);
            if (hand.Count > 0)
            {
                _group = Group.Hand;
                return hand[0];
            }
            foreach (Group group in new[] { Group.PlayerBoard, Group.EnemyBoard, Group.System })
            {
                var items = GetGroupItems(group);
                if (items.Count > 0)
                {
                    _group = group;
                    return items[0];
                }
            }
            return null;
        }

        /// <summary>Move to the next/previous group and focus its first item.</summary>
        private void SwitchGroup(bool forward)
        {
            const int groupCount = 4;
            for (int i = 0; i < groupCount; i++)
            {
                int next = ((int)_group + (forward ? i + 1 : -(i + 1)) + groupCount * 2) % groupCount;
                var candidate = (Group)next;
                var items = GetGroupItems(candidate);
                if (items.Count == 0) continue;

                _group = candidate;
                ScreenReader.Say(GetGroupName(_group), interrupt: true);
                NavigationHelper.FocusItem(items[0]);
                return;
            }

            ScreenReader.Say(Loc.Get("battle_nothing_to_focus"));
        }

        private string GetGroupName(Group group)
        {
            switch (group)
            {
                case Group.Hand: return Loc.Get("group_hand");
                case Group.PlayerBoard: return Loc.Get("group_your_board");
                case Group.EnemyBoard: return Loc.Get("group_enemy_board");
                default: return Loc.Get("group_system");
            }
        }

        /// <summary>Collect the navigation items belonging to a group, in reading order.</summary>
        private List<UINavigationItem> GetGroupItems(Group group)
        {
            var items = new List<UINavigationItem>();
            var battle = Battle.instance;
            if (battle == null) return items;

            switch (group)
            {
                case Group.Hand:
                    AddContainerItems(items, battle.player?.handContainer);
                    break;

                case Group.PlayerBoard:
                    AddBoardItems(items, battle.player);
                    break;

                case Group.EnemyBoard:
                    AddBoardItems(items, battle.enemy);
                    break;

                case Group.System:
                    AddNavItem(items, RedrawBellSystem.nav);
                    AddNavItem(items, WaveDeploySystem.nav);
                    foreach (var item in NavigationHelper.GetNavigableItems())
                    {
                        if (item.GetComponentInParent<CardPocket>() != null
                            || (item.clickHandler != null
                                && item.clickHandler.GetComponentInParent<CardPocket>() != null))
                        {
                            AddNavItem(items, item);
                        }
                    }
                    break;
            }
            return items;
        }

        private static void AddContainerItems(List<UINavigationItem> items, CardContainer container)
        {
            if (container == null) return;
            foreach (Entity entity in container)
                AddNavItem(items, entity != null ? entity.uINavigationItem : null);
        }

        private static void AddBoardItems(List<UINavigationItem> items, Character character)
        {
            if (character == null) return;
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    AddNavItem(items, occupant != null ? occupant.uINavigationItem : null);
                }
            }
        }

        private static CardSlotLane GetLane(Character character, int row)
        {
            try
            {
                return Battle.instance.GetRow(character, row) as CardSlotLane;
            }
            catch
            {
                return null;
            }
        }

        private static void AddNavItem(List<UINavigationItem> items, UINavigationItem item)
        {
            if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                return;
            if (!item.enabled) return;
            if (items.Contains(item)) return;
            items.Add(item);
        }

    }
}
