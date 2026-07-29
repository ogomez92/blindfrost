using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Scroll-aware navigation over the town's buildings: the ring of every
    /// building seen, scrolling one into view, and landing focus on it.
    /// </summary>
    public partial class TownHandler
    {
        // ---- Scroll-aware building navigation ----

        /// <summary>
        /// Arrow keys walk a ring of every building seen since entering the town,
        /// scrolling the town to bring each target into view before focusing it.
        /// The ring keeps two kinds of building reachable that the plain
        /// navigation strands: the Gate (start/continue run), which the mod's
        /// help-panel filter drops because the tutorial town wraps it in a
        /// HelpPanelShower, and any building that has scrolled off-screen (the
        /// game's CheckLayer then treats it as non-navigable). Falls back to base
        /// navigation when the town has no scroller (nothing to strand).
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            var scroller = GetTownScroller();
            if (scroller == null || !scroller.ContentLargerThanBounds())
            {
                // Nothing scrolls off-screen — every building stays in view and
                // reachable, so the plain spatial navigation is fine.
                base.Navigate(dir);
                return;
            }

            var ring = TownRing(scroller);
            if (ring.Count == 0)
            {
                base.Navigate(dir);
                return;
            }

            Building current = CurrentBuilding() ?? _ringCurrent;
            int index = current != null ? ring.IndexOf(current) : -1;

            bool forward = dir == NavDirection.Down || dir == NavDirection.Right;
            int next;
            if (index < 0)
                next = forward ? 0 : ring.Count - 1;
            else
            {
                next = forward ? index + 1 : index - 1;
                if (next >= ring.Count) next = 0;
                if (next < 0) next = ring.Count - 1;
            }

            GoToBuilding(scroller, ring[next]);
        }

        /// <summary>The Scroller whose subtree holds the town buildings, or null.</summary>
        private Scroller GetTownScroller()
        {
            if (_scroller == null && Time.unscaledTime >= _nextScrollerSearch)
            {
                _nextScrollerSearch = Time.unscaledTime + 1f;
                foreach (var s in Object.FindObjectsOfType<Scroller>())
                {
                    if (s != null && s.GetComponentInChildren<Building>(includeInactive: true) != null)
                    {
                        _scroller = s;
                        break;
                    }
                }
            }
            return _scroller;
        }

        /// <summary>
        /// Every building known this town session, in reading order (top rows
        /// first, then left to right). Grows as buildings come into view; a
        /// building that later scrolls off-screen stays in the ring so we can
        /// scroll back to it. Sorted by world position — a uniform scroll shifts
        /// them all together, so their relative order is stable.
        /// </summary>
        private List<Building> TownRing(Scroller scroller)
        {
            // Seed from the raw registered navigation items, NOT GetNavigableItems:
            // its help-panel filter drops the Gate, which the tutorial town wraps
            // in a HelpPanelShower (Town.tutorialPrompt). That filter is exactly
            // why the Gate never appeared in the focus list. The registered set
            // still excludes locked buildings (they are unregistered) and the
            // Back Button (not a Building).
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem != null)
            {
                foreach (var item in navSystem.AvailableNavigationItems)
                {
                    if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                        continue;
                    var b = BuildingOf(item);
                    if (b == null || !b.transform.IsChildOf(scroller.transform))
                        continue;
                    if (!_knownBuildings.Contains(b))
                        _knownBuildings.Add(b);
                }
            }
            _knownBuildings.RemoveAll(b => b == null);

            var ring = new List<Building>(_knownBuildings);
            ring.Sort((a, b) =>
            {
                Vector3 pa = a.transform.position, pb = b.transform.position;
                return Mathf.Abs(pa.y - pb.y) > 0.05f
                    ? pb.y.CompareTo(pa.y)
                    : pa.x.CompareTo(pb.x);
            });
            return ring;
        }

        /// <summary>The building the game currently has focused, or null.</summary>
        private Building CurrentBuilding()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem != null ? navSystem.currentNavigationItem : null;
            return item != null ? BuildingOf(item) : null;
        }

        /// <summary>The Building a navigation item belongs to (item or its click handler).</summary>
        private static Building BuildingOf(UINavigationItem item)
        {
            if (item == null)
                return null;
            var building = item.GetComponentInParent<Building>();
            if (building == null && item.clickHandler != null)
                building = item.clickHandler.GetComponentInParent<Building>();
            return building;
        }

        /// <summary>
        /// Scroll the town so <paramref name="target"/> is in view, then queue it
        /// for focus. The item may still be re-registering after the scroll, so
        /// TryFocusPending retries over a short window.
        /// </summary>
        private void GoToBuilding(Scroller scroller, Building target)
        {
            ScrollScrollerTo(scroller, target.transform);
            _pendingBuilding = target;
            _pendingDeadline = Time.unscaledTime + 1.5f;
            TryFocusPending(); // usually ready at once (item already in view)
        }

        /// <summary>
        /// Mirror ScrollToNavigation: move the scroller content so the target is
        /// centred, then snap to it immediately. Snapping matters — an off-screen
        /// item fails the game's CheckLayer, and UINavigationSystem.Update would
        /// null our focus before a smooth scroll finished bringing it on-screen.
        /// </summary>
        private static void ScrollScrollerTo(Scroller scroller, Transform target)
        {
            if (scroller.horizontal)
            {
                float value = scroller.transform.position.x - target.position.x;
                scroller.ScrollTo(new Vector2(value, scroller.targetPos.y));
            }
            else
            {
                float value = scroller.transform.position.y - target.position.y;
                scroller.ScrollTo(new Vector2(scroller.targetPos.x, value));
            }
            scroller.rectTransform.anchoredPosition = scroller.targetPos;
        }

        /// <summary>
        /// Focus the pending building once its navigation item is active and
        /// registered again (a building that scrolled off-screen re-registers a
        /// frame or two after it re-enters view). Gives up after the deadline.
        /// </summary>
        private void TryFocusPending()
        {
            if (_pendingBuilding == null)
                return;
            if (Time.unscaledTime > _pendingDeadline)
            {
                _pendingBuilding = null;
                return;
            }

            var navItem = _pendingBuilding.GetComponentInChildren<UINavigationItem>(includeInactive: false);
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navItem == null || navSystem == null)
                return;
            if (!navItem.isSelectable || !navItem.gameObject.activeInHierarchy
                || !navSystem.AvailableNavigationItems.Contains(navItem))
                return; // still (re)activating after the scroll — try again next frame

            NavigationHelper.FocusItem(navItem);
            _ringCurrent = _pendingBuilding;
            _pendingBuilding = null;
        }

    }
}
