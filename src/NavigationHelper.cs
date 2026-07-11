using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Shared keyboard navigation utilities.
    /// Provides arrow key input that works with the game's UINavigationSystem.
    /// </summary>
    public static class NavigationHelper
    {
        private static float _lastNavTime;
        private static float _navRepeatDelay = 0.3f;
        private static float _navRepeatRate = 0.1f;
        private static bool _navHeld;

        /// <summary>
        /// Check for arrow key navigation input.
        /// Returns the navigation direction pressed, or None.
        /// Handles initial press and hold-to-repeat.
        /// </summary>
        public static NavDirection GetNavigationInput()
        {
            NavDirection dir = NavDirection.None;

            if (Input.GetKey(KeyCode.UpArrow)) dir = NavDirection.Up;
            else if (Input.GetKey(KeyCode.DownArrow)) dir = NavDirection.Down;
            else if (Input.GetKey(KeyCode.LeftArrow)) dir = NavDirection.Left;
            else if (Input.GetKey(KeyCode.RightArrow)) dir = NavDirection.Right;

            if (dir == NavDirection.None)
            {
                _navHeld = false;
                return NavDirection.None;
            }

            float now = Time.unscaledTime;

            // First press
            if (!_navHeld)
            {
                _navHeld = true;
                _lastNavTime = now;
                return dir;
            }

            // Hold-to-repeat
            float delay = (now - _lastNavTime > _navRepeatDelay + _navRepeatRate)
                ? _navRepeatRate
                : _navRepeatDelay;

            if (now - _lastNavTime >= delay)
            {
                _lastNavTime = now;
                return dir;
            }

            return NavDirection.None;
        }

        /// <summary>
        /// Check if Enter/Return was pressed this frame (confirm/activate).
        /// </summary>
        public static bool IsConfirmPressed()
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        /// <summary>
        /// Check if Escape was pressed this frame (back/cancel).
        /// Note: Only use in contexts where we won't conflict with game's Escape handling.
        /// </summary>
        public static bool IsBackPressed()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }

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
        /// Force the game's navigation system to focus on a specific item.
        /// Also positions the virtual cursor on it.
        /// </summary>
        public static void FocusItem(UINavigationItem item)
        {
            if (item == null) return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            // Switch to controller mode so navigation is active
            EnsureControllerMode();

            navSystem.SetCurrentNavigationItem(item);
        }

        /// <summary>
        /// Simulate a click/press on the currently focused navigation item.
        /// Uses ExecuteEvents directly since CustomEventSystem.Press is private.
        /// </summary>
        public static void ActivateCurrent()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            var current = navSystem.currentNavigationItem;
            if (current == null || current.clickHandler == null) return;

            var clickHandler = current.clickHandler;
            var pointerData = new PointerEventData(EventSystem.current);

            // Simulate full click sequence: down -> up -> click
            ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerDownHandler);
            CoroutineManager.Start(ReleaseNextFrame(clickHandler, pointerData));
        }

        private static System.Collections.IEnumerator ReleaseNextFrame(GameObject clickHandler, PointerEventData pointerData)
        {
            yield return null;
            ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>
        /// Check if an item is a help panel trigger (HelpPanelShower).
        /// These are excluded from navigation since help is on F1.
        /// </summary>
        private static bool IsHelpItem(UINavigationItem item)
        {
            GameObject obj = item.clickHandler ?? item.gameObject;
            return obj.GetComponent<HelpPanelShower>() != null
                || obj.GetComponentInParent<HelpPanelShower>() != null
                || item.gameObject.GetComponent<HelpPanelShower>() != null
                || item.gameObject.GetComponentInParent<HelpPanelShower>() != null;
        }

        /// <summary>
        /// Ensure the game thinks we're in controller/gamepad mode,
        /// so UINavigationSystem processes navigation instead of clearing it.
        /// </summary>
        public static void EnsureControllerMode()
        {
            var cursor = Cursor3d.instance;
            if (cursor != null && cursor.usingMouse)
            {
                cursor.usingMouse = false;
                cursor.usingTouch = false;
                VirtualPointer.Show();
            }
        }
    }

    /// <summary>Navigation direction for arrow key input.</summary>
    public enum NavDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}
