using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The entry points every screen handler calls, and the fallbacks they share.
    /// <see cref="Describe"/> and <see cref="DescribeCore"/> are the recognition
    /// cascade: they work out what a focused nav item actually is and hand it to
    /// the right describer, ending at the screen's own button text;
    /// <see cref="DescribeDetailParts"/> is the same recognition for the Details
    /// review buffer. The describers themselves live in the sibling parts —
    /// Board, Entities, Mechanics, Upgrades, Tribes, BattleHud, MapAndTown —
    /// and the shop price suffix appended to every read is in ShopPrices.
    /// </summary>
    public static partial class ItemDescriber
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
        /// Describe a navigation item using the full recognition cascade, then
        /// append its shop price and affordability when it is a priced ware.
        /// </summary>
        public static string Describe(UINavigationItem item, ScreenHandler owner)
        {
            if (item == null) return null;
            return WithShopPrice(item, DescribeCore(item, owner));
        }

        /// <summary>
        /// The recognition cascade itself. Split out from <see cref="Describe"/>
        /// so shop pricing wraps every branch (a shop card, an upgrade on a
        /// shelf, the crown holder — all reachable through different branches).
        /// </summary>
        private static string DescribeCore(UINavigationItem item, ScreenHandler owner)
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

            // The drop zone for playing a card that needs no target. Same problem as
            // the bells: it is held in a CardControllerBattle field rather than the
            // hierarchy and carries no component of its own, so it would fall through
            // to the object-name fallback and read as its variable name,
            // "Use On Hand Anchor" — which names neither a game term nor an action.
            var cardController = Battle.instance?.playerCardController as CardControllerBattle;
            if (cardController != null && item == cardController.useOnHandAnchor)
                return Loc.Get("battle_use_on_hand");

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
                return WithCompanionRow(boardEntity, DescribeEntityFocus(boardEntity));

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
                return WithCompanionRow(entity, DescribeEntityFocus(entity));

            // Unlock/challenge banners (tribe hut flags, challenge shrine): the
            // challenge condition and how much progress remains
            var challenge = FindComponent<ChallengeProgressDisplay>(item);
            if (challenge != null)
            {
                string challengeText = DescribeChallengeProgress(challenge);
                if (!string.IsNullOrEmpty(challengeText))
                    return challengeText;
            }

            // Tribe flag on the character-select screen. The flag object carries
            // only a sprite, so this reads the tribe it stands for ("Snowdwellers")
            // instead of the bare object name ("flag image").
            var tribeFlag = FindComponent<TribeFlagDisplay>(item);
            if (tribeFlag != null)
            {
                string tribe = DescribeTribeFlag(tribeFlag);
                if (!string.IsNullOrEmpty(tribe))
                    return tribe;
            }

            // Anything else that pops keyword panels on hover (stat icons, misc UI)
            string keywordPanels = DescribeKeywordPanels(item, owner);
            if (keywordPanels != null)
                return keywordPanels;

            // Fall back to standard button text
            return owner.GetButtonText(item);
        }

        /// <summary>
        /// On the too-many-companions screen, a card's row (active or reserve)
        /// IS the decision being made — splice it in right after the name, the
        /// same spot battlefield cards get their slot. No-op everywhere else.
        /// </summary>
        private static string WithCompanionRow(Entity entity, string desc)
        {
            string row = CompanionLimitNarrator.DescribeRow(entity);
            if (string.IsNullOrEmpty(row))
                return desc;
            if (string.IsNullOrEmpty(desc))
                return row;

            int afterName = desc.IndexOf(", ");
            return afterName < 0
                ? desc + ", " + row
                : desc.Substring(0, afterName) + ", " + row + desc.Substring(afterName);
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

        /// <summary>CardData.title, guarded against a localization miss.</summary>
        private static string SafeTitle(CardData card)
        {
            try { return card?.title; }
            catch { return null; }
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
    }
}
