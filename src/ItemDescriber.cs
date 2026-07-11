using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Builds screen-reader descriptions for focused UI items.
    /// Shared by all screen handlers: recognizes battlefield slots, town buildings,
    /// card pockets, campaign map nodes, and card entities, falling back to button text.
    /// </summary>
    public static class ItemDescriber
    {
        /// <summary>
        /// Describe a navigation item using the full recognition cascade.
        /// </summary>
        public static string Describe(UINavigationItem item, ScreenHandler owner)
        {
            if (item == null) return null;

            // Card/entity first: a card placed on the board is a child of its CardSlot,
            // so the slot check would shadow it and lose counter/status/description info.
            // Parents only — an occupied slot item must still fall through to DescribeSlot.
            var boardEntity = item.GetComponentInParent<Entity>();
            if (boardEntity == null && item.clickHandler != null)
                boardEntity = item.clickHandler.GetComponentInParent<Entity>();
            if (boardEntity != null)
                return DescribeEntity(boardEntity);

            // Battlefield card slot (diamond placement slots)
            var slot = item.GetComponent<CardSlot>() ?? item.GetComponentInParent<CardSlot>();
            if (slot != null)
                return DescribeSlot(slot);

            // Town building (Gate, ChallengeShrine, PetHut, etc.)
            var building = FindComponent<Building>(item);
            if (building != null)
                return DescribeBuilding(building);

            // Card pockets (draw/discard pile UI in the HUD)
            var pocket = FindComponent<CardPocket>(item);
            if (pocket != null)
                return DescribePocket(pocket);

            // Campaign map node
            var mapNode = FindComponent<MapNode>(item);
            if (mapNode != null)
                return DescribeMapNode(mapNode);

            // Card/entity (units, items, charms in card form)
            var entity = FindComponent<Entity>(item);
            if (entity != null)
                return DescribeEntity(entity);

            // Fall back to standard button text
            return owner.GetButtonText(item);
        }

        /// <summary>Look for a component on the item, its click handler, parents, or children.</summary>
        private static T FindComponent<T>(UINavigationItem item) where T : Component
        {
            var comp = item.GetComponentInParent<T>();
            if (comp == null && item.clickHandler != null)
                comp = item.clickHandler.GetComponentInParent<T>();
            if (comp == null)
                comp = item.GetComponentInChildren<T>();
            return comp;
        }

        /// <summary>
        /// Describe a card pocket: which pile it is and how many cards it holds.
        /// </summary>
        public static string DescribePocket(CardPocket pocket)
        {
            int count = pocket.Count;
            bool isDiscard = pocket.GetComponent<Discarder>() != null
                || pocket.name.ToLowerInvariant().Contains("discard");

            string key = isDiscard
                ? (count == 1 ? "pocket_discard_one" : "pocket_discard")
                : (count == 1 ? "pocket_draw_one" : "pocket_draw");

            return Loc.Get(key, count);
        }

        /// <summary>
        /// Describe a campaign map node: what it is and whether it can be visited.
        /// </summary>
        public static string DescribeMapNode(MapNode node)
        {
            var parts = new List<string> { GetMapNodeName(node) };

            var campaignNode = node.campaignNode;
            if (campaignNode != null && campaignNode.cleared)
                parts.Add(Loc.Get("map_node_cleared"));
            else if (!node.interactable)
                parts.Add(Loc.Get("map_node_not_reachable"));

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Get the display name of a map node from its localized label,
        /// falling back to the campaign node type name.
        /// </summary>
        public static string GetMapNodeName(MapNode node)
        {
            // The game shows banner labels on hover — battle nodes have two
            // ("Battle" + the squad name), so collect every distinct text.
            var labels = new List<string>();
            foreach (var tmp in node.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                string text = tmp != null ? tmp.text?.Trim() : null;
                if (string.IsNullOrEmpty(text)) continue;

                text = TextProcessor.StripRichText(text);
                // Skip bare numbers (gold amounts and similar overlays)
                if (string.IsNullOrEmpty(text) || int.TryParse(text, out _)) continue;

                if (!labels.Contains(text))
                    labels.Add(text);
                if (labels.Count >= 2) break;
            }
            if (labels.Count > 0)
                return string.Join(", ", labels);

            var type = node.campaignNode?.type;
            if (type != null)
            {
                string typeName = ScreenHandler.CleanName(type.name);
                if (!string.IsNullOrEmpty(typeName))
                    return typeName;
            }

            return ScreenHandler.CleanName(node.name);
        }

        /// <summary>
        /// Describe a Town building: localized name, challenge progress, and state.
        /// </summary>
        public static string DescribeBuilding(Building building)
        {
            var parts = new List<string>();

            // Building name from localized title
            if (building.type != null)
            {
                try
                {
                    string title = building.type.titleKey.GetLocalizedString();
                    if (!string.IsNullOrEmpty(title))
                        parts.Add(title);
                }
                catch
                {
                    // Localization may not be ready
                }
            }

            // Fallback to cleaned GameObject name if no localized title
            if (parts.Count == 0)
                parts.Add(ScreenHandler.CleanName(building.gameObject.name));

            // Check for challenge progress display (ChallengeShrine and similar)
            var challengeDisplay = building.GetComponentInChildren<ChallengeProgressDisplay>();
            if (challengeDisplay != null)
            {
                if (challengeDisplay.text != null)
                {
                    string challengeText = challengeDisplay.text.text?.Trim();
                    if (!string.IsNullOrEmpty(challengeText))
                        parts.Add(TextProcessor.StripRichText(challengeText));
                }
                if (challengeDisplay.progressText != null)
                {
                    string progress = challengeDisplay.progressText.text?.Trim();
                    if (!string.IsNullOrEmpty(progress))
                        parts.Add(TextProcessor.StripRichText(progress));
                }
            }

            // Building state
            if (!building.built && building.buildStarted)
                parts.Add(Loc.Get("building_under_construction"));
            else if (building.HasUncheckedUnlocks)
                parts.Add(Loc.Get("building_new_unlock"));

            return string.Join(", ", parts);
        }

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

        /// <summary>"Your side, row 1, slot 2" — side, row and position of a battlefield slot.</summary>
        public static string GetSlotPosition(CardSlot slot)
        {
            var parts = new List<string>();

            if (slot.owner != null && References.Battle != null)
            {
                parts.Add(slot.owner == References.Battle.player
                    ? Loc.Get("slot_your_side")
                    : Loc.Get("slot_enemy_side"));
            }

            var lane = slot.GetComponentInParent<CardSlotLane>();
            if (lane != null)
            {
                if (References.Battle != null)
                {
                    int rowIndex = References.Battle.GetRowIndex(lane);
                    if (rowIndex >= 0)
                        parts.Add(Loc.Get("slot_row", rowIndex + 1));
                }

                int slotIndex = lane.slots.IndexOf(slot);
                if (slotIndex >= 0)
                    parts.Add(Loc.Get("slot_position", slotIndex + 1));
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Short combat summary of a unit: name, health, counter, statuses.
        /// Used for slot occupants and targeting, where the full card text is too much.
        /// </summary>
        public static string SummarizeEntity(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string> { entity.data.title };

            if (entity.hp.max > 0)
                parts.Add(Loc.Get("stat_health", entity.hp.current));
            if (entity.damage.max > 0)
                parts.Add(Loc.Get("stat_attack", entity.damage.current));
            if (entity.counter.max > 0)
                parts.Add(Loc.Get("battle_acts_in", entity.counter.current));

            string statuses = DescribeStatusEffects(entity);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Describe a focused item while the player is holding a card:
        /// position first (that's the decision being made), then a short occupant summary.
        /// </summary>
        public static string DescribeTarget(UINavigationItem item)
        {
            if (item == null) return null;

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

                return string.Join(", ", parts);
            }

            return entity != null ? SummarizeEntity(entity) : null;
        }

        /// <summary>
        /// Describe a card/entity: name, stats, and expanded description with keyword explanations.
        /// </summary>
        public static string DescribeEntity(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string>();

            string title = entity.data.title;
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            if (entity.damage.max > 0)
                parts.Add(Loc.Get("stat_attack", entity.damage.current));

            if (entity.hp.max > 0)
                parts.Add(Loc.Get("stat_health", entity.hp.current));

            if (entity.counter.max > 0)
                parts.Add(Loc.Get("stat_counter", entity.counter.current));

            // Active status effects (Snow, Frost, Shell, injuries...)
            string statuses = DescribeStatusEffects(entity);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            // Description text — expanded with keyword descriptions
            try
            {
                string rawDesc = Card.GetDescription(entity.data);
                if (!string.IsNullOrEmpty(rawDesc))
                {
                    string processed = TextProcessor.ProcessForScreenReader(rawDesc);
                    if (!string.IsNullOrEmpty(processed))
                        parts.Add(processed);
                }
            }
            catch
            {
                // Card.GetDescription may fail if the card isn't fully initialized
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// List an entity's visible status effects as "Name amount" pairs.
        /// </summary>
        public static string DescribeStatusEffects(Entity entity)
        {
            if (entity?.statusEffects == null || entity.statusEffects.Count == 0)
                return null;

            var parts = new List<string>();
            foreach (var effect in entity.statusEffects)
            {
                if (effect == null || !effect.visible)
                    continue;

                int amount;
                try { amount = effect.GetAmount(); }
                catch { amount = effect.count; }
                if (amount <= 0) continue;

                string name = GetStatusName(effect);
                parts.Add($"{name} {amount}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Human-readable name of a status effect via its keyword.</summary>
        public static string GetStatusName(StatusEffectData effect)
        {
            try
            {
                if (!string.IsNullOrEmpty(effect.keyword))
                {
                    var keyword = AddressableLoader.Get<KeywordData>("KeywordData", effect.keyword);
                    if (keyword != null && !string.IsNullOrEmpty(keyword.title))
                        return keyword.title;
                }
            }
            catch { /* keyword lookup can fail during load */ }

            return ScreenHandler.CleanName(effect.name);
        }
    }
}
