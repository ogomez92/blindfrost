using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

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
        /// When true, focused cards read with their full text and keyword
        /// explanations (the pre-review-buffers behavior). When false (the
        /// default), focus reads stay short — name, stats, effect names —
        /// and the details wait in the Details review buffer and on I.
        /// Toggled with V, persisted in the save file.
        /// </summary>
        public static bool VerboseFocus;

        /// <summary>
        /// Describe a navigation item using the full recognition cascade.
        /// </summary>
        public static string Describe(UINavigationItem item, ScreenHandler owner)
        {
            if (item == null) return null;

            // Inside the open inventory overlay, some items read differently:
            // card-menu buttons get role names, and while a charm is held the
            // eligible cards read as assignment targets (charm slots first).
            string deckpack = DeckpackNavigator.DescribeItem(item, owner);
            if (deckpack != null)
                return deckpack;

            // The battle bells' nav items reference their systems through
            // serialized fields, not the hierarchy — a component walk from the
            // item finds nothing and they'd read as their object name ("Bell").
            // Match them by identity against the systems' static nav fields.
            if (item == RedrawBellSystem.nav)
            {
                var bellSystem = Object.FindObjectOfType<RedrawBellSystem>();
                if (bellSystem != null)
                    return DescribeRedrawBell(bellSystem);
            }
            if (item == WaveDeploySystem.nav)
            {
                // Both wave systems publish to the same static nav field
                var overflowSystem = Object.FindObjectOfType<WaveDeploySystemOverflow>();
                if (overflowSystem != null)
                    return DescribeWaveBell(overflowSystem);
                var waveSystem = Object.FindObjectOfType<WaveDeploySystem>();
                if (waveSystem != null)
                    return DescribeWaveBell(waveSystem);
            }

            // Charm/crown displays (shop shelves, journal, charm icons on cards).
            // Charm icons are children of their card, so this must come before the
            // entity lookup: focusing a charm reads the charm, not the whole card.
            // Parents only — a card's own nav item must not match charm children.
            var upgradeDisplay = item.GetComponentInParent<UpgradeDisplay>();
            if (upgradeDisplay == null && item.clickHandler != null)
                upgradeDisplay = item.clickHandler.GetComponentInParent<UpgradeDisplay>();
            if (upgradeDisplay != null && upgradeDisplay.data != null)
                return VerboseFocus
                    ? DescribeUpgradeData(upgradeDisplay.data)
                    : DescribeUpgradeDataShort(upgradeDisplay.data);

            // Card/entity first: a card placed on the board is a child of its CardSlot,
            // so the slot check would shadow it and lose counter/status/description info.
            // Parents only — an occupied slot item must still fall through to DescribeSlot.
            var boardEntity = item.GetComponentInParent<Entity>();
            if (boardEntity == null && item.clickHandler != null)
                boardEntity = item.clickHandler.GetComponentInParent<Entity>();
            if (boardEntity != null)
                return DescribeEntityFocus(boardEntity);

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

            // Boss reward options (charm / crown / bell blessing)
            var bossReward = FindComponent<BossRewardSelect>(item);
            if (bossReward != null)
                return DescribeBossReward(bossReward) ?? owner.GetButtonText(item);

            // Crown holder in the shop
            var crownHolder = FindComponent<CrownHolderShop>(item);
            if (crownHolder != null)
                return DescribeCrownHolder(crownHolder);

            // Redraw bell — its hover panel explains what ringing does
            var redrawBell = FindComponent<RedrawBellSystem>(item);
            if (redrawBell != null)
                return DescribeRedrawBell(redrawBell);

            // Wave bell — incoming wave, call-early option, overflow warning
            var waveBell = FindComponent<WaveDeploySystemOverflow>(item);
            if (waveBell != null)
                return DescribeWaveBell(waveBell);

            // Run modifier bells (battle/map HUD)
            var modifierIcon = FindComponent<ModifierIcon>(item);
            if (modifierIcon != null)
                return DescribeModifierIcon(modifierIcon);

            // Card/entity (units, items, charms in card form)
            var entity = FindComponent<Entity>(item);
            if (entity != null)
                return DescribeEntityFocus(entity);

            // Unlock/challenge banners (tribe hut flags, challenge shrine): the
            // challenge condition and how much progress remains
            var challenge = FindComponent<ChallengeProgressDisplay>(item);
            if (challenge != null)
            {
                string challengeText = DescribeChallengeProgress(challenge);
                if (!string.IsNullOrEmpty(challengeText))
                    return challengeText;
            }

            // Anything else that pops keyword panels on hover (stat icons, misc UI)
            string keywordPanels = DescribeKeywordPanels(item, owner);
            if (keywordPanels != null)
                return keywordPanels;

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
        /// True when DescribeBuilding would yield only the building's name — no
        /// challenge line, no construction or new-unlock state. Such a name
        /// ("Balloon") tells the player nothing on its own, so the focus read
        /// folds in the help text rather than leaving it to the I key.
        /// </summary>
        public static bool BuildingFocusIsBareName(Building building)
        {
            if (building == null)
                return false;
            if (building.GetComponentInChildren<ChallengeProgressDisplay>() != null)
                return false;
            if (!building.built && building.buildStarted)
                return false;
            if (building.HasUncheckedUnlocks)
                return false;
            return true;
        }

        /// <summary>
        /// A building's in-game help text (BuildingType.helpKey, packed as
        /// "title|body|note"), split into one part per segment. This is what the
        /// I key reads; the Details review buffer steps through the same parts.
        /// Empty list when the building has no help.
        /// </summary>
        public static List<string> GetBuildingHelpParts(Building building)
        {
            var parts = new List<string>();
            if (building?.type == null)
                return parts;

            try
            {
                if (building.type.helpKey.IsEmpty)
                    return parts;

                string packed = building.type.helpKey.GetLocalizedString();
                foreach (string segment in packed.Split('|'))
                {
                    string clean = TextProcessor.StripRichText(segment)?.Trim();
                    if (!string.IsNullOrEmpty(clean))
                        parts.Add(clean);
                }
            }
            catch
            {
                // Localization may not be ready — the summary read is enough
            }
            return parts;
        }

        /// <summary>
        /// Describe an unlock challenge banner: the condition text and the
        /// current progress ("Kill 100 enemies, 6 out of 100").
        /// </summary>
        public static string DescribeChallengeProgress(ChallengeProgressDisplay display)
        {
            var parts = new List<string>();

            string text = display.text != null ? display.text.text?.Trim() : null;
            if (!string.IsNullOrEmpty(text))
                parts.Add(TextProcessor.StripRichText(text));

            string progress = display.progressText != null
                ? display.progressText.text?.Trim() : null;
            if (!string.IsNullOrEmpty(progress))
                parts.Add(progress);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
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
                parts.Add(Loc.Get("stat_attack", GetShownAttack(entity)));
            if (entity.counter.max > 0)
            {
                parts.Add(Loc.Get("battle_acts_in", entity.counter.current));
                if (entity.IsSnowed)
                    parts.Add(Loc.Get("counter_frozen"));
            }

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
                parts.Add(Loc.Get("stat_attack", GetShownAttack(entity)));

            if (entity.hp.max > 0)
                parts.Add(Loc.Get("stat_health", entity.hp.current));

            if (entity.counter.max > 0)
            {
                parts.Add(Loc.Get("stat_counter", entity.counter.current));
                if (entity.IsSnowed)
                    parts.Add(Loc.Get("counter_frozen"));
            }

            // Active status effects (Snow, Frost, Shell...)
            var extraKeywords = new List<string>();
            string statuses = DescribeStatusEffects(entity, extraKeywords);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            // Injuries — the game shows red "Injured" text plus a keyword panel
            int injuryCount = 0;
            try { injuryCount = entity.data.injuries?.Count ?? 0; } catch { }
            if (injuryCount > 0)
            {
                parts.Add(injuryCount == 1
                    ? Loc.Get("card_injured_one")
                    : Loc.Get("card_injured", injuryCount));
                extraKeywords.Add("injured");
            }

            // Crown and charms (card upgrades)
            string upgrades = DescribeUpgrades(entity.data);
            if (!string.IsNullOrEmpty(upgrades))
                parts.Add(upgrades);

            // Hidden keywords: extra panels the inspect view pops for effects
            // whose mechanics aren't in the card text
            try
            {
                foreach (var hidden in entity.GetHiddenKeywords())
                {
                    if (hidden == null) continue;
                    TextProcessor.CacheKeyword(hidden);
                    extraKeywords.Add(hidden.name);
                }
            }
            catch
            {
                // Effects may not be initialized yet
            }

            // Description text — expanded with keyword descriptions.
            // Keyword statuses announced above (Frenzy, Snow...) never appear in
            // the description text — the game shows them as icons with a hover
            // panel — so their ids are passed along to get explanations appended.
            string rawDesc = null;
            try
            {
                rawDesc = Card.GetDescription(entity.data);
            }
            catch
            {
                // Card.GetDescription may fail if the card isn't fully initialized
            }

            string processed = TextProcessor.ProcessForScreenReader(rawDesc, extraKeywords);

            // Cards with no ability text show italic flavour text in the
            // description box instead (Card.SetDescription) — read it there too
            if (string.IsNullOrWhiteSpace(rawDesc))
            {
                string flavour = GetFlavourText(entity.data);
                if (!string.IsNullOrEmpty(flavour))
                    parts.Add(flavour);
            }

            if (!string.IsNullOrEmpty(processed))
                parts.Add(processed);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Focus read for a card: short by default, full when VerboseFocus.</summary>
        public static string DescribeEntityFocus(Entity entity)
        {
            string desc = VerboseFocus ? DescribeEntity(entity) : DescribeEntityShort(entity);

            // For a card on the board, slot its position in right after the name
            // in the short "Row R S" form (e.g. "Row 1 3") so the player can map
            // the battlefield by ear before the stats.
            string slot = GetEntitySlotShort(entity);
            if (string.IsNullOrEmpty(slot))
                return desc;
            if (string.IsNullOrEmpty(desc))
                return slot;

            int afterName = desc.IndexOf(", ");
            return afterName < 0
                ? desc + ", " + slot
                : desc.Substring(0, afterName) + ", " + slot + desc.Substring(afterName);
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
        /// Short focus read: name, stats, and effect NAMES with stack counts —
        /// no card text, no keyword explanations. Those wait in the Details
        /// review buffer (Ctrl+Up) and in the game's inspect view (I).
        /// </summary>
        public static string DescribeEntityShort(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string>();

            string title = entity.data.title;
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            if (entity.damage.max > 0)
                parts.Add(Loc.Get("stat_attack", GetShownAttack(entity)));

            if (entity.hp.max > 0)
                parts.Add(Loc.Get("stat_health", entity.hp.current));

            if (entity.counter.max > 0)
            {
                parts.Add(Loc.Get("stat_counter", entity.counter.current));
                if (entity.IsSnowed)
                    parts.Add(Loc.Get("counter_frozen"));
            }

            string statuses = DescribeStatusEffects(entity);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            int injuryCount = 0;
            try { injuryCount = entity.data.injuries?.Count ?? 0; } catch { }
            if (injuryCount > 0)
            {
                parts.Add(injuryCount == 1
                    ? Loc.Get("card_injured_one")
                    : Loc.Get("card_injured", injuryCount));
            }

            // Effect names from the card text ("Snow 2", "Consume"), skipping
            // ones the active statuses already announced
            string rawDesc = null;
            try { rawDesc = Card.GetDescription(entity.data); } catch { }
            foreach (string mention in TextProcessor.ExtractKeywordMentions(rawDesc))
            {
                if (statuses == null || !statuses.Contains(mention))
                    parts.Add(mention);
            }

            // Upgrades by name only — their effect text is a detail
            AddUpgradeNames(parts, entity.data);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Crown plus charm/token titles, without their effect text.</summary>
        private static void AddUpgradeNames(List<string> parts, CardData data)
        {
            if (data?.upgrades == null || data.upgrades.Count == 0)
                return;

            var names = new List<string>();
            foreach (var upgrade in data.upgrades)
            {
                if (upgrade == null) continue;

                if (upgrade.type == CardUpgradeData.Type.Crown)
                {
                    parts.Add(Loc.Get("card_crowned"));
                    continue;
                }

                string upgradeTitle;
                try { upgradeTitle = upgrade.title; }
                catch { upgradeTitle = null; }
                names.Add(string.IsNullOrEmpty(upgradeTitle)
                    ? ScreenHandler.CleanName(upgrade.name)
                    : upgradeTitle);
            }

            if (names.Count == 1)
                parts.Add(FormatSingleCharmName(names[0]));
            else if (names.Count > 1)
                parts.Add(Loc.Get("card_charms", names.Count, string.Join(", ", names)));
        }

        /// <summary>
        /// The short-read label for a lone charm. Charm titles already end in the
        /// word "Charm" ("Coldheart Charm"), so the usual "Charm: {0}" wrapper
        /// would say charm twice. When the title already carries the localized
        /// charm word, read it on its own; the full effect text still waits in
        /// the Details review buffer.
        /// </summary>
        private static string FormatSingleCharmName(string title)
        {
            string charmWord = Loc.Get("upgrade_charm");
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(charmWord)
                && title.IndexOf(charmWord, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return title.EndsWith(".") ? title : title + ".";

            return Loc.Get("card_charm_one", title);
        }

        /// <summary>Short focus read for a standalone charm or crown: name and kind.</summary>
        public static string DescribeUpgradeDataShort(CardUpgradeData data)
        {
            string title;
            try { title = data.title; }
            catch { title = null; }
            if (string.IsNullOrEmpty(title))
                title = ScreenHandler.CleanName(data.name);

            string kind = Loc.Get(
                data.type == CardUpgradeData.Type.Crown ? "upgrade_crown"
                : data.type == CardUpgradeData.Type.Token ? "upgrade_token"
                : "upgrade_charm");

            return $"{title}, {kind}";
        }

        /// <summary>
        /// The Details review buffer for a focused item: the same information
        /// as the verbose read, split into steppable pieces — summary, card
        /// text, then one item per charm and per keyword explanation.
        /// </summary>
        public static List<string> DescribeDetailParts(UINavigationItem item, ScreenHandler owner)
        {
            if (item == null) return null;

            // Same recognition order as Describe: charm icons are children of
            // their card, so the upgrade check must come first
            var upgradeDisplay = item.GetComponentInParent<UpgradeDisplay>();
            if (upgradeDisplay == null && item.clickHandler != null)
                upgradeDisplay = item.clickHandler.GetComponentInParent<UpgradeDisplay>();
            if (upgradeDisplay != null && upgradeDisplay.data != null)
                return BuildUpgradeDetailParts(upgradeDisplay.data);

            var entity = FindComponent<Entity>(item);
            if (entity != null)
                return BuildEntityDetailParts(entity);

            // Let the active screen supply the rich details for its own items —
            // the same information its I key reads (town building help, campaign
            // map node waves and rewards).
            var ownerParts = owner?.GetFocusedDetailParts(item);
            if (ownerParts != null && ownerParts.Count > 0)
                return ownerParts;

            // Anything else (bells, buttons): the full focus description, split
            // into sentences for stepping
            string full = Describe(item, owner);
            return SplitSentences(full);
        }

        /// <summary>Detail pieces for a card, in reading order.</summary>
        public static List<string> BuildEntityDetailParts(Entity entity)
        {
            if (entity?.data == null) return null;

            var items = new List<string>();

            string summary = DescribeEntityShort(entity);
            if (!string.IsNullOrEmpty(summary))
                items.Add(summary);

            // Status keyword ids so their explanations are appended below,
            // exactly like the verbose read does
            var extraKeywords = new List<string>();
            DescribeStatusEffects(entity, extraKeywords);

            int injuryCount = 0;
            try { injuryCount = entity.data.injuries?.Count ?? 0; } catch { }
            if (injuryCount > 0)
                extraKeywords.Add("injured");

            try
            {
                foreach (var hidden in entity.GetHiddenKeywords())
                {
                    if (hidden == null) continue;
                    TextProcessor.CacheKeyword(hidden);
                    extraKeywords.Add(hidden.name);
                }
            }
            catch
            {
                // Effects may not be initialized yet
            }

            string rawDesc = null;
            try { rawDesc = Card.GetDescription(entity.data); } catch { }

            var explanations = new List<string>();
            string text = TextProcessor.ProcessDescriptionParts(rawDesc, extraKeywords, explanations);
            if (!string.IsNullOrEmpty(text))
                items.Add(text);

            if (string.IsNullOrWhiteSpace(rawDesc))
            {
                string flavour = GetFlavourText(entity.data);
                if (!string.IsNullOrEmpty(flavour))
                    items.Add(flavour);
            }

            // One item per charm, with its full effect text
            string upgrades = DescribeUpgrades(entity.data);
            if (!string.IsNullOrEmpty(upgrades))
                items.Add(upgrades);

            items.AddRange(explanations);
            return items;
        }

        /// <summary>Detail pieces for a standalone charm/crown.</summary>
        private static List<string> BuildUpgradeDetailParts(CardUpgradeData data)
        {
            var items = new List<string> { DescribeUpgradeDataShort(data) };

            string rawText = null;
            try { rawText = data.text; } catch { }

            var explanations = new List<string>();
            string text = TextProcessor.ProcessDescriptionParts(rawText, null, explanations);
            if (!string.IsNullOrEmpty(text))
                items.Add(text);
            items.AddRange(explanations);

            return items;
        }

        /// <summary>Split a long readout into sentence-sized buffer items.</summary>
        private static List<string> SplitSentences(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var items = new List<string>();
            foreach (string part in text.Split(new[] { ". " }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = part.Trim().TrimEnd('.');
                if (trimmed.Length > 0)
                    items.Add(trimmed);
            }
            return items;
        }

        /// <summary>Localized flavour text (lore), shown on cards without ability text.</summary>
        public static string GetFlavourText(CardData data)
        {
            try
            {
                var key = data?.flavourKey;
                if (key == null || key.IsEmpty) return null;
                return TextProcessor.StripRichText(key.GetLocalizedString());
            }
            catch
            {
                return null; // localization may not be loaded yet
            }
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

        /// <summary>
        /// Attack value as the card shows it: base damage plus temporary
        /// modifiers (Spice, Frost, ongoing effects). The game's attack icon
        /// displays damage + tempDamage, never below zero.
        /// </summary>
        public static int GetShownAttack(Entity entity)
        {
            int value = entity.damage.current;
            try { value += entity.tempDamage.Value; } catch { }
            return Mathf.Max(0, value);
        }

        /// <summary>
        /// Describe a card's upgrades: crown (deploys at battle start) and attached charms.
        /// </summary>
        public static string DescribeUpgrades(CardData data)
        {
            if (data?.upgrades == null || data.upgrades.Count == 0)
                return null;

            var parts = new List<string>();
            var charms = new List<string>();
            var tokens = new List<string>();

            foreach (var upgrade in data.upgrades)
            {
                if (upgrade == null) continue;

                if (upgrade.type == CardUpgradeData.Type.Crown)
                {
                    parts.Add(Loc.Get("card_crowned"));
                }
                else if (upgrade.type == CardUpgradeData.Type.Charm
                    || upgrade.type == CardUpgradeData.Type.Token)
                {
                    string title;
                    try { title = upgrade.title; }
                    catch { title = null; }
                    if (string.IsNullOrEmpty(title))
                        title = ScreenHandler.CleanName(upgrade.name);

                    // The upgrade's hover panel shows its effect text — read it too
                    string text = null;
                    try { text = TextProcessor.ProcessForScreenReader(upgrade.text); }
                    catch { }

                    var list = upgrade.type == CardUpgradeData.Type.Charm ? charms : tokens;
                    list.Add(string.IsNullOrEmpty(text) ? title : $"{title}. {text}");
                }
            }

            if (charms.Count == 1)
                parts.Add(Loc.Get("card_charm_one", charms[0]));
            else if (charms.Count > 1)
                parts.Add(Loc.Get("card_charms", charms.Count, string.Join(", ", charms)));

            if (tokens.Count == 1)
                parts.Add(Loc.Get("card_token_one", tokens[0]));
            else if (tokens.Count > 1)
                parts.Add(Loc.Get("card_tokens", tokens.Count, string.Join(", ", tokens)));

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// List an entity's visible status effects as "Name amount" pairs.
        /// Optionally collects the effects' keyword ids so their explanations
        /// can be appended to the full card readout.
        /// </summary>
        public static string DescribeStatusEffects(Entity entity, List<string> keywordIds = null)
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

                if (keywordIds != null && !string.IsNullOrEmpty(effect.keyword))
                    keywordIds.Add(effect.keyword);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// Describe a standalone charm or crown (shop shelf, journal, charm icon on
        /// a card): name, kind, and the effect text its hover panel shows.
        /// </summary>
        public static string DescribeUpgradeData(CardUpgradeData data)
        {
            string title;
            try { title = data.title; }
            catch { title = null; }
            if (string.IsNullOrEmpty(title))
                title = ScreenHandler.CleanName(data.name);

            string kind = Loc.Get(
                data.type == CardUpgradeData.Type.Crown ? "upgrade_crown"
                : data.type == CardUpgradeData.Type.Token ? "upgrade_token"
                : "upgrade_charm");

            string text = null;
            try { text = TextProcessor.ProcessForScreenReader(data.text); }
            catch { }

            return string.IsNullOrEmpty(text)
                ? $"{title}, {kind}"
                : $"{title}, {kind}. {text}";
        }

        /// <summary>
        /// Describe a boss reward option from the data its hover panel shows:
        /// either a keyword (title + body) or a title/body pair set by the
        /// charm/crown/modifier subclasses. Returns null if none is readable.
        /// </summary>
        public static string DescribeBossReward(BossRewardSelect reward)
        {
            var parts = new List<string>();

            var keyword = ReflectionUtil.GetField<KeywordData>(reward, "popUpKeyword");
            if (keyword != null)
            {
                string explanation = TextProcessor.GetKeywordExplanation(keyword)
                    ?? TextProcessor.GetKeywordTitle(keyword);
                if (!string.IsNullOrEmpty(explanation))
                    parts.Add(explanation);
            }

            string title = ReflectionUtil.GetField<string>(reward, "title");
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            string body = ReflectionUtil.GetField<string>(reward, "body");
            if (!string.IsNullOrEmpty(body))
                parts.Add(TextProcessor.ProcessForScreenReader(body));

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Describe the shop's crown holder: the crown for sale, or empty.</summary>
        public static string DescribeCrownHolder(CrownHolderShop holder)
        {
            if (!holder.hasCrown)
                return Loc.Get("crown_holder_empty");

            var data = holder.GetCrownData();
            return data != null ? DescribeUpgradeData(data) : Loc.Get("upgrade_crown");
        }

        /// <summary>
        /// Describe the redraw bell like its hover panel: the redraw keyword body
        /// (formatted with the player's hand size) plus charged state.
        /// </summary>
        public static string DescribeRedrawBell(RedrawBellSystem bell)
        {
            var parts = new List<string>();

            var keyword = ReflectionUtil.GetField<KeywordData>(bell, "popUpKeyword");
            parts.Add(TextProcessor.GetKeywordTitle(keyword) ?? Loc.Get("battle_bell_name"));

            try
            {
                string body = keyword?.body;
                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        int handSize = Events.GetHandSize(References.PlayerData.handSize);
                        body = string.Format(body, handSize);
                    }
                    catch { /* leave the placeholder if hand size is unavailable */ }
                    parts.Add(TextProcessor.ProcessForScreenReader(body));
                }
            }
            catch { /* localization may not be ready */ }

            string state = null;
            try
            {
                var stateKey = ReflectionUtil.GetField<LocalizedString>(bell,
                    bell.IsCharged ? "textCharged" : "textNotCharged");
                // Tag-aware: the game strings start with the word tags
                // <Charged!> / <Not Charged>, which StripRichText would delete
                state = TextProcessor.ProcessForScreenReader(stateKey?.GetLocalizedString());
            }
            catch { }
            if (string.IsNullOrEmpty(state))
            {
                state = bell.IsCharged
                    ? Loc.Get("battle_bell_charged")
                    : Loc.Get("battle_bell_charging", bell.counter.current);
            }
            parts.Add(state);

            // The bell's counter is only shown visually on its counter icon
            if (!bell.IsCharged)
                parts.Add(Loc.Get("battle_bell_counter", bell.counter.current));

            return string.Join(". ", parts);
        }

        /// <summary>
        /// Describe the wave bell like its hover panel: how many units arrive in
        /// how many turns, whether it can be rung early (and the gold reward), or
        /// how many incoming units won't fit on the board.
        /// </summary>
        public static string DescribeWaveBell(WaveDeploySystemOverflow system)
        {
            var parts = new List<string>();

            var popup = ReflectionUtil.GetField<KeywordData>(system, "popup");
            parts.Add(TextProcessor.GetKeywordTitle(popup) ?? Loc.Get("battle_wave_bell_name"));

            var waves = ReflectionUtil.GetField<List<BattleWaveManager.Wave>>(system, "waves");
            int currentWave = ReflectionUtil.GetIntField(system, "currentWave", -1);
            int counter = ReflectionUtil.GetIntField(system, "counter", 0);

            if (waves == null || currentWave < 0 || currentWave >= waves.Count)
            {
                parts.Add(Loc.Get("battle_all_waves_spawned"));
                return string.Join(". ", parts);
            }

            int unitCount = waves[currentWave]?.units?.Count ?? 0;
            parts.Add(GetGameText(system, "popupDesc", unitCount, counter)
                ?? Loc.Get("battle_wave_incoming", unitCount, counter));

            int overflow = unitCount - CountEmptyEnemySpaces();
            if (overflow > 0)
            {
                parts.Add(GetGameText(system, "popupOverflowDesc", overflow)
                    ?? Loc.Get("battle_wave_overflow", overflow));
            }
            else if (ReflectionUtil.GetBoolField(system, "canCallEarly", false))
            {
                parts.Add(GetGameText(system, "popupHitDesc")
                    ?? Loc.Get("battle_wave_call_early"));

                int reward = ReflectionUtil.GetIntField(system, "deployEarlyReward", 0)
                    + ReflectionUtil.GetIntField(system, "deployEarlyRewardPerTurn", 0) * counter;
                if (reward > 0)
                {
                    parts.Add(GetGameText(system, "popupRewardDesc", reward)
                        ?? Loc.Get("battle_wave_call_reward", reward));
                }
            }

            return string.Join(". ", parts);
        }

        /// <summary>
        /// Describe the standard wave bell (battles without the overflow
        /// variant): how many units arrive in how many turns.
        /// </summary>
        public static string DescribeWaveBell(WaveDeploySystem system)
        {
            var parts = new List<string> { Loc.Get("battle_wave_bell_name") };

            var waveManager = ReflectionUtil.GetField<BattleWaveManager>(system, "waveManager");
            int currentWave = ReflectionUtil.GetIntField(system, "currentWave", -1);
            int counter = ReflectionUtil.GetIntField(system, "counter", 0);

            if (waveManager == null || waveManager.list == null
                || currentWave < 0 || currentWave >= waveManager.list.Count)
            {
                parts.Add(Loc.Get("battle_all_waves_spawned"));
                return string.Join(". ", parts);
            }

            int unitCount = waveManager.list[currentWave]?.units?.Count ?? 0;
            parts.Add(Loc.Get("battle_wave_incoming", unitCount, counter));
            return string.Join(". ", parts);
        }

        /// <summary>Read a LocalizedString field off a game component, formatted and stripped. Null on any failure.</summary>
        private static string GetGameText(object obj, string fieldName, params object[] args)
        {
            try
            {
                var key = ReflectionUtil.GetField<LocalizedString>(obj, fieldName);
                if (key == null) return null;

                string text = key.GetLocalizedString();
                if (string.IsNullOrEmpty(text)) return null;

                if (args != null && args.Length > 0)
                {
                    try { text = string.Format(text, args); }
                    catch { /* keep the unformatted template */ }
                }
                // Tag-aware processing: the game wraps numbers in angle brackets
                // ("<{0}> enemies arriving in <{1}> turns"), which plain
                // StripRichText would delete along with the rich text tags
                return TextProcessor.ProcessForScreenReader(text);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Open slots on the enemy board — how much of an incoming wave fits.</summary>
        private static int CountEmptyEnemySpaces()
        {
            try
            {
                int empty = 0;
                foreach (var row in References.Battle.GetRows(References.Battle.enemy))
                {
                    if (row != null && row.canBePlacedOn)
                        empty += row.max - row.Count;
                }
                return empty;
            }
            catch
            {
                return int.MaxValue; // unknown board — don't report a false overflow
            }
        }

        /// <summary>
        /// Describe a run modifier bell (battle/map HUD) from the same title/body
        /// its hover panel shows.
        /// </summary>
        public static string DescribeModifierIcon(ModifierIcon icon)
        {
            var parts = new List<string>();

            // Stacked bells hold a list of modifiers; pop shows one panel each
            if (icon is ModifierIconMultiple)
            {
                var modifiers = ReflectionUtil.GetField<List<GameModifierData>>(icon, "modifiers");
                if (modifiers != null)
                {
                    foreach (var modifier in modifiers)
                        AddModifierText(parts, modifier);
                }
            }

            // Single bells cache panel text in private title/body fields
            if (parts.Count == 0)
            {
                string title = ReflectionUtil.GetField<string>(icon, "title");
                string body = ReflectionUtil.GetField<string>(icon, "body");
                if (!string.IsNullOrEmpty(title))
                    parts.Add(title);
                if (!string.IsNullOrEmpty(body))
                    parts.Add(TextProcessor.ProcessForScreenReader(body));
            }

            if (parts.Count == 0)
            {
                var modifier = ReflectionUtil.GetField<GameModifierData>(icon, "modifier");
                AddModifierText(parts, modifier);
            }

            return parts.Count > 0
                ? string.Join(". ", parts)
                : Loc.Get("battle_modifier_bell");
        }

        private static void AddModifierText(List<string> parts, GameModifierData modifier)
        {
            if (modifier == null) return;
            try
            {
                string title = modifier.titleKey.GetLocalizedString();
                if (!string.IsNullOrEmpty(title))
                    parts.Add(title);

                string body = modifier.descriptionKey.GetLocalizedString();
                if (!string.IsNullOrEmpty(body))
                    parts.Add(TextProcessor.ProcessForScreenReader(body));
            }
            catch
            {
                // Localization may not be ready
            }
        }

        /// <summary>
        /// Generic fallback for UI that pops keyword panels on hover (stat icons
        /// and other CardPopUpTarget carriers): button text plus each keyword's
        /// title and body. Returns null when the item pops nothing.
        /// </summary>
        private static string DescribeKeywordPanels(UINavigationItem item, ScreenHandler owner)
        {
            var target = item.GetComponentInParent<CardPopUpTarget>();
            if (target == null && item.clickHandler != null)
                target = item.clickHandler.GetComponentInParent<CardPopUpTarget>();
            if (target == null || target.keywords == null || target.keywords.Length == 0)
                return null;

            var parts = new List<string>();
            string buttonText = owner.GetButtonText(item);
            if (!string.IsNullOrEmpty(buttonText))
                parts.Add(buttonText);

            foreach (var keyword in target.keywords)
            {
                if (keyword == null) continue;
                string explanation = TextProcessor.GetKeywordExplanation(keyword)
                    ?? TextProcessor.GetKeywordTitle(keyword);
                if (!string.IsNullOrEmpty(explanation))
                    parts.Add(explanation);
            }

            return parts.Count > 0 ? string.Join(". ", parts) : null;
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
