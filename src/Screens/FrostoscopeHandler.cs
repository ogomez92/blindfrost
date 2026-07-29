using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the NewFrostGuardian overlay: the Frostoscope preview of
    /// the NEXT run's final boss, shown after a victory. In vanilla the whole
    /// meaning ("a new guardian awaits") is conveyed by the scene simply
    /// appearing, plus card art the player pans across with the mouse. This
    /// handler says what the scene is, reads the revealed boss roster, and
    /// exits through the sequence's own End() on Enter or Escape.
    /// </summary>
    public class FrostoscopeHandler : NavigableScreenHandler
    {
        public override string Name => "NewFrostGuardian";

        private FrostoscopeSequence _sequence;
        private bool _rosterSpoken;
        private float _nextPoll;

        public override void OnEnter()
        {
            base.OnEnter();
            _sequence = null;
            _rosterSpoken = false;
            _nextPoll = 0f;
        }

        public override string GetHelpText() => Loc.Get("help_overlay_continue");

        protected override bool TryAnnounceScreen()
        {
            ScreenReader.SayEvent(
                Loc.Get("scene_NewFrostGuardian") + " " + Loc.Get("overlay_continue_hint"),
                interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // The cards flip up over a couple of seconds — read the roster once
            // they exist (or say the scope is empty).
            if (_rosterSpoken || Time.unscaledTime < _nextPoll)
                return;
            _nextPoll = Time.unscaledTime + 0.5f;

            if (_sequence == null)
            {
                _sequence = Object.FindObjectOfType<FrostoscopeSequence>();
                if (_sequence == null)
                    return;
            }

            var nothing = ReflectionUtil.GetField<GameObject>(_sequence, "nothingHere");
            if (nothing != null && nothing.activeInHierarchy)
            {
                _rosterSpoken = true;
                ScreenReader.SayEvent(Loc.Get("frostoscope_nothing"));
                return;
            }

            string roster = ReadRoster();
            if (roster != null)
            {
                _rosterSpoken = true;
                ScreenReader.SayEvent(Loc.Get("frostoscope_roster", roster));
            }
        }

        /// <summary>The boss and escort units on the scope, boss first.</summary>
        private string ReadRoster()
        {
            var names = new List<string>();
            AddContainer(names, ReflectionUtil.GetField<CardContainer>(_sequence, "leaderCardHolder"));
            var holders = ReflectionUtil.GetField<CardContainer[]>(_sequence, "cardHolders");
            if (holders != null)
                foreach (var holder in holders)
                    AddContainer(names, holder);
            return names.Count > 0 ? string.Join(", ", names) : null;
        }

        private static void AddContainer(List<string> names, CardContainer container)
        {
            if (container == null)
                return;
            foreach (Entity entity in container)
            {
                string summary = ItemDescriber.SummarizeEntity(entity);
                if (string.IsNullOrEmpty(summary))
                    summary = entity?.data?.title;
                if (!string.IsNullOrEmpty(summary))
                    names.Add(summary);
            }
        }

        private float _endedAt;

        protected override void HandleInput()
        {
            if (NavigationHelper.IsConfirmPressed() || NavigationHelper.IsBackPressed())
            {
                // If End() didn't unload (scene variant without unloadSceneOnEnd),
                // a second press forces the unload — the boss checker waits on it,
                // so a stuck scope would otherwise soft-lock the whole game.
                if (_endedAt > 0f && Time.unscaledTime - _endedAt > 1.5f)
                {
                    DebugLogger.LogInput(Name, "Force-unload NewFrostGuardian");
                    new Routine(SceneManager.Unload("NewFrostGuardian"));
                    return;
                }
                if (_sequence != null)
                {
                    DebugLogger.LogInput(Name, "End Frostoscope");
                    _sequence.End();
                    _endedAt = Time.unscaledTime;
                }
                return;
            }
            base.HandleInput();
        }
    }
}
