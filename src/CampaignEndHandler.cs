using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the CampaignEnd screen — the win / defeat / vanquished run
    /// summary shown when a journey ends. The screen is driven by
    /// <c>DefeatSequence</c>, which reveals its panels over several seconds and
    /// only activates the Back To Town / Scores buttons at the very end. The
    /// generic handler read those button labels but never the numbers that are
    /// the whole point of the screen — the run stats, town progress, and the
    /// score breakdown. This handler announces the result on entry, then, once
    /// the sequence has settled, reads a full summary. Every line goes through
    /// SayEvent, so it is also captured in the Events review buffer and can be
    /// replayed with Ctrl+Up. Arrow-key navigation of the buttons is inherited
    /// from the base handler unchanged.
    /// </summary>
    public class CampaignEndHandler : NavigableScreenHandler
    {
        public override string Name => "CampaignEnd";

        private DefeatSequence _sequence;
        private bool _summarySpoken;
        private float _nextSequenceSearch;

        /// <summary>
        /// Safety net: if the sequence is missing or wired differently than
        /// expected, read the summary anyway once this long has passed. The
        /// vanilla reveal (title, stats, challenge fill, score count-up) runs
        /// well under this, so the button-layout signal normally fires first.
        /// </summary>
        private const float SummaryTimeout = 14f;

        public override void OnEnter()
        {
            base.OnEnter();
            _sequence = null;
            _summarySpoken = false;
            _nextSequenceSearch = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            _sequence = null;
        }

        protected override bool TryAnnounceScreen()
        {
            ScreenReader.SayEvent(GetResultAnnouncement(), interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Once the sequence has revealed everything (its buttons are the last
            // thing to appear), read the full summary. Queued after the screen
            // title, and recorded so Ctrl+Up can replay it.
            if (!_summarySpoken && SequenceSettled())
            {
                _summarySpoken = true;
                foreach (string line in BuildSummary())
                    if (!string.IsNullOrEmpty(line))
                        ScreenReader.SayEvent(line);
            }
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_campaignend");
        }

        /// <summary>The DefeatSequence driving this screen, found lazily.</summary>
        private DefeatSequence Sequence()
        {
            if (_sequence == null && Time.unscaledTime >= _nextSequenceSearch)
            {
                _nextSequenceSearch = Time.unscaledTime + 0.5f;
                _sequence = Object.FindObjectOfType<DefeatSequence>();
            }
            return _sequence;
        }

        /// <summary>
        /// True once the whole reveal has finished — the buttons layout is the
        /// last thing DefeatSequence.Routine activates. Falls back to a timeout
        /// so the summary is never left unspoken.
        /// </summary>
        private bool SequenceSettled()
        {
            var seq = Sequence();
            if (seq != null && IsLayoutActive(seq, "buttonsLayout"))
                return true;
            return Time.unscaledTime - EnterTime > SummaryTimeout;
        }

        /// <summary>"Journey over. Victory!" — the scene title plus the result word.</summary>
        private string GetResultAnnouncement()
        {
            string journey = Loc.Get("scene_CampaignEnd");
            string result = GetResultWord();
            return string.IsNullOrEmpty(result) ? journey : journey + " " + result;
        }

        /// <summary>
        /// Win, defeat, or vanquished. Prefers the sequence's active title
        /// layout (only that distinguishes vanquished), falling back to the
        /// campaign result flag while the layouts are still animating in.
        /// </summary>
        private string GetResultWord()
        {
            var seq = Sequence();
            if (seq != null)
            {
                if (IsLayoutActive(seq, "vanquishedLayout"))
                    return Loc.Get("campaignend_vanquished");
                if (IsLayoutActive(seq, "winLayout"))
                    return Loc.Get("campaignend_win");
                if (IsLayoutActive(seq, "defeatLayout"))
                    return Loc.Get("campaignend_defeat");
            }

            try
            {
                if (References.Campaign != null)
                    return References.Campaign.result == Campaign.Result.Win
                        ? Loc.Get("campaignend_win")
                        : Loc.Get("campaignend_defeat");
            }
            catch
            {
                // Campaign state may already be torn down — the title is enough
            }
            return null;
        }

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

        // ---- End buttons fallback ----------------------------------------
        // The Back To Town / Restart / Scores buttons are assumed to be
        // navigation items, but if this screen turns out to be free-cursor
        // driven like BattleWin, arrow keys would find nothing and Enter would
        // dead-end the run. These pseudo-buttons keep an exit reachable.

        private int _buttonIndex = -1;

        private struct EndButton
        {
            public GameObject Target;
            public string Label;
        }

        /// <summary>Active clickable buttons on the end screen with labels.</summary>
        private List<EndButton> CollectEndButtons()
        {
            var results = new List<EndButton>();
            var seq = Sequence();
            if (seq == null)
                return results;

            foreach (string field in new[] { "buttonsLayout", "restartButton", "scoresButton" })
            {
                var root = ReflectionUtil.GetField<GameObject>(seq, field);
                if (root == null || !root.activeInHierarchy)
                    continue;
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    if (button == null || !button.gameObject.activeInHierarchy)
                        continue;
                    if (results.Exists(r => r.Target == button.gameObject))
                        continue;
                    var tmp = button.GetComponentInChildren<TMPro.TMP_Text>();
                    string label = tmp != null && !string.IsNullOrWhiteSpace(tmp.text)
                        ? TextProcessor.StripRichText(tmp.text).Trim()
                        : CleanName(button.gameObject.name);
                    results.Add(new EndButton { Target = button.gameObject, Label = label });
                }
            }
            return results;
        }

        protected override void Navigate(NavDirection dir)
        {
            if (NavigationHelper.GetNavigableItems().Count > 0)
            {
                base.Navigate(dir);
                return;
            }

            var buttons = CollectEndButtons();
            if (buttons.Count == 0)
            {
                base.Navigate(dir);
                return;
            }

            bool forward = dir == NavDirection.Down || dir == NavDirection.Right;
            if (_buttonIndex < 0)
                _buttonIndex = forward ? 0 : buttons.Count - 1;
            else
                _buttonIndex = ((_buttonIndex + (forward ? 1 : -1))
                    % buttons.Count + buttons.Count) % buttons.Count;
            ScreenReader.Say(buttons[_buttonIndex].Label, interrupt: true);
        }

        protected override void Confirm()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem?.currentNavigationItem != null)
            {
                base.Confirm();
                return;
            }

            var buttons = CollectEndButtons();
            if (buttons.Count > 0)
            {
                if (_buttonIndex < 0 || _buttonIndex >= buttons.Count)
                {
                    // Nothing chosen yet: land on the first button and require a
                    // second Enter, so a blind press can't restart the run
                    _buttonIndex = 0;
                    ScreenReader.Say(
                        buttons[0].Label + " " + Loc.Get("campaignend_press_again"),
                        interrupt: true);
                    return;
                }

                var chosen = buttons[_buttonIndex];
                DebugLogger.LogInput(Name, $"Pressing end button: {chosen.Label}");
                NavigationHelper.PressObject(chosen.Target);
                return;
            }

            base.Confirm();
        }

        /// <summary>Read a private TMP_Text field's shown text, stripped of rich tags. Null if hidden or empty.</summary>
        private static string ReadTmp(object obj, string fieldName)
        {
            var tmp = ReflectionUtil.GetField<TMP_Text>(obj, fieldName);
            if (tmp == null || !tmp.gameObject.activeInHierarchy || string.IsNullOrEmpty(tmp.text))
                return null;
            return TextProcessor.StripRichText(tmp.text)?.Trim();
        }

        private static bool IsLayoutActive(DefeatSequence seq, string fieldName)
        {
            var go = ReflectionUtil.GetField<GameObject>(seq, fieldName);
            return go != null && go.activeInHierarchy;
        }
    }
}
