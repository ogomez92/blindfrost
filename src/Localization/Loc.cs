using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The lookup engine for screen reader strings: the language-keyed string
    /// table, the reads against it (Get, TryGet) and the writes into it (Add,
    /// AddForAll). Serves translations for the game's active locale, falling
    /// back current language -> English -> key name. Also holds the language
    /// override read from language.txt, which lets the mod speak a language
    /// the game itself does not offer. The string data lives in the
    /// Loc.Strings.* parts.
    /// </summary>
    public static partial class Loc
    {
        // language code -> (key -> translation)
        private static readonly Dictionary<string, Dictionary<string, string>> _strings
            = new Dictionary<string, Dictionary<string, string>>();

        // When set, overrides the game locale for all mod speech (language.txt)
        private static string _overrideLang;

        /// <summary>
        /// Initialize and register all screen reader strings.
        /// Called once during mod load.
        /// </summary>
        public static void Initialize()
        {
            RegisterDefaults();
        }

        /// <summary>
        /// Get a localized string for the current game language.
        /// </summary>
        public static string Get(string key)
        {
            string langCode = GetCurrentLanguageCode();

            // Try current language
            if (_strings.TryGetValue(langCode, out var langDict) && langDict.TryGetValue(key, out var text))
                return text;

            // Fall back to English
            if (langCode != "en" && _strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enText))
                return enText;

            // Last resort: return key
            DebugLogger.Log(DebugLogger.LogCategory.ScreenReader, $"Missing localization: [{langCode}] {key}");
            return key;
        }

        /// <summary>
        /// Try to get a localized string. Returns false if the key is unknown
        /// in both the current language and the English fallback.
        /// </summary>
        public static bool TryGet(string key, out string text)
        {
            string langCode = GetCurrentLanguageCode();

            if (_strings.TryGetValue(langCode, out var langDict) && langDict.TryGetValue(key, out text))
                return true;

            if (_strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out text))
                return true;

            text = null;
            return false;
        }

        /// <summary>
        /// Get a localized string with template parameters.
        /// Use {0}, {1}, etc. as placeholders.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Add a localized string for a specific language.
        /// </summary>
        public static void Add(string langCode, string key, string value)
        {
            if (!_strings.TryGetValue(langCode, out var langDict))
            {
                langDict = new Dictionary<string, string>();
                _strings[langCode] = langDict;
            }
            langDict[key] = value;
        }

        /// <summary>
        /// Add a string for all languages at once (convenience for language-neutral strings).
        /// </summary>
        public static void AddForAll(string key, string value)
        {
            foreach (var lang in _strings.Keys)
            {
                _strings[lang][key] = value;
            }
            // Also ensure English has it as the ultimate fallback
            Add("en", key, value);
        }

        /// <summary>
        /// Apply the language override from [modDirectory]/language.txt, if
        /// present. The game itself only offers English, Japanese, Korean and
        /// Chinese, so the mod's other translations (Spanish, German,
        /// French...) can only be reached this way. The file holds a single
        /// language code such as "es"; a missing or empty file follows the
        /// game's language setting. Must run after Initialize().
        /// </summary>
        public static void LoadLanguageOverride(string modDirectory)
        {
            try
            {
                string path = Path.Combine(modDirectory, "language.txt");
                if (!File.Exists(path))
                    return;

                string code = File.ReadAllText(path).Trim();
                if (code.Length == 0)
                    return;

                // Normalize case against the registered languages ("ES" → "es",
                // "zh-hans" → "zh-Hans"); unknown codes follow the game language
                foreach (string lang in _strings.Keys)
                {
                    if (string.Equals(lang, code, StringComparison.OrdinalIgnoreCase))
                    {
                        _overrideLang = lang;
                        return;
                    }
                }
                DebugLogger.Log(DebugLogger.LogCategory.ScreenReader,
                    $"language.txt has unknown code '{code}'; following the game language");
            }
            catch
            {
                // Unreadable file — follow the game language
            }
        }

        /// <summary>
        /// Returns the locale code of the game's currently selected language,
        /// unless language.txt overrides it.
        /// </summary>
        public static string GetCurrentLanguageCode()
        {
            if (_overrideLang != null)
                return _overrideLang;

            try
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                if (locale != null)
                    return locale.Identifier.Code;
            }
            catch
            {
                // Localization system not ready yet
            }
            return "en";
        }
    }
}
