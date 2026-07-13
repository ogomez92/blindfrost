using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Character select: tribe, then leader, then optionally a starting pet.
    /// Cards are picked via CardControllerSelectCard (see
    /// NavigationHelper.TryPressSelectCard) and confirmed in the inspect
    /// panel that NavigableScreenHandler drives (Enter/Escape). This screen's
    /// panel confirms through CharacterSelectScreen.Continue() — the base
    /// TakeCard() path has no card selector here — and the handler announces
    /// the stage flow: leaders, pet choice, journey start.
    /// </summary>
    public class CharacterSelectHandler : NavigableScreenHandler
    {
        public override string Name => "CharacterSelect";

        private CharacterSelectScreen _screen;
        private SelectLeader _leaderSelection;
        private SelectStartingPet _petSelection;

        private bool _petWasRunning;
        private bool _startAnnounced;

        public override void OnEnter()
        {
            base.OnEnter();
            _screen = null;
            _petWasRunning = false;
            _startAnnounced = false;
            EnsureRefs();
        }

        /// <summary>
        /// The screen object may not exist yet on the first frames of the
        /// scene — resolve lazily and cache.
        /// </summary>
        private void EnsureRefs()
        {
            if (_screen != null) return;
            _screen = Object.FindObjectOfType<CharacterSelectScreen>();
            if (_screen == null) return;

            _leaderSelection = ReflectionUtil.GetField<SelectLeader>(_screen, "leaderSelection");
            _petSelection = ReflectionUtil.GetField<SelectStartingPet>(_screen, "petSelection");
        }

        protected override bool TryAnnounceScreen()
        {
            EnsureRefs();
            string msg = (_leaderSelection != null && _leaderSelection.running)
                ? Loc.Get("charselect_leaders")
                : Loc.Get("scene_CharacterSelect");
            ScreenReader.Say(msg, interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            EnsureRefs();
            if (_screen == null) return;

            // Pet stage started (after confirming the leader)
            bool petRunning = _petSelection != null && _petSelection.running;
            if (petRunning && !_petWasRunning)
                ScreenReader.Say(Loc.Get("charselect_pets"), interrupt: true);
            _petWasRunning = petRunning;

            // Final confirm pressed: the run is being created
            if (!_startAnnounced && ReflectionUtil.GetBoolField(_screen, "loadingToCampaign", false))
            {
                _startAnnounced = true;
                ScreenReader.Say(Loc.Get("charselect_starting"), interrupt: true);
            }
        }

        /// <summary>
        /// Exactly what the unreachable "Let's Go!" button does here:
        /// next stage (pet choice) or start the run.
        /// </summary>
        protected override void ConfirmInspectPanel(InspectNewUnitSequence panel)
        {
            if (_screen != null)
            {
                DebugLogger.LogInput(Name, "Continue (Let's Go)");
                _screen.Continue();
                return;
            }
            base.ConfirmInspectPanel(panel);
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_charselect");
        }
    }
}
