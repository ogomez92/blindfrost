using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Processes game card/effect text into screen-reader-friendly plain text.
    /// This part holds the pipeline the rest of the mod calls — expand the game's
    /// tags, strip rich text, append the keyword explanations — plus the
    /// <see cref="KeywordInfo"/> record and the cache of it that the Keywords,
    /// Tags and Mentions parts all read.
    /// </summary>
    public static partial class TextProcessor
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
