using System.Collections.Generic;
using UnityEngine.Localization;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The battle HUD around the card rows: the draw and discard pockets, the
    /// redraw bell, both wave bells (including how much of an incoming wave the
    /// board has room for), and the run modifier bells.
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
