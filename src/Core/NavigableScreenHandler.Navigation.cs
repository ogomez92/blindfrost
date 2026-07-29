using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Keyboard input, arrow-key navigation and confirm, refocus pumping, and
    /// the focus tracker that announces the focused item when it changes.
    /// </summary>
    public abstract partial class NavigableScreenHandler
    {
        /// <summary>Arrow key navigation and Enter activation. Override for custom input.</summary>
        protected virtual void HandleInput()
        {
            // I: inspect the focused item. Not while the chosen-card confirm
            // panel is open (Enter/Escape drive that) or while typing.
            if (Input.GetKeyDown(KeyCode.I) && ActiveInspectPanel == null
                && !NavigationHelper.IsTextInputFocused())
            {
                OnInspectKey();
                return;
            }

            // P: open the run inventory (deck, reserve, charms) where one exists.
            // Closing is handled by the deckpack routing, which owns the keys then.
            if (Input.GetKeyDown(KeyCode.P) && ActiveInspectPanel == null
                && !NavigationHelper.IsTextInputFocused())
            {
                DebugLogger.LogInput(Name, "Inventory");
                DeckpackNavigator.ToggleInventory();
                return;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                Navigate(dir);
            }

            if (NavigationHelper.IsConfirmPressed())
            {
                var panel = ActiveInspectPanel;
                if (panel != null)
                    ConfirmInspectPanel(panel);
                else
                    Confirm();
            }
            else if (NavigationHelper.IsBackPressed())
            {
                var panel = ActiveInspectPanel;
                if (panel != null)
                    CancelInspectPanel(panel);
            }
        }

        /// <summary>Move focus in the given direction. Default: linear spatial navigation.</summary>
        protected virtual void Navigate(NavDirection dir)
        {
            var items = GetItems();
            if (items.Count == 0)
            {
                // Silence here reads as a dead keyboard — say why nothing moves
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

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
        /// The item to land focus on when nothing sensible is focused — after an
        /// overlay (the inventory) closes and the game leaves focus in limbo.
        /// Defaults to the first navigable item; battle puts it on the hand.
        /// </summary>
        protected virtual UINavigationItem DefaultFocusItem()
        {
            var items = GetItems();
            return items.Count > 0 ? items[0] : null;
        }

        /// <summary>
        /// Ask the screen to put focus back on its default item over the next
        /// short window. Used when the inventory closes — the game frequently
        /// leaves the screen with no focus at all.
        /// </summary>
        public void RequestRefocus()
        {
            _refocusDeadline = Time.unscaledTime + 1f;
        }

        /// <summary>
        /// Drive a pending RequestRefocus. Waits until the target item is
        /// actually navigable again (an overlay's close animation can still own
        /// the layer — focusing an item the game hasn't re-registered would just
        /// null the selection), then focuses it and announces it fresh.
        /// </summary>
        private void PumpRefocus()
        {
            if (_refocusDeadline <= 0f)
                return;
            if (Time.unscaledTime > _refocusDeadline)
            {
                _refocusDeadline = 0f;
                return;
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null)
                return;

            var item = DefaultFocusItem();
            if (item == null || !navSystem.AvailableNavigationItems.Contains(item))
                return; // not ready yet — try again next frame

            NavigationHelper.FocusItem(item);
            _refocusDeadline = 0f;

            // We deliberately moved focus, so let it be spoken even if the close
            // transition asked to stay quiet, and force a fresh announcement.
            _inspectSuppressUntil = 0f;
            _lastFocused = null;
        }

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

        /// <summary>
        /// Whether this particular item is worth speaking when focus lands on it.
        /// Return false for items that are only destinations, never places to
        /// browse — the focus is still tracked, it just isn't announced.
        /// </summary>
        protected virtual bool ShouldAnnounceFocus(UINavigationItem item) => true;

        /// <summary>
        /// Keep focus changes silent for a moment so they don't talk over an
        /// announcement that matters more (deckpack pickups, menu openings).
        /// Focus is still tracked — the item is just not spoken.
        /// </summary>
        internal void SuppressFocusFor(float seconds)
        {
            _inspectSuppressUntil = Mathf.Max(
                _inspectSuppressUntil, Time.unscaledTime + seconds);
        }

        /// <summary>Announce the focused item when it changes.</summary>
        private void CheckAndAnnounceFocus()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            UINavigationItem current = navSystem.currentNavigationItem;
            if (current == _lastFocused) return;

            _lastFocused = current;
            if (current == null) return;

            if (SuppressFocusAnnouncements || InspectPanelSuppression) return;
            if (!ShouldAnnounceFocus(current)) return;

            string text = GetItemDescription(current);
            if (string.IsNullOrEmpty(text))
            {
                // Never focus silently — an unnamed item still needs a voice
                text = ScreenHandler.CleanName(current.gameObject.name);
            }
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
