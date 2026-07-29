using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Walking the map: which navigation items are nodes, the order they are
    /// visited in, and the description read out when one takes focus.
    /// </summary>
    public partial class MapHandler
    {
        /// <summary>
        /// Left/Right walk the map nodes in path order; Up/Down cycle the HUD items.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            var all = NavigationHelper.GetNavigableItems();
            var nodeItems = new List<UINavigationItem>();
            var otherItems = new List<UINavigationItem>();

            foreach (var item in all)
            {
                if (GetMapNode(item) != null)
                    nodeItems.Add(item);
                else
                    otherItems.Add(item);
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            UINavigationItem current = navSystem?.currentNavigationItem;

            UINavigationItem next = null;
            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                if (nodeItems.Count == 0)
                {
                    ScreenReader.Say(Loc.Get("map_not_ready"), interrupt: true);
                    return;
                }

                // Order nodes along the journey: by tier (depth), then branch position
                nodeItems.Sort(CompareNodeItems);
                next = NavigationHelper.NavigateLinear(nodeItems, current, dir, vertical: false);

                // Only one location revealed: silence would read as "arrows are broken"
                if (next == current)
                {
                    ScreenReader.Say(Loc.Get("map_only_location"), interrupt: true);
                    return;
                }
            }
            else
            {
                // The battle draw/discard pockets render on the map HUD but are
                // always empty here — announcing "Draw pile, 0 cards" only confuses
                otherItems.RemoveAll(IsEmptyPocket);

                if (otherItems.Count == 0)
                {
                    ScreenReader.Say(Loc.Get("map_no_controls"), interrupt: true);
                    return;
                }

                otherItems.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
                // Treat up/down as prev/next within the HUD list
                var mapped = dir == NavDirection.Up ? NavDirection.Left : NavDirection.Right;
                next = NavigationHelper.NavigateLinear(otherItems, current, mapped, vertical: false);
            }

            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        private static bool IsEmptyPocket(UINavigationItem item)
        {
            var pocket = item.GetComponentInParent<CardPocket>();
            if (pocket == null && item.clickHandler != null)
                pocket = item.clickHandler.GetComponentInParent<CardPocket>();
            return pocket != null && pocket.Count == 0;
        }

        private static int CompareNodeItems(UINavigationItem a, UINavigationItem b)
        {
            var nodeA = GetMapNode(a)?.campaignNode;
            var nodeB = GetMapNode(b)?.campaignNode;
            if (nodeA == null || nodeB == null) return 0;

            int byTier = nodeA.tier.CompareTo(nodeB.tier);
            if (byTier != 0) return byTier;
            return nodeA.positionIndex.CompareTo(nodeB.positionIndex);
        }

        private static MapNode GetMapNode(UINavigationItem item)
        {
            var node = item.GetComponentInParent<MapNode>();
            if (node == null && item.clickHandler != null)
                node = item.clickHandler.GetComponentInParent<MapNode>();
            return node;
        }

        protected override string GetItemDescription(UINavigationItem item)
        {
            var mapNode = GetMapNode(item);
            if (mapNode != null)
                return DescribeNode(mapNode, includeHints: true);

            return base.GetItemDescription(item);
        }

        /// <summary>Describe a map node: name, type, state, and a short battle preview.</summary>
        private string DescribeNode(MapNode mapNode, bool includeHints)
        {
            var node = mapNode.campaignNode;
            if (node == null)
                return ItemDescriber.GetMapNodeName(mapNode);

            string name = ItemDescriber.GetMapNodeName(mapNode);
            var parts = new List<string> { name };

            // Node category (battle, boss, shop...) when the label doesn't already say it
            string category = GetNodeCategory(node);
            if (!string.IsNullOrEmpty(category)
                && name.IndexOf(category, System.StringComparison.OrdinalIgnoreCase) < 0)
                parts.Add(category);

            // State relative to the player
            CampaignNode current = GetPlayerNode();
            if (node == current)
            {
                parts.Add(Loc.Get("map_node_here"));
                // Standing on an uncleared node (run start): Enter is how you begin it
                if (!node.cleared && includeHints)
                    parts.Add(Loc.Get("map_node_enter"));
            }
            else if (node.cleared)
                parts.Add(Loc.Get("map_node_cleared"));
            else if (IsDirectDestination(current, node))
            {
                parts.Add(includeHints ? Loc.Get("map_node_available") : Loc.Get("map_node_available_short"));

                // At a fork, what this branch costs — the map gives no second chances
                string consequence = DescribeForkConsequence(current, node);
                if (consequence != null)
                    parts.Add(consequence);
            }
            else if (mapNode.reachable)
                parts.Add(Loc.Get("map_node_ahead"));
            else
                parts.Add(Loc.Get("map_node_not_reachable"));

            // Short battle preview: number of waves
            if (node.type != null && node.type.isBattle)
            {
                try
                {
                    var waves = node.data?.GetSaveCollection<BattleWaveManager.WaveData>("waves");
                    if (waves != null && waves.Length > 0)
                        parts.Add(Loc.Get("map_battle_waves", waves.Length));
                }
                catch { /* wave data not present for this node */ }
            }

            return string.Join(", ", parts);
        }

        /// <summary>Category word derived from the CampaignNodeType subclass name.</summary>
        private static string GetNodeCategory(CampaignNode node)
        {
            if (node.type == null) return null;

            if (node.type.isBoss)
                return Loc.Get("node_type_boss");

            string typeName = node.type.GetType().Name.Replace("CampaignNodeType", "");
            if (string.IsNullOrEmpty(typeName)) return null;

            if (Loc.TryGet("node_type_" + typeName.ToLowerInvariant(), out string localized))
                return localized;

            return ScreenHandler.CleanName(typeName);
        }

    }
}
