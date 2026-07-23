using HarmonyLib;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Voices the charm-gain popup (GainCharmSequence): the charm block event,
    /// the charm shop and the clunk shop all end in the same silent popup — a
    /// charm graphic, a hover description that is never read, and an Assign
    /// button. The Harmony prefix below announces the charm (name, kind and
    /// full effect text, recorded as a replayable event) the moment the
    /// sequence starts; while the popup is up this owns the keys: Enter
    /// presses Assign (the deckpack then auto-picks the charm up, see
    /// DeckpackNavigator), Escape ends the popup keeping the charm in the
    /// deck pack, arrows and I re-read the charm.
    /// </summary>
    public static class CharmGainNarrator
    {
        private static GainCharmSequence _sequence;
        private static CardUpgradeData _charm;
        private static bool _reserved;

        /// <summary>Forget the sequence on screen changes — no announcements.</summary>
        public static void Reset()
        {
            _sequence = null;
            _charm = null;
            _reserved = false;
        }

        /// <summary>
        /// A GainCharmSequence coroutine was started. The charm is added to the
        /// inventory right away, so this is the "you got it" moment even though
        /// the popup itself tweens in a couple of seconds later.
        /// </summary>
        internal static void OnSequenceStarted(GainCharmSequence sequence)
        {
            _sequence = sequence;
            _charm = ReflectionUtil.GetField<CardUpgradeData>(sequence, "charmData");
            _reserved = false;
            if (_charm == null)
                return;

            string msg = Loc.Get("charm_gained", ItemDescriber.DescribeUpgradeData(_charm))
                + " " + Loc.Get("charm_gained_hint");
            ScreenReader.SayEvent(msg, interrupt: true);
            // The game will focus the Assign button when the popup shows — keep
            // that from talking over the charm read; the button is described
            // with the charm's name anyway if focus is revisited later.
            (ScreenManager.ActiveHandler as NavigableScreenHandler)?.SuppressFocusFor(5f);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "CharmGain",
                $"Charm gained: {_charm.name}");
        }

        /// <summary>
        /// True while the popup is on screen waiting for the player's choice.
        /// Once Assign is pressed the deckpack opens (and owns the keys); once
        /// the sequence ends its object deactivates.
        /// </summary>
        public static bool PopupActive
        {
            get
            {
                try
                {
                    return _sequence != null && _charm != null && !_reserved
                        && _sequence.gameObject.activeInHierarchy
                        && !DeckpackNavigator.IsOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Focus read for the popup's Assign button: name the charm and both options.</summary>
        public static string DescribeFocused()
        {
            if (!PopupActive)
                return null;
            return Loc.Get("charm_assign_button", CharmTitle());
        }

        /// <summary>
        /// Key handling while the popup is up. Returns true when it owns the
        /// keys — the event screen's own input must stay out of the way then.
        /// </summary>
        public static bool RouteInput(NavigableScreenHandler owner)
        {
            if (!PopupActive)
                return false;

            // Escape: end the popup without opening the deckpack. The charm was
            // already added to the inventory when the sequence began, so it
            // simply stays in the deck pack for later.
            if (NavigationHelper.IsBackPressed())
            {
                _reserved = true;
                DebugLogger.LogInput("CharmGain", "Reserve for later");
                try { _sequence.End(); } catch { }
                ScreenReader.SayEvent(Loc.Get("charm_reserved", CharmTitle()), interrupt: true);
                owner?.SuppressFocusFor(2f);
                return true;
            }

            // Enter: press the Assign button (the game opens the deckpack; the
            // navigator then auto-picks this charm up for placement)
            if (NavigationHelper.IsConfirmPressed())
            {
                DebugLogger.LogInput("CharmGain", "Assign now");
                NavigationHelper.ActivateCurrent();
                return true;
            }

            // Arrows or I: the popup is a single item — re-read the charm
            if (NavigationHelper.GetNavigationInput() != NavDirection.None
                || Input.GetKeyDown(KeyCode.I))
            {
                ScreenReader.Say(ItemDescriber.DescribeUpgradeData(_charm), interrupt: true);
                return true;
            }

            // Everything else (P and friends) is swallowed while the popup blocks
            return true;
        }

        /// <summary>
        /// The charm to auto-pick-up when the deckpack opens out of this popup.
        /// Consumed on first call; only served while the popup object is still
        /// live so a later manual open doesn't re-trigger the pickup.
        /// </summary>
        public static CardUpgradeData TakePendingCharm()
        {
            if (_charm == null || _reserved)
                return null;
            try
            {
                if (_sequence == null || !_sequence.gameObject.activeInHierarchy)
                    return null;
            }
            catch
            {
                return null;
            }
            var charm = _charm;
            _charm = null;
            return charm;
        }

        private static string CharmTitle()
        {
            string title = null;
            try { title = _charm.title; }
            catch { }
            return string.IsNullOrEmpty(title)
                ? ScreenHandler.CleanName(_charm.name)
                : title;
        }
    }

    [HarmonyPatch(typeof(GainCharmSequence), nameof(GainCharmSequence.Run))]
    internal static class GainCharmSequenceStartPatch
    {
        private static void Prefix(GainCharmSequence __instance)
        {
            try { CharmGainNarrator.OnSequenceStarted(__instance); }
            catch { /* narration must never break the sequence */ }
        }
    }
}
