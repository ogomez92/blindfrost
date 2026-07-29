using System.Collections.Generic;
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
    public partial class CharacterSelectHandler : NavigableScreenHandler
    {
        public override string Name => "CharacterSelect";

        private CharacterSelectScreen _screen;
        private SelectLeader _leaderSelection;
        private SelectStartingPet _petSelection;
        private SelectTribe _tribeSelection;

        private bool _petWasRunning;
        private bool _leaderWasRunning;
        private bool _startAnnounced;
        private bool _leaderIntroPending;
        private float _leaderIntroSince;

        private bool _tribeStageWasActive;
        private bool _tribeFocusPending;
        private bool _tribeNavUsed;
        private float _tribeStageSince;

        /// <summary>How long the tribe flags get to settle before the mod moves
        /// focus onto the first playable one (the screen line lands first).</summary>
        private const float TribeFocusDelay = 0.7f;

        public override void OnEnter()
        {
            base.OnEnter();
            _screen = null;
            _tribeSelection = null;
            _petWasRunning = false;
            _leaderWasRunning = false;
            _startAnnounced = false;
            _leaderIntroPending = false;
            _tribeStageWasActive = false;
            _tribeFocusPending = false;
            _tribeNavUsed = false;
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
            string msg;
            if (_leaderSelection != null && _leaderSelection.running)
            {
                msg = Loc.Get("charselect_leaders");
                _leaderWasRunning = true;
            }
            else if (TribeStageVisible())
            {
                // Normal mode starts on the tribe choice; without this the
                // player only heard the bare screen name and flag focus reads
                msg = Loc.Get("charselect_tribes");
                // The game shows flags for tribes this save has not unlocked
                // (see the tribe-lock notes in ItemDescriber) — say so up front
                // so "locked" on a flag isn't a surprise
                if (AnyTribeLocked())
                    msg += " " + Loc.Get("charselect_tribes_locked");
            }
            else if (Time.unscaledTime - EnterTime < 3f)
            {
                // Neither stage is up yet — the flags/leaders may still be
                // loading in. Retry instead of latching onto the bare name.
                return false;
            }
            else
            {
                msg = Loc.Get("scene_CharacterSelect");
            }
            ScreenReader.SayEvent(msg, interrupt: true);
            return true;
        }

        /// <summary>Tribe flags on screen mean the tribe stage is active
        /// (SelectTribe has no running flag to ask).</summary>
        private static bool TribeStageVisible()
        {
            foreach (var flag in Object.FindObjectsOfType<TribeFlagDisplay>())
                if (flag != null && flag.gameObject.activeInHierarchy)
                    return true;
            return false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            EnsureRefs();
            if (_screen == null) return;

            UpdateTribeStageFocus();

            // Leader stage started (after picking a tribe). The intro is
            // deferred until the candidates finish generating so it can also
            // read the chosen tribe's starting deck.
            bool leaderRunning = _leaderSelection != null && _leaderSelection.running;
            if (leaderRunning && !_leaderWasRunning)
            {
                _leaderIntroPending = true;
                _leaderIntroSince = Time.unscaledTime;
            }
            _leaderWasRunning = leaderRunning;
            if (!leaderRunning)
                _leaderIntroPending = false;
            else if (_leaderIntroPending)
                TryAnnounceLeaderIntro();

            // Pet stage started (after confirming the leader)
            bool petRunning = _petSelection != null && _petSelection.running;
            if (petRunning && !_petWasRunning)
                ScreenReader.SayEvent(Loc.Get("charselect_pets"), interrupt: true);
            _petWasRunning = petRunning;

            // Final confirm pressed: the run is being created
            if (!_startAnnounced && ReflectionUtil.GetBoolField(_screen, "loadingToCampaign", false))
            {
                _startAnnounced = true;
                ScreenReader.SayEvent(Loc.Get("charselect_starting"), interrupt: true);
            }
        }

        /// <summary>
        /// Announce the leader stage: the intro line plus the starting deck of
        /// the chosen tribe (the cards every run with it begins with). Waits
        /// for GenerateLeaders to finish so the candidates' tribe is known;
        /// falls back to the plain intro if the stage never settles.
        /// </summary>
        private void TryAnnounceLeaderIntro()
        {
            var characters = _leaderSelection.generating ? null : GetLeaderCharacters();
            bool ready = characters != null && characters.Count > 0;
            if (!ready && Time.unscaledTime - _leaderIntroSince < 4f)
                return;

            _leaderIntroPending = false;
            string msg = Loc.Get("charselect_leaders");
            if (ready)
            {
                var tribe = characters[0]?.data?.classData;
                string deck = ItemDescriber.DescribeTribeStartingDeck(tribe);
                if (!string.IsNullOrEmpty(deck))
                    msg += " " + deck;
            }
            ScreenReader.SayEvent(msg, interrupt: true);
        }

        /// <summary>The leader stage's candidate list (SelectLeader.characters,
        /// private). Null before the stage has generated anything.</summary>
        private List<SelectLeader.Character> GetLeaderCharacters()
        {
            return _leaderSelection == null
                ? null
                : ReflectionUtil.GetField<List<SelectLeader.Character>>(
                    _leaderSelection, "characters");
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
