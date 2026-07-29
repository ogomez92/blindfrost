using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Watches the cinema bars (the letterbox strips at the top and bottom of
    /// the screen) and announces their text whenever it appears or changes.
    /// Story events have no TitleSetter — the cinema bar IS their title and
    /// story channel: "Break the ice!", "Choose a new companion!", shop enter
    /// prompts, intro narration, the final boss script all flow through it.
    /// </summary>
    public static class CinemaBarReader
    {
        private static string _lastTopScript = "";
        private static string _lastBottomScript = "";
        private static string _lastTopPrompt = "";
        private static string _lastBottomPrompt = "";
        private static bool _wasActive;

        /// <summary>
        /// Poll the bars for text changes. Called every frame from the main
        /// update loop, AFTER ScreenManager.Update: screen handlers announce
        /// with interrupt, so they must speak first and sync this reader —
        /// otherwise the screen announcement would cut the bar text off.
        /// </summary>
        public static void Update()
        {
            if (!IsBarActive())
            {
                // Reset when the bars hide, so the same text re-announces
                // if it is shown again later (e.g. after an inspect closes).
                if (_wasActive)
                {
                    _wasActive = false;
                    _lastTopScript = _lastBottomScript = "";
                    _lastTopPrompt = _lastBottomPrompt = "";
                }
                return;
            }
            _wasActive = true;

            // Scripts first (story text), then prompts (the action line)
            AnnounceIfChanged(GetTopScript(), ref _lastTopScript, isPrompt: false);
            AnnounceIfChanged(GetBottomScript(), ref _lastBottomScript, isPrompt: false);
            AnnounceIfChanged(GetTopPrompt(), ref _lastTopPrompt, isPrompt: true);
            AnnounceIfChanged(GetBottomPrompt(), ref _lastBottomPrompt, isPrompt: true);
        }

        /// <summary>
        /// All text currently on the bars, composed for a screen announcement
        /// (scripts, then prompts, then the key hint). Null if nothing shown.
        /// </summary>
        public static string CurrentText()
        {
            if (!IsBarActive())
                return null;

            var parts = new List<string>();
            AddPart(parts, GetTopScript());
            AddPart(parts, GetBottomScript());
            AddPart(parts, GetTopPrompt());
            AddPart(parts, GetBottomPrompt());
            if (parts.Count == 0)
                return null;

            string text = string.Join(" ", parts);
            if (HasPromptAction())
                text += " " + Loc.Get("event_prompt_action");
            return text;
        }

        /// <summary>The active prompt line ("Break the ice!"), or null.</summary>
        public static string CurrentPrompt()
        {
            if (!IsBarActive())
                return null;
            string prompt = GetTopPrompt();
            if (string.IsNullOrEmpty(prompt))
                prompt = GetBottomPrompt();
            return string.IsNullOrEmpty(prompt) ? null : prompt;
        }

        /// <summary>
        /// Mark everything currently shown as already announced. Screen
        /// handlers call this after including CurrentText() in their own
        /// announcement, so this reader does not repeat it.
        /// </summary>
        public static void SyncAnnounced()
        {
            if (!IsBarActive())
                return;
            _wasActive = true;
            _lastTopScript = GetTopScript();
            _lastBottomScript = GetBottomScript();
            _lastTopPrompt = GetTopPrompt();
            _lastBottomPrompt = GetBottomPrompt();
        }

        private static void AnnounceIfChanged(string current, ref string last, bool isPrompt)
        {
            if (current == last)
                return;
            last = current;
            if (string.IsNullOrEmpty(current))
                return;

            string text = current;
            if (isPrompt && HasPromptAction())
                text += " " + Loc.Get("event_prompt_action");

            // Queue, don't interrupt: bar text often lands right after another
            // announcement (a hit, a focus change) that must finish first.
            ScreenReader.SayEvent(text, interrupt: false);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "CinemaBarReader",
                $"Bar text: {text}");
        }

        private static void AddPart(List<string> parts, string text)
        {
            if (!string.IsNullOrEmpty(text))
                parts.Add(text);
        }

        /// <summary>True when a visible prompt has an input action bound (the
        /// on-screen mouse/gamepad glyph) — Enter activates it for us.</summary>
        private static bool HasPromptAction()
        {
            try
            {
                var top = CinemaBarSystem.Top;
                var bottom = CinemaBarSystem.Bottom;
                return (top?.buttonImage != null && !string.IsNullOrEmpty(top.buttonImage.actionName))
                    || (bottom?.buttonImage != null && !string.IsNullOrEmpty(bottom.buttonImage.actionName));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBarActive()
        {
            try
            {
                // Top/Bottom are set in CinemaBarSystem.Awake; IsActive throws
                // while no instance exists (early boot, main menu).
                return CinemaBarSystem.Top != null && CinemaBarSystem.IsActive();
            }
            catch
            {
                return false;
            }
        }

        private static string GetTopScript() => Clean(CinemaBarSystem.Top?.script?.text);
        private static string GetBottomScript() => Clean(CinemaBarSystem.Bottom?.script?.text);
        private static string GetTopPrompt() => Clean(CinemaBarSystem.Top?.prompt?.text);
        private static string GetBottomPrompt() => Clean(CinemaBarSystem.Bottom?.prompt?.text);

        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return TextProcessor.StripRichText(text)?.Trim() ?? "";
        }
    }
}
