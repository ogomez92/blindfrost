using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Crowns, charms and tokens — on a card or standing alone on a shelf — plus boss
    /// reward options and the shop's crown holder.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>Crown plus charm/token titles, without their effect text.</summary>
        private static void AddUpgradeNames(List<string> parts, CardData data)
        {
            if (data?.upgrades == null || data.upgrades.Count == 0)
                return;

            var names = new List<string>();
            foreach (var upgrade in data.upgrades)
            {
                if (upgrade == null) continue;

                if (upgrade.type == CardUpgradeData.Type.Crown)
                {
                    parts.Add(Loc.Get("card_crowned"));
                    continue;
                }

                string upgradeTitle;
                try { upgradeTitle = upgrade.title; }
                catch { upgradeTitle = null; }
                names.Add(string.IsNullOrEmpty(upgradeTitle)
                    ? ScreenHandler.CleanName(upgrade.name)
                    : upgradeTitle);
            }

            if (names.Count == 1)
                parts.Add(FormatSingleCharmName(names[0]));
            else if (names.Count > 1)
                parts.Add(Loc.Get("card_charms", names.Count, string.Join(", ", names)));
        }

        /// <summary>
        /// The short-read label for a lone charm. Charm titles already end in the
        /// word "Charm" ("Coldheart Charm"), so the usual "Charm: {0}" wrapper
        /// would say charm twice. When the title already carries the localized
        /// charm word, read it on its own; the full effect text still waits in
        /// the Details review buffer.
        /// </summary>
        private static string FormatSingleCharmName(string title)
        {
            string charmWord = Loc.Get("upgrade_charm");
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(charmWord)
                && title.IndexOf(charmWord, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return title.EndsWith(".") ? title : title + ".";

            return Loc.Get("card_charm_one", title);
        }

        /// <summary>Short focus read for a standalone charm or crown: name and kind.</summary>
        public static string DescribeUpgradeDataShort(CardUpgradeData data)
        {
            string title;
            try { title = data.title; }
            catch { title = null; }
            if (string.IsNullOrEmpty(title))
                title = ScreenHandler.CleanName(data.name);

            string kind = Loc.Get(
                data.type == CardUpgradeData.Type.Crown ? "upgrade_crown"
                : data.type == CardUpgradeData.Type.Token ? "upgrade_token"
                : "upgrade_charm");

            return $"{title}, {kind}";
        }

        /// <summary>Detail pieces for a standalone charm/crown.</summary>
        private static List<string> BuildUpgradeDetailParts(CardUpgradeData data)
        {
            var items = new List<string> { DescribeUpgradeDataShort(data) };

            string rawText = null;
            try { rawText = data.text; } catch { }

            var explanations = new List<string>();
            string text = TextProcessor.ProcessDescriptionParts(rawText, null, explanations);
            if (!string.IsNullOrEmpty(text))
                items.Add(text);
            items.AddRange(explanations);

            return items;
        }

        /// <summary>
        /// Describe a card's upgrades: crown (deploys at battle start) and attached charms.
        /// </summary>
        public static string DescribeUpgrades(CardData data)
        {
            if (data?.upgrades == null || data.upgrades.Count == 0)
                return null;

            var parts = new List<string>();
            var charms = new List<string>();
            var tokens = new List<string>();

            foreach (var upgrade in data.upgrades)
            {
                if (upgrade == null) continue;

                if (upgrade.type == CardUpgradeData.Type.Crown)
                {
                    parts.Add(Loc.Get("card_crowned"));
                }
                else if (upgrade.type == CardUpgradeData.Type.Charm
                    || upgrade.type == CardUpgradeData.Type.Token)
                {
                    string title;
                    try { title = upgrade.title; }
                    catch { title = null; }
                    if (string.IsNullOrEmpty(title))
                        title = ScreenHandler.CleanName(upgrade.name);

                    // The upgrade's hover panel shows its effect text — read it too
                    string text = null;
                    try { text = TextProcessor.ProcessForScreenReader(upgrade.text); }
                    catch { }

                    var list = upgrade.type == CardUpgradeData.Type.Charm ? charms : tokens;
                    list.Add(string.IsNullOrEmpty(text) ? title : $"{title}. {text}");
                }
            }

            if (charms.Count == 1)
                parts.Add(Loc.Get("card_charm_one", charms[0]));
            else if (charms.Count > 1)
                parts.Add(Loc.Get("card_charms", charms.Count, string.Join(", ", charms)));

            if (tokens.Count == 1)
                parts.Add(Loc.Get("card_token_one", tokens[0]));
            else if (tokens.Count > 1)
                parts.Add(Loc.Get("card_tokens", tokens.Count, string.Join(", ", tokens)));

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// Describe a standalone charm or crown (shop shelf, journal, charm icon on
        /// a card): name, kind, and the effect text its hover panel shows.
        /// </summary>
        public static string DescribeUpgradeData(CardUpgradeData data)
        {
            string title;
            try { title = data.title; }
            catch { title = null; }
            if (string.IsNullOrEmpty(title))
                title = ScreenHandler.CleanName(data.name);

            string kind = Loc.Get(
                data.type == CardUpgradeData.Type.Crown ? "upgrade_crown"
                : data.type == CardUpgradeData.Type.Token ? "upgrade_token"
                : "upgrade_charm");

            string text = null;
            try { text = TextProcessor.ProcessForScreenReader(data.text); }
            catch { }

            return string.IsNullOrEmpty(text)
                ? $"{title}, {kind}"
                : $"{title}, {kind}. {text}";
        }

        /// <summary>
        /// Describe a boss reward option from the data its hover panel shows:
        /// either a keyword (title + body) or a title/body pair set by the
        /// charm/crown/modifier subclasses. Returns null if none is readable.
        /// </summary>
        public static string DescribeBossReward(BossRewardSelect reward)
        {
            var parts = new List<string>();

            var keyword = ReflectionUtil.GetField<KeywordData>(reward, "popUpKeyword");
            if (keyword != null)
            {
                string explanation = TextProcessor.GetKeywordExplanation(keyword)
                    ?? TextProcessor.GetKeywordTitle(keyword);
                if (!string.IsNullOrEmpty(explanation))
                    parts.Add(explanation);
            }

            string title = ReflectionUtil.GetField<string>(reward, "title");
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            string body = ReflectionUtil.GetField<string>(reward, "body");
            if (!string.IsNullOrEmpty(body))
                parts.Add(TextProcessor.ProcessForScreenReader(body));

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Describe the shop's crown holder: the crown for sale, or empty.</summary>
        public static string DescribeCrownHolder(CrownHolderShop holder)
        {
            if (!holder.hasCrown)
                return Loc.Get("crown_holder_empty");

            var data = holder.GetCrownData();
            return data != null ? DescribeUpgradeData(data) : Loc.Get("upgrade_crown");
        }
    }
}
