using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Walking the map: which navigation items are nodes, and the order the
    /// arrow keys visit them in — the journey path left and right, the HUD
    /// up and down.
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

    }
}
