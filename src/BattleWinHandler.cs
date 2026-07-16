using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the BattleWin overlay (victory splash after a
    /// won battle). Its Continue button has no UINavigationItem — the vanilla
    /// game expects the free-moving controller cursor here — so item-based
    /// navigation finds nothing and Enter has no focus target. This handler
    /// waits for the continue layout to animate in, announces any injuries and
    /// the continue prompt, and presses the button directly on Enter.
    /// </summary>
    public class BattleWinHandler : NavigableScreenHandler
    {
        public override string Name => "BattleWin";

        private BattleVictorySequence _sequence;
        private GameObject _continueLayout;
        private bool _promptSpoken;
        private float _pressedTime;

        public override void OnEnter()
        {
            base.OnEnter();
            _sequence = null;
            _continueLayout = null;
            _promptSpoken = false;
            _pressedTime = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            _sequence = null;
            _continueLayout = null;
        }

        protected override bool TryAnnounceScreen()
        {
            ScreenReader.SayEvent(Loc.Get("scene_BattleWin"), interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Announce injuries and the continue prompt once the button appears
            // (BattleVictorySequence.Run activates continueLayout ~1.5s in,
            // later when injuries are revealed first). Queued, not interrupting,
            // so the scene announcement finishes first.
            if (!_promptSpoken && ContinueReady)
            {
                _promptSpoken = true;
                string injuries = DescribeInjuries();
                string prompt = Loc.Get("battlewin_continue");
                ScreenReader.SayEvent(injuries != null ? injuries + " " + prompt : prompt);
            }

            // Safety net: if the simulated press reached nothing (button wired
            // differently than expected), end the sequence directly rather than
            // leaving the player stuck on this screen.
            if (_pressedTime > 0f && Time.unscaledTime - _pressedTime > 0.6f)
            {
                _pressedTime = 0f;
                if (_sequence != null
                    && ReflectionUtil.GetBoolField(_sequence, "active", fallback: false))
                {
                    DebugLogger.LogInput(Name, "Continue press had no effect; calling End() directly");
                    _sequence.End();
                }
            }
        }

        /// <summary>
        /// True once the continue layout is active. Finds the sequence and its
        /// private continueLayout field on first use.
        /// </summary>
        private bool ContinueReady
        {
            get
            {
                if (_sequence == null)
                {
                    _sequence = Object.FindObjectOfType<BattleVictorySequence>();
                    if (_sequence != null)
                        _continueLayout = ReflectionUtil.GetField<GameObject>(_sequence, "continueLayout");
                }
                return _continueLayout != null && _continueLayout.activeInHierarchy;
            }
        }

        /// <summary>The Continue button is the only control; any arrow lands on it.</summary>
        protected override void Navigate(NavDirection dir)
        {
            if (ContinueReady)
            {
                ScreenReader.Say(Loc.Get("battlewin_continue"), interrupt: true);
                return;
            }
            base.Navigate(dir);
        }

        protected override void Confirm()
        {
            if (!ContinueReady)
            {
                // If the continue layout never shows up (reflection failed, or
                // the sequence is wired unexpectedly), Enter must still lead
                // out — being stuck on the victory screen ends the whole run.
                if (Time.unscaledTime - EnterTime > 8f && _sequence != null)
                {
                    DebugLogger.LogInput(Name, "Continue layout never appeared; calling End() directly");
                    _sequence.End();
                    return;
                }

                // Screen still animating in — say so instead of dead silence
                ScreenReader.Say(Loc.Get("battlewin_not_ready"), interrupt: true);
                return;
            }

            GameObject target = FindContinueClickTarget();
            DebugLogger.LogInput(Name,
                $"Pressing Continue via {(target != null ? target.name : "End() fallback")}");
            if (target != null)
            {
                NavigationHelper.PressObject(target);
                _pressedTime = Time.unscaledTime;
            }
            else
            {
                _sequence.End();
            }
        }

        /// <summary>
        /// Find the clickable object under the continue layout: a uGUI Button
        /// if present, otherwise anything handling pointer down/click.
        /// </summary>
        private GameObject FindContinueClickTarget()
        {
            var button = _continueLayout.GetComponentInChildren<UnityEngine.UI.Button>();
            if (button != null)
                return button.gameObject;

            foreach (var behaviour in _continueLayout.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is IPointerDownHandler || behaviour is IPointerClickHandler)
                    return behaviour.gameObject;
            }
            return null;
        }

        /// <summary>Names of companions injured this battle, or null if none.</summary>
        private string DescribeInjuries()
        {
            try
            {
                CardData[] injuries = InjurySystem.GetInjuriesThisBattle();
                if (injuries == null || injuries.Length == 0)
                    return null;

                var names = new List<string>();
                foreach (CardData card in injuries)
                {
                    if (card != null && !string.IsNullOrEmpty(card.title))
                        names.Add(card.title);
                }
                if (names.Count == 0)
                    return null;

                return Loc.Get("battlewin_injuries", string.Join(", ", names));
            }
            catch
            {
                // Injury lookup touches battle state that may already be torn
                // down — the continue prompt matters more than the injury list
                return null;
            }
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_battlewin");
        }
    }
}
