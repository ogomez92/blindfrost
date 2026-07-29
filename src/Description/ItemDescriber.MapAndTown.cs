using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The world outside a battle: campaign map nodes and whether they can be
    /// visited, town buildings with their state and in-game help text, and the
    /// unlock challenge banners that hang on them.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>
        /// Describe a campaign map node: what it is and whether it can be visited.
        /// </summary>
        public static string DescribeMapNode(MapNode node)
        {
            var parts = new List<string> { GetMapNodeName(node) };

            var campaignNode = node.campaignNode;
            if (campaignNode != null && campaignNode.cleared)
                parts.Add(Loc.Get("map_node_cleared"));
            else if (!node.interactable)
                parts.Add(Loc.Get("map_node_not_reachable"));

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Get the display name of a map node from its localized label,
        /// falling back to the campaign node type name.
        /// </summary>
        public static string GetMapNodeName(MapNode node)
        {
            // The game shows banner labels on hover — battle nodes have two
            // ("Battle" + the squad name), so collect every distinct text.
            var labels = new List<string>();
            foreach (var tmp in node.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                string text = tmp != null ? tmp.text?.Trim() : null;
                if (string.IsNullOrEmpty(text)) continue;

                text = TextProcessor.StripRichText(text);
                // Skip bare numbers (gold amounts and similar overlays)
                if (string.IsNullOrEmpty(text) || int.TryParse(text, out _)) continue;

                if (!labels.Contains(text))
                    labels.Add(text);
                if (labels.Count >= 2) break;
            }
            if (labels.Count > 0)
                return string.Join(", ", labels);

            var type = node.campaignNode?.type;
            if (type != null)
            {
                string typeName = ScreenHandler.CleanName(type.name);
                if (!string.IsNullOrEmpty(typeName))
                    return typeName;
            }

            return ScreenHandler.CleanName(node.name);
        }

        /// <summary>
        /// Describe a Town building: localized name, challenge progress, and state.
        /// </summary>
        public static string DescribeBuilding(Building building)
        {
            var parts = new List<string>();

            // Building name from localized title
            if (building.type != null)
            {
                try
                {
                    string title = building.type.titleKey.GetLocalizedString();
                    if (!string.IsNullOrEmpty(title))
                        parts.Add(title);
                }
                catch
                {
                    // Localization may not be ready
                }
            }

            // Fallback to cleaned GameObject name if no localized title
            if (parts.Count == 0)
                parts.Add(ScreenHandler.CleanName(building.gameObject.name));

            // Check for challenge progress display (ChallengeShrine and similar)
            var challengeDisplay = building.GetComponentInChildren<ChallengeProgressDisplay>();
            if (challengeDisplay != null)
            {
                if (challengeDisplay.text != null)
                {
                    string challengeText = challengeDisplay.text.text?.Trim();
                    if (!string.IsNullOrEmpty(challengeText))
                        parts.Add(TextProcessor.StripRichText(challengeText));
                }
                if (challengeDisplay.progressText != null)
                {
                    string progress = challengeDisplay.progressText.text?.Trim();
                    if (!string.IsNullOrEmpty(progress))
                        parts.Add(TextProcessor.StripRichText(progress));
                }
            }

            // Building state
            if (!building.built && building.buildStarted)
                parts.Add(Loc.Get("building_under_construction"));
            else if (building.HasUncheckedUnlocks)
                parts.Add(Loc.Get("building_new_unlock"));

            return string.Join(", ", parts);
        }

        /// <summary>
        /// True when DescribeBuilding would yield only the building's name — no
        /// challenge line, no construction or new-unlock state. Such a name
        /// ("Balloon") tells the player nothing on its own, so the focus read
        /// folds in the help text rather than leaving it to the I key.
        /// </summary>
        public static bool BuildingFocusIsBareName(Building building)
        {
            if (building == null)
                return false;
            if (building.GetComponentInChildren<ChallengeProgressDisplay>() != null)
                return false;
            if (!building.built && building.buildStarted)
                return false;
            if (building.HasUncheckedUnlocks)
                return false;
            return true;
        }

        /// <summary>
        /// A building's in-game help text (BuildingType.helpKey, packed as
        /// "title|body|note"), split into one part per segment. This is what the
        /// I key reads; the Details review buffer steps through the same parts.
        /// Empty list when the building has no help.
        /// </summary>
        public static List<string> GetBuildingHelpParts(Building building)
        {
            var parts = new List<string>();
            if (building?.type == null)
                return parts;

            try
            {
                if (building.type.helpKey.IsEmpty)
                    return parts;

                string packed = building.type.helpKey.GetLocalizedString();
                foreach (string segment in packed.Split('|'))
                {
                    string clean = TextProcessor.ProcessRawText(segment)?.Trim();
                    if (!string.IsNullOrEmpty(clean))
                        parts.Add(clean);
                }
            }
            catch
            {
                // Localization may not be ready — the summary read is enough
            }
            return parts;
        }

        /// <summary>
        /// Describe an unlock challenge banner: the condition text and the
        /// current progress ("Kill 100 enemies, 6 out of 100").
        /// </summary>
        public static string DescribeChallengeProgress(ChallengeProgressDisplay display)
        {
            var parts = new List<string>();

            string text = display.text != null ? display.text.text?.Trim() : null;
            if (!string.IsNullOrEmpty(text))
                parts.Add(TextProcessor.StripRichText(text));

            string progress = display.progressText != null
                ? display.progressText.text?.Trim() : null;
            if (!string.IsNullOrEmpty(progress))
                parts.Add(progress);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }
}
