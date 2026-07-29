using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The game's inline tag language: walking text tag by tag and turning
    /// keyword, card, sprite and formatting tags into readable words.
    /// </summary>
    public static partial class TextProcessor
    {
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

    }
}
