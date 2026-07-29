using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the CardFramesUnlocked overlay (entering Town after a run):
    /// a celebration fan of the cards that earned a chiseled or gold frame.
    /// The banner text exists as a TMP field on the sequence; the exit is a
    /// screen click in vanilla, so Enter (or Escape) calls the sequence's own
    /// End(), which unloads the scene.
    /// </summary>
    public class CardFramesUnlockedHandler : NavigableScreenHandler
    {
        public override string Name => "CardFramesUnlocked";

        private CardFramesUnlockedSequence _sequence;
        private int _retries;
        private float _nextTry;

        public override void OnEnter()
        {
            base.OnEnter();
            _sequence = null;
            _retries = 0;
            _nextTry = 0f;
        }

        public override string GetHelpText() => Loc.Get("help_overlay_continue");

        protected override bool TryAnnounceScreen()
        {
            if (Time.unscaledTime < _nextTry)
                return false;

            if (_sequence == null)
                _sequence = Object.FindObjectOfType<CardFramesUnlockedSequence>();

            string banner = ReadBanner();
            if (banner == null && _retries < 6)
            {
                _retries++;
                _nextTry = Time.unscaledTime + 0.5f;
                return false;
            }

            var parts = new List<string> { Loc.Get("scene_CardFramesUnlocked") };
            if (banner != null)
                parts.Add(banner);
            string cards = ListCards();
            if (cards != null)
                parts.Add(cards);
            parts.Add(Loc.Get("overlay_continue_hint"));
            ScreenReader.SayEvent(string.Join(" ", parts), interrupt: true);
            return true;
        }

        /// <summary>The banner ("2 cards have earned a Gold Frame!"), if set.</summary>
        private string ReadBanner()
        {
            var tmp = ReflectionUtil.GetField<TMP_Text>(_sequence, "text");
            if (tmp == null || string.IsNullOrWhiteSpace(tmp.text))
                return null;
            return TextProcessor.StripRichText(tmp.text)?.Trim();
        }

        /// <summary>Names of the framed cards, from both fan containers.</summary>
        private string ListCards()
        {
            if (_sequence == null)
                return null;
            var names = new List<string>();
            foreach (string field in new[] { "container1", "container2" })
            {
                var container = ReflectionUtil.GetField<CardHand>(_sequence, field);
                if (container == null)
                    continue;
                foreach (Entity entity in container)
                {
                    string title = entity?.data?.title;
                    if (!string.IsNullOrEmpty(title))
                        names.Add(title);
                }
            }
            return names.Count > 0 ? string.Join(", ", names) : null;
        }

        protected override void HandleInput()
        {
            if (NavigationHelper.IsConfirmPressed() || NavigationHelper.IsBackPressed())
            {
                Close();
                return;
            }
            base.HandleInput();
        }

        private void Close()
        {
            if (_sequence == null)
                _sequence = Object.FindObjectOfType<CardFramesUnlockedSequence>();
            if (_sequence != null)
            {
                DebugLogger.LogInput(Name, "End card frames sequence");
                _sequence.End();
            }
        }
    }
}
