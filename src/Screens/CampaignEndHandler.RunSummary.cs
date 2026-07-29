using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The numbers the end screen exists for, read once the reveal has settled:
    /// the run-stats paper, the town challenges this journey advanced, and the
    /// Your Score breakdown.
    /// </summary>
    public partial class CampaignEndHandler
    {
        /// <summary>The summary lines, in reading order: run stats, town progress, score.</summary>
        private List<string> BuildSummary()
        {
            var lines = new List<string>();
            var seq = Sequence();

            string stats = ReadStatsPanel(seq);
            if (!string.IsNullOrEmpty(stats))
                lines.Add(stats);

            string town = ReadTownProgress();
            if (!string.IsNullOrEmpty(town))
                lines.Add(town);

            string score = ReadScore();
            if (!string.IsNullOrEmpty(score))
                lines.Add(score);

            return lines;
        }

        /// <summary>
        /// The run-stats paper panel: the leader's name and the per-run stat
        /// lines ("Damage Dealt: 24"), read top to bottom straight off the panel.
        /// </summary>
        private string ReadStatsPanel(DefeatSequence seq)
        {
            if (seq == null)
                return null;
            var layout = ReflectionUtil.GetField<GameObject>(seq, "statsLayout");
            if (layout == null || !layout.activeInHierarchy)
                return null;

            var texts = new List<TMP_Text>();
            foreach (var tmp in layout.GetComponentsInChildren<TMP_Text>())
            {
                if (tmp != null && tmp.gameObject.activeInHierarchy
                    && !string.IsNullOrWhiteSpace(tmp.text))
                    texts.Add(tmp);
            }
            if (texts.Count == 0)
                return null;

            // Visual order: title at the top, stats down the panel
            texts.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

            var parts = new List<string>();
            foreach (var tmp in texts)
            {
                string clean = TextProcessor.StripRichText(tmp.text)?.Trim();
                if (!string.IsNullOrEmpty(clean) && !parts.Contains(clean))
                    parts.Add(clean);
            }
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// Town progress: the challenge lines shown mid-screen ("Kill 91
        /// enemies") with their fill counts. Null when no challenge advanced.
        /// </summary>
        private string ReadTownProgress()
        {
            var displays = Object.FindObjectsOfType<ChallengeProgressDisplay>();
            if (displays == null || displays.Length == 0)
                return null;

            var parts = new List<string>();
            foreach (var display in displays)
            {
                if (display == null || !display.gameObject.activeInHierarchy)
                    continue;
                string text = ItemDescriber.DescribeChallengeProgress(display);
                if (!string.IsNullOrEmpty(text) && !parts.Contains(text))
                    parts.Add(text);
            }
            return parts.Count > 0
                ? Loc.Get("campaignend_town_progress", string.Join(", ", parts))
                : null;
        }

        /// <summary>
        /// The Your Score breakdown read from ScoreSequence's own fields: time,
        /// battles won, and blings each with their point delta, then the total
        /// and global rank (which label themselves). Null when this game mode
        /// submits no score.
        /// </summary>
        private string ReadScore()
        {
            var score = Object.FindObjectOfType<ScoreSequence>();
            if (score == null)
                return null;

            var parts = new List<string>();
            AddScoreLine(parts, "campaignend_time", score, "timeText", "timeScoreText");
            AddScoreLine(parts, "campaignend_battles", score, "battlesText", "battlesScoreText");
            AddScoreLine(parts, "campaignend_blings", score, "goldText", "goldScoreText");

            // Total and rank strings already include their own labels
            string total = ReadTmp(score, "totalScoreText");
            if (!string.IsNullOrEmpty(total))
                parts.Add(total);
            string rank = ReadTmp(score, "globalRankText");
            if (!string.IsNullOrEmpty(rank))
                parts.Add(rank);

            return parts.Count > 0
                ? Loc.Get("campaignend_score", string.Join(", ", parts))
                : null;
        }

        /// <summary>"Battles won 0, minus 100" — a stat value and its point delta.</summary>
        private static void AddScoreLine(List<string> parts, string labelKey,
            ScoreSequence score, string valueField, string deltaField)
        {
            string value = ReadTmp(score, valueField);
            if (string.IsNullOrEmpty(value))
                return;

            string line = Loc.Get(labelKey) + " " + value;
            string delta = ReadTmp(score, deltaField);
            if (!string.IsNullOrEmpty(delta))
                line += ", " + delta;
            parts.Add(line);
        }

        /// <summary>Read a private TMP_Text field's shown text, stripped of rich tags. Null if hidden or empty.</summary>
        private static string ReadTmp(object obj, string fieldName)
        {
            var tmp = ReflectionUtil.GetField<TMP_Text>(obj, fieldName);
            if (tmp == null || !tmp.gameObject.activeInHierarchy || string.IsNullOrEmpty(tmp.text))
                return null;
            return TextProcessor.StripRichText(tmp.text)?.Trim();
        }
    }
}
