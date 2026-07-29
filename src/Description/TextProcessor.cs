using System;
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

            /// <summary>
            /// The game really has a keyword by this id. False means the lookup
            /// missed and Title is only the id echoed back — good enough to read
            /// where the game shows the tag text, but not proof of a keyword.
            /// </summary>
            public bool Found;
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
            var explanations = new List<string>();
            string plainText = ProcessDescriptionParts(rawText, extraKeywords, explanations);

            var sb = new StringBuilder(plainText);
            foreach (string explanation in explanations)
            {
                if (sb.Length > 0) sb.Append(". ");
                sb.Append(explanation);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Same processing, but the keyword explanations come back as separate
        /// strings ("Title: body. Note: note") instead of being appended, so
        /// the Details review buffer can offer them one item at a time.
        /// Card mention summaries stay inline in the returned text.
        /// </summary>
        public static string ProcessDescriptionParts(
            string rawText, IEnumerable<string> extraKeywords, List<string> explanations)
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

            // Collect keyword descriptions
            var seen = new HashSet<string>();
            foreach (var kw in mentionedKeywords)
            {
                if (seen.Contains(kw.Title)) continue;
                seen.Add(kw.Title);

                if (string.IsNullOrEmpty(kw.Description)) continue;
                string explanation = $"{kw.Title}: {ProcessRawText(kw.Description)}";
                if (!string.IsNullOrEmpty(kw.Note))
                    explanation += $". Note: {ProcessRawText(kw.Note)}";
                explanations.Add(explanation);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Keyword ids that render as inline stat glyphs (the heart, sword, and
        /// hourglass icons a card's text draws mid-sentence). A bare glyph with no
        /// amount only repeats a stat the short focus read has already given, so it
        /// is noise — but the same glyph carrying an amount is an effect in its own
        /// right ("Health 3" heals, "Counter 2" counts an enemy down), and a card
        /// that applies it may well have no such stat itself.
        /// </summary>
        private static readonly HashSet<string> StatIconKeywords =
            new HashSet<string> { "health", "attack", "counter" };

        /// <summary>
        /// The effect lines of a card's text, one mention per line, in the
        /// game's own words ("Apply 2 Snow", "Deal 8 additional damage to
        /// Snow'd targets", "When hit, gain +1 Attack") — no explanations.
        /// Used by the short focus read, where the meaning waits in the
        /// Details review buffer.
        ///
        /// Earlier versions compressed each line down to "keyword amount"
        /// ("Snow 2"), but that made an enemy that APPLIES 2 Snow on hit
        /// read exactly like a unit that IS snowed for 2 turns, and turned
        /// "Deal 8 additional damage to Snow'd targets" into a baffling
        /// "Snow 8" — so the line's full wording is kept instead.
        /// </summary>
        public static List<string> ExtractKeywordMentions(string rawText)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(rawText))
                return result;

            // Card.GetDescription puts each effect on its own line
            foreach (string line in rawText.Split('\n'))
            {
                if (IsBareStatGlyphLine(line))
                    continue;
                AddProseMention(line, result);
            }
            return result;
        }

        /// <summary>
        /// A line that is nothing but stat icons without amounts (a bare heart
        /// or sword the card draws for layout). Reading it back would only
        /// repeat a stat the focus read already gave. The same glyph carrying
        /// an amount, or embedded in a sentence, is a real effect and is kept.
        /// </summary>
        private static bool IsBareStatGlyphLine(string line)
        {
            bool sawStatGlyph = false;
            foreach (Match match in Regex.Matches(line, @"<([^>]*)>"))
            {
                string tag = match.Groups[1].Value.Trim();
                int eqIdx = tag.IndexOf('=');
                if (eqIdx <= 0)
                    continue;

                string key = tag.Substring(0, eqIdx).Trim();
                string value = tag.Substring(eqIdx + 1).Trim();

                if (key == "keyword")
                {
                    string[] parts = value.Split(' ');
                    if (parts.Length > 1 || !StatIconKeywords.Contains(parts[0].ToLowerInvariant()))
                        return false;
                    sawStatGlyph = true;
                }
                else if (IsSpriteKey(key))
                {
                    // The same three stats drawn as icons rather than keywords
                    if (!StatIconKeywords.Contains(SpriteName(value).ToLowerInvariant()))
                        return false;
                    sawStatGlyph = true;
                }
            }
            if (!sawStatGlyph)
                return false;

            // Only a glyph line with no other words qualifies
            string withoutTags = Regex.Replace(line, @"<[^>]*>", "").Trim();
            return withoutTags.Length == 0;
        }

        /// <summary>The line's own words, for an effect the game words as prose.</summary>
        private static void AddProseMention(string line, List<string> result)
        {
            string prose;
            try { prose = StripRichText(ExpandTags(line, new List<KeywordInfo>(), null)); }
            catch { return; }

            if (!string.IsNullOrEmpty(prose) && !result.Contains(prose))
                result.Add(prose);
        }

        /// <summary>
        /// A mention's effect name without its amount ("Shroom 3" -> "Shroom"), for
        /// matching against text that names the same effect at a different amount.
        /// </summary>
        public static string MentionName(string mention)
        {
            return string.IsNullOrEmpty(mention)
                ? mention
                : Regex.Replace(mention, @"\s[+\-x]?\d+$", "");
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
                    parts.Add(Loc.Get("battle_acts_in", data.counter));

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
            string key = CacheKey(kwData.name);
            if (_keywordCache.ContainsKey(key)) return;

            // title/body/note are localized properties and can throw while
            // localization loads — don't cache on failure so a later call retries
            try
            {
                _keywordCache[key] = new KeywordInfo
                {
                    Title = string.IsNullOrEmpty(kwData.title) ? kwData.name : kwData.title,
                    Description = kwData.body,
                    Note = kwData.note,
                    Found = true,
                };
            }
            catch { }
        }

        /// <summary>
        /// AddressableLoader files keywords under their lower-cased name, so a
        /// lookup only lands when the id is lower-cased too. The cache follows the
        /// same rule, or an id cached from a KeywordData ("Snow") would never be
        /// found again by the tag that names it ("snow").
        /// </summary>
        private static string CacheKey(string keywordName)
        {
            return keywordName.ToLowerInvariant();
        }

        /// <summary>Safe display title for a keyword, falling back to its id.</summary>
        public static string GetKeywordTitle(KeywordData kwData)
        {
            if (kwData == null) return null;
            CacheKeyword(kwData);
            return _keywordCache.TryGetValue(CacheKey(kwData.name), out var info)
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

            if (!_keywordCache.TryGetValue(CacheKey(kwData.name), out var info)
                || string.IsNullOrEmpty(info.Description))
                return null;

            string text = $"{info.Title}: {ProcessRawText(info.Description)}";
            if (!string.IsNullOrEmpty(info.Note))
                text += $". Note: {ProcessRawText(info.Note)}";
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
                        return ProcessSpriteTag(value, keywords);
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

            // TMP shorthand colour. <#fff> on its own is pure formatting, but the
            // game also writes the colour and the words it paints in one tag —
            // "You can feed <#7569CF Monchi> any items you don't need!". Dropping
            // the whole tag there swallows the sentence's subject.
            if (tag[0] == '#')
            {
                int spaceIdx = tag.IndexOf(' ');
                return spaceIdx > 0 ? tag.Substring(spaceIdx + 1).Trim() : null;
            }

            // Formatting keywords — skip
            if (_tmpFormattingTags.Contains(tag))
                return null;

            // Any other word tag (<Not Charged>, <Redraw Bell>, <Charms>) is the
            // game's inline highlight: Text.ProcessTag renders unmatched tags as
            // their own coloured text, so they are real words — read them
            return tag;
        }

        private static readonly HashSet<string> _tmpFormattingTags = new HashSet<string>
        {
            "sup", "sub", "nobr", "noparse", "lowercase", "uppercase",
            "allcaps", "smallcaps", "mark", "page"
        };

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

        /// <summary>True for the three spellings the game uses for a sprite tag.</summary>
        private static bool IsSpriteKey(string key)
        {
            return key == "sprite" || key == "sprite name" || key == "spr";
        }

        /// <summary>
        /// The icon name out of a sprite tag's value. Raw card text writes it bare
        /// ("snow"); text the game has already rendered quotes it and appends
        /// attributes ("\"snow\" color=#4B6A9CFF").
        /// </summary>
        private static string SpriteName(string value)
        {
            string name = value.Trim();
            int spaceIdx = name.IndexOf(' ');
            if (spaceIdx > 0)
                name = name.Substring(0, spaceIdx);
            return name.Trim('"');
        }

        /// <summary>
        /// A sprite tag is usually the game drawing a keyword as its icon in the
        /// middle of a sentence — "Gain a &lt;sprite name=crown&gt;", "Cannot trigger
        /// until &lt;sprite name=ink&gt; is cleared", "add Frenzy to
        /// &lt;sprite name=crown&gt;'d allies". A sighted player reads the icon as the
        /// word, so it has to become the word here; dropping it leaves sentences
        /// with no subject ("Gain a", "Cannot trigger until is cleared").
        ///
        /// Names that are not keywords are button glyphs and layout art
        /// (&lt;sprite=26&gt;, the key icons ControllerButtonSystem splices in) — those
        /// stay silent, since their names are not words anyone wants read aloud.
        /// </summary>
        private static string ProcessSpriteTag(string value, List<KeywordInfo> keywords)
        {
            // A coloured sprite tag is text the game has already rendered, and there
            // it only draws the icon when it writes the keyword's name right after
            // it (Text.ProcessTag). Reading the icon too would say "Snow Snow 2".
            if (value.IndexOf("color=", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;

            string name = SpriteName(value);
            if (name.Length == 0 || char.IsDigit(name[0]))
                return null;

            var kwInfo = GetKeywordInfo(name);
            if (!kwInfo.Found)
                return null;

            // Stat icons name a stat the readout already gives; the rest carry
            // meaning worth explaining, exactly as a <keyword=...> tag would
            if (!string.IsNullOrEmpty(kwInfo.Description)
                && !StatIconKeywords.Contains(name.ToLowerInvariant()))
                keywords.Add(kwInfo);

            return kwInfo.Title;
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
            string key = CacheKey(keywordName);
            if (_keywordCache.TryGetValue(key, out var cached))
                return cached;

            var info = new KeywordInfo { Title = keywordName };

            try
            {
                var kwData = AddressableLoader.Get<KeywordData>("KeywordData", key);
                if (kwData != null)
                {
                    info.Title = kwData.title ?? keywordName;
                    info.Description = kwData.body;
                    info.Note = kwData.note;
                    info.Found = true;
                }
            }
            catch
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TextProcessor",
                    $"Failed to load keyword: {keywordName}");
            }

            // Only a hit is worth caching: a miss here can just mean addressables
            // were not ready yet, and a cached miss would silence that keyword's
            // icon for the rest of the session
            if (info.Found)
                _keywordCache[key] = info;
            return info;
        }

        /// <summary>
        /// Plain text for a string the game has NOT rendered yet — a keyword body,
        /// a speech bubble, anything straight off a LocalizedString. Those are
        /// written in the same tag language as card text ("Add &lt;card=Junk&gt; to your
        /// hand", "&lt;Combos&gt; give double &lt;keyword=blings&gt;"), and the tags hold the
        /// nouns. StripRichText deletes them, which is right for text TMP already
        /// rendered and disastrous here — it leaves "Add to your hand".
        ///
        /// Expanded one level: keywords the text names are read but not explained,
        /// and cards it names are not summarized, so an explanation stays an
        /// explanation instead of unfolding into every keyword it touches.
        /// </summary>
        public static string ProcessRawText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            try { return StripRichText(ExpandTags(text, new List<KeywordInfo>(), null)); }
            catch { return StripRichText(text); }
        }

        /// <summary>
        /// Strip all rich text / TMP tags from a string. For text the game has
        /// already rendered, where the tags left are pure formatting — use
        /// ProcessRawText for anything still carrying the game's own tags.
        /// Removes color, size, bold, italic, strikethrough, sprite, etc.
        /// </summary>
        public static string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove all <...> tags
            text = Regex.Replace(text, @"<[^>]+>", "");

            // Newlines are the game's paragraph breaks ("...next wave\n\nCan't be
            // called early..."); read them as sentence pauses, not run-ons
            text = Regex.Replace(text, @"([.!?:,;])?[ \t]*\n\s*",
                m => m.Groups[1].Success ? m.Groups[1].Value + " " : ". ");

            // Clean up resulting whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }
    }
}
