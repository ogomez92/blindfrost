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

            ScreenReader.Say(Loc.Get("tutorial_prompt", text), interrupt: false);
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
        /// Replace "drag" instructions with keyboard/gamepad accessible language.
        /// Blind players use select-and-place, not drag-and-drop.
        /// </summary>
        private static string MakeDragAccessible(string text)
        {
            // "Right Click" inspects a card with a mouse; the mod binds I for
            // keyboard users ("Press Right Click on any card to Inspect it")
            text = Regex.Replace(text,
                @"\bright[\s-]?click(ing)?\b",
                "the I key",
                RegexOptions.IgnoreCase);

            // "drag X from Y on to Z" → "select X from Y and place it on Z"
            text = Regex.Replace(text,
                @"\bdrag\b(.+?)\bfrom\b(.+?)\bon\s*to\b",
                "select$1from$2and place it on",
                RegexOptions.IgnoreCase);

            // "drag X to Y" → "select X and place it on Y"
            text = Regex.Replace(text,
                @"\bdrag\b(.+?)\bto\b",
                "select$1and place it on",
                RegexOptions.IgnoreCase);

            // "drag X onto Y" → "select X and place it on Y"
            text = Regex.Replace(text,
                @"\bdrag\b(.+?)\bonto\b",
                "select$1and place it on",
                RegexOptions.IgnoreCase);

            // Generic remaining "drag" → "select and place"
            text = Regex.Replace(text,
                @"\bdrag\b",
                "select and place",
                RegexOptions.IgnoreCase);

            return text;
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
                string announcement = string.Join(". ", parts);
                ScreenReader.Say(announcement, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, "PopupReader",
                    $"Help panel: {announcement}");
            }
        }
    }
}
