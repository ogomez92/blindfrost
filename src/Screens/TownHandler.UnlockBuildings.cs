using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The town's unlock buildings (Tribe Hall, Pet House, Inventor's Hut,
    /// companion hut, Icebreaker Hut): browsing, opening and summarizing entries.
    /// </summary>
    public partial class TownHandler
    {
        // ---- The town's unlock buildings -------------------------------------
        // The Tribe Hall, Pet House, Inventor's Hut, companion hut and Icebreaker
        // Hut are one design in five costumes: things earned, things not yet
        // earned, and a challenge display for what comes next. TownUnlockReader
        // turns each into named entries with a lock state; this drives them.

        /// <summary>
        /// Which of the unlock buildings is open, if any. The Pet House, the
        /// Inventor's Hut and the companion hut all lay their unlocks out as
        /// cards in slots and are read identically.
        /// </summary>
        private static bool IsCardHut(BuildingDisplay overlay)
        {
            return overlay.GetComponentInChildren<PetHutSequence>(includeInactive: false) != null
                || overlay.GetComponentInChildren<InventorHutSequence>(includeInactive: false) != null
                || overlay.GetComponentInChildren<BuildingCardUnlockSequence>(includeInactive: false) != null;
        }

        /// <summary>
        /// Summarize an unlock building once (and on I), then browse its entries
        /// one at a time with the arrows; Enter opens whatever the building has
        /// to open. Returns false when this overlay is not one of them.
        /// </summary>
        private bool HandleUnlockBuildingOverlay(BuildingDisplay overlay)
        {
            var tribeHall = overlay.GetComponentInChildren<TribeHutSequence>(includeInactive: false);
            var icebreaker = tribeHall == null
                ? overlay.GetComponentInChildren<IcebreakerHutSequence>(includeInactive: false)
                : null;
            bool cardHut = tribeHall == null && icebreaker == null && IsCardHut(overlay);
            if (tribeHall == null && icebreaker == null && !cardHut)
                return false;

            // A detail panel is open over the building — a tribe's lore page, an
            // Icebreaker map-node preview. Read that panel alone: sweeping the
            // whole overlay also picked up the unlock challenge sitting behind
            // it, which is how a bare "51 of 100" arrived with nothing attached.
            if (ReadOpenDetail(overlay))
                return true;

            _unlockEntries = tribeHall != null
                ? TownUnlockReader.TribeHallEntries(tribeHall)
                : icebreaker != null
                    ? TownUnlockReader.IcebreakerEntries(icebreaker)
                    : TownUnlockReader.CardHutEntries(overlay);

            // Still building its slots — own the keys, try again next frame
            if (_unlockEntries.Count == 0)
                return true;
            if (_unlockIndex >= _unlockEntries.Count)
                _unlockIndex = _unlockEntries.Count - 1;

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                bool forward = dir == NavDirection.Down || dir == NavDirection.Right;
                _unlockIndex = (_unlockIndex + (forward ? 1 : -1) + _unlockEntries.Count)
                    % _unlockEntries.Count;
                ScreenReader.Say(DescribeUnlockEntry(_unlockIndex), interrupt: true);
                return true;
            }

            if (NavigationHelper.IsConfirmPressed())
            {
                ActivateUnlockEntry(overlay, tribeHall, icebreaker);
                return true;
            }

            // Summarize on entry, and re-read on I.
            bool reRead = Input.GetKeyDown(KeyCode.I) && !NavigationHelper.IsTextInputFocused();
            if (_overlayAnnounced && !reRead)
                return true;

            string hintKey = tribeHall != null ? "tribehall_hint"
                : icebreaker != null ? "icebreaker_hint"
                : "unlockhut_hint";
            string hint = _overlayAnnounced ? null : HintOnce(hintKey);
            _overlayAnnounced = true;
            ScreenReader.SayEvent(
                BuildUnlockSummary(overlay, tribeHall != null, icebreaker != null)
                    + (hint != null ? " " + hint : ""),
                interrupt: true);
            return true;
        }

        /// <summary>
        /// Announce the browsed entry. A card entry reads as the card itself and
        /// takes the game's focus with it: the review buffers (Ctrl+Up) show the
        /// Details of whatever the game has focused, so without that the pet or
        /// item being browsed was invisible to them.
        /// </summary>
        private string DescribeUnlockEntry(int index)
        {
            var entry = _unlockEntries[index];
            if (entry.Card == null)
                return TownUnlockReader.DescribeEntry(entry, index, _unlockEntries.Count);

            var nav = entry.Card.GetComponentInChildren<UINavigationItem>(true);
            if (nav != null)
                NavigationHelper.FocusItem(nav);

            string desc = ItemDescriber.DescribeEntityFocus(entry.Card) ?? entry.Label;
            return desc + " " + Loc.Get("overlay_position", index + 1, _unlockEntries.Count);
        }

        /// <summary>
        /// Enter on the browsed entry: open a tribe's lore page or an Icebreaker
        /// node preview. A locked entry opens nothing — say what it is waiting on
        /// instead of pressing into silence.
        /// </summary>
        private void ActivateUnlockEntry(BuildingDisplay overlay,
            TribeHutSequence tribeHall, IcebreakerHutSequence icebreaker)
        {
            var entry = _unlockEntries[_unlockIndex];

            if (!entry.Unlocked)
            {
                string next = TownUnlockReader.NextUnlockLine(overlay, UnlockIntroKey(tribeHall, icebreaker));
                DebugLogger.LogInput(Name, $"Locked unlock entry: {entry.Label}");
                ScreenReader.Say(
                    Loc.Get("unlock_entry_locked", entry.Label)
                        + (string.IsNullOrEmpty(next) ? "" : " " + next),
                    interrupt: true);
                return;
            }

            _lastOverlayText = null; // whatever opens should read out fresh
            _detailTitle = entry.Label;

            if (tribeHall != null)
            {
                // The banners fire this through a wired InputAction; calling it
                // straight is exact and cannot miss the right tribe.
                var display = overlay.GetComponentInChildren<TribeDisplaySequence>(includeInactive: true);
                if (display != null)
                {
                    DebugLogger.LogInput(Name, $"Open tribe page: {entry.Label}");
                    // Ask for the page by class name first — that cannot open the
                    // wrong tribe. It matches against the panel's own name list
                    // and does nothing at all when the name is absent (a modded
                    // tribe), which the index call then covers.
                    if (entry.Tribe != null)
                        display.Run(entry.Tribe.name);
                    if (!display.gameObject.activeInHierarchy)
                        display.Run(_unlockIndex);
                    return;
                }
            }
            else if (icebreaker != null)
            {
                DebugLogger.LogInput(Name, $"Inspect map event: {entry.Label}");
                // TryInspect indexes a list the hut fills in its own setup
                // coroutine — pressing Enter before that lands would throw
                try { icebreaker.TryInspect(_unlockIndex); }
                catch (System.Exception ex)
                {
                    _detailTitle = null;
                    DebugLogger.Log(DebugLogger.LogCategory.Game, Name,
                        $"Map event inspect failed: {ex.Message}");
                    ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
                }
                return;
            }

            // A card hut has nothing to open — repeat the card instead of
            // leaving Enter silent
            _detailTitle = null;
            ScreenReader.Say(DescribeUnlockEntry(_unlockIndex), interrupt: true);
        }

        /// <summary>Which "what's next" wording fits this building.</summary>
        private static string UnlockIntroKey(TribeHutSequence tribeHall, IcebreakerHutSequence icebreaker)
        {
            if (tribeHall != null) return "tribe_unlock_intro";
            if (icebreaker != null) return "icebreaker_unlock_intro";
            return "unlock_next_intro";
        }

        /// <summary>
        /// Read an open detail panel (a tribe's lore page, a map-node preview)
        /// and nothing else, titled with the entry it was opened from. Returns
        /// true while one is open, so the browse keys stay out of its way.
        /// </summary>
        private bool ReadOpenDetail(BuildingDisplay overlay)
        {
            var detail = ActiveDetail(overlay);
            if (detail == null)
            {
                _detailTitle = null;
                return false;
            }

            bool reRead = Input.GetKeyDown(KeyCode.I) && !NavigationHelper.IsTextInputFocused();
            string text = ReadVisibleText(detail.transform);
            if (string.IsNullOrEmpty(text) || (text == _lastOverlayText && !reRead))
                return true;

            _lastOverlayText = text;
            ScreenReader.SayEvent(
                (string.IsNullOrEmpty(_detailTitle) ? "" : _detailTitle + ". ")
                    + text + " " + Loc.Get("unlock_detail_back"),
                interrupt: true);
            return true;
        }

        /// <summary>Building name, what this save has earned here, and the
        /// challenge standing between it and the next unlock.</summary>
        private string BuildUnlockSummary(BuildingDisplay overlay, bool tribeHall, bool icebreaker)
        {
            var parts = new List<string>();

            string name = OverlayBuildingName(overlay);
            if (!string.IsNullOrEmpty(name))
                parts.Add(name);

            string countKey = tribeHall ? "tribehall_unlocked"
                : icebreaker ? "icebreaker_unlocked"
                : "unlockhut_unlocked";
            string unlocked = TownUnlockReader.UnlockedLine(_unlockEntries, countKey);
            if (!string.IsNullOrEmpty(unlocked))
                parts.Add(unlocked);

            string next = TownUnlockReader.NextUnlockLine(overlay,
                tribeHall ? "tribe_unlock_intro"
                : icebreaker ? "icebreaker_unlock_intro"
                : "unlock_next_intro");
            parts.Add(string.IsNullOrEmpty(next) ? Loc.Get("unlock_all_done") : next);

            return string.Join(". ", parts);
        }

    }
}
