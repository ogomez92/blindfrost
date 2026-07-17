using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Owns the keyboard while a HelpPanelSystem popup is open — the modal
    /// panel the game uses for confirms ("Skip Tutorial?" with Retry/Skip,
    /// the give-up confirm) and first-time help. The popup pushes its own
    /// navigation layer, but handlers with custom navigation (the town's
    /// building ring) kept driving the screen underneath: arrows hovered
    /// invisible buildings, the game snapped focus back to the popup's
    /// default button after each press, and the other answer was unreachable.
    /// While the popup's layer is active: arrows move between its buttons,
    /// Enter presses the focused one, Escape presses the popup's Back arrow.
    /// Everything else is swallowed so the screen underneath cannot react.
    /// </summary>
    public static class HelpPanelRouter
    {
        private static HelpPanelSystem _panel;
        private static readonly List<UINavigationItem> _items = new List<UINavigationItem>();

        /// <summary>Drop cached scene objects (mod unload/reload hygiene).</summary>
        public static void Reset()
        {
            _panel = null;
            _items.Clear();
        }

        /// <summary>
        /// True while a help-panel popup owns the keys. Called from the screen
        /// handlers' input chain; when it returns true the handler must not
        /// process navigation or Enter itself.
        /// </summary>
        public static bool RouteInput()
        {
            var panel = ActivePanel();
            if (panel == null)
                return false;

            // Something else can open over the popup (the pause menu) and push
            // its own navigation layer — only own the keys while the popup's
            // layer is the active one. (IsChildOf both ways: the layer and the
            // HelpPanelSystem component may sit on different popup nodes.)
            var layer = UINavigationSystem.ActiveNavigationLayer;
            if (layer == null
                || !(layer.transform.IsChildOf(panel.transform)
                     || panel.transform.IsChildOf(layer.transform)))
                return false;

            RefreshItems(panel);

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem != null ? navSystem.currentNavigationItem : null;
            if (current != null && !_items.Contains(current))
                current = null; // focus is somewhere under the popup — never act on it

            if (NavigationHelper.IsConfirmPressed())
            {
                if (current != null)
                {
                    DebugLogger.LogInput("HelpPanel", "Confirm");
                    NavigationHelper.ActivateCurrent();
                }
                else if (_items.Count > 0)
                {
                    // Land focus on an answer button instead of clicking
                    // through the popup at whatever sits below it.
                    NavigationHelper.FocusItem(DefaultItem(panel));
                }
                return true;
            }

            if (NavigationHelper.IsBackPressed())
            {
                PressBack(panel);
                return true;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None && _items.Count > 0)
            {
                bool vertical = dir == NavDirection.Up || dir == NavDirection.Down;
                var next = NavigationHelper.NavigateLinear(_items, current, dir, vertical)
                    ?? _items[0];
                NavigationHelper.FocusItem(next);
            }

            return true;
        }

        /// <summary>The open HelpPanelSystem popup, or null.</summary>
        private static HelpPanelSystem ActivePanel()
        {
            if (!HelpPanelSystem.Active)
                return null;
            // The panel object is reused (toggled active); cache it and only
            // pay the scene search when the cache is empty or destroyed.
            if (_panel == null)
                _panel = Object.FindObjectOfType<HelpPanelSystem>();
            return (_panel != null && _panel.gameObject.activeInHierarchy) ? _panel : null;
        }

        /// <summary>
        /// The popup's navigable buttons in reading order. The Back arrow sits
        /// alone at the left edge and the answer buttons run left to right
        /// under the text, so an x-sort gives Back, then each answer.
        /// </summary>
        private static void RefreshItems(HelpPanelSystem panel)
        {
            _items.Clear();
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null)
                return;

            foreach (var item in navSystem.AvailableNavigationItems)
            {
                if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                    continue;
                if (!item.transform.IsChildOf(panel.transform))
                    continue;
                _items.Add(item);
            }
            _items.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
        }

        /// <summary>First answer button, falling back to the Back arrow.</summary>
        private static UINavigationItem DefaultItem(HelpPanelSystem panel)
        {
            var backRoot = ReflectionUtil.GetField<GameObject>(panel, "backButton");
            foreach (var item in _items)
            {
                if (backRoot == null || !item.transform.IsChildOf(backRoot.transform))
                    return item;
            }
            return _items[0];
        }

        /// <summary>
        /// Escape: press the popup's Back arrow (cancels without answering).
        /// Popups shown with canGoBack=false have no arrow — those demand an
        /// answer, so say that instead of leaving Escape silent.
        /// </summary>
        private static void PressBack(HelpPanelSystem panel)
        {
            var backRoot = ReflectionUtil.GetField<GameObject>(panel, "backButton");
            if (backRoot == null || !backRoot.activeInHierarchy)
            {
                ScreenReader.Say(Loc.Get("help_panel_no_back"), interrupt: true);
                return;
            }

            DebugLogger.LogInput("HelpPanel", "Back");
            foreach (var item in _items)
            {
                if (item.transform.IsChildOf(backRoot.transform))
                {
                    NavigationHelper.PressObject(item.clickHandler ?? item.gameObject);
                    ScreenReader.Say(Loc.Get("help_panel_closed"), interrupt: true);
                    return;
                }
            }

            // No navigation item on the arrow (never seen, but the popup must
            // stay escapable) — press its Button directly.
            var button = backRoot.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
            {
                NavigationHelper.PressObject(button.gameObject);
                ScreenReader.Say(Loc.Get("help_panel_closed"), interrupt: true);
            }
        }
    }
}
