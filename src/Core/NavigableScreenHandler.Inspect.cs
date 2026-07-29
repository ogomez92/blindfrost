using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Inspect surfaces: the game's zoomed InspectSystem view (open/route/close)
    /// and the InspectNewUnitSequence confirm panel (announce/confirm/cancel).
    /// </summary>
    public abstract partial class NavigableScreenHandler
    {
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
                greeting = TextProcessor.ProcessRawText(
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
    }
}
