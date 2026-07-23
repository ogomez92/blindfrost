using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the "Event" scene: map story events (the frozen-companion
    /// ice block, item chests, charms, munchers, shops...). The screen's title
    /// and story text live in the cinema bars (announced by CinemaBarReader);
    /// this handler names the screen on entry, gives bare interaction points
    /// (the ice block) the bar prompt as context, and announces ice-crack
    /// progress. The base class's I key fires the game's real inspect — the
    /// tutorial refuses to let a companion be chosen until one card has
    /// actually been inspected.
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

        public override void OnEnter()
        {
            base.OnEnter();
            _barRetries = 0;
            _nextBarTry = 0f;
            _companion = null;
            _companionSearched = false;
            _nextCrackPoll = 0f;
            _lastCrackDamage = -1;
            _dragController = null;
            CharmGainNarrator.Reset();
        }

        public override string GetHelpText() => Loc.Get("help_event");

        public override void OnUpdate()
        {
            base.OnUpdate();
            AnnounceCrackProgress();
        }

        // ---- Card pickup (muncher and other feed-a-card events) -----------
        // The muncher event asks the player to DRAG a deck card onto it; its
        // cards use a plain CardController, so the select-card Enter path
        // never fired and the event was a keyboard dead-end. Mirrors the
        // battle pickup: press via reflection, the game's own navigation
        // state then restricts arrows to valid targets, Enter releases.

        private CardController _dragController;

        private bool IsDraggingCard =>
            _dragController != null && _dragController.dragging != null;

        protected override void HandleInput()
        {
            // The charm-gain popup (charm block, charm/clunk shop) owns the keys
            if (CharmGainNarrator.RouteInput(this))
                return;

            // G: current gold (Bling). Shops price everything in it and read
            // affordability on focus, so let the player check their total here
            // the same way they can in battle and on the map.
            if (Input.GetKeyDown(KeyCode.G) && !NavigationHelper.IsTextInputFocused())
            {
                DebugLogger.LogInput(Name, "Gold");
                if (ItemDescriber.TryGetPlayerGold(out int gold))
                    ScreenReader.Say(Loc.Get("gold_amount", gold), interrupt: true);
                else
                    ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
                return;
            }

            // Escape puts a held card back
            if (IsDraggingCard && NavigationHelper.IsBackPressed())
            {
                string title = _dragController.dragging?.data?.title ?? "";
                DebugLogger.LogInput(Name, "Cancel event card pickup");
                _dragController.DragCancel();
                ScreenReader.Say(Loc.Get("battle_pickup_cancelled", title), interrupt: true);
                return;
            }
            base.HandleInput();
        }

        protected override void Confirm()
        {
            // Holding a card: release it on the focused target (the muncher)
            if (IsDraggingCard)
            {
                string title = _dragController.dragging?.data?.title ?? "";
                DebugLogger.LogInput(Name, "Release event card");
                if (ReflectionUtil.InvokeMethod(_dragController, "Release"))
                    ScreenReader.SayEvent(Loc.Get("event_card_released", title));
                return;
            }

            // A focused own card with a plain drag controller: pick it up.
            // Select-card controllers keep their existing path in base.Confirm.
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            Entity entity = current != null ? current.GetComponentInParent<Entity>() : null;
            if (entity == null && current?.clickHandler != null)
                entity = current.clickHandler.GetComponentInParent<Entity>();

            var controller = entity?.display?.hover?.controller;
            if (entity != null && controller != null
                && !(controller is CardControllerSelectCard)
                && controller.enabled && controller.canPress)
            {
                DebugLogger.LogInput(Name, $"Pick up event card: {entity.data?.title}");
                controller.hoverEntity = entity;
                if (ReflectionUtil.SetField(controller, "pressEntity", entity)
                    && ReflectionUtil.InvokeMethod(controller, "Press")
                    && controller.dragging != null)
                {
                    _dragController = controller;
                    string msg = Loc.Get("event_card_picked_up", entity.data?.title ?? "");
                    string hint = HintOnce("event_pickup_hint");
                    if (hint != null)
                        msg += " " + hint;
                    ScreenReader.Say(msg, interrupt: true);
                    return;
                }
            }

            base.Confirm();
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
            ScreenReader.SayEvent(text, interrupt: true);
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
            // The charm-gain popup's Assign button: name the charm and the options
            string charmDesc = CharmGainNarrator.DescribeFocused();
            if (charmDesc != null)
                return charmDesc;

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
                    ScreenReader.SayEvent(Loc.Get("event_crack", damage), interrupt: true);
            }
        }
    }
}
