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

        private InspectNewUnitSequence _inspectPanel;
        private bool _inspectWasRunning;
        private float _nextInspectPoll;
        private float _inspectSuppressUntil;

        private InspectSystem _inspectView;
        private float _nextInspectViewSearch;
        private float _inspectViewOpenTime;

        /// <summary>Deadline (unscaled time) for an in-progress RequestRefocus, or 0.</summary>
        private float _refocusDeadline;

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
            _inspectPanel = null;
            _inspectWasRunning = false;
            _nextInspectPoll = 0f;
            _inspectSuppressUntil = 0f;
            _inspectView = null;
            _nextInspectViewSearch = 0f;
            _inspectViewOpenTime = 0f;
            _refocusDeadline = 0f;
            DeckpackNavigator.Reset();
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

                // A precondition that never becomes true (a handler waiting on
                // state that this particular screen never reaches) must not
                // leave the screen unnamed forever
                if (!_announced && Time.unscaledTime - _enterTime > AnnounceDelay + 12f)
                {
                    _announced = true;
                    ScreenReader.SayEvent(CleanName(Name), interrupt: true);
                    DebugLogger.Log(DebugLogger.LogCategory.Handler, Name,
                        "Screen announcement timed out; spoke handler name");
                }
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

            // Watch for the inspect/confirm panel opening (cheap 4 Hz poll)
            if (Time.unscaledTime >= _nextInspectPoll)
            {
                _nextInspectPoll = Time.unscaledTime + 0.25f;
                bool running = ActiveInspectPanel != null;
                if (running != _inspectWasRunning)
                {
                    _inspectWasRunning = running;
                    if (running)
                        OnInspectPanelOpened(ActiveInspectPanel);
                }
            }

            // Give the UI a moment to initialize before accepting input
            if (Time.unscaledTime - _enterTime < 0.3f) return;

            // Blocking cinematics (card combine, final-boss shade) own the keys
            // first; then the game's inspect view; then the inventory overlay
            if (!OverlayWatcher.RouteInput()
                && !RouteInputToInspectView()
                && !DeckpackNavigator.RouteInput(this))
                HandleInput();
            PumpRefocus();
            CheckAndAnnounceFocus();
        }

        // ---- The game's inspect view (InspectSystem) ----
        // The zoomed card view the game opens on right-click. I opens it on
        // the focused card via the game's real ActionInspect: tutorials gate
        // on the resulting Events.OnInspect ("inspect the Ooba Bear"), so the
        // mod's spoken description alone does not satisfy them.

        /// <summary>The scene's InspectSystem, or null where none exists.</summary>
        protected InspectSystem GetInspectSystem(bool forceSearch = false)
        {
            // Throttled: screens without an InspectSystem would otherwise pay
            // a scene-wide search every frame
            if (_inspectView == null
                && (forceSearch || Time.unscaledTime >= _nextInspectViewSearch))
            {
                _nextInspectViewSearch = Time.unscaledTime + 1f;
                _inspectView = Object.FindObjectOfType<InspectSystem>();
            }
            return _inspectView;
        }

        /// <summary>
        /// While the inspect view is open, every key routes to it: Escape,
        /// Enter or I closes; everything else is swallowed so navigation and
        /// screen shortcuts do not run underneath the zoomed card.
        /// Returns true while the view is open.
        /// </summary>
        private bool RouteInputToInspectView()
        {
            var inspectView = GetInspectSystem();
            if (inspectView == null || inspectView.inspect == null)
                return false;

            bool close = NavigationHelper.IsBackPressed()
                || NavigationHelper.IsConfirmPressed()
                || Input.GetKeyDown(KeyCode.I);
            // Give the open animation a moment; InspectSystem has the same guard
            if (close && Time.unscaledTime - _inspectViewOpenTime > 0.3f)
            {
                inspectView.InspectEnd();
                ScreenReader.Say(Loc.Get("inspect_closed"), interrupt: true);
            }
            return true;
        }

        /// <summary>
        /// I pressed. Default: the game's real inspect on the focused card.
        /// Screens whose I key reads a different info surface (town buildings,
        /// map nodes) override this.
        /// </summary>
        protected virtual void OnInspectKey()
        {
            InspectFocusedCard();
        }

        internal void InspectFocusedCard()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            Entity entity = item != null ? item.GetComponentInParent<Entity>() : null;
            if (entity == null && item?.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();

            var inspectView = GetInspectSystem(forceSearch: true);
            if (entity == null || entity.display == null || inspectView == null)
            {
                ScreenReader.Say(Loc.Get("nothing_to_inspect"), interrupt: true);
                return;
            }

            DebugLogger.LogInput(Name, $"Inspect: {entity.data?.title ?? entity.name}");
            var action = new ActionInspect(entity, inspectView);
            if (Events.CheckAction(action))
            {
                action.Process();
                _inspectViewOpenTime = Time.unscaledTime;
                ScreenReader.Say(
                    Loc.Get("inspect_opened", entity.data?.title ?? CleanName(entity.name)),
                    interrupt: true);
            }
            else
            {
                ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
            }
        }

        // ---- Inspect/confirm panel (InspectNewUnitSequence) ----
        // Opens when a card is picked on select screens: character select,
        // companion map events, the starting pet choice. Its buttons
        // ("Let's Go!" / X / rename) are NOT navigation items, so without
        // this the keyboard is completely locked out while it is open.

        /// <summary>The inspect panel currently running, or null.</summary>
        protected InspectNewUnitSequence ActiveInspectPanel
        {
            get
            {
                if (_inspectPanel == null || !_inspectPanel.gameObject.activeInHierarchy)
                    _inspectPanel = Object.FindObjectOfType<InspectNewUnitSequence>();
                return (_inspectPanel != null && _inspectPanel.IsRunning) ? _inspectPanel : null;
            }
        }

        /// <summary>Announce the panel: who was chosen, their greeting bubble,
        /// and how to proceed.</summary>
        protected virtual void OnInspectPanelOpened(InspectNewUnitSequence panel)
        {
            Entity unit = ReflectionUtil.GetField<Entity>(panel, "unit");
            string title = unit?.data?.title;
            string msg = !string.IsNullOrEmpty(title)
                ? Loc.Get("charselect_chosen", title)
                : Loc.Get("charselect_chosen_generic");

            // The unit greets from a speech bubble on the panel ("Hi! I'm
            // <name>!") — visual-only in vanilla, so fold it in here
            string greeting = ReflectionUtil.GetField<string>(panel, "greeting");
            if (!string.IsNullOrEmpty(greeting) && !string.IsNullOrEmpty(title))
            {
                greeting = TextProcessor.StripRichText(
                    greeting.Replace("<name>", title))?.Trim();
                if (!string.IsNullOrEmpty(greeting))
                    msg = greeting + " " + msg;
            }
            ScreenReader.Say(msg, interrupt: true);
        }

        /// <summary>
        /// Enter while the panel is open. Default: the panel's own TakeCard()
        /// (confirms the unit into the deck) when a card selector is wired.
        /// CharacterSelect overrides this — its panel confirms elsewhere.
        /// </summary>
        protected virtual void ConfirmInspectPanel(InspectNewUnitSequence panel)
        {
            DebugLogger.LogInput(Name, "Confirm inspect panel");
            if (panel.cardSelector != null)
            {
                panel.TakeCard();
                return;
            }
            ScreenReader.Say(Loc.Get("inspect_no_confirm"), interrupt: true);
        }

        /// <summary>
        /// Escape while the panel is open: close it and put the card back.
        /// Safe to intercept — the game has no keyboard path on these panels.
        /// </summary>
        protected virtual void CancelInspectPanel(InspectNewUnitSequence panel)
        {
            DebugLogger.LogInput(Name, "Cancel inspect panel");
            panel.End(); // Run() tail returns the card and pops the nav layer

            // Selecting disabled the screen's select-card controller(s);
            // re-enable so browsing works again. Only select-type controllers
            // are touched — organizer/battle controllers are never disabled
            // by these panels.
            foreach (var controller in Object.FindObjectsOfType<CardControllerSelectCard>())
            {
                if (!controller.enabled)
                    controller.Enable();
            }

            // Let the cancel message finish before focus chatter resumes
            _inspectSuppressUntil = Time.unscaledTime + 1.5f;
            ScreenReader.Say(Loc.Get("charselect_cancelled"), interrupt: true);
        }

        /// <summary>
        /// True while the inspect panel is open or briefly after cancelling —
        /// keeps the focus tracker from talking over the panel announcements.
        /// </summary>
        private bool InspectPanelSuppression
            => _inspectWasRunning || Time.unscaledTime < _inspectSuppressUntil;

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

            if (SuppressFocusAnnouncements || InspectPanelSuppression) return;

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
