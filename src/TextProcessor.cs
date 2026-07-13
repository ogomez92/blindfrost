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
            return ProcessForScreenReader(rawText, null);
        }

        /// <summary>
        /// Same, but also appends explanations for extra keyword ids announced
        /// elsewhere in the readout. Keyword statuses (Frenzy, Snow...) are shown
        /// as icons by the game and never appear in the description text, so their
        /// meaning is lost unless passed in here. Keywords the text already
        /// mentions are not explained twice.
        /// </summary>
        public static string ProcessForScreenReader(string rawText, IEnumerable<string> extraKeywords)
        {
            var mentionedKeywords = new List<KeywordInfo>();
            var mentionedCards = new List<CardData>();

            string plainText = "";
            if (!string.IsNullOrEmpty(rawText))
            {
                // StripRichText also collapses whitespace and trims
                plainText = StripRichText(ExpandTags(rawText, mentionedKeywords, mentionedCards));
            }

            if (extraKeywords != null)
            {
                foreach (string keywordName in extraKeywords)
                {
                    if (string.IsNullOrEmpty(keywordName)) continue;
                    var kwInfo = GetKeywordInfo(keywordName);
                    if (!string.IsNullOrEmpty(kwInfo.Description))
                        mentionedKeywords.Add(kwInfo);
                }
            }

            var sb = new StringBuilder(plainText);

            // Summarize cards the text mentions — the game pops their full tooltip
            // on hover. One level deep only; their keywords join the shared
            // explanation pass below.
            var seenCards = new HashSet<string>();
            foreach (var card in mentionedCards)
            {
                if (card == null || !seenCards.Add(card.name)) continue;
                string summary = SummarizeMentionedCard(card, mentionedKeywords);
                if (string.IsNullOrEmpty(summary)) continue;
                if (sb.Length > 0) sb.Append(". ");
                sb.Append(summary);
            }

            // Append keyword descriptions
            var seen = new HashSet<string>();
            foreach (var kw in mentionedKeywords)
            {
                if (seen.Contains(kw.Title)) continue;
                seen.Add(kw.Title);

                if (!string.IsNullOrEmpty(kw.Description))
                {
                    string desc = StripRichText(kw.Description);
                    if (sb.Length > 0) sb.Append(". ");
                    sb.Append($"{kw.Title}: {desc}");
                }
                if (!string.IsNullOrEmpty(kw.Note))
                {
                    string note = StripRichText(kw.Note);
                    if (sb.Length > 0) sb.Append(". ");
                    sb.Append($"Note: {note}");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Short readout of a card referenced by another card's text, mirroring the
        /// tooltip the game shows: name, stats, passive effect icons, description.
        /// Keywords found along the way are collected for the caller's
        /// explanation pass; nested card mentions are not followed.
        /// </summary>
        private static string SummarizeMentionedCard(CardData data, List<KeywordInfo> keywords)
        {
            var parts = new List<string>();
            try
            {
                parts.Add(Loc.Get("card_mentions", data.title));
                if (data.hasAttack)
                    parts.Add(Loc.Get("stat_attack", data.damage));
                if (data.hasHealth)
                    parts.Add(Loc.Get("stat_health", data.hp));
                if (data.counter > 0)
                    parts.Add(Loc.Get("stat_counter", data.counter));

                if (data.startWithEffects != null)
                {
                    foreach (var stacks in data.startWithEffects)
                    {
                        if (stacks?.data == null || !stacks.data.visible) continue;

                        string name = stacks.data.name;
                        if (!string.IsNullOrEmpty(stacks.data.keyword))
                        {
                            var kwInfo = GetKeywordInfo(stacks.data.keyword);
                            name = kwInfo.Title;
                            if (!string.IsNullOrEmpty(kwInfo.Description))
                                keywords.Add(kwInfo);
                        }
                        parts.Add($"{name} {stacks.count}");
                    }
                }

                string desc = Card.GetDescription(data);
                if (!string.IsNullOrEmpty(desc))
                {
                    string plain = StripRichText(ExpandTags(desc, keywords, null));
                    if (!string.IsNullOrEmpty(plain))
                        parts.Add(plain);
                }
            }
            catch
            {
                // Partial card data still reads fine
            }
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// Seed the keyword cache from a KeywordData we already hold, so callers
        /// can pass its name as an extra keyword without an addressables lookup.
        /// </summary>
        public static void CacheKeyword(KeywordData kwData)
        {
            if (kwData == null || string.IsNullOrEmpty(kwData.name)) return;
            if (_keywordCache.ContainsKey(kwData.name)) return;

            // title/body/note are localized properties and can throw while
            // localization loads — don't cache on failure so a later call retries
            try
            {
                _keywordCache[kwData.name] = new KeywordInfo
                {
                    Title = string.IsNullOrEmpty(kwData.title) ? kwData.name : kwData.title,
                    Description = kwData.body,
                    Note = kwData.note,
                };
            }
            catch { }
        }

        /// <summary>Safe display title for a keyword, falling back to its id.</summary>
        public static string GetKeywordTitle(KeywordData kwData)
        {
            if (kwData == null) return null;
            CacheKeyword(kwData);
            return _keywordCache.TryGetValue(kwData.name, out var info)
                ? info.Title
                : kwData.name;
        }

        /// <summary>
        /// "Title: body. Note: note" for a keyword's hover panel, or null when the
        /// keyword has no body text. Used by describers replicating panel content.
        /// </summary>
        public static string GetKeywordExplanation(KeywordData kwData)
        {
            if (kwData == null) return null;
            CacheKeyword(kwData);

            if (!_keywordCache.TryGetValue(kwData.name, out var info)
                || string.IsNullOrEmpty(info.Description))
                return null;

            string text = $"{info.Title}: {StripRichText(info.Description)}";
            if (!string.IsNullOrEmpty(info.Note))
                text += $". Note: {StripRichText(info.Note)}";
            return text;
        }

        /// <summary>
        /// Process text expanding custom game tags into readable text.
        /// Collects keyword info for appending descriptions, and the cards the
        /// text mentions (pass null to skip mention tracking).
        /// </summary>
        private static string ExpandTags(string text, List<KeywordInfo> keywords, List<CardData> cards)
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

                    string expanded = ProcessTag(tagContent, keywords, cards);
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
        private static string ProcessTag(string tag, List<KeywordInfo> keywords, List<CardData> cards)
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
                        return ProcessCardTag(value, cards);
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
        /// Returns the card's localized title and collects the card so a
        /// summary can be appended (the game pops its tooltip on hover).
        /// </summary>
        private static string ProcessCardTag(string cardName, List<CardData> cards)
        {
            try
            {
                var cardData = AddressableLoader.Get<CardData>("CardData", cardName);
                if (cardData != null)
                {
                    cards?.Add(cardData);
                    return cardData.title;
                }
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
