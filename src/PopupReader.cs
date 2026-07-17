using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Detects and reads overlay popups that appear on top of any screen.
    /// Help panels, tutorial prompts, dialogs — anything that isn't a full scene change.
    /// </summary>
    public static class PopupReader
    {
        private static bool _helpPanelWasActive;
        private static bool _promptWasActive;
        private static string _lastPromptText;
        private static int _promptReadDelay;
        private static bool _buttonHintSpoken;

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

            ScreenReader.SayEvent(Loc.Get("tutorial_prompt", text), interrupt: false);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "PopupReader",
                $"Tutorial prompt: {text}");
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
        /// Replace mouse instructions (drag, click) with keyboard commands.
        /// Blind players pick up and place with Enter instead of dragging,
        /// and inspect with the I key instead of Right Click.
        /// </summary>
        private static string MakeDragAccessible(string text)
        {
            // "Right Click" inspects a card with a mouse; the mod binds I for
            // keyboard users. Collapse a leading "press" first so the rule
            // below can't produce "press press the I key".
            text = Regex.Replace(text,
                @"\bpress\s+(right[\s-]?click)\b",
                "$1",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text,
                @"\bright[\s-]?clicking\b",
                "pressing the I key",
                RegexOptions.IgnoreCase);
            // "Right Click [on] any card" / "You can Right Click the X"
            // → "press the I key on any card" / "You can press the I key on the X"
            text = Regex.Replace(text,
                @"\bright[\s-]?click\b\s*(on\s+)?(?=(a|an|any|the|your|it)\b)",
                "press the I key on ",
                RegexOptions.IgnoreCase);
            // Any other mention just names the key
            text = Regex.Replace(text,
                @"\bright[\s-]?click\b",
                "the I key",
                RegexOptions.IgnoreCase);

            // "Left Click" is the game's display name for the confirm action
            // ("Use Left Click to Inspect Charms") — the mod's confirm is Enter
            text = Regex.Replace(text,
                @"\bleft[\s-]?clicking\b",
                "pressing Enter",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text,
                @"\bleft[\s-]?click\b",
                "Enter",
                RegexOptions.IgnoreCase);

            // Remaining generic clicks activate the focused item
            text = Regex.Replace(text,
                @"\bclicking\b(\s+on\b)?",
                "pressing Enter on",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text,
                @"\bclick\b(\s+on\b)?",
                "press Enter on",
                RegexOptions.IgnoreCase);

            // Drag wording becomes the mod's select-and-place model. Structural
            // rules keep the sentence's destination phrase intact; sentence
            // punctuation bounds the match so a "to" in a later sentence can't
            // swallow text. RewriteDrag keeps gerunds ("dragging" → "selecting").
            string beforeDrag = text;

            // "drag X from Y on to Z" → "select X from Y and place it on Z"
            text = RewriteDrag(text, @"(?<x>[^.!?]+?\bfrom\b[^.!?]+?)\bon\s?to\b", "it on");

            // "drag X in front of Y to …": the destination phrase has no "to",
            // and the generic to-rule below would cut the sentence at the
            // unrelated "to protect them" instead
            text = RewriteDrag(text, @"(?<x>[^.!?]*?\S[^.!?]*?)\bin\s+front\s+of\b", "it in front of");

            // "drag X onto Y" / "drag X on to Y" → "select X and place it on Y"
            text = RewriteDrag(text, @"(?<x>[^.!?]*?\S[^.!?]*?)\bon\s?to\b", "it on");

            // "drag X to Y" → "select X and place it on Y". The subject must
            // have content: matching is case-insensitive, so a bare label like
            // "Drag To Feed" must not treat its "To" as the destination marker
            text = RewriteDrag(text, @"(?<x>[^.!?]*?\S[^.!?]*?)\bto\b", "it on");

            // Bare mentions with no destination in the sentence
            text = Regex.Replace(text, @"\bdragging\b", "selecting and placing",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bdrag\b", "select and place",
                RegexOptions.IgnoreCase);

            // Any rewritten drag instruction gets the keyboard how-to appended.
            // "In front of a unit" placements are chosen by selecting that unit
            // (your card takes its slot and pushes it back), which the generic
            // "choose the destination" wording doesn't make obvious.
            if (text != beforeDrag)
            {
                bool inFront = Regex.IsMatch(text, @"\bin front of\b", RegexOptions.IgnoreCase);
                text += " " + Loc.Get(inFront ? "tutorial_drag_hint_infront" : "tutorial_drag_hint");
            }

            return text;
        }

        /// <summary>
        /// Rewrite one "drag …" sentence shape, keeping the verb form:
        /// "Drag X on to Y" → "select X and place it on Y",
        /// "by dragging X on to Y" → "by selecting X and placing it on Y".
        /// The pattern must capture the dragged thing as group "x".
        /// </summary>
        private static string RewriteDrag(string text, string tailPattern, string destination)
        {
            return Regex.Replace(text,
                @"\bdrag(?<ing>ging)?\b" + tailPattern,
                m => m.Groups["ing"].Success
                    ? "selecting" + m.Groups["x"].Value + "and placing " + destination
                    : "select" + m.Groups["x"].Value + "and place " + destination,
                RegexOptions.IgnoreCase);
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
