using UnityEngine;
using TMPro;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Watches the two text overlays that can appear over any screen without
    /// changing it: the help panel (HelpPanelSystem) and the tutorial prompt —
    /// Snowbo's blue speech bubble, driven by PromptSystem. Both are polled
    /// every frame and announced when they open and whenever their text
    /// changes, with the mod's own key hints folded into the lesson that makes
    /// each one useful.
    /// </summary>
    public static partial class PopupReader
    {
        private static bool _helpPanelWasActive;
        private static bool _promptWasActive;
        private static string _lastPromptText;
        private static int _promptReadDelay;
        private static bool _buttonHintSpoken;
        private static bool _inspectHintSpoken;
        private static bool _counterKeysHintSpoken;
        private static bool _healthKeysHintSpoken;

        /// <summary>
        /// Check for popup state changes. Called every frame from the main update loop.
        /// </summary>
        public static void Update()
        {
            CheckHelpPanel();
            CheckPrompt();
        }

        /// <summary>
        /// Detect when the HelpPanelSystem opens and read its content.
        /// </summary>
        private static void CheckHelpPanel()
        {
            bool isActive = HelpPanelSystem.Active;

            if (isActive && !_helpPanelWasActive)
            {
                OnHelpPanelOpened();
            }

            _helpPanelWasActive = isActive;
        }

        /// <summary>
        /// Detect when a tutorial/guide Prompt appears and read its text.
        /// The Prompt is the blue speech bubble (Snowbo) used for tutorials.
        /// </summary>
        private static void CheckPrompt()
        {
            Prompt prompt = null;
            try
            {
                prompt = PromptSystem.Prompt;
            }
            catch
            {
                // PromptSystem not initialized yet
                return;
            }

            if (prompt == null)
                return;

            bool isActive = prompt.active;

            if (isActive && !_promptWasActive)
            {
                // Prompt just became active — wait 1 frame for text to be set
                _promptReadDelay = 2;
                _lastPromptText = null;
            }

            if (isActive && _promptReadDelay > 0)
            {
                _promptReadDelay--;
                if (_promptReadDelay == 0)
                {
                    ReadPromptText(prompt);
                }
            }

            // Also detect text changes while prompt stays active (tutorial advances)
            if (isActive && _promptReadDelay == 0)
            {
                string currentText = GetPromptText(prompt);
                if (!string.IsNullOrEmpty(currentText) && currentText != _lastPromptText)
                {
                    _lastPromptText = currentText;
                    AnnouncePromptText(currentText);
                }
            }

            _promptWasActive = isActive;
        }

        /// <summary>
        /// Read text from the prompt and announce it.
        /// </summary>
        private static void ReadPromptText(Prompt prompt)
        {
            string text = GetPromptText(prompt);
            if (string.IsNullOrEmpty(text))
                return;

            _lastPromptText = text;
            AnnouncePromptText(text);
        }

        /// <summary>
        /// Get the current display text from a Prompt's TMP_Text component.
        /// </summary>
        private static string GetPromptText(Prompt prompt)
        {
            var tmpText = prompt.GetComponentInChildren<TMP_Text>(true);
            if (tmpText == null)
                return null;

            string text = tmpText.text;
            if (string.IsNullOrEmpty(text))
                return null;

            // Strip all rich text tags
            text = TextProcessor.StripRichText(text);

            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>
        /// Process prompt text for accessibility and announce it.
        /// Replaces "drag" language with select-and-place instructions.
        /// </summary>
        private static void AnnouncePromptText(string text)
        {
            // Replace drag-based instructions with accessible select-and-place language
            text = MakeDragAccessible(text);

            text += TutorialKeyHint(text);

            ScreenReader.SayEvent(Loc.Get("tutorial_prompt", text), interrupt: false);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "PopupReader",
                $"Tutorial prompt: {text}");
        }

        /// <summary>
        /// The mod's readout keys have no tutorial of their own, so each one
        /// rides along with the game's lesson that makes it useful — the
        /// counter keys when counters are explained, the health keys when the
        /// game teaches pulling a wounded companion back to heal. Each fires
        /// once per session; returns "" when this prompt teaches none of them.
        /// </summary>
        private static string TutorialKeyHint(string text)
        {
            string phase = ActiveTutorialPhase();

            // "Counters tick down each turn; at zero the unit acts" — the
            // moment the player first needs to read the whole board's clocks
            if (!_counterKeysHintSpoken && phase == "PhaseCounters")
            {
                _counterKeysHintSpoken = true;
                return " " + Loc.Get("tutorial_counter_keys_hint");
            }

            // "Recall a hurt companion to heal it" — useless without a way to
            // find out who is hurt in the first place
            if (!_healthKeysHintSpoken && phase == "PhaseRecallToHeal")
            {
                _healthKeysHintSpoken = true;
                return " " + Loc.Get("tutorial_health_keys_hint");
            }

            // Inspecting is taught by PhaseInspectEnemy, but the same lesson
            // reaches players outside the scripted tutorial too, so the
            // rewritten "press the I key" text still counts as a trigger
            if (!_inspectHintSpoken
                && (phase == "PhaseInspectEnemy"
                    || (text.IndexOf("the I key", System.StringComparison.OrdinalIgnoreCase) >= 0
                        && text.IndexOf("inspect", System.StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                _inspectHintSpoken = true;
                return " " + Loc.Get("tutorial_inspect_hint");
            }

            return "";
        }

        /// <summary>
        /// The type name of the tutorial phase currently driving prompts, or
        /// null outside the scripted tutorial. Phase class names are baked into
        /// the game assembly, so this identifies the lesson in every language —
        /// matching the prompt's words would only ever work in English.
        /// </summary>
        private static string ActiveTutorialPhase()
        {
            try
            {
                var system = Object.FindObjectOfType<TutorialParentSystem>();
                if (system == null)
                    return null;
                var phase = ReflectionUtil.GetField<object>(system, "currentPhase");
                return phase?.GetType().Name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The processed text of the currently visible tutorial prompt, or null.
        /// Used to explain WHY an action was refused (tutorial gates shake this
        /// prompt as their only feedback).
        /// </summary>
        public static string ActivePromptText()
        {
            try
            {
                Prompt prompt = PromptSystem.Prompt;
                if (prompt != null && prompt.active)
                {
                    string text = GetPromptText(prompt);
                    if (!string.IsNullOrEmpty(text))
                        return MakeDragAccessible(text);
                }
            }
            catch
            {
                // PromptSystem not initialized
            }
            return null;
        }

        /// <summary>
        /// Read all text from the help panel: title, body, and note.
        /// </summary>
        private static void OnHelpPanelOpened()
        {
            var panel = Object.FindObjectOfType<HelpPanelSystem>();
            if (panel == null) return;

            var texts = panel.GetComponentsInChildren<TMP_Text>(false);
            if (texts == null || texts.Length == 0) return;

            var parts = new System.Collections.Generic.List<string>();

            foreach (var txt in texts)
            {
                if (txt == null || !txt.gameObject.activeInHierarchy)
                    continue;

                string content = txt.text?.Trim();
                if (string.IsNullOrEmpty(content))
                    continue;

                // Strip rich text tags for clean reading
                content = TextProcessor.StripRichText(content);
                if (!string.IsNullOrEmpty(content))
                    parts.Add(content);
            }

            if (parts.Count > 0)
            {
                // Help panels give mouse instructions too ("drag", "click")
                string announcement = MakeDragAccessible(string.Join(". ", parts));

                // A popup with answer buttons (Retry/Skip, the give-up
                // confirm): say how to choose one, the first time only.
                if (!_buttonHintSpoken && HasChoiceButtons(panel))
                {
                    _buttonHintSpoken = true;
                    announcement += " " + Loc.Get("help_panel_hint");
                }

                ScreenReader.SayEvent(announcement, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, "PopupReader",
                    $"Help panel: {announcement}");
            }
        }

        /// <summary>Whether the popup spawned answer buttons (beyond the Back arrow).</summary>
        private static bool HasChoiceButtons(HelpPanelSystem panel)
        {
            var group = ReflectionUtil.GetField<Transform>(panel, "buttonGroup");
            return group != null && group.childCount > 0;
        }
    }
}
