using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Watches for full-screen blocking sequences that are NOT scenes the
    /// ScreenManager can route (they overlay the active scene without changing
    /// any scene key): the card-combine reveal and the final-boss shade
    /// cinematic. While one runs, this owns the keyboard — Enter answers the
    /// game's polled "press Select" prompt via SelectSimulator, and arrows are
    /// swallowed so they can't drive the invisible screen underneath.
    /// The narration for these moments lives in VisualNarrator (Harmony
    /// prefixes); the prompt text itself is read by CinemaBarReader.
    /// </summary>
    public static class OverlayWatcher
    {
        private static CombineCardSequence _combine;
        private static FinalBossSequenceSystem _finalBoss;
        private static float _nextSearch;

        /// <summary>Drop cached scene objects (mod unload/reload hygiene).</summary>
        public static void Reset()
        {
            _combine = null;
            _finalBoss = null;
            _nextSearch = 0f;
        }

        /// <summary>
        /// True while a blocking overlay owns the keys. Called from the screen
        /// handlers' input chain; when it returns true the handler must not
        /// process navigation or Enter itself.
        /// </summary>
        public static bool RouteInput()
        {
            if (!IsBlockingOverlayActive())
                return false;

            // Enter answers the "press Select to continue" poll. Anything else
            // is swallowed: the underlying screen must not react while the
            // cinematic owns the display.
            if (NavigationHelper.IsConfirmPressed())
                SelectSimulator.Arm();
            return true;
        }

        /// <summary>A combine reveal or the final-boss cinematic is running.</summary>
        private static bool IsBlockingOverlayActive()
        {
            // Cheap gate: both overlays always show the cinema bars.
            bool barsActive;
            try
            {
                barsActive = CinemaBarSystem.Top != null && CinemaBarSystem.IsActive();
            }
            catch
            {
                barsActive = false;
            }
            if (!barsActive)
                return false;

            // Scene-wide searches are throttled; the cached objects stay valid
            // for the lifetime of their scene.
            if (Time.unscaledTime >= _nextSearch)
            {
                _nextSearch = Time.unscaledTime + 1f;
                if (_combine == null)
                    _combine = Object.FindObjectOfType<CombineCardSequence>();
                if (_finalBoss == null)
                    _finalBoss = Object.FindObjectOfType<FinalBossSequenceSystem>();
            }

            if (_combine != null && _combine.gameObject.activeInHierarchy)
                return true;

            if (_finalBoss != null
                && ReflectionUtil.GetBoolField(_finalBoss, "running", fallback: false))
                return true;

            return false;
        }
    }
}
