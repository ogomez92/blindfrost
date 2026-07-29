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
    public abstract partial class NavigableScreenHandler : ScreenHandler
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
            // first; then a help-panel popup; then the game's inspect view;
            // then the inventory overlay
            if (!OverlayWatcher.RouteInput()
                && !HelpPanelRouter.RouteInput()
                && !RouteInputToInspectView()
                && !DeckpackNavigator.RouteInput(this))
                HandleInput();
            PumpRefocus();
            CheckAndAnnounceFocus();
        }
    }
}
