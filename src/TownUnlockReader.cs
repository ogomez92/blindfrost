using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Reads the town's unlock buildings: the Tribe Hall, the Pet House, the
    /// Inventor's Hut, the companion hut and the Icebreaker Hut.
    ///
    /// They are the same building in different clothes — a row of things this
    /// save has earned, a row it has not, and a challenge display for whatever
    /// comes next. Almost none of that is text: the tribe banners carry no
    /// label at all (only a flag sprite), the huts put card art in slots, and
    /// the challenge progress sits off in its own corner. Reading every visible
    /// string, which is what the generic overlay branch does, therefore
    /// produced a bare "enemies. 51/100" with nothing to attach it to, and the
    /// banners could only be announced as "Tribe banner".
    ///
    /// This turns each building into a list of named entries with a lock state,
    /// plus the line that says what is still to earn.
    /// </summary>
    public static class TownUnlockReader
    {
        /// <summary>One earnable thing in a town building: a tribe, a pet, an
        /// item, a companion, a map event.</summary>
        public sealed class Entry
        {
            public string Label;
            public bool Unlocked;
            public ClassData Tribe;   // Tribe Hall only
            public Entity Card;       // card huts only
        }

        // ---- Tribe Hall ------------------------------------------------------

        /// <summary>
        /// The hall's banners as tribes, in the order it lays them out.
        /// TribeHutSequence.SetupFlags pairs its flag array with
        /// GameModeNormal.classes by index and treats the first
        /// (1 + checked unlocks) as earned, so the same arithmetic names them.
        /// </summary>
        public static List<Entry> TribeHallEntries(TribeHutSequence hut)
        {
            var entries = new List<Entry>();
            if (hut == null) return entries;

            var flags = ReflectionUtil.GetField<TribeFlagDisplay[]>(hut, "flags");
            if (flags == null) return entries;

            ClassData[] classes = null;
            try { classes = AddressableLoader.Get<GameMode>("GameMode", "GameModeNormal")?.classes; }
            catch (System.Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TownUnlockReader",
                    $"Tribe hall classes unreadable: {ex.Message}");
            }
            if (classes == null) return entries;

            int unlockedCount = 1 + (hut.building?.checkedUnlocks?.Count ?? 0);
            int count = Mathf.Min(flags.Length, classes.Length);
            for (int i = 0; i < count; i++)
            {
                var tribe = classes[i];
                entries.Add(new Entry
                {
                    Label = ItemDescriber.GetTribeName(tribe) ?? Loc.Get("tribe_banner"),
                    Unlocked = i < unlockedCount,
                    Tribe = tribe,
                });
            }
            return entries;
        }

        // ---- Pet House / Inventor's Hut / companion hut -----------------------

        /// <summary>
        /// One entry per slot in a card hut, in reading order: the card's name
        /// where a slot holds one, "locked" where it is still shut. The slots
        /// are the building's own CardContainers, so an empty one is exactly
        /// the door the game is still keeping closed.
        /// </summary>
        public static List<Entry> CardHutEntries(BuildingDisplay overlay)
        {
            var entries = new List<Entry>();
            if (overlay == null) return entries;

            var slots = new List<CardContainer>(
                overlay.GetComponentsInChildren<CardContainer>(includeInactive: false));
            slots.Sort(ByReadingOrder);

            foreach (var slot in slots)
            {
                if (slot == null) continue;

                Entity card = slot.Empty ? null : slot.GetTop();
                string title = card?.data?.title;
                entries.Add(new Entry
                {
                    Label = string.IsNullOrEmpty(title) ? Loc.Get("unlock_slot_locked") : title,
                    Unlocked = !string.IsNullOrEmpty(title),
                    Card = card,
                });
            }
            return entries;
        }

        // ---- Icebreaker Hut ---------------------------------------------------

        /// <summary>
        /// The hut's map-node buttons as event types. IcebreakerHutSequence
        /// fills the first (checked unlocks) of them from the metaprogression
        /// "events" list and leaves the rest un-interactable.
        /// </summary>
        public static List<Entry> IcebreakerEntries(IcebreakerHutSequence hut)
        {
            var entries = new List<Entry>();
            if (hut == null) return entries;

            List<string> events = null;
            string key = ReflectionUtil.GetField<string>(hut, "metaprogressionKey") ?? "events";
            try { events = MetaprogressionSystem.Get<List<string>>(key); }
            catch (System.Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TownUnlockReader",
                    $"Icebreaker event list unreadable: {ex.Message}");
            }
            if (events == null) return entries;

            int unlockedCount = hut.building?.checkedUnlocks?.Count ?? 0;
            for (int i = 0; i < events.Count; i++)
            {
                string asset = events[i];
                entries.Add(new Entry
                {
                    Label = Loc.TryGet("event_node_" + asset, out string named)
                        ? named
                        : ScreenHandler.CleanName(asset),
                    Unlocked = i < unlockedCount,
                });
            }
            return entries;
        }

        // ---- Shared -----------------------------------------------------------

        /// <summary>"2 of 7 unlocked: Wolfie, Berry Bunny." Names only what is
        /// earned — the locked entries are browsed one at a time.</summary>
        public static string UnlockedLine(List<Entry> entries, string key)
        {
            if (entries == null || entries.Count == 0) return null;

            var names = new List<string>();
            foreach (var entry in entries)
                if (entry.Unlocked) names.Add(entry.Label);

            return names.Count == 0
                ? Loc.Get(key + "_none", entries.Count)
                : Loc.Get(key, names.Count, entries.Count, string.Join(", ", names));
        }

        /// <summary>
        /// What the building is asking for next, from its challenge displays:
        /// the reward it grants, the condition, and how far along it is
        /// ("Next unlock, Havok: win 3 battles in a row, 1 of 3"). Null when
        /// nothing is left to earn here. A building shows one display per
        /// challenge it currently offers, so all of them are read.
        /// </summary>
        public static string NextUnlockLine(BuildingDisplay overlay, string introKey)
        {
            if (overlay == null) return null;

            var lines = new List<string>();
            foreach (var display in overlay.GetComponentsInChildren<ChallengeProgressDisplay>(
                         includeInactive: false))
            {
                string line = DescribeChallengeDisplay(display);
                if (!string.IsNullOrEmpty(line) && !lines.Contains(line))
                    lines.Add(line);
            }
            if (lines.Count == 0) return null;

            return Loc.Get(introKey) + " " + string.Join(". ", lines);
        }

        /// <summary>A challenge display as "reward: condition, progress". The
        /// reward name lives on the ChallengeData the display was assigned, which
        /// the game keeps private.</summary>
        private static string DescribeChallengeDisplay(ChallengeProgressDisplay display)
        {
            string progress = ItemDescriber.DescribeChallengeProgress(display);
            if (string.IsNullOrEmpty(progress))
                return null;

            var challenge = ReflectionUtil.GetField<ChallengeData>(display, "challengeData");
            if (challenge == null || challenge.hidden)
                return progress;

            string reward = null;
            try { reward = TextProcessor.ProcessRawText(challenge.titleKey.GetLocalizedString()); }
            catch { /* localization not ready */ }

            return string.IsNullOrEmpty(reward) ? progress : reward.Trim() + ": " + progress;
        }

        /// <summary>A browsed entry as "Snowdwellers, unlocked, 1 of 3".</summary>
        public static string DescribeEntry(Entry entry, int index, int total)
        {
            string state = Loc.Get(entry.Unlocked ? "unlock_state_unlocked" : "unlock_state_locked");
            return Loc.Get("overlay_item", entry.Label + ", " + state, index + 1, total);
        }

        /// <summary>Top row first, then left to right — how a sighted player
        /// reads the grid.</summary>
        private static int ByReadingOrder(Component a, Component b)
        {
            Vector3 pa = a.transform.position, pb = b.transform.position;
            return Mathf.Abs(pa.y - pb.y) > 0.05f
                ? pb.y.CompareTo(pa.y)
                : pa.x.CompareTo(pb.x);
        }
    }
}
