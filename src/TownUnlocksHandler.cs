using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the TownUnlocks overlay (entering Town with pending
    /// unlocks): each unlock shows a panel with a title and description that
    /// exist as TMP fields but were never read. The panel closes on a screen
    /// click in vanilla, so Enter (or Escape) calls the panel's own Close();
    /// the sequence then advances to the next unlock or unloads itself.
    /// </summary>
    public class TownUnlocksHandler : NavigableScreenHandler
    {
        public override string Name => "TownUnlocks";

        private GainUnlockSequence _panel;
        private bool _panelWasActive;
        private float _nextPoll;

        public override void OnEnter()
        {
            base.OnEnter();
            _panel = null;
            _panelWasActive = false;
            _nextPoll = 0f;
        }

        public override string GetHelpText() => Loc.Get("help_overlay_continue");

        protected override bool TryAnnounceScreen()
        {
            ScreenReader.SayEvent(Loc.Get("scene_TownUnlocks"), interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            AnnouncePanel();
        }

        /// <summary>Read each unlock panel as it appears.</summary>
        private void AnnouncePanel()
        {
            if (Time.unscaledTime < _nextPoll)
                return;
            _nextPoll = Time.unscaledTime + 0.2f;

            if (_panel == null)
            {
                var found = Object.FindObjectOfType<GainUnlockSequence>(true);
                if (found == null)
                    return;
                _panel = found;
            }

            bool active = _panel.gameObject.activeSelf;
            if (active && !_panelWasActive)
            {
                string title = ReadTmp(_panel.titleElement);
                string desc = ReadTmp(_panel.descriptionElement);
                string text = string.IsNullOrEmpty(desc) ? title : $"{title}. {desc}";
                if (!string.IsNullOrEmpty(text))
                    ScreenReader.SayEvent(
                        Loc.Get("townunlock_gained", text) + " "
                        + Loc.Get("overlay_continue_hint"));
            }
            _panelWasActive = active;
        }

        private static string ReadTmp(TMP_Text tmp)
        {
            if (tmp == null || string.IsNullOrWhiteSpace(tmp.text))
                return null;
            return TextProcessor.StripRichText(tmp.text)?.Replace("\n", ", ").Trim();
        }

        protected override void HandleInput()
        {
            if (NavigationHelper.IsConfirmPressed() || NavigationHelper.IsBackPressed())
            {
                if (_panel != null && _panel.gameObject.activeSelf)
                {
                    DebugLogger.LogInput(Name, "Close unlock panel");
                    _panel.Close();
                }
                return;
            }
            base.HandleInput();
        }
    }
}
