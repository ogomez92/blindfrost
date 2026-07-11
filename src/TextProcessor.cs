using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Processes game card/effect text into screen-reader-friendly plain text.
    /// Expands keyword tags into readable names with descriptions.
    /// Strips all rich text formatting (color, size, sprites, bold, etc.)
    /// </summary>
    public static class TextProcessor
    {
        // Cache keyword descriptions to avoid repeated lookups
        private static readonly Dictionary<string, KeywordInfo> _keywordCache
            = new Dictionary<string, KeywordInfo>();

        private struct KeywordInfo
        {
            public string Title;
            public string Description;
            public string Note;
        }

        /// <summary>
        /// Process raw card description text into plain screen-reader text.
        /// Expands keywords, strips formatting, appends keyword descriptions.
        /// </summary>
        public static string ProcessForScreenReader(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return "";

            var mentionedKeywords = new List<KeywordInfo>();
            string plainText = ExpandTags(rawText, mentionedKeywords);

            // Strip any remaining rich text tags
            plainText = StripRichText(plainText);

            // Clean up whitespace
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            if (string.IsNullOrEmpty(plainText))
                return "";

            // Append keyword descriptions
            if (mentionedKeywords.Count > 0)
            {
                var sb = new StringBuilder(plainText);
                var seen = new HashSet<string>();
                foreach (var kw in mentionedKeywords)
                {
                    if (seen.Contains(kw.Title)) continue;
                    seen.Add(kw.Title);

                    if (!string.IsNullOrEmpty(kw.Description))
                    {
                        string desc = StripRichText(kw.Description);
                        sb.Append($". {kw.Title}: {desc}");
                    }
                    if (!string.IsNullOrEmpty(kw.Note))
                    {
                        string note = StripRichText(kw.Note);
                        sb.Append($". Note: {note}");
                    }
                }
                return sb.ToString();
            }

            return plainText;
        }

        /// <summary>
        /// Process text expanding custom game tags into readable text.
        /// Collects keyword info for appending descriptions.
        /// </summary>
        private static string ExpandTags(string text, List<KeywordInfo> keywords)
        {
            var sb = new StringBuilder();
            int i = 0;
            int len = text.Length;

            while (i < len)
            {
                if (text[i] == '<')
                {
                    int closeIdx = text.IndexOf('>', i);
                    if (closeIdx < 0)
                    {
                        sb.Append(text[i]);
                        i++;
                        continue;
                    }

                    string tagContent = text.Substring(i + 1, closeIdx - i - 1);
                    i = closeIdx + 1;

                    string expanded = ProcessTag(tagContent, keywords);
                    if (expanded != null)
                        sb.Append(expanded);
                }
                else
                {
                    sb.Append(text[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Process a single tag and return its plain text representation.
        /// </summary>
        private static string ProcessTag(string tag, List<KeywordInfo> keywords)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            // Closing tags like </color>, </b>, </s>, </size> — skip
            if (tag[0] == '/')
                return null;

            // Numeric tags: <3>, <+2>, <-1>, <x2> — these are effect amounts
            char first = tag[0];
            bool isNumeric = char.IsDigit(first) || first == '+' || first == '-' || first == 'x';
            if (isNumeric)
            {
                string numStr = Regex.Replace(tag, "[^0-9]", "");
                if (int.TryParse(numStr, out int amount))
                {
                    string prefix = (first == '+' || first == '-' || first == 'x') ? first.ToString() : "";
                    return $"{prefix}{amount} ";
                }
                return tag;
            }

            // key=value tags
            int eqIdx = tag.IndexOf('=');
            if (eqIdx > 0)
            {
                string key = tag.Substring(0, eqIdx).Trim();
                string value = tag.Substring(eqIdx + 1).Trim();

                switch (key)
                {
                    case "keyword":
                        return ProcessKeywordTag(value, keywords);
                    case "card":
                        return ProcessCardTag(value);
                    case "sprite":
                    case "sprite name":
                    case "spr":
                        // Sprite icons — skip entirely
                        return null;
                    case "color":
                    case "size":
                        // Formatting tags — skip
                        return null;
                    default:
                        return null;
                }
            }

            // Single character tags like <b>, <s>, <i> — skip
            if (tag.Length <= 2)
                return null;

            // Unknown tags — skip formatting ones, return others as text
            if (tag == "b" || tag == "s" || tag == "i" || tag == "u")
                return null;

            return null;
        }

        /// <summary>
        /// Process a keyword tag like "shell", "shell 5", or "shell 5 silenced".
        /// Returns the keyword title with count, and collects the keyword info.
        /// </summary>
        private static string ProcessKeywordTag(string value, List<KeywordInfo> keywords)
        {
            string[] parts = value.Split(' ');
            string keywordName = parts[0];
            bool silenced = parts.Length > 2 && parts[2] == "silenced";

            // Get keyword data
            var kwInfo = GetKeywordInfo(keywordName);

            // Build display text
            var sb = new StringBuilder();

            if (silenced)
                sb.Append("(silenced) ");

            sb.Append(kwInfo.Title);

            // Add stack count if present
            if (parts.Length > 1 && int.TryParse(parts[1], out int count))
            {
                sb.Append($" {count}");
            }

            // Collect for descriptions section
            if (!string.IsNullOrEmpty(kwInfo.Description))
                keywords.Add(kwInfo);

            return sb.ToString();
        }

        /// <summary>
        /// Process a card reference tag like "cardName".
        /// Returns the card's localized title.
        /// </summary>
        private static string ProcessCardTag(string cardName)
        {
            try
            {
                var cardData = AddressableLoader.Get<CardData>("CardData", cardName);
                if (cardData != null)
                    return cardData.title;
            }
            catch { }
            return cardName;
        }

        /// <summary>
        /// Get keyword title and description, with caching.
        /// </summary>
        private static KeywordInfo GetKeywordInfo(string keywordName)
        {
            if (_keywordCache.TryGetValue(keywordName, out var cached))
                return cached;

            var info = new KeywordInfo { Title = keywordName };

            try
            {
                var kwData = AddressableLoader.Get<KeywordData>("KeywordData", keywordName);
                if (kwData != null)
                {
                    info.Title = kwData.title ?? keywordName;
                    info.Description = kwData.body;
                    info.Note = kwData.note;
                }
            }
            catch
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TextProcessor",
                    $"Failed to load keyword: {keywordName}");
            }

            _keywordCache[keywordName] = info;
            return info;
        }

        /// <summary>
        /// Strip all rich text / TMP tags from a string.
        /// Removes color, size, bold, italic, strikethrough, sprite, etc.
        /// </summary>
        public static string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove all <...> tags
            text = Regex.Replace(text, @"<[^>]+>", "");

            // Clean up resulting whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }
    }
}
