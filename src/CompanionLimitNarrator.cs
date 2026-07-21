using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Voices the too-many-companions screen (CompanionLimitSequence), which
    /// opens over the map when the party outgrows the companion limit. The
    /// screen is two card rows — Active and Reserve — where pressing a card
    /// toggles which row it sits in, and every part of that was silent: the
    /// screen opened without a word, presses moved cards with no feedback
    /// (Enter felt dead), and focus reads didn't say which row a companion
    /// was in. Announces the opening (roster, counts, and what Enter does),
    /// each move with the updated active count, and supplies the row suffix
    /// for focused cards via DescribeRow.
    /// </summary>
    public static class CompanionLimitNarrator
    {
        private static CompanionLimitSequence _active;
        private static float _openTime;
        private static bool _wasOver;

        internal static void OnOpened(CompanionLimitSequence sequence)
        {
            _active = sequence;
            _openTime = Time.unscaledTime;

            int limit = 0;
            var activeNames = new List<string>();
            var reserveNames = new List<string>();
            try
            {
                limit = sequence.owner.data.companionLimit;
                CollectCompanions(sequence.owner.data.inventory.deck, activeNames);
                CollectCompanions(sequence.owner.data.inventory.reserve, reserveNames);
            }
            catch
            {
                // Inventory not readable — announce the screen without counts
            }
            _wasOver = activeNames.Count > limit;

            var parts = new List<string>
            {
                Loc.Get("companion_limit_open", activeNames.Count, limit)
            };
            if (activeNames.Count > 0)
                parts.Add(Loc.Get("companion_limit_active_list", string.Join(", ", activeNames)));
            if (reserveNames.Count > 0)
                parts.Add(Loc.Get("companion_limit_reserve_list", string.Join(", ", reserveNames)));

            string msg = string.Join(" ", parts);
            ScreenReader.SayEvent(msg, interrupt: true);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "CompanionLimit", $"Opened: {msg}");
        }

        /// <summary>The screen's card lists hold the leader too — only Friendly
        /// cards (companions) appear in its rows.</summary>
        private static void CollectCompanions(IEnumerable<CardData> cards, List<string> names)
        {
            if (cards == null) return;
            foreach (CardData card in cards)
            {
                if (card?.cardType == null || card.cardType.name != "Friendly")
                    continue;
                string title;
                try { title = card.title; }
                catch { title = null; }
                names.Add(string.IsNullOrEmpty(title)
                    ? ScreenHandler.CleanName(card.name)
                    : title);
            }
        }

        internal static void OnMoved(CompanionLimitSequence sequence, Entity entity, bool toActive)
        {
            if (sequence == null || entity?.data == null)
                return;

            int count, limit;
            try
            {
                count = sequence.activeContainer.Count;
                limit = sequence.owner.data.companionLimit;
            }
            catch
            {
                return;
            }

            string msg = Loc.Get(
                toActive ? "companion_limit_moved_active" : "companion_limit_moved_reserve",
                entity.data.title, count, limit);

            bool over = count > limit;
            if (over)
                msg += " " + Loc.Get("companion_limit_over");
            else if (_wasOver)
                msg += " " + Loc.Get("companion_limit_can_continue");
            _wasOver = over;

            // A press right at open is the game's own doing (default focus plus
            // a leaked submit can move a card before the player touches
            // anything) — queue it behind the opening announcement instead of
            // cutting that off. Later presses are the player's; answer at once.
            bool interrupt = Time.unscaledTime - _openTime > 1.5f;
            ScreenReader.SayEvent(msg, interrupt);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "CompanionLimit", $"Moved: {msg}");
        }

        /// <summary>
        /// "active row" / "reserve row" for a card sitting in the running
        /// screen's rows, spliced into its focus read. Null anywhere else.
        /// </summary>
        public static string DescribeRow(Entity entity)
        {
            var sequence = _active;
            if (sequence == null || entity == null || !sequence.IsRunning)
                return null;
            try
            {
                if (entity.InContainerGroup(sequence.activeContainer))
                    return Loc.Get("companion_limit_row_active");
                if (entity.InContainerGroup(sequence.reserveContainer))
                    return Loc.Get("companion_limit_row_reserve");
            }
            catch
            {
                // Containers mid-teardown
            }
            return null;
        }
    }

    /// <summary>Run() is called once per opening (UISequence.Begin starts the
    /// coroutine), so its stub's prefix is the moment the screen appears.</summary>
    [HarmonyPatch(typeof(CompanionLimitSequence), "Run")]
    internal static class CompanionLimitOpenPatch
    {
        private static void Prefix(CompanionLimitSequence __instance)
        {
            CompanionLimitNarrator.OnOpened(__instance);
        }
    }

    [HarmonyPatch(typeof(CompanionLimitSequence), nameof(CompanionLimitSequence.MoveToDeck))]
    internal static class CompanionLimitMoveToDeckPatch
    {
        private static void Postfix(CompanionLimitSequence __instance, Entity entity)
        {
            CompanionLimitNarrator.OnMoved(__instance, entity, toActive: true);
        }
    }

    [HarmonyPatch(typeof(CompanionLimitSequence), nameof(CompanionLimitSequence.MoveToReserve))]
    internal static class CompanionLimitMoveToReservePatch
    {
        private static void Postfix(CompanionLimitSequence __instance, Entity entity)
        {
            CompanionLimitNarrator.OnMoved(__instance, entity, toActive: false);
        }
    }
}
