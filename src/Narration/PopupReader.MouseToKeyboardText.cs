using System.Text.RegularExpressions;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Rewrites the game's mouse wording into the mod's keyboard model, so a
    /// tutorial or help panel teaches the controls a blind player actually has:
    /// right click becomes the I key, left click and bare clicks become Enter,
    /// and "drag X on to Y" becomes "select X and place it on Y" followed by
    /// the how-to hint. Pure text in, text out — it knows nothing about popups.
    /// </summary>
    public static partial class PopupReader
    {
        /// <summary>
        /// Replace mouse instructions (drag, click) with keyboard commands.
        /// Blind players pick up and place with Enter instead of dragging,
        /// and inspect with the I key instead of Right Click.
        /// </summary>
        private static string MakeDragAccessible(string text)
        {
            // "Right Click" inspects a card with a mouse; the mod binds I for
            // keyboard users. Collapse a leading "press" first so the rule
            // below can't produce "press press the I key".
            text = Regex.Replace(text,
                @"\bpress\s+(right[\s-]?click)\b",
                "$1",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text,
                @"\bright[\s-]?clicking\b",
                "pressing the I key",
                RegexOptions.IgnoreCase);
            // "Right Click [on] any card" / "You can Right Click the X"
            // → "press the I key on any card" / "You can press the I key on the X"
            text = Regex.Replace(text,
                @"\bright[\s-]?click\b\s*(on\s+)?(?=(a|an|any|the|your|it)\b)",
                "press the I key on ",
                RegexOptions.IgnoreCase);
            // Any other mention just names the key
            text = Regex.Replace(text,
                @"\bright[\s-]?click\b",
                "the I key",
                RegexOptions.IgnoreCase);

            // "Left Click" is the game's display name for the confirm action
            // ("Use Left Click to Inspect Charms") — the mod's confirm is Enter
            text = Regex.Replace(text,
                @"\bleft[\s-]?clicking\b",
                "pressing Enter",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text,
                @"\bleft[\s-]?click\b",
                "Enter",
                RegexOptions.IgnoreCase);

            // Remaining generic clicks activate the focused item
            text = Regex.Replace(text,
                @"\bclicking\b(\s+on\b)?",
                "pressing Enter on",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text,
                @"\bclick\b(\s+on\b)?",
                "press Enter on",
                RegexOptions.IgnoreCase);

            // Drag wording becomes the mod's select-and-place model. Structural
            // rules keep the sentence's destination phrase intact; sentence
            // punctuation bounds the match so a "to" in a later sentence can't
            // swallow text. RewriteDrag keeps gerunds ("dragging" → "selecting").
            string beforeDrag = text;

            // "drag X from Y on to Z" → "select X from Y and place it on Z"
            text = RewriteDrag(text, @"(?<x>[^.!?]+?\bfrom\b[^.!?]+?)\bon\s?to\b", "it on");

            // "drag X in front of Y to …": the destination phrase has no "to",
            // and the generic to-rule below would cut the sentence at the
            // unrelated "to protect them" instead
            text = RewriteDrag(text, @"(?<x>[^.!?]*?\S[^.!?]*?)\bin\s+front\s+of\b", "it in front of");

            // "drag X onto Y" / "drag X on to Y" → "select X and place it on Y"
            text = RewriteDrag(text, @"(?<x>[^.!?]*?\S[^.!?]*?)\bon\s?to\b", "it on");

            // "drag X to Y" → "select X and place it on Y". The subject must
            // have content: matching is case-insensitive, so a bare label like
            // "Drag To Feed" must not treat its "To" as the destination marker
            text = RewriteDrag(text, @"(?<x>[^.!?]*?\S[^.!?]*?)\bto\b", "it on");

            // Bare mentions with no destination in the sentence
            text = Regex.Replace(text, @"\bdragging\b", "selecting and placing",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\bdrag\b", "select and place",
                RegexOptions.IgnoreCase);

            // Any rewritten drag instruction gets the keyboard how-to appended.
            // "In front of a unit" placements are chosen by selecting that unit
            // (your card takes its slot and pushes it back), which the generic
            // "choose the destination" wording doesn't make obvious.
            if (text != beforeDrag)
            {
                bool inFront = Regex.IsMatch(text, @"\bin front of\b", RegexOptions.IgnoreCase);
                text += " " + Loc.Get(inFront ? "tutorial_drag_hint_infront" : "tutorial_drag_hint");
            }

            return text;
        }

        /// <summary>
        /// Rewrite one "drag …" sentence shape, keeping the verb form:
        /// "Drag X on to Y" → "select X and place it on Y",
        /// "by dragging X on to Y" → "by selecting X and placing it on Y".
        /// The pattern must capture the dragged thing as group "x".
        /// </summary>
        private static string RewriteDrag(string text, string tailPattern, string destination)
        {
            return Regex.Replace(text,
                @"\bdrag(?<ing>ging)?\b" + tailPattern,
                m => m.Groups["ing"].Success
                    ? "selecting" + m.Groups["x"].Value + "and placing " + destination
                    : "select" + m.Groups["x"].Value + "and place " + destination,
                RegexOptions.IgnoreCase);
        }
    }
}
