using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Tribes: flag-to-ClassData lookup, names and playstyle blurbs, unlock state and
    /// lock reasons, starting decks, leaders, and the tribe detail buffer.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>
        /// Describe the tribe a character-select flag stands for: its name plus a
        /// short playstyle blurb ("Snowdwellers. The starting tribe..."). The flag
        /// component holds only a sprite, so the tribe is recovered from
        /// SelectTribe (below); the game exposes no tribe name or description, so
        /// both come from the mod's own strings keyed by the tribe's internal id
        /// ("Basic", "Magic", "Clunk"). Null when the flag can't be matched to a
        /// tribe (e.g. mid-selection animation).
        /// </summary>
        public static string DescribeTribeFlag(TribeFlagDisplay flag)
        {
            return DescribeTribe(FindTribeForFlag(flag));
        }

        /// <summary>
        /// The same read for a tribe reached without a flag: name (marked as
        /// locked when the save has not earned it) plus the playstyle blurb.
        /// </summary>
        public static string DescribeTribe(ClassData tribe)
        {
            if (tribe == null)
                return null;

            string name = GetTribeName(tribe);
            if (string.IsNullOrEmpty(name))
                return null;

            string head = IsTribeLocked(tribe) ? Loc.Get("tribe_locked", name) : name;

            string internalName = tribe.name;
            if (!string.IsNullOrEmpty(internalName)
                && Loc.TryGet("tribe_desc_" + internalName, out string desc)
                && !string.IsNullOrEmpty(desc))
                return head + ". " + desc;

            return head;
        }

        /// <summary>
        /// Map a flag to its ClassData. SelectTribe builds its flags and tribes
        /// as two lists in lock-step (one flag per tribe, same order), so the
        /// flag's index in the flag list is the tribe's index in the tribe list.
        /// </summary>
        private static ClassData FindTribeForFlag(TribeFlagDisplay flag)
        {
            if (flag == null)
                return null;

            var select = Object.FindObjectOfType<SelectTribe>();
            if (select == null)
                return null;

            var flags = ReflectionUtil.GetField<List<TribeFlagDisplay>>(select, "flags");
            var tribes = ReflectionUtil.GetField<List<ClassData>>(select, "tribes");
            if (flags == null || tribes == null)
                return null;

            int index = flags.IndexOf(flag);
            return index >= 0 && index < tribes.Count ? tribes[index] : null;
        }

        /// <summary>
        /// A tribe's player-facing name. The ClassData asset name is an internal
        /// id ("Basic", "Magic", "Clunk"), so it is mapped to the real tribe name
        /// through a "tribe_name_&lt;id&gt;" string; an unmapped (e.g. modded)
        /// tribe falls back to its cleaned-up asset name.
        /// </summary>
        public static string GetTribeName(ClassData tribe)
        {
            if (tribe == null)
                return null;

            string internalName = tribe.name;
            if (!string.IsNullOrEmpty(internalName)
                && Loc.TryGet("tribe_name_" + internalName, out string localized))
                return localized;

            return ScreenHandler.CleanName(internalName);
        }

        // ---- Tribe unlock state ---------------------------------------------
        // The game means locked tribes to be unreachable: CharacterSelectScreen
        // builds the flag list from Campaign.Data.GameMode.classes and removes
        // MetaprogressionSystem.GetLockedClasses() from it first. That removal
        // compares ScriptableObjects (DataFile.Equals is an instance-id test),
        // and the ClassData instances behind References.Classes are NOT the
        // ones serialized into GameMode.classes, so it removes nothing — the
        // log reads "Locked Classes: [Magic, Clunk]" immediately followed by
        // "Available Classes: [Basic, Magic, Clunk]", and SelectTribe.Run then
        // calls SetAvailable()/SetUnlocked() on every flag it is given. The
        // same mismatch makes the town hall show all three banners.
        //
        // Asset names are stable across both instance sets, so matching on the
        // name still tells the truth about what the save has earned. The mod
        // uses that to announce locked tribes as locked and to refuse them on
        // Enter, rather than silently starting a run with a tribe the player
        // never unlocked.

        private static HashSet<string> _lockedTribeNames;
        private static float _lockedTribesRead;

        /// <summary>
        /// Asset names of the tribes this save has not unlocked. Cached for a
        /// few seconds: each rebuild reads the save file (and the unlock set
        /// cannot change while a select screen is open). Empty when the lock
        /// state can't be read — an unknown state must never block a tribe the
        /// player has actually earned.
        /// </summary>
        private static HashSet<string> LockedTribeNames()
        {
            if (_lockedTribeNames != null && Time.unscaledTime - _lockedTribesRead < 5f)
                return _lockedTribeNames;

            var names = new HashSet<string>();
            try
            {
                var locked = MetaprogressionSystem.GetLockedClasses();
                if (locked != null)
                {
                    foreach (ClassData tribe in locked)
                        if (tribe != null && !string.IsNullOrEmpty(tribe.name))
                            names.Add(tribe.name);
                }
            }
            catch (System.Exception ex)
            {
                names.Clear();
                DebugLogger.Log(DebugLogger.LogCategory.Game, "ItemDescriber",
                    $"Tribe lock state unreadable: {ex.Message}");
            }

            _lockedTribeNames = names;
            _lockedTribesRead = Time.unscaledTime;
            return names;
        }

        /// <summary>True when this save has not unlocked the tribe yet.</summary>
        public static bool IsTribeLocked(ClassData tribe)
        {
            if (tribe == null || string.IsNullOrEmpty(tribe.name))
                return false;
            return LockedTribeNames().Contains(tribe.name);
        }

        /// <summary>
        /// How the player earns a locked tribe. Wildfrost grants tribes off the
        /// town progress meter (MetaprogressSequence fills it from battles won
        /// and hands out the next unlock in line), so there is no per-tribe
        /// challenge to quote; the unlock's related building is named when the
        /// data has one.
        /// </summary>
        public static string GetTribeLockReason(ClassData tribe)
        {
            string reason = Loc.Get("tribe_locked_hint");

            try
            {
                var building = tribe?.requiresUnlock?.relatedBuilding;
                string title = building != null
                    ? TextProcessor.ProcessRawText(building.titleKey.GetLocalizedString())
                    : null;
                if (!string.IsNullOrEmpty(title))
                    reason += " " + Loc.Get("tribe_locked_building", title.Trim());
            }
            catch
            {
                // Localization may not be ready — the generic hint stands alone
            }

            return reason;
        }

        /// <summary>
        /// The cards a tribe's runs start with — the whole starting deck, in
        /// deck order, duplicates aggregated ("Scrappy Sword, 3 copies").
        /// </summary>
        public static string DescribeTribeStartingDeck(ClassData tribe)
        {
            try
            {
                var deck = tribe?.startingInventory?.deck;
                if (deck == null) return null;

                var order = new List<string>();
                var counts = new Dictionary<string, int>();
                foreach (CardData card in deck)
                {
                    string title = SafeTitle(card);
                    if (string.IsNullOrEmpty(title)) continue;
                    if (!counts.ContainsKey(title))
                    {
                        counts[title] = 0;
                        order.Add(title);
                    }
                    counts[title]++;
                }
                if (order.Count == 0) return null;

                var parts = new List<string>();
                foreach (string title in order)
                    parts.Add(counts[title] > 1
                        ? Loc.Get("card_count_multiple", title, counts[title])
                        : title);
                return Loc.Get("tribe_starting_deck", string.Join(", ", parts));
            }
            catch { return null; }
        }

        /// <summary>CardData.title, guarded against a localization miss.</summary>
        private static string SafeTitle(CardData card)
        {
            try { return card?.title; }
            catch { return null; }
        }

        /// <summary>
        /// A one-line spoken summary of who a tribe fields: its leaders and its
        /// starting deck. What the right arrow reads on the tribe-select
        /// screen. Null only if a tribe lists neither.
        ///
        /// The companions a tribe can recruit are deliberately left out: the
        /// tribe stage draws nothing but flags, so that roster is information
        /// no sighted player can reach from here, and reading a dozen names
        /// buried the two lines that matter.
        /// </summary>
        public static string DescribeTribeRoster(ClassData tribe)
        {
            if (tribe == null) return null;

            var segments = new List<string>();

            string leaders = DescribeTribeLeaders(tribe);
            if (!string.IsNullOrEmpty(leaders))
                segments.Add(leaders);

            string deck = DescribeTribeStartingDeck(tribe);
            if (!string.IsNullOrEmpty(deck))
                segments.Add(deck);

            return segments.Count > 0 ? string.Join(" ", segments) : null;
        }

        /// <summary>
        /// The leaders a tribe can be played with. Base-game leader cards are
        /// nameless templates — a leader only gets its name and stats when the
        /// leader stage clones it and runs CardScriptLeader — so when no
        /// template carries a real name this says they're randomly generated
        /// instead of parroting the "Leader" placeholder title. Modded tribes
        /// whose leader cards do have names still get them listed.
        /// </summary>
        public static string DescribeTribeLeaders(ClassData tribe)
        {
            try
            {
                var leaders = tribe?.leaders;
                if (leaders == null || leaders.Length == 0) return null;

                var names = new List<string>();
                foreach (var leader in leaders)
                {
                    string title = SafeTitle(leader);
                    if (string.IsNullOrEmpty(title) || IsLeaderPlaceholderTitle(leader, title))
                        continue;
                    if (!names.Contains(title))
                        names.Add(title);
                }
                if (names.Count > 0)
                    return Loc.Get("tribe_leaders", string.Join(", ", names));

                return Loc.Get("tribe_leaders_random");
            }
            catch { return null; }
        }

        /// <summary>A template title that is just the card-type name
        /// ("Leader") rather than a character name.</summary>
        private static bool IsLeaderPlaceholderTitle(CardData leader, string title)
        {
            if (title == "Leader") return true;
            try { return title == leader?.cardType?.title; }
            catch { return false; }
        }

        /// <summary>
        /// Detail-buffer parts for a focused tribe flag (Ctrl+Up steps through
        /// them): the tribe's name and playstyle, its leaders, then the
        /// starting deck. Recruitable companions are left out for the same
        /// reason as in <see cref="DescribeTribeRoster"/>.
        /// </summary>
        public static List<string> BuildTribeDetailParts(TribeFlagDisplay flag)
        {
            var tribe = FindTribeForFlag(flag);
            if (tribe == null) return null;

            var parts = new List<string>();

            string head = DescribeTribeFlag(flag);
            if (!string.IsNullOrEmpty(head))
                parts.Add(head);

            // Why it can't be chosen comes before what's in it — the leaders and
            // deck below are what the player is working towards, not an offer
            if (IsTribeLocked(tribe))
                parts.Add(GetTribeLockReason(tribe));

            string leaders = DescribeTribeLeaders(tribe);
            if (!string.IsNullOrEmpty(leaders))
                parts.Add(leaders);

            string deckLine = DescribeTribeStartingDeck(tribe);
            if (!string.IsNullOrEmpty(deckLine))
                parts.Add(deckLine);

            return parts.Count > 0 ? parts : null;
        }
    }
}
