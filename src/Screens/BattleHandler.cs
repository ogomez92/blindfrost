using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Battle scene.
    /// Navigation model: Up/Down switch groups (hand, your board, enemy board, system items),
    /// Left/Right move within the group. Enter picks up a card, Enter again places it.
    /// Announces turns, phases, waves, and full card descriptions with status effects.
    /// Extra keys: H hand, B board, W waves, R redraw bell, G gold, T turn, M modifiers.
    /// </summary>
    public partial class BattleHandler : NavigableScreenHandler
    {
        public override string Name => "Battle";

        private enum Group { Hand, PlayerBoard, EnemyBoard, System }

        private Group _group = Group.Hand;
        private Battle.Phase _lastAnnouncedPhase = Battle.Phase.None;
        private bool _subscribed;

        public override void OnEnter()
        {
            base.OnEnter();
            _group = Group.Hand;
            _lastAnnouncedPhase = Battle.Phase.None;
            Subscribe();
        }

        public override void OnExit()
        {
            base.OnExit();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            Events.OnBattlePhaseStart += OnPhaseStart;
            Events.OnBattleTurnStart += OnTurnStart;
            Events.OnRedrawBellHit += OnRedrawBellHit;
            Events.OnEntityPostHit += OnEntityPostHit;
            Events.OnEntityKilled += OnEntityKilled;
            Events.OnStatusEffectApplied += OnStatusApplied;
            Events.OnEntityTrigger += OnEntityTrigger;
            Events.OnKillCombo += OnKillCombo;
            Events.OnDropGold += OnDropGold;
            Events.OnCardInjured += OnCardInjured;
            Events.OnStatusIconChanged += OnStatusIconChanged;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            Events.OnBattlePhaseStart -= OnPhaseStart;
            Events.OnBattleTurnStart -= OnTurnStart;
            Events.OnRedrawBellHit -= OnRedrawBellHit;
            Events.OnEntityPostHit -= OnEntityPostHit;
            Events.OnEntityKilled -= OnEntityKilled;
            Events.OnStatusEffectApplied -= OnStatusApplied;
            Events.OnEntityTrigger -= OnEntityTrigger;
            Events.OnKillCombo -= OnKillCombo;
            Events.OnDropGold -= OnDropGold;
            Events.OnCardInjured -= OnCardInjured;
            Events.OnStatusIconChanged -= OnStatusIconChanged;
        }

        protected override bool TryAnnounceScreen()
        {
            var battle = Battle.instance;
            if (battle == null || battle.player == null)
                return false;

            var parts = new List<string> { Loc.Get("screen_battle") };

            int waveCount = WaveDeployer.GetWaves(WaveDeployer.Find())?.Count ?? 0;
            if (waveCount > 0)
                parts.Add(Loc.Get("battle_wave_total", waveCount));

            int handCount = battle.player.handContainer?.Count ?? 0;
            if (handCount > 0)
                parts.Add(Loc.Get("battle_hand_count", handCount));

            // Crowned cards deploy before the battle starts — tell the player now
            int crowned = CountCrownedInHand(battle);
            if (crowned == 1)
                parts.Add(Loc.Get("battle_crown_deploy_one"));
            else if (crowned > 1)
                parts.Add(Loc.Get("battle_crown_deploy", crowned));

            // Navigation instructions only the first time this session; F1 repeats them
            string hint = HintOnce("battle_hint");
            if (hint != null)
                parts.Add(hint);

            ScreenReader.SayEvent(string.Join(" ", parts), interrupt: true);
            return true;
        }

        // ---- Game event announcements -------------------------------------

        private void OnPhaseStart(Battle.Phase phase)
        {
            if (phase == _lastAnnouncedPhase) return;
            _lastAnnouncedPhase = phase;

            switch (phase)
            {
                case Battle.Phase.Play:
                    var battle = Battle.instance;
                    int hand = battle?.player?.handContainer?.Count ?? 0;
                    string wave = GetWaveCounterText(atTurnStart: true);
                    string msg = Loc.Get("battle_your_turn", hand);
                    if (wave != null)
                        msg += " " + wave;
                    ScreenReader.SayEvent(msg);
                    break;
                case Battle.Phase.Battle:
                    ScreenReader.SayEvent(Loc.Get("battle_resolving"));
                    break;
                case Battle.Phase.End:
                    ScreenReader.SayEvent(Loc.Get("battle_over"));
                    break;
                case Battle.Phase.LastStand:
                    // A dice standoff: the game blocks until Roll is pressed
                    _lastStandResultSeen = -1;
                    string subject = LastStandSystem.subject?.data?.title;
                    ScreenReader.SayEvent(subject != null
                        ? Loc.Get("battle_last_stand", subject)
                        : Loc.Get("battle_last_stand_generic"), interrupt: true);
                    break;
            }
        }

        /// <summary>Outcome of the last dice roll we already announced.</summary>
        private int _lastStandResultSeen = -1;

        public override void OnUpdate()
        {
            RedirectFromUseOnHandAnchor();
            base.OnUpdate();
            WatchLastStand();
        }

        /// <summary>
        /// At battle start the game parks focus on the "use on hand" targeting
        /// anchor, which reads as the meaningless "Use On Hand Anchor". It only
        /// matters while holding a self-target card, so when nothing is held move
        /// focus to the hand instead.
        /// </summary>
        private void RedirectFromUseOnHandAnchor()
        {
            var controller = Battle.instance?.playerCardController as CardControllerBattle;
            var anchor = controller?.useOnHandAnchor;
            if (anchor == null || controller.dragging != null)
                return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null || navSystem.currentNavigationItem != anchor)
                return;

            var hand = GetGroupItems(Group.Hand);
            if (hand.Count > 0)
            {
                _group = Group.Hand;
                NavigationHelper.FocusItem(hand[0]);
            }
        }

        /// <summary>
        /// The anchor is a drop zone, not a place to browse. All through battle
        /// setup the game re-parks default focus on it while the hand is still
        /// being drawn, so the redirect above has nowhere to send focus yet and
        /// it read as a spurious "Play without a target" before the first turn.
        /// It only means anything while a card is held — and then the targeting
        /// description names it properly — so stay silent otherwise.
        /// </summary>
        protected override bool ShouldAnnounceFocus(UINavigationItem item)
        {
            if (IsTargeting())
                return true;

            var controller = Battle.instance?.playerCardController as CardControllerBattle;
            return controller == null || item != controller.useOnHandAnchor;
        }

        /// <summary>
        /// The last stand dice roll resolves inside a coroutine with no event
        /// hook — watch the system's private result field for the outcome.
        /// </summary>
        private void WatchLastStand()
        {
            var battle = Battle.instance;
            if (battle == null || battle.phase != Battle.Phase.LastStand)
                return;

            var system = Object.FindObjectOfType<LastStandSystem>();
            if (system == null) return;

            int result = ReflectionUtil.GetIntField(system, "result", -1);
            if (result != -1 && result != _lastStandResultSeen)
            {
                _lastStandResultSeen = result;
                ScreenReader.SayEvent(Loc.Get(result == 0
                    ? "battle_last_stand_won"
                    : "battle_last_stand_lost"), interrupt: true);
            }
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_battle");
        }
    }
}
