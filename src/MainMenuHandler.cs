using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Main Menu screen.
    /// Provides arrow key navigation between menu buttons and announces them via screen reader.
    /// </summary>
    public class MainMenuHandler : ScreenHandler
    {
        public override string Name => "MainMenu";

        private UINavigationItem _lastFocused;
        private bool _announced;
        private float _enterTime;

        public override void OnEnter()
        {
            base.OnEnter();
            _lastFocused = null;
            _announced = false;
            _enterTime = Time.unscaledTime;
        }

        public override void OnExit()
        {
            base.OnExit();
            _lastFocused = null;
        }

        public override void OnUpdate()
        {
            // Small delay before announcing to let UI settle
            if (!_announced && Time.unscaledTime - _enterTime > 0.5f)
            {
                _announced = true;
                ScreenReader.Say(Loc.Get("screen_main_menu"), interrupt: true);
            }

            if (!_announced) return;

            // Get navigable items (buttons on screen)
            var items = GetMenuItems();
            if (items.Count == 0) return;

            // Handle arrow key navigation
            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
                UINavigationItem current = navSystem?.currentNavigationItem;

                // Only handle up/down for vertical menu
                if (dir == NavDirection.Up || dir == NavDirection.Down)
                {
                    UINavigationItem next = NavigationHelper.NavigateLinear(items, current, dir, vertical: true);
                    if (next != null)
                    {
                        NavigationHelper.FocusItem(next);
                    }
                }
            }

            // Handle Enter to activate
            if (NavigationHelper.IsConfirmPressed())
            {
                var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
                if (navSystem?.currentNavigationItem != null)
                {
                    DebugLogger.LogInput(Name, "Confirm");
                    NavigationHelper.ActivateCurrent();
                }
            }

            // Announce focused item if it changed
            CheckAndAnnounceFocus();
        }

        /// <summary>
        /// Get the main menu buttons sorted top-to-bottom (by Y position descending).
        /// </summary>
        private List<UINavigationItem> GetMenuItems()
        {
            var items = NavigationHelper.GetNavigableItems();
            // Sort by Y position descending (top to bottom in screen space)
            items.Sort((a, b) => b.Position.y.CompareTo(a.Position.y));
            return items;
        }

        /// <summary>
        /// Check if the focused navigation item changed, and announce the new one.
        /// </summary>
        private void CheckAndAnnounceFocus()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            UINavigationItem current = navSystem.currentNavigationItem;
            if (current == _lastFocused) return;

            _lastFocused = current;

            if (current == null) return;

            string text = GetButtonText(current);
            if (!string.IsNullOrEmpty(text))
            {
                ScreenReader.Say(text, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Focused: {text}");
            }
        }
    }
}
