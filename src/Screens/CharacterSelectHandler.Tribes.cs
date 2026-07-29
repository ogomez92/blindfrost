using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The tribe stage alone: the TribeEntry model over SelectTribe's flags, the
    /// opening-focus correction onto the first playable tribe, the remapped arrows
    /// (up/down between tribes, left/right read the roster), the flag's detail
    /// parts, and the Enter that refuses a tribe this save has not unlocked. The
    /// screen-wide dispatch that routes here lives in CharacterSelectHandler.cs.
    /// </summary>
    public partial class CharacterSelectHandler
    {
        // ---- Tribe-stage navigation ------------------------------------------
        // On the "Select a Tribe" stage the flags are the only tribe content, and
        // the game's spatial navigation lets up/down wander off them. Here the
        // arrows are remapped: up/down move between the tribes (and the back
        // button, as a final stop), left/right read the focused tribe's roster
        // (also in the Details buffer on Ctrl+Up). Escape and the back button run
        // the screen's own Back().

        private sealed class TribeEntry
        {
            public TribeFlagDisplay Flag;   // null on the back-button entry
            public ClassData Tribe;         // null on the back-button entry
            public UINavigationItem Nav;
            public bool Locked;             // tribe this save has not earned
        }

        /// <summary>
        /// The tribe flags (with their tribe and nav item, in the game's flag
        /// order) followed by the back button. SelectTribe keeps its flags and
        /// tribes as lock-step lists.
        /// </summary>
        private List<TribeEntry> GetTribeEntries()
        {
            var entries = new List<TribeEntry>();
            var select = TribeSelection();
            if (select == null) return entries;

            var flags = ReflectionUtil.GetField<List<TribeFlagDisplay>>(select, "flags");
            var tribes = ReflectionUtil.GetField<List<ClassData>>(select, "tribes");
            if (flags == null || tribes == null) return entries;

            int count = Mathf.Min(flags.Count, tribes.Count);
            for (int i = 0; i < count; i++)
            {
                var flag = flags[i];
                if (flag == null || !flag.gameObject.activeInHierarchy) continue;

                var nav = flag.GetComponentInChildren<UINavigationItem>(true);
                if (nav == null) continue;

                entries.Add(new TribeEntry
                {
                    Flag = flag,
                    Tribe = tribes[i],
                    Nav = nav,
                    Locked = ItemDescriber.IsTribeLocked(tribes[i]),
                });
            }

            // The back button as a final up/down stop, so it stays reachable and
            // Enter on it goes back (only when the game offers a back button).
            if (entries.Count > 0)
            {
                var backNav = GetBackButtonNav();
                if (backNav != null)
                    entries.Add(new TribeEntry { Flag = null, Tribe = null, Nav = backNav });
            }
            return entries;
        }

        /// <summary>The scene's SelectTribe, cached — GetTribeEntries runs every
        /// frame now that the tribe stage owns its own opening focus.</summary>
        private SelectTribe TribeSelection()
        {
            if (_tribeSelection == null)
                _tribeSelection = Object.FindObjectOfType<SelectTribe>();
            return _tribeSelection;
        }

        /// <summary>True when any flag on screen belongs to a tribe this save has
        /// not unlocked.</summary>
        private bool AnyTribeLocked()
        {
            foreach (var entry in GetTribeEntries())
                if (entry.Locked) return true;
            return false;
        }

        /// <summary>The first tribe the player may actually choose, or null when
        /// the list holds nothing but locked tribes and the back button.</summary>
        private static TribeEntry FirstPlayableEntry(List<TribeEntry> entries)
        {
            foreach (var entry in entries)
                if (entry.Tribe != null && !entry.Locked) return entry;
            return null;
        }

        /// <summary>True (with the entries) while the tribe-choice stage is the one
        /// being navigated — not the leader or pet stage.</summary>
        private bool TribeNavActive(out List<TribeEntry> entries)
        {
            entries = GetTribeEntries();
            if (entries.Count == 0) return false;
            if (_leaderSelection != null && _leaderSelection.running) return false;
            if (_petSelection != null && _petSelection.running) return false;
            return true;
        }

        private static int CurrentTribeIndex(List<TribeEntry> entries, UINavigationItem current)
        {
            if (current == null) return -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Nav == current) return i;
                if (entries[i].Flag != null
                    && current.GetComponentInParent<TribeFlagDisplay>() == entries[i].Flag)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Put the opening focus of the tribe stage on the first tribe the
        /// player can actually pick.
        ///
        /// The game's UINavigationDefaultSystem chooses the flag nearest the 3d
        /// cursor, which parks in the middle of the row — so focus opened on the
        /// middle tribe, and because the game leaves unearned tribes on screen
        /// (see the tribe-lock notes in ItemDescriber) that was often a tribe
        /// the player has not unlocked. The correction is delayed until the
        /// flags settle, and repeats while the default system keeps snatching
        /// focus back onto a locked flag — but only until the player's first
        /// arrow press, after which browsing locked tribes is their choice.
        /// </summary>
        private void UpdateTribeStageFocus()
        {
            if (!TribeNavActive(out var entries))
            {
                _tribeStageWasActive = false;
                _tribeFocusPending = false;
                return;
            }

            if (!_tribeStageWasActive)
            {
                // Stage opened (first entry, or returning here from the leaders)
                _tribeStageWasActive = true;
                _tribeStageSince = Time.unscaledTime;
                _tribeFocusPending = true;
                _tribeNavUsed = false;
                // Don't read out the game's pick just to correct it a moment later
                SuppressFocusFor(TribeFocusDelay + 0.05f);
            }

            if (_tribeNavUsed) return;
            if (Time.unscaledTime - _tribeStageSince < TribeFocusDelay) return;

            var target = FirstPlayableEntry(entries);
            if (target == null)
            {
                _tribeFocusPending = false; // nothing playable to land on
                return;
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            int index = CurrentTribeIndex(entries, current);
            bool strayed = index < 0 || entries[index].Locked;
            if (!_tribeFocusPending && !strayed) return;

            _tribeFocusPending = false;
            if (current == target.Nav) return;

            NavigationHelper.FocusItem(target.Nav);
            ResetFocusTracking(); // we moved it deliberately — announce it
            DebugLogger.Log(DebugLogger.LogCategory.Handler, Name,
                $"Opening tribe focus set to {target.Tribe?.name}");
        }

        private void NavigateTribes(List<TribeEntry> entries, NavDirection dir)
        {
            _tribeNavUsed = true;
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            int index = CurrentTribeIndex(entries, navSystem?.currentNavigationItem);

            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                int next;
                if (index < 0)
                    next = dir == NavDirection.Down ? 0 : entries.Count - 1;
                else
                {
                    next = index + (dir == NavDirection.Down ? 1 : -1);
                    if (next >= entries.Count) next = 0;
                    if (next < 0) next = entries.Count - 1;
                }
                NavigationHelper.FocusItem(entries[next].Nav);
            }
            else // Left or Right: read the focused tribe's roster (nothing on back)
            {
                var tribe = entries[index < 0 ? 0 : index].Tribe;
                if (tribe != null)
                    SpeakTribeRoster(tribe);
            }
        }

        private void SpeakTribeRoster(ClassData tribe)
        {
            string text = ItemDescriber.DescribeTribeRoster(tribe);
            ScreenReader.Say(
                !string.IsNullOrEmpty(text) ? text : Loc.Get("tribe_no_roster"),
                interrupt: true);
        }

        public override List<string> GetFocusedDetailParts(UINavigationItem item)
        {
            if (item != null)
            {
                var flag = item.GetComponentInParent<TribeFlagDisplay>();
                if (flag == null && item.clickHandler != null)
                    flag = item.clickHandler.GetComponentInParent<TribeFlagDisplay>();
                if (flag != null)
                {
                    var parts = ItemDescriber.BuildTribeDetailParts(flag);
                    if (parts != null && parts.Count > 0)
                        return parts;
                }
            }
            return base.GetFocusedDetailParts(item);
        }

        protected override void Confirm()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            var backNav = GetBackButtonNav();
            if (backNav != null && current == backNav && _screen != null)
            {
                DebugLogger.LogInput(Name, "Back (Enter on back button)");
                _screen.Back();
                return;
            }

            // A locked tribe is still fully pressable — the game's own filter
            // never removed it (see the tribe-lock notes in ItemDescriber), and
            // SelectTribe.Run cleared the padlock on every flag it was handed.
            // Refuse it here and say what it takes to earn it instead of
            // starting a run with a tribe this save never unlocked.
            if (TribeNavActive(out var entries))
            {
                int index = CurrentTribeIndex(entries, current);
                if (index >= 0 && entries[index].Locked)
                {
                    var tribe = entries[index].Tribe;
                    DebugLogger.LogInput(Name, $"Blocked locked tribe: {tribe?.name}");
                    ScreenReader.Say(
                        Loc.Get("tribe_locked_blocked",
                            ItemDescriber.GetTribeName(tribe),
                            ItemDescriber.GetTribeLockReason(tribe)),
                        interrupt: true);
                    return;
                }
            }

            base.Confirm();
        }
    }
}
