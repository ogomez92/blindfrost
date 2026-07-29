using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The effect lines a card's text mentions: pulling them out line by line,
    /// skipping bare stat glyphs, and summarizing cards the text names.
    /// </summary>
    public static partial class TextProcessor
    {
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

    }
}
