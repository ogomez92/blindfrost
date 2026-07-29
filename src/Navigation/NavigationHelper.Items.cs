using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Enumerating the navigable items on screen: layer filtering, linear
    /// movement between them, and the help-panel exclusion rules.
    /// </summary>
    public static partial class NavigationHelper
    {
        /// <summary>
        /// Get all active UINavigationItems in the current layer, sorted spatially.
        /// Filters to items on the active navigation layer that are selectable.
        /// </summary>
        public static List<UINavigationItem> GetNavigableItems()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return new List<UINavigationItem>();

            var activeLayer = UINavigationSystem.ActiveNavigationLayer;
            var items = new List<UINavigationItem>();

            foreach (var item in navSystem.AvailableNavigationItems)
            {
                if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                    continue;

                // Match the layer check logic from CheckLayer (which is internal):
                // Item is valid if it ignores layers OR is on the active layer
                if (!item.ignoreLayers && item.navigationLayer != activeLayer)
                    continue;

                // Skip HelpPanelShower items — help is accessible via F1,
                // and these often have ignoreLayers=true which pollutes navigation
                if (IsHelpItem(item))
                    continue;

                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Navigate to the next/previous item in a list based on direction.
        /// For vertical menus: Up = previous, Down = next.
        /// For horizontal menus: Left = previous, Right = next.
        /// </summary>
        public static UINavigationItem NavigateLinear(
            List<UINavigationItem> items, UINavigationItem current, NavDirection dir, bool vertical = true)
        {
            if (items == null || items.Count == 0) return null;

            int currentIndex = current != null ? items.IndexOf(current) : -1;

            bool forward = vertical ? (dir == NavDirection.Down) : (dir == NavDirection.Right);
            bool backward = vertical ? (dir == NavDirection.Up) : (dir == NavDirection.Left);

            if (!forward && !backward) return current;

            int newIndex;
            if (currentIndex < 0)
            {
                // Nothing selected, pick first or last
                newIndex = forward ? 0 : items.Count - 1;
            }
            else
            {
                newIndex = forward ? currentIndex + 1 : currentIndex - 1;
                // Wrap around
                if (newIndex >= items.Count) newIndex = 0;
                if (newIndex < 0) newIndex = items.Count - 1;
            }

            return items[newIndex];
        }

        /// <summary>
        /// Check if an item is a help panel trigger (HelpPanelShower).
        /// These are excluded from navigation since help is on F1.
        /// </summary>
        private static bool IsHelpItem(UINavigationItem item)
        {
            GameObject obj = item.clickHandler ?? item.gameObject;
            return IsUnderHelpShower(obj) || IsUnderHelpShower(item.gameObject);
        }

        /// <summary>
        /// True when the nearest HelpPanelShower above this object marks a help
        /// button. Some screens (BattleWin) put a HelpPanelShower on the root
        /// canvas that also hosts the navigation layer — that one describes the
        /// whole screen, and treating it as a help button filtered out every
        /// item on the victory screen, including Continue.
        /// </summary>
        private static bool IsUnderHelpShower(GameObject obj)
        {
            if (obj == null) return false;
            var shower = obj.GetComponentInParent<HelpPanelShower>();
            if (shower == null) return false;
            return shower.GetComponent<UINavigationLayer>() == null
                && shower.GetComponent<Canvas>() == null;
        }
    }
}
