using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Furniture outside the battle line: card pockets, campaign map nodes, town
    /// buildings and their help text, challenge banners, the bells and modifier icons.
    /// </summary>
    public static partial class ItemDescriber
    {
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
                    string clean = TextProcessor.ProcessRawText(segment)?.Trim();
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
    }
}
