using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Battlefield geography: a slot's side/row/position, the targeting read while a
    /// card is held, who stands opposite a slot, and the recall drop zone.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>
        /// Describe a battlefield slot: side, row, position, and occupant if any.
        /// </summary>
        public static string DescribeSlot(CardSlot slot)
        {
            var parts = new List<string>();

            string position = GetSlotPosition(slot);
            if (!string.IsNullOrEmpty(position))
                parts.Add(position);

            // Describe occupant or empty state
            if (slot.Empty)
            {
                parts.Add(Loc.Get("slot_empty"));
            }
            else
            {
                var occupant = slot.GetTop();
                string summary = SummarizeEntity(occupant);
                parts.Add(summary ?? Loc.Get("slot_occupied"));
            }

            return parts.Count > 0 ? string.Join(", ", parts) : Loc.Get("slot_empty");
        }

        /// <summary>
        /// "Your row 1 2" / "Enemy row 1 2" — a battlefield slot's location with
        /// the side folded into the row, so browsing slots no longer repeats
        /// "your side, enemy side". The trailing number is the slot; both row and
        /// slot are 1-based. Falls back to a bare "Row 1, Slot 2" when the side
        /// is unknown, and to "Your side" when the row can't be resolved.
        /// </summary>
        public static string GetSlotPosition(CardSlot slot)
        {
            bool hasSide = slot.owner != null && References.Battle != null;
            bool isPlayer = hasSide && slot.owner == References.Battle.player;

            int rowIndex = -1;
            int slotIndex = -1;
            var lane = slot.GetComponentInParent<CardSlotLane>();
            if (lane != null)
            {
                if (References.Battle != null)
                    rowIndex = References.Battle.GetRowIndex(lane);
                slotIndex = lane.slots.IndexOf(slot);
            }

            // Best case: side and row both known — "Your row 1 2"
            if (hasSide && rowIndex >= 0)
            {
                string row = Loc.Get(isPlayer ? "slot_your_row" : "slot_enemy_row", rowIndex + 1);
                return slotIndex >= 0 ? row + " " + (slotIndex + 1) : row;
            }

            // Partial information: fall back to the separate side / row / slot pieces
            var parts = new List<string>();
            if (hasSide)
                parts.Add(Loc.Get(isPlayer ? "slot_your_side" : "slot_enemy_side"));
            if (rowIndex >= 0)
                parts.Add(Loc.Get("slot_row", rowIndex + 1));
            if (slotIndex >= 0)
                parts.Add(Loc.Get("slot_position", slotIndex + 1));

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Describe a focused item while the player is holding a card:
        /// position first (that's the decision being made), then a short occupant summary.
        /// </summary>
        public static string DescribeTarget(UINavigationItem item)
        {
            if (item == null) return null;

            var battle = Battle.instance;

            // Recall zone: the discard pile is a valid drop target for board
            // units that can be recalled (a free action that takes them off the board)
            var discard = battle?.player?.discardContainer;
            if (discard != null)
            {
                var container = item.GetComponentInParent<CardContainer>();
                if (container == null && item.clickHandler != null)
                    container = item.clickHandler.GetComponentInParent<CardContainer>();
                if (item == discard.nav || container == discard)
                    return DescribeRecallZone();
            }

            // "Use on hand" anchor: drop zone for cards played without a target
            if (battle?.playerCardController is CardControllerBattle battleController
                && battleController.useOnHandAnchor != null
                && item == battleController.useOnHandAnchor)
                return Loc.Get("battle_play_anchor");

            // Slot items (empty or occupied placement targets)
            var slot = item.GetComponent<CardSlot>() ?? item.GetComponentInParent<CardSlot>();

            // Entity items (units offered as direct targets)
            var entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();

            if (entity != null && slot == null)
                slot = entity.GetComponentInParent<CardSlot>();

            if (slot != null)
            {
                var parts = new List<string>();
                string position = GetSlotPosition(slot);
                if (!string.IsNullOrEmpty(position))
                    parts.Add(position);

                var occupant = entity ?? (slot.Empty ? null : slot.GetTop());
                parts.Add(occupant != null
                    ? (SummarizeEntity(occupant) ?? Loc.Get("slot_occupied"))
                    : Loc.Get("slot_empty"));

                // Placing a unit is a decision about who it will face
                string opposite = DescribeOpposite(slot);
                if (!string.IsNullOrEmpty(opposite))
                    parts.Add(opposite);

                return string.Join(", ", parts);
            }

            return entity != null ? SummarizeEntity(entity) : null;
        }

        /// <summary>
        /// A board card's slot as "Row {row} {slot}" (e.g. "Row 1 3"), or null
        /// when the entity is not sitting in a battlefield slot (hand, shop, ...).
        /// </summary>
        public static string GetEntitySlotShort(Entity entity)
        {
            if (entity == null || References.Battle == null)
                return null;

            var slot = entity.GetComponentInParent<CardSlot>();
            var lane = slot != null ? slot.GetComponentInParent<CardSlotLane>() : null;
            if (lane == null)
                return null;

            int rowIndex = References.Battle.GetRowIndex(lane);
            int slotIndex = lane.slots.IndexOf(slot);
            if (rowIndex < 0 || slotIndex < 0)
                return null;

            return Loc.Get("slot_row", rowIndex + 1) + " " + (slotIndex + 1);
        }

        /// <summary>
        /// Who stands across the line from a slot: the same row and column on
        /// the other side. Both sides number columns up from their own front
        /// line and the board is rendered mirrored, so your column N squares off
        /// against the enemy's column N — and the two column 1s are the pair that
        /// actually trade blows, since a basic attack strikes the first enemy in
        /// its row. Lets the battle line be mapped by ear without leaving the
        /// slot to go hunting through the opposing row.
        /// Null when the slot is not on the battlefield or has no counterpart.
        /// </summary>
        public static string DescribeOpposite(CardSlot slot)
        {
            var battle = References.Battle;
            var lane = slot != null ? slot.GetComponentInParent<CardSlotLane>() : null;
            if (battle == null || lane?.slots == null)
                return null;

            int column = lane.slots.IndexOf(slot);
            if (column < 0)
                return null;

            CardSlotLane opposite;
            try
            {
                opposite = battle.GetOppositeRow(lane);
            }
            catch
            {
                return null;
            }
            if (opposite?.slots == null || column >= opposite.slots.Count)
                return null;

            Entity occupant = opposite.slots[column]?.GetTop();
            if (occupant?.data == null)
                return Loc.Get("slot_opposite_empty");

            bool isPlayer = occupant.owner != null && occupant.owner == battle.player;
            return Loc.Get("slot_opposite",
                Loc.Get(isPlayer ? "battle_your_unit" : "battle_enemy_unit", occupant.data.title));
        }

        /// <summary>
        /// Describe the recall drop zone, appending the game's own localized
        /// explanation of the Recall keyword when available.
        /// </summary>
        public static string DescribeRecallZone()
        {
            string text = Loc.Get("battle_recall_zone");
            try
            {
                var keyword = AddressableLoader.Get<KeywordData>("KeywordData", "recall");
                if (keyword != null && !string.IsNullOrEmpty(keyword.body))
                {
                    string body = TextProcessor.ProcessForScreenReader(keyword.body);
                    if (!string.IsNullOrEmpty(body))
                        text += " " + body;
                }
            }
            catch
            {
                // Keyword lookup is optional flavor; the base string is enough
            }
            return text;
        }
    }
}
