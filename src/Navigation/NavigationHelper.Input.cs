using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Reading the keyboard: arrow keys with hold-to-repeat, confirm and back,
    /// the text-field guard that mutes letter bindings while typing, and
    /// forcing the game into controller mode so its UINavigationSystem keeps
    /// processing navigation instead of clearing it.
    /// </summary>
    public static partial class NavigationHelper
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
            // Ctrl+arrows belong to the review buffers (ReviewBuffers) —
            // while Ctrl is held the real selection must never move, so
            // reviewing is always safe.
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                _navHeld = false;
                return NavDirection.None;
            }

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
        /// True while a text input field has focus (console, run naming).
        /// Letter-key mod bindings must stay inactive then.
        /// </summary>
        public static bool IsTextInputFocused()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null) return false;
            return selected.GetComponent<TMPro.TMP_InputField>() != null
                || selected.GetComponent<UnityEngine.UI.InputField>() != null;
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
}
