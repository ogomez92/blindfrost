using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Virtual rows: page entries the game builds without navigation items
    /// (challenges, battle log, stats, overall statistics blocks, lore pages),
    /// plus opening and reading a lore page aloud.
    /// </summary>
    public partial class PauseMenuHandler
    {
        /// <summary>A page entry without a navigation item: spoken text plus an optional action.</summary>
        private struct VirtualRow
        {
            public string Text;
            public System.Action Activate;
            public Transform Anchor;
            public int Order;
        }

        /// <summary>
        /// Rows on the current page that the game builds without navigation
        /// items: challenges (text + progress), battle log lines, stats, and
        /// lore page buttons (activatable). Entries that do have a navigation
        /// item in our content list are skipped to avoid double announcements.
        /// Ordered column-major like the focusable content.
        /// </summary>
        private List<VirtualRow> GetVirtualRows(List<UINavigationItem> contentItems)
        {
            var rows = new List<VirtualRow>();

            foreach (var entry in Object.FindObjectsOfType<ChallengeEntry>())
                AddVirtualRow(rows, contentItems, entry.transform, DescribeRowTexts(entry), null);

            foreach (var entry in Object.FindObjectsOfType<BattleLogEntry>())
                AddVirtualRow(rows, contentItems, entry.transform, DescribeRowTexts(entry), null);

            foreach (var stat in Object.FindObjectsOfType<StatDisplay>())
                AddVirtualRow(rows, contentItems, stat.transform, DescribeRowTexts(stat), null);

            // The "Overall Statistics" page has no per-row components — the
            // game renders it into a few large text blocks (names and values
            // in parallel columns). Split those into one row per stat.
            foreach (var stats in Object.FindObjectsOfType<OverallStatsDisplay>())
                AddOverallStatRows(rows, contentItems, stats);

            foreach (var page in Object.FindObjectsOfType<LorePage>())
            {
                LorePage captured = page;
                AddVirtualRow(rows, contentItems, page.transform,
                    DescribeLorePage(page), () => OpenLorePage(captured));
            }

            rows.Sort((a, b) =>
            {
                int byPosition = CompareColumnMajorPosition(a.Anchor.position, b.Anchor.position);
                // List.Sort is unstable and stat lines share their block's anchor
                return byPosition != 0 ? byPosition : a.Order.CompareTo(b.Order);
            });
            return rows;
        }

        /// <summary>
        /// Stat rows of the "Overall Statistics" page. OverallStatsDisplay
        /// writes the whole page into a few large TMP texts: names in one
        /// block, values in a parallel block, lines separated by br tags
        /// (centred locales inline the value into the name block instead).
        /// Split the blocks and re-pair the lines into one row per stat.
        /// </summary>
        private static void AddOverallStatRows(List<VirtualRow> rows,
            List<UINavigationItem> contentItems, OverallStatsDisplay display)
        {
            var nameGroups = ReflectionUtil.GetField<TMP_Text[]>(display, "nameGroups");
            var valueGroups = ReflectionUtil.GetField<TMP_Text[]>(display, "valueGroups");
            if (nameGroups == null) return;

            for (int group = 0; group < nameGroups.Length; group++)
            {
                var nameBlock = nameGroups[group];
                if (nameBlock == null) continue;

                var valueBlock = valueGroups != null && group < valueGroups.Length
                    ? valueGroups[group] : null;
                string[] names = SplitBlockLines(nameBlock.text);
                string[] values = valueBlock != null ? SplitBlockLines(valueBlock.text) : new string[0];

                for (int line = 0; line < names.Length; line++)
                {
                    string name = names[line];
                    if (string.IsNullOrEmpty(name)) continue; // blank separator line
                    string value = line < values.Length ? values[line] : null;
                    if (value == "-")
                        value = Loc.Get("stat_no_value");
                    string text = string.IsNullOrEmpty(value) ? name : name + " " + value;
                    AddVirtualRow(rows, contentItems, nameBlock.transform, text, null);
                }
            }
        }

        /// <summary>Split a stats text block into plain-text lines.</summary>
        private static string[] SplitBlockLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string[0];
            string[] lines = text.Split(new[] { "<br>", "\n" }, System.StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = TextProcessor.StripRichText(lines[i]);
            return lines;
        }

        /// <summary>Add a virtual row unless it is already reachable as a focusable item.</summary>
        private static void AddVirtualRow(List<VirtualRow> rows, List<UINavigationItem> contentItems,
            Transform anchor, string text, System.Action activate)
        {
            if (anchor == null || string.IsNullOrEmpty(text)) return;
            if (!anchor.gameObject.activeInHierarchy) return;

            foreach (var item in contentItems)
            {
                if (item != null && (item.transform.IsChildOf(anchor) || anchor.IsChildOf(item.transform)))
                    return; // already navigable the normal way
            }

            rows.Add(new VirtualRow { Text = text, Activate = activate, Anchor = anchor, Order = rows.Count });
        }

        /// <summary>
        /// Lore page entry. The page's whole story canvas doubles as the
        /// button's face, so reading its texts gives every page the same
        /// header — identify pages by number and data asset name instead.
        /// </summary>
        private static string DescribeLorePage(LorePage page)
        {
            // Prefab names identify pages well: LorePageWildfrost -> "Lore Page Wildfrost"
            string text = CleanName(page.gameObject.name);
            if (string.IsNullOrEmpty(text) || text == "unknown")
                text = Loc.Get("pause_lore_page") + " " + (page.transform.GetSiblingIndex() + 1);

            if (!page.isUnlocked)
                return text + ", " + Loc.Get("pause_lore_locked");

            if (page.isNew)
                text += ", " + Loc.Get("pause_lore_new");
            return text + ". " + Loc.Get("pause_lore_open_hint");
        }

        /// <summary>Open a lore page and read its full story text aloud.</summary>
        private static void OpenLorePage(LorePage page)
        {
            if (!page.isUnlocked)
            {
                page.Select(); // plays the game's deny feedback
                ScreenReader.Say(Loc.Get("pause_lore_locked"), interrupt: true);
                return;
            }

            page.Select();

            var root = page.canvas != null ? (Component)page.canvas : page;
            var blocks = new List<TMP_Text>(root.GetComponentsInChildren<TMP_Text>(false));
            // Reading order: top to bottom, left to right. The canvas scales
            // uniformly while opening, so relative positions stay valid.
            blocks.Sort((a, b) =>
            {
                int byHeight = b.transform.position.y.CompareTo(a.transform.position.y);
                return byHeight != 0
                    ? byHeight
                    : a.transform.position.x.CompareTo(b.transform.position.x);
            });

            var parts = new List<string>();
            foreach (var text in blocks)
            {
                if (text == null) continue;
                string value = TextProcessor.StripRichText(text.text?.Trim());
                if (!string.IsNullOrEmpty(value) && !parts.Contains(value))
                    parts.Add(value);
                if (parts.Count >= 40) break;
            }

            if (parts.Count > 0)
                // Recorded as an event so the long story can be re-read line
                // by line with Ctrl+Up instead of being lost to an interrupt
                ScreenReader.SayEvent(string.Join(". ", parts) + " " + Loc.Get("pause_lore_close_hint"));
            else
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
        }
    }
}
