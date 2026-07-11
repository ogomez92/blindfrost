using UnityEngine;
using TMPro;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Fallback accessibility handler for any screen without a specialized handler.
    /// Announces the screen name, focused UI elements, and provides arrow key navigation.
    /// </summary>
    public class GenericScreenHandler : NavigableScreenHandler
    {
        public override string Name => $"Generic({_sceneName})";

        private string _sceneName;
        private int _titleRetries;
        private float _nextTitleTry;

        public void SetScene(string sceneName)
        {
            _sceneName = sceneName ?? "Unknown";
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _titleRetries = 0;
            _nextTitleTry = 0f;
        }

        /// <summary>
        /// Announce the screen name: curated localized name for known scenes,
        /// otherwise the game's own title UI, otherwise the cleaned scene name.
        /// </summary>
        protected override bool TryAnnounceScreen()
        {
            // Known scenes have a curated localized name — always correct, available instantly
            if (Loc.TryGet("scene_" + _sceneName, out string knownName))
            {
                ScreenReader.Say(knownName, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Screen title (known scene): {knownName}");
                return true;
            }

            // Unknown scene: look for the game's title UI, retrying while it loads
            if (Time.unscaledTime < _nextTitleTry)
                return false;

            string title = FindScreenTitle();
            if (title != null)
            {
                ScreenReader.Say(title, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Screen title: {title}");
                return true;
            }

            _titleRetries++;
            if (_titleRetries >= 4)
            {
                // Give up and announce the scene name
                ScreenReader.Say(CleanName(_sceneName), interrupt: true);
                return true;
            }

            _nextTitleTry = Time.unscaledTime + 0.5f;
            return false;
        }

        /// <summary>
        /// Search the scene for screen title text.
        /// Only trusts explicit title markers (TitleSetter, objects named "title"/"header").
        /// No font-size guessing: it picked up random tooltip text (e.g. "Apply 10 Snow" in Town).
        /// </summary>
        private string FindScreenTitle()
        {
            string title = FindTitleFromTitleSetter();
            if (title != null) return title;

            return FindTitleByObjectName();
        }

        /// <summary>Find title from TitleSetter components (game's primary title system).</summary>
        private string FindTitleFromTitleSetter()
        {
            var titleSetters = Object.FindObjectsOfType<TitleSetter>();
            foreach (var setter in titleSetters)
            {
                if (setter == null || !setter.gameObject.activeInHierarchy)
                    continue;

                // TitleSetter has a child TMP_Text via LocalizeStringEvent
                var tmp = setter.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    string text = tmp.text?.Trim();
                    if (!string.IsNullOrEmpty(text) && text.Length >= 3)
                        return text;
                }
            }
            return null;
        }

        /// <summary>Find title by searching for GameObjects named "title", "header", etc.</summary>
        private string FindTitleByObjectName()
        {
            var allTexts = Object.FindObjectsOfType<TMP_Text>();
            if (allTexts == null) return null;

            // Priority list of name patterns to look for (case-insensitive)
            string[] titlePatterns = { "title", "header", "heading", "pagetitle", "screentitle" };

            foreach (var pattern in titlePatterns)
            {
                foreach (var txt in allTexts)
                {
                    if (txt == null || !txt.gameObject.activeInHierarchy)
                        continue;

                    // Skip text that belongs to cards
                    if (IsCardText(txt))
                        continue;

                    // Check this object and its parents for the pattern
                    Transform check = txt.transform;
                    int depth = 0;
                    while (check != null && depth < 3)
                    {
                        if (check.name.ToLowerInvariant().Contains(pattern))
                        {
                            string content = txt.text?.Trim();
                            if (!string.IsNullOrEmpty(content) && content.Length >= 3 && content.Length <= 100)
                                return content;
                        }
                        check = check.parent;
                        depth++;
                    }
                }
            }
            return null;
        }

        /// <summary>Check if a TMP_Text belongs to a card/entity (not a screen title).</summary>
        private static bool IsCardText(TMP_Text txt)
        {
            return txt.GetComponentInParent<Entity>() != null
                || txt.GetComponentInParent<Card>() != null
                || txt.GetComponentInParent<CardPopUpPanel>() != null;
        }
    }
}
