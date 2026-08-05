using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The lookup engine for screen reader strings: the language-keyed string
    /// table, the reads against it (Get, TryGet) and the writes into it (Add,
    /// AddForAll). Also owns which language the mod speaks — the explicit
    /// choice made in the settings menu (F2), which is persisted to
    /// language.txt and lets the mod speak a language the game itself does not
    /// offer, and the automatic fallback chain used when no choice has been
    /// made. The string data lives in the Loc.Strings.* parts.
    /// </summary>
    public static partial class Loc
    {
        // language code -> (key -> translation)
        private static readonly Dictionary<string, Dictionary<string, string>> _strings
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>Marker stored in language.txt for "follow the system".</summary>
        public const string Automatic = "auto";

        /// <summary>
        /// A key that only the full per-screen handler tables define, so its
        /// presence tells a fully translated language from one that has the
        /// core announcements only. See Loc.Strings.Handlers.*.cs.
        /// </summary>
        private const string FullCoverageKey = "nav_nothing";

        /// <summary>
        /// Display order for the language list in the settings menu: the fully
        /// translated languages first, then the ones with core coverage.
        /// </summary>
        private static readonly string[] LanguageOrder =
        {
            "en", "de", "es", "fr",
            "it", "pt", "ru", "pl", "tr", "ja", "ko", "zh-Hans", "zh-Hant",
        };

        /// <summary>Each language's name in its own language, for the settings menu.</summary>
        private static readonly Dictionary<string, string> Endonyms
            = new Dictionary<string, string>
            {
                { "en", "English" },
                { "de", "Deutsch" },
                { "es", "Español" },
                { "fr", "Français" },
                { "it", "Italiano" },
                { "pt", "Português" },
                { "ru", "Русский" },
                { "pl", "Polski" },
                { "tr", "Türkçe" },
                { "ja", "日本語" },
                { "ko", "한국어" },
                { "zh-Hans", "简体中文" },
                { "zh-Hant", "繁體中文" },
            };

        // The explicit choice from the settings menu / language.txt, or null
        // while the mod follows the automatic chain.
        private static string _overrideLang;

        // Where language.txt lives; captured at load so the settings menu can
        // write the choice back without threading the path through the UI.
        private static string _modDirectory;

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

        // ---- Language selection ------------------------------------------------

        /// <summary>
        /// Restore the language chosen in the settings menu. The choice lives in
        /// [modDirectory]/language.txt — a single language code such as "es", or
        /// "auto" to follow the system. The file doubles as a hand-editable
        /// setting for players who prefer that; a save-data copy is used only as
        /// a fallback for installs where the mod folder is read-only.
        /// Must run after Initialize().
        /// </summary>
        public static void LoadLanguageOverride(string modDirectory)
        {
            _modDirectory = modDirectory;
            _overrideLang = null;

            string stored = ReadLanguageFile(modDirectory) ?? ReadLanguageSaveData();
            if (string.IsNullOrEmpty(stored))
                return;

            if (IsAutomatic(stored))
                return;

            string resolved = NormalizeLanguage(stored);
            if (resolved != null)
                _overrideLang = resolved;
            else
                DebugLogger.Log(DebugLogger.LogCategory.ScreenReader,
                    $"language.txt has unknown code '{stored}'; following the system language");
        }

        /// <summary>
        /// Choose the language the mod speaks, applied immediately and kept for
        /// the next launch. Pass null or "auto" to follow the system again.
        /// Returns false if the choice could not be persisted (it still applies
        /// for this session).
        /// </summary>
        public static bool SetLanguage(string langCode)
        {
            _overrideLang = IsAutomatic(langCode) ? null : NormalizeLanguage(langCode);

            string stored = _overrideLang ?? Automatic;
            bool persisted = WriteLanguageFile(stored);
            if (!persisted)
                persisted = WriteLanguageSaveData(stored);
            return persisted;
        }

        /// <summary>The explicit language choice, or null while following the system.</summary>
        public static string SelectedLanguage => _overrideLang;

        /// <summary>Registered language codes, fully translated ones first.</summary>
        public static List<string> AvailableLanguages()
        {
            var codes = new List<string>();
            foreach (string code in LanguageOrder)
            {
                if (_strings.ContainsKey(code))
                    codes.Add(code);
            }
            // Any language registered without a place in the display order
            foreach (string code in _strings.Keys)
            {
                if (!codes.Contains(code))
                    codes.Add(code);
            }
            return codes;
        }

        /// <summary>
        /// Whether this language has the per-screen handler strings, not just
        /// the core announcements. Partial languages fall back to English for
        /// everything else, which the settings menu says out loud.
        /// </summary>
        public static bool IsFullyTranslated(string langCode)
        {
            return langCode != null
                && _strings.TryGetValue(langCode, out var dict)
                && dict.ContainsKey(FullCoverageKey);
        }

        /// <summary>The language's own name for itself ("Español"), for the settings menu.</summary>
        public static string LanguageName(string langCode)
        {
            if (langCode != null && Endonyms.TryGetValue(langCode, out string name))
                return name;
            return langCode ?? "";
        }

        /// <summary>
        /// Returns the locale code the mod speaks: the explicit choice if one
        /// was made, otherwise the automatic chain — the operating system's
        /// language (the mod talks through the system screen reader, so that is
        /// the language the player already listens in), then the game's own
        /// language setting, then English.
        /// </summary>
        public static string GetCurrentLanguageCode()
        {
            if (_overrideLang != null)
                return _overrideLang;

            string system = SystemLanguageCode();
            if (system != null)
                return system;

            string game = GameLanguageCode();
            if (game != null)
                return game;

            return "en";
        }

        // Get() runs several times per announcement, so the automatic chain is
        // resolved once rather than re-derived on every lookup.
        private static bool _systemLangResolved;
        private static string _systemLang;
        private static string _lastGameCode;
        private static string _lastGameLang;

        /// <summary>
        /// The operating system's language, if the mod has strings for it.
        /// Resolved once — the OS language cannot change while the game runs.
        /// </summary>
        private static string SystemLanguageCode()
        {
            if (_systemLangResolved)
                return _systemLang;

            _systemLangResolved = true;

            // Unity's own reading of the OS language first: Unity is free to
            // run the scripting threads under the invariant culture, in which
            // case CurrentUICulture reports nothing at all.
            try
            {
                _systemLang = FromUnitySystemLanguage(Application.systemLanguage);
                if (_systemLang != null)
                    return _systemLang;
            }
            catch
            {
                // Fall through to the culture-based reading
            }

            try
            {
                CultureInfo culture = CultureInfo.CurrentUICulture;
                while (culture != null && !string.IsNullOrEmpty(culture.Name))
                {
                    // "es-ES" -> "es"; Chinese keeps its script tag ("zh-Hans")
                    string exact = NormalizeLanguage(culture.Name);
                    if (exact != null)
                    {
                        _systemLang = exact;
                        break;
                    }
                    culture = culture.Parent;
                }
            }
            catch
            {
                // No culture information — fall through to the game language
            }
            return _systemLang;
        }

        /// <summary>
        /// Map Unity's system language to a registered language code. Only the
        /// languages the mod actually speaks are listed; anything else falls
        /// through to the culture reading and then to the game's own setting.
        /// </summary>
        private static string FromUnitySystemLanguage(SystemLanguage language)
        {
            string code;
            switch (language)
            {
                case SystemLanguage.English: code = "en"; break;
                case SystemLanguage.German: code = "de"; break;
                case SystemLanguage.Spanish: code = "es"; break;
                case SystemLanguage.French: code = "fr"; break;
                case SystemLanguage.Italian: code = "it"; break;
                case SystemLanguage.Portuguese: code = "pt"; break;
                case SystemLanguage.Russian: code = "ru"; break;
                case SystemLanguage.Polish: code = "pl"; break;
                case SystemLanguage.Turkish: code = "tr"; break;
                case SystemLanguage.Japanese: code = "ja"; break;
                case SystemLanguage.Korean: code = "ko"; break;
                case SystemLanguage.ChineseSimplified: code = "zh-Hans"; break;
                case SystemLanguage.ChineseTraditional: code = "zh-Hant"; break;
                // Plain "Chinese" predates the split and does not say which
                case SystemLanguage.Chinese: code = "zh-Hans"; break;
                default: return null;
            }
            return _strings.ContainsKey(code) ? code : null;
        }

        /// <summary>
        /// The game's selected locale, if the mod has strings for it. The player
        /// can change this mid-session, so it is re-read every time and only the
        /// code-to-language match is cached.
        /// </summary>
        private static string GameLanguageCode()
        {
            try
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                if (locale == null)
                    return null;

                string code = locale.Identifier.Code;
                if (code != _lastGameCode)
                {
                    _lastGameCode = code;
                    _lastGameLang = NormalizeLanguage(code);
                }
                return _lastGameLang;
            }
            catch
            {
                // Localization system not ready yet
            }
            return null;
        }

        /// <summary>
        /// Match a locale code against the registered languages, ignoring case
        /// ("ES" -> "es") and region ("es-MX" -> "es"). Chinese is matched on
        /// its script tag first so "zh-Hant-TW" does not collapse onto "zh-Hans".
        /// Returns null when no language matches.
        /// </summary>
        private static string NormalizeLanguage(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            foreach (string lang in _strings.Keys)
            {
                if (string.Equals(lang, code, StringComparison.OrdinalIgnoreCase))
                    return lang;
            }

            // Chinese: the script tag decides, and Taiwan/Hong Kong/Macau are traditional
            if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                bool traditional = code.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0
                    || code.IndexOf("-TW", StringComparison.OrdinalIgnoreCase) >= 0
                    || code.IndexOf("-HK", StringComparison.OrdinalIgnoreCase) >= 0
                    || code.IndexOf("-MO", StringComparison.OrdinalIgnoreCase) >= 0;
                string chinese = traditional ? "zh-Hant" : "zh-Hans";
                return _strings.ContainsKey(chinese) ? chinese : null;
            }

            // Drop the region: "pt-BR" -> "pt"
            int dash = code.IndexOf('-');
            if (dash > 0)
            {
                string bare = code.Substring(0, dash);
                foreach (string lang in _strings.Keys)
                {
                    if (string.Equals(lang, bare, StringComparison.OrdinalIgnoreCase))
                        return lang;
                }
            }

            return null;
        }

        private static bool IsAutomatic(string code)
        {
            return string.IsNullOrEmpty(code)
                || string.Equals(code, Automatic, StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, "automatic", StringComparison.OrdinalIgnoreCase);
        }

        // ---- Persistence -------------------------------------------------------

        private static string ReadLanguageFile(string modDirectory)
        {
            try
            {
                string path = Path.Combine(modDirectory, "language.txt");
                if (!File.Exists(path))
                    return null;
                string code = File.ReadAllText(path).Trim();
                return code.Length == 0 ? null : code;
            }
            catch
            {
                return null; // Unreadable file — fall back to the save data
            }
        }

        private static bool WriteLanguageFile(string stored)
        {
            if (string.IsNullOrEmpty(_modDirectory))
                return false;
            try
            {
                File.WriteAllText(Path.Combine(_modDirectory, "language.txt"), stored);
                return true;
            }
            catch
            {
                // Read-only install (a game folder needing admin rights) —
                // the caller falls back to the save file
                return false;
            }
        }

        private static string ReadLanguageSaveData()
        {
            try
            {
                return SaveSystem.LoadProgressData<string>("accessibilityLanguage");
            }
            catch
            {
                return null;
            }
        }

        private static bool WriteLanguageSaveData(string stored)
        {
            try
            {
                SaveSystem.SaveProgressData("accessibilityLanguage", stored);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
