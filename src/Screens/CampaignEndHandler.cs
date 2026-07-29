using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the CampaignEnd screen — the win / defeat / vanquished run
    /// summary shown when a journey ends. The screen is driven by
    /// <c>DefeatSequence</c>, which reveals its panels over several seconds and
    /// only activates the Back To Town / Scores buttons at the very end. The
    /// generic handler read those button labels but never the numbers that are
    /// the whole point of the screen — the run stats, town progress, and the
    /// score breakdown. This handler announces the result on entry, then, once
    /// the sequence has settled, reads a full summary. Every line goes through
    /// SayEvent, so it is also captured in the Events review buffer and can be
    /// replayed with Ctrl+Up. Arrow-key navigation of the buttons is inherited
    /// from the base handler unchanged.
    ///
    /// This part owns the screen's lifecycle: finding the sequence, waiting for
    /// it to settle, and announcing which way the journey ended.
    /// </summary>
    public partial class CampaignEndHandler : NavigableScreenHandler
    {
        public override string Name => "CampaignEnd";

        private DefeatSequence _sequence;
        private bool _summarySpoken;
        private float _nextSequenceSearch;

        /// <summary>
        /// Safety net: if the sequence is missing or wired differently than
        /// expected, read the summary anyway once this long has passed. The
        /// vanilla reveal (title, stats, challenge fill, score count-up) runs
        /// well under this, so the button-layout signal normally fires first.
        /// </summary>
        private const float SummaryTimeout = 14f;

        public override void OnEnter()
        {
            base.OnEnter();
            _sequence = null;
            _summarySpoken = false;
            _nextSequenceSearch = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            _sequence = null;
        }

        protected override bool TryAnnounceScreen()
        {
            ScreenReader.SayEvent(GetResultAnnouncement(), interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Once the sequence has revealed everything (its buttons are the last
            // thing to appear), read the full summary. Queued after the screen
            // title, and recorded so Ctrl+Up can replay it.
            if (!_summarySpoken && SequenceSettled())
            {
                _summarySpoken = true;
                foreach (string line in BuildSummary())
                    if (!string.IsNullOrEmpty(line))
                        ScreenReader.SayEvent(line);
            }
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_campaignend");
        }

        /// <summary>The DefeatSequence driving this screen, found lazily.</summary>
        private DefeatSequence Sequence()
        {
            if (_sequence == null && Time.unscaledTime >= _nextSequenceSearch)
            {
                _nextSequenceSearch = Time.unscaledTime + 0.5f;
                _sequence = Object.FindObjectOfType<DefeatSequence>();
            }
            return _sequence;
        }

        /// <summary>
        /// True once the whole reveal has finished — the buttons layout is the
        /// last thing DefeatSequence.Routine activates. Falls back to a timeout
        /// so the summary is never left unspoken.
        /// </summary>
        private bool SequenceSettled()
        {
            var seq = Sequence();
            if (seq != null && IsLayoutActive(seq, "buttonsLayout"))
                return true;
            return Time.unscaledTime - EnterTime > SummaryTimeout;
        }

        /// <summary>"Journey over. Victory!" — the scene title plus the result word.</summary>
        private string GetResultAnnouncement()
        {
            string journey = Loc.Get("scene_CampaignEnd");
            string result = GetResultWord();
            return string.IsNullOrEmpty(result) ? journey : journey + " " + result;
        }

        /// <summary>
        /// Win, defeat, or vanquished. Prefers the sequence's active title
        /// layout (only that distinguishes vanquished), falling back to the
        /// campaign result flag while the layouts are still animating in.
        /// </summary>
        private string GetResultWord()
        {
            var seq = Sequence();
            if (seq != null)
            {
                if (IsLayoutActive(seq, "vanquishedLayout"))
                    return Loc.Get("campaignend_vanquished");
                if (IsLayoutActive(seq, "winLayout"))
                    return Loc.Get("campaignend_win");
                if (IsLayoutActive(seq, "defeatLayout"))
                    return Loc.Get("campaignend_defeat");
            }

            try
            {
                if (References.Campaign != null)
                    return References.Campaign.result == Campaign.Result.Win
                        ? Loc.Get("campaignend_win")
                        : Loc.Get("campaignend_defeat");
            }
            catch
            {
                // Campaign state may already be torn down — the title is enough
            }
            return null;
        }

        private static bool IsLayoutActive(DefeatSequence seq, string fieldName)
        {
            var go = ReflectionUtil.GetField<GameObject>(seq, fieldName);
            return go != null && go.activeInHierarchy;
        }
    }
}
