using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the "Event" scene: map story events (the frozen-companion
    /// ice block, item chests, charms, munchers, shops...). The screen's title
    /// and story text live in the cinema bars (announced by CinemaBarReader);
    /// this handler names the screen on entry, gives bare interaction points
    /// (the ice block) the bar prompt as context, announces ice-crack progress,
    /// and binds I to the game's real inspect — the tutorial refuses to let a
    /// companion be chosen until one card has actually been inspected.
    /// </summary>
    public class EventScreenHandler : NavigableScreenHandler
    {
        public override string Name => "Event";

        private int _barRetries;
        private float _nextBarTry;

        private EventRoutineCompanion _companion;
        private bool _companionSearched;
        private float _nextCrackPoll;
        private int _lastCrackDamage = -1;

        private InspectSystem _inspectSystem;
        private bool _inspectSearched;
        private float _inspectOpenTime;

        public override void OnEnter()
        {
            base.OnEnter();
            _barRetries = 0;
            _nextBarTry = 0f;
            _companion = null;
            _companionSearched = false;
            _nextCrackPoll = 0f;
            _lastCrackDamage = -1;
            _inspectSystem = null;
            _inspectSearched = false;
            _inspectOpenTime = 0f;
        }

        public override string GetHelpText() => Loc.Get("help_event");

        public override void OnUpdate()
        {
            base.OnUpdate();
            AnnounceCrackProgress();
        }

        /// <summary>
        /// Announce "Event." plus whatever the cinema bar shows ("Break the
        /// ice!"). The bar text is set by the event's Populate coroutine, so
        /// retry briefly while it loads before announcing the name alone.
        /// </summary>
        protected override bool TryAnnounceScreen()
        {
            string bar = CinemaBarReader.CurrentText();
            if (bar == null && _barRetries < 4)
            {
                if (Time.unscaledTime < _nextBarTry)
                    return false;
                _barRetries++;
                _nextBarTry = Time.unscaledTime + 0.5f;
                return false;
            }

            string text = Loc.Get("scene_Event");
            if (bar != null)
            {
                text += " " + bar;
                CinemaBarReader.SyncAnnounced();
            }
            ScreenReader.Say(text, interrupt: true);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Screen: {text}");
            return true;
        }

        /// <summary>
        /// Bare interaction points (the ice block, a chest) have no Entity and
        /// read as their object name. When such a point is the only thing to
        /// interact with, give it the bar prompt as context:
        /// "Ice. Break the ice! Press Enter."
        /// </summary>
        protected override string GetItemDescription(UINavigationItem item)
        {
            string desc = base.GetItemDescription(item);

            Entity entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            if (entity != null)
                return desc;

            string prompt = CinemaBarReader.CurrentPrompt();
            if (prompt == null || GetItems().Count != 1)
                return desc;

            string name = string.IsNullOrEmpty(desc)
                ? CleanName((item.clickHandler ?? item.gameObject).name)
                : desc;
            return $"{name}. {prompt} {Loc.Get("event_prompt_action")}";
        }

        protected override void HandleInput()
        {
            // While the game's inspect view is open, every key routes to it:
            // Escape, I or Enter closes; arrows are swallowed so navigation
            // does not run underneath the zoomed card.
            var inspectSystem = GetInspectSystem();
            if (inspectSystem != null && inspectSystem.inspect != null)
            {
                bool close = NavigationHelper.IsBackPressed()
                    || NavigationHelper.IsConfirmPressed()
                    || Input.GetKeyDown(KeyCode.I);
                // Give the open animation a moment; InspectSystem has the same guard
                if (close && Time.unscaledTime - _inspectOpenTime > 0.3f)
                {
                    inspectSystem.InspectEnd();
                    ScreenReader.Say(Loc.Get("inspect_closed"), interrupt: true);
                }
                return;
            }

            // I: the game's real inspect on the focused card. Unlike the mod's
            // spoken card description (already read on focus), this fires
            // ActionInspect/Events.OnInspect — which is what the tutorial
            // waits for before it allows choosing a companion. Not while the
            // chosen-card confirm panel is open: Enter/Escape drive that.
            if (Input.GetKeyDown(KeyCode.I) && ActiveInspectPanel == null
                && !NavigationHelper.IsTextInputFocused())
            {
                InspectFocusedCard(inspectSystem);
                return;
            }

            base.HandleInput();
        }

        private void InspectFocusedCard(InspectSystem inspectSystem)
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            Entity entity = item != null ? item.GetComponentInParent<Entity>() : null;
            if (entity == null && item?.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();

            if (entity == null || entity.display == null || inspectSystem == null)
            {
                ScreenReader.Say(Loc.Get("nothing_to_inspect"), interrupt: true);
                return;
            }

            DebugLogger.LogInput(Name, $"Inspect: {entity.data?.title ?? entity.name}");
            var action = new ActionInspect(entity, inspectSystem);
            if (Events.CheckAction(action))
            {
                action.Process();
                _inspectOpenTime = Time.unscaledTime;
                ScreenReader.Say(
                    Loc.Get("inspect_opened", entity.data?.title ?? CleanName(entity.name)),
                    interrupt: true);
            }
            else
            {
                ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
            }
        }

        /// <summary>
        /// The frozen-companion event takes 4 hits to open. Announce progress
        /// after each hit — Enter otherwise gives no feedback until the ice
        /// breaks. The break itself announces through the bar prompt change.
        /// </summary>
        private void AnnounceCrackProgress()
        {
            if (Time.unscaledTime < _nextCrackPoll)
                return;
            _nextCrackPoll = Time.unscaledTime + 0.25f;

            if (!_companionSearched)
            {
                _companionSearched = true;
                _companion = Object.FindObjectOfType<EventRoutineCompanion>();
            }
            if (_companion == null || _companion.node?.data == null)
                return;

            int damage = 0;
            if (_companion.node.data.TryGetValue("damage", out object value))
            {
                try { damage = System.Convert.ToInt32(value); }
                catch { return; }
            }

            if (_lastCrackDamage < 0)
            {
                // Baseline on entry — don't announce pre-existing damage
                _lastCrackDamage = damage;
                return;
            }
            if (damage != _lastCrackDamage)
            {
                _lastCrackDamage = damage;
                if (!_companion.broken)
                    ScreenReader.Say(Loc.Get("event_crack", damage), interrupt: true);
            }
        }

        private InspectSystem GetInspectSystem()
        {
            if (!_inspectSearched || _inspectSystem == null)
            {
                _inspectSearched = true;
                _inspectSystem = Object.FindObjectOfType<InspectSystem>();
            }
            return _inspectSystem;
        }
    }
}
