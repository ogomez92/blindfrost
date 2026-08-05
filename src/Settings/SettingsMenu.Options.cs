using System;
using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The settings the menu offers, and their persistence. Each option knows
    /// how to say its own name and value and how to step to the next value;
    /// the menu in SettingsMenu.cs only moves between them.
    ///
    /// Settings are stored where they are cheapest to reach at the moment they
    /// are needed: the language in language.txt next to the DLL (see Loc), the
    /// rest in the game's own save data alongside the other progress keys.
    /// </summary>
    public static partial class SettingsMenu
    {
        /// <summary>One line in the settings menu.</summary>
        private class Option
        {
            public Func<string> Name;
            public Func<string> Value;
            public Action<int> Step;

            /// <summary>Re-speak the setting's name with the new value (the language switch).</summary>
            public bool ReannounceOnChange;

            public string DescribeName() => Name();
            public string DescribeValue() => Value();
            public void Change(int delta) => Step(delta);
        }

        private static List<Option> _options;

        private static List<Option> Options
        {
            get
            {
                if (_options == null)
                    _options = BuildOptions();
                return _options;
            }
        }

        private static List<Option> BuildOptions()
        {
            return new List<Option>
            {
                // Language comes first deliberately: it is the setting a player
                // who cannot understand the rest of the menu needs to reach, and
                // opening the menu lands on it.
                new Option
                {
                    Name = () => Loc.Get("settings_language"),
                    Value = DescribeLanguage,
                    Step = StepLanguage,
                    ReannounceOnChange = true,
                },
                new Option
                {
                    Name = () => Loc.Get("settings_detail"),
                    Value = () => Loc.Get(ItemDescriber.VerboseFocus
                        ? "settings_detail_full"
                        : "settings_detail_short"),
                    Step = _ => SetVerboseFocus(!ItemDescriber.VerboseFocus),
                },
                new Option
                {
                    Name = () => Loc.Get("settings_key_repeat"),
                    Value = () => Loc.Get(NavSpeedKeys[_navSpeed]),
                    Step = StepNavSpeed,
                },
                new Option
                {
                    Name = () => Loc.Get("settings_debug"),
                    Value = () => Loc.Get(DebugEnabled ? "settings_on" : "settings_off"),
                    Step = _ => SetDebug(!DebugEnabled),
                },
            };
        }

        // ---- Language ----------------------------------------------------------

        /// <summary>The choices for the language setting: automatic, then every registered language.</summary>
        private static List<string> LanguageChoices()
        {
            var choices = new List<string> { null }; // null = automatic
            choices.AddRange(Loc.AvailableLanguages());
            return choices;
        }

        private static string DescribeLanguage()
        {
            string selected = Loc.SelectedLanguage;
            if (selected == null)
                // Automatic is only useful if it also says what it resolved to
                return Loc.Get("settings_language_auto") + " ("
                    + Loc.LanguageName(Loc.GetCurrentLanguageCode()) + ")";

            string name = Loc.LanguageName(selected);
            if (!Loc.IsFullyTranslated(selected))
                name += ", " + Loc.Get("settings_language_partial");
            return name;
        }

        private static void StepLanguage(int delta)
        {
            var choices = LanguageChoices();
            int current = choices.IndexOf(Loc.SelectedLanguage);
            if (current < 0)
                current = 0;

            // Wraps: the list is long enough that walking back from the top is
            // the fastest way to the languages at the bottom
            int next = ((current + delta) % choices.Count + choices.Count) % choices.Count;

            if (!Loc.SetLanguage(choices[next]))
                DebugLogger.Log(DebugLogger.LogCategory.ScreenReader,
                    "Language choice could not be saved; it applies for this session only");
        }

        // ---- Detail level ------------------------------------------------------

        /// <summary>
        /// Apply and persist the focus detail level. Shared with the V key in
        /// Main so both routes leave the same state behind.
        /// </summary>
        public static void SetVerboseFocus(bool verbose)
        {
            ItemDescriber.VerboseFocus = verbose;
            try
            {
                // Stored as a string ("1"/"0"): SaveSystem.LoadProgressData<T>
                // constrains T to a reference type, so bool can't be used directly.
                SaveSystem.SaveProgressData("accessibilityVerboseFocus", verbose ? "1" : "0");
            }
            catch
            {
                // Not persisted this run; the setting still applies until quit
            }
        }

        // ---- Key repeat speed --------------------------------------------------

        /// <summary>Hold-to-repeat presets: initial delay and repeat interval, in seconds.</summary>
        private static readonly float[][] NavSpeeds =
        {
            new[] { 0.60f, 0.25f }, // slow
            new[] { 0.30f, 0.10f }, // normal (the mod's original timing)
            new[] { 0.15f, 0.05f }, // fast
        };

        private static readonly string[] NavSpeedKeys =
        {
            "settings_key_repeat_slow",
            "settings_key_repeat_normal",
            "settings_key_repeat_fast",
        };

        private static int _navSpeed = 1;

        private static void StepNavSpeed(int delta)
        {
            int next = _navSpeed + delta;
            if (next < 0) next = NavSpeeds.Length - 1;
            if (next >= NavSpeeds.Length) next = 0;
            SetNavSpeed(next);
        }

        private static void SetNavSpeed(int index)
        {
            _navSpeed = Math.Max(0, Math.Min(NavSpeeds.Length - 1, index));
            NavigationHelper.NavRepeatDelay = NavSpeeds[_navSpeed][0];
            NavigationHelper.NavRepeatRate = NavSpeeds[_navSpeed][1];
            try
            {
                SaveSystem.SaveProgressData("accessibilityNavSpeed", _navSpeed.ToString());
            }
            catch
            {
                // Not persisted this run; the setting still applies until quit
            }
        }

        // ---- Debug logging -----------------------------------------------------

        private static bool DebugEnabled =>
            WildfrostAccessibilityMod.Instance != null && WildfrostAccessibilityMod.Instance.debugMode;

        /// <summary>Apply the debug logging setting. Shared with the F10 key in Main.</summary>
        public static void SetDebug(bool enabled)
        {
            if (WildfrostAccessibilityMod.Instance != null)
                WildfrostAccessibilityMod.Instance.debugMode = enabled;
        }

        // ---- Startup -----------------------------------------------------------

        /// <summary>
        /// Restore every setting saved on a previous run. Called once during
        /// mod load, after Loc has resolved the language.
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                string stored = SaveSystem.LoadProgressData<string>("accessibilityVerboseFocus");
                ItemDescriber.VerboseFocus = stored == "1";
            }
            catch
            {
                ItemDescriber.VerboseFocus = false;
            }

            int speed = 1;
            try
            {
                string stored = SaveSystem.LoadProgressData<string>("accessibilityNavSpeed");
                if (!string.IsNullOrEmpty(stored) && int.TryParse(stored, out int parsed))
                    speed = parsed;
            }
            catch
            {
                // Keep the default timing
            }

            _navSpeed = Math.Max(0, Math.Min(NavSpeeds.Length - 1, speed));
            NavigationHelper.NavRepeatDelay = NavSpeeds[_navSpeed][0];
            NavigationHelper.NavRepeatRate = NavSpeeds[_navSpeed][1];
        }
    }
}
