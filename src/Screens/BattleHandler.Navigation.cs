using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Focus movement: group switching, the targeting grid built from the
    /// valid drop targets, and collecting each group's navigation items.
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
        /// Grid navigation over the valid targets while holding a card.
        /// Battlefield slots group into their lanes (a lane spans both sides,
        /// so Left/Right can cross from your slots to the enemy's); anything
        /// else (the recall zone, the play-without-target anchor, hand cards
        /// offered as targets) groups by vertical position. Up/Down change row
        /// landing on the horizontally closest target; Left/Right stay in the
        /// row; edges say so instead of wrapping around.
        /// </summary>
        private void NavigateTargeting(NavDirection dir)
        {
            var items = NavigationHelper.GetNavigableItems();
            if (items.Count == 0)
                return;

            var rows = BuildTargetRows(items);
            if (rows.Count == 0)
                return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;

            int rowIdx = -1, colIdx = -1;
            for (int r = 0; r < rows.Count && rowIdx < 0; r++)
            {
                int c = rows[r].IndexOf(current);
                if (c >= 0) { rowIdx = r; colIdx = c; }
            }
            if (rowIdx < 0)
            {
                NavigationHelper.FocusItem(rows[0][0]);
                return;
            }

            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                int nextCol = colIdx + (dir == NavDirection.Right ? 1 : -1);
                if (nextCol < 0 || nextCol >= rows[rowIdx].Count)
                {
                    ScreenReader.Say(Loc.Get("nav_edge"), interrupt: true);
                    return;
                }
                NavigationHelper.FocusItem(rows[rowIdx][nextCol]);
                return;
            }

            int nextRow = rowIdx + (dir == NavDirection.Down ? 1 : -1);
            if (nextRow < 0 || nextRow >= rows.Count)
            {
                ScreenReader.Say(Loc.Get("nav_edge"), interrupt: true);
                return;
            }

            // Hold your place in the line: land on the same column when both
            // rows are battlefield lanes, and on the closest target otherwise
            var from = rows[rowIdx][colIdx];
            UINavigationItem best = null;
            int columnKey = GetTargetColumnKey(from);
            if (columnKey != int.MaxValue)
            {
                foreach (var item in rows[nextRow])
                {
                    if (GetTargetColumnKey(item) == columnKey)
                    {
                        best = item;
                        break;
                    }
                }
            }

            if (best == null)
            {
                float x = from.Position.x;
                float bestDistance = float.MaxValue;
                foreach (var item in rows[nextRow])
                {
                    float distance = Mathf.Abs(item.Position.x - x);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = item;
                    }
                }
            }
            NavigationHelper.FocusItem(best);
        }

        /// <summary>
        /// Arrange targeting items into visual rows, top to bottom, each row
        /// left to right. Slot-bound targets key on their lane index; loose
        /// items (recall zone, hand anchor) cluster by Y position.
        /// </summary>
        private static List<List<UINavigationItem>> BuildTargetRows(List<UINavigationItem> items)
        {
            var laneRows = new Dictionary<int, List<UINavigationItem>>();
            var loose = new List<UINavigationItem>();

            foreach (var item in items)
            {
                int lane = GetTargetLaneIndex(item);
                if (lane >= 0)
                {
                    if (!laneRows.TryGetValue(lane, out var row))
                        laneRows[lane] = row = new List<UINavigationItem>();
                    row.Add(item);
                }
                else
                {
                    loose.Add(item);
                }
            }

            var rows = new List<List<UINavigationItem>>();
            foreach (var row in laneRows.Values)
            {
                // Lanes walk in spoken-column order, so Right always moves to
                // the next column number
                row.Sort((a, b) => GetTargetColumnKey(a).CompareTo(GetTargetColumnKey(b)));
                rows.Add(row);
            }

            // Loose items whose Y positions are close enough share a row
            var looseRows = new List<List<UINavigationItem>>();
            const float rowTolerance = 1.25f;
            loose.Sort((a, b) => b.Position.y.CompareTo(a.Position.y));
            List<UINavigationItem> currentRow = null;
            float currentY = 0f;
            foreach (var item in loose)
            {
                if (currentRow == null || Mathf.Abs(item.Position.y - currentY) > rowTolerance)
                {
                    currentRow = new List<UINavigationItem>();
                    currentY = item.Position.y;
                    rows.Add(currentRow);
                    looseRows.Add(currentRow);
                }
                currentRow.Add(item);
            }

            // The recall zone, the play anchor and hand cards offered as targets
            // have no column, so screen order is all they have
            foreach (var row in looseRows)
                row.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            rows.Sort((a, b) => AverageY(b).CompareTo(AverageY(a)));
            return rows;
        }

        private static float AverageY(List<UINavigationItem> row)
        {
            float sum = 0f;
            foreach (var item in row)
                sum += item.Position.y;
            return row.Count > 0 ? sum / row.Count : 0f;
        }

        /// <summary>
        /// The battlefield lane a targeting item belongs to (same index for
        /// both sides — they are halves of the same visual row), or -1 for
        /// anything not sitting in a lane.
        /// </summary>
        private static int GetTargetLaneIndex(UINavigationItem item)
        {
            var slot = GetTargetSlot(item);
            var lane = slot != null ? slot.GetComponentInParent<CardSlotLane>() : null;
            if (lane == null)
                return -1;
            try
            {
                return References.Battle != null ? References.Battle.GetRowIndex(lane) : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>The battlefield slot a targeting item stands for, or null.</summary>
        private static CardSlot GetTargetSlot(UINavigationItem item)
        {
            var slot = item.GetComponent<CardSlot>() ?? item.GetComponentInParent<CardSlot>();
            if (slot != null)
                return slot;

            var entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            return entity != null ? entity.GetComponentInParent<CardSlot>() : null;
        }

        /// <summary>Sorts the enemy's columns after all of yours.</summary>
        private const int EnemyColumnOffset = 1000;

        /// <summary>
        /// A lane item's place in the spoken order: your columns counting up
        /// from your front line, then the enemy's counting up from theirs.
        /// Both sides number columns from the front, and the game renders your
        /// half mirrored, so your column 1 sits at the right edge of your side —
        /// ordering by screen position would count columns DOWN as you move
        /// right. Items with no column sort last.
        /// </summary>
        private static int GetTargetColumnKey(UINavigationItem item)
        {
            var slot = GetTargetSlot(item);
            var lane = slot != null ? slot.GetComponentInParent<CardSlotLane>() : null;
            if (lane?.slots == null)
                return int.MaxValue;

            int column = lane.slots.IndexOf(slot);
            if (column < 0)
                return int.MaxValue;

            bool isEnemy = References.Battle != null
                && slot.owner != null
                && slot.owner != References.Battle.player;
            return (isEnemy ? EnemyColumnOffset : 0) + column;
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
