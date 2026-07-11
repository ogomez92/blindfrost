using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Base class for screen handlers with the standard accessibility loop:
    /// announce the screen on entry, arrow key navigation, Enter to activate,
    /// and announcing the focused item whenever it changes.
    /// Subclasses customize via GetScreenAnnouncement / GetItemDescription / GetItems.
    /// </summary>
    public abstract class NavigableScreenHandler : ScreenHandler
    {
        private UINavigationItem _lastFocused;
        private bool _announced;
        private float _enterTime;
        private UINavigationLayer _lastNavLayer;

        /// <summary>Seconds to wait after entry before announcing the screen (lets UI settle).</summary>
        protected virtual float AnnounceDelay => 0.5f;

        /// <summary>Time this screen became active (unscaled).</summary>
        protected float EnterTime => _enterTime;

        public override void OnEnter()
        {
            base.OnEnter();
            _lastFocused = null;
            _announced = false;
            _enterTime = Time.unscaledTime;
            _lastNavLayer = UINavigationSystem.ActiveNavigationLayer;
        }

        public override void OnExit()
        {
            base.OnExit();
            _lastFocused = null;
        }

        public override void OnUpdate()
        {
            // Announce the screen once the UI has settled.
            // TryAnnounceScreen may return false to retry while content is still loading.
            if (!_announced && Time.unscaledTime - _enterTime >= AnnounceDelay)
            {
                _announced = TryAnnounceScreen();
            }

            // Detect navigation layer changes (popups/panels opening within the same scene)
            var currentLayer = UINavigationSystem.ActiveNavigationLayer;
            if (currentLayer != _lastNavLayer)
            {
                _lastNavLayer = currentLayer;
                _lastFocused = null; // Re-announce focus when the active panel changes
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name,
                    $"Navigation layer changed: {currentLayer?.name ?? "null"}");
                OnNavigationLayerChanged(currentLayer);
            }

            // Give the UI a moment to initialize before accepting input
            if (Time.unscaledTime - _enterTime < 0.3f) return;

            HandleInput();
            CheckAndAnnounceFocus();
        }

        /// <summary>Arrow key navigation and Enter activation. Override for custom input.</summary>
        protected virtual void HandleInput()
        {
            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                Navigate(dir);
            }

            if (NavigationHelper.IsConfirmPressed())
            {
                Confirm();
            }
        }

        /// <summary>Move focus in the given direction. Default: linear spatial navigation.</summary>
        protected virtual void Navigate(NavDirection dir)
        {
            var items = GetItems();
            if (items.Count == 0) return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            UINavigationItem current = navSystem?.currentNavigationItem;

            UINavigationItem next;
            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                // Sort top-to-bottom for vertical navigation
                items.Sort((a, b) => b.Position.y.CompareTo(a.Position.y));
                next = NavigationHelper.NavigateLinear(items, current, dir, vertical: true);
            }
            else
            {
                // Sort left-to-right for horizontal navigation
                items.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
                next = NavigationHelper.NavigateLinear(items, current, dir, vertical: false);
            }

            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        /// <summary>Activate the focused item. Override for custom confirm behavior.</summary>
        protected virtual void Confirm()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem?.currentNavigationItem != null)
            {
                DebugLogger.LogInput(Name, "Confirm");
                NavigationHelper.ActivateCurrent();
            }
        }

        /// <summary>The set of items reachable with arrow keys. Override to filter or reorder.</summary>
        protected virtual List<UINavigationItem> GetItems()
        {
            return NavigationHelper.GetNavigableItems();
        }

        /// <summary>
        /// Announce this screen (name, context, hints). Return false to retry next frame
        /// while content is still loading; return true once announced.
        /// </summary>
        protected abstract bool TryAnnounceScreen();

        /// <summary>Called when the active navigation layer changes (panel/popup opened).</summary>
        protected virtual void OnNavigationLayerChanged(UINavigationLayer layer)
        {
        }

        /// <summary>Force the next focus check to re-announce even if focus did not change.</summary>
        protected void ResetFocusTracking()
        {
            _lastFocused = null;
        }

        /// <summary>
        /// When true, focus changes are tracked but not spoken. Used while the game
        /// moves focus on its own (e.g. battle resolution) so event narration isn't cut off.
        /// </summary>
        protected virtual bool SuppressFocusAnnouncements => false;

        /// <summary>Announce the focused item when it changes.</summary>
        private void CheckAndAnnounceFocus()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            UINavigationItem current = navSystem.currentNavigationItem;
            if (current == _lastFocused) return;

            _lastFocused = current;
            if (current == null) return;

            if (SuppressFocusAnnouncements) return;

            string text = GetItemDescription(current);
            if (!string.IsNullOrEmpty(text))
            {
                ScreenReader.Say(text, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Focused: {text}");
            }
        }

        /// <summary>
        /// Describe a focused item. Default cascade handles battlefield slots, town buildings,
        /// card pockets, map nodes, and card entities before falling back to button text.
        /// </summary>
        protected virtual string GetItemDescription(UINavigationItem item)
        {
            return ItemDescriber.Describe(item, this);
        }
    }
}
