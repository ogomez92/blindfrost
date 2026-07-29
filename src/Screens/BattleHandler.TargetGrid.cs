using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The grid of valid drop targets while a card is held: arranging them into
    /// visual rows, stepping along and between those rows without wrapping, and
    /// the lane and column keys that ordering rests on.
    /// </summary>
    public partial class BattleHandler
    {
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
    }
}
