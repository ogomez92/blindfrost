using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The fallback for the screen's exits: arrow keys and Enter driving the
    /// Back To Town / Restart / Scores buttons directly when the game registers
    /// no navigation items for them.
    /// </summary>
    public partial class CampaignEndHandler
    {
        // ---- End buttons fallback ----------------------------------------
        // The Back To Town / Restart / Scores buttons are assumed to be
        // navigation items, but if this screen turns out to be free-cursor
        // driven like BattleWin, arrow keys would find nothing and Enter would
        // dead-end the run. These pseudo-buttons keep an exit reachable.

        private int _buttonIndex = -1;

        private struct EndButton
        {
            public GameObject Target;
            public string Label;
        }

        /// <summary>Active clickable buttons on the end screen with labels.</summary>
        private List<EndButton> CollectEndButtons()
        {
            var results = new List<EndButton>();
            var seq = Sequence();
            if (seq == null)
                return results;

            foreach (string field in new[] { "buttonsLayout", "restartButton", "scoresButton" })
            {
                var root = ReflectionUtil.GetField<GameObject>(seq, field);
                if (root == null || !root.activeInHierarchy)
                    continue;
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    if (button == null || !button.gameObject.activeInHierarchy)
                        continue;
                    if (results.Exists(r => r.Target == button.gameObject))
                        continue;
                    var tmp = button.GetComponentInChildren<TMPro.TMP_Text>();
                    string label = tmp != null && !string.IsNullOrWhiteSpace(tmp.text)
                        ? TextProcessor.StripRichText(tmp.text).Trim()
                        : CleanName(button.gameObject.name);
                    results.Add(new EndButton { Target = button.gameObject, Label = label });
                }
            }
            return results;
        }

        protected override void Navigate(NavDirection dir)
        {
            if (NavigationHelper.GetNavigableItems().Count > 0)
            {
                base.Navigate(dir);
                return;
            }

            var buttons = CollectEndButtons();
            if (buttons.Count == 0)
            {
                base.Navigate(dir);
                return;
            }

            bool forward = dir == NavDirection.Down || dir == NavDirection.Right;
            if (_buttonIndex < 0)
                _buttonIndex = forward ? 0 : buttons.Count - 1;
            else
                _buttonIndex = ((_buttonIndex + (forward ? 1 : -1))
                    % buttons.Count + buttons.Count) % buttons.Count;
            ScreenReader.Say(buttons[_buttonIndex].Label, interrupt: true);
        }

        protected override void Confirm()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem?.currentNavigationItem != null)
            {
                base.Confirm();
                return;
            }

            var buttons = CollectEndButtons();
            if (buttons.Count > 0)
            {
                if (_buttonIndex < 0 || _buttonIndex >= buttons.Count)
                {
                    // Nothing chosen yet: land on the first button and require a
                    // second Enter, so a blind press can't restart the run
                    _buttonIndex = 0;
                    ScreenReader.Say(
                        buttons[0].Label + " " + Loc.Get("campaignend_press_again"),
                        interrupt: true);
                    return;
                }

                var chosen = buttons[_buttonIndex];
                DebugLogger.LogInput(Name, $"Pressing end button: {chosen.Label}");
                NavigationHelper.PressObject(chosen.Target);
                return;
            }

            base.Confirm();
        }
    }
}
