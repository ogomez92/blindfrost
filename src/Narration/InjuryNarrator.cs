using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Voices the two screens injuries put in front of the player, both of
    /// which arrived as a bare button with nothing to explain it.
    ///
    /// The recovery panel opens over the map when injured companions heal. It
    /// carries a title and the healed cards, none of which is a navigable item
    /// — the map handler finds only the "Nice!" button and reads that, so the
    /// screen announced itself as one word.
    ///
    /// The injured-companion event offers a companion from an earlier run,
    /// found hurt on the road. The game explains it with a one-time tutorial
    /// prompt keyed to save data, so every player past their first sees the
    /// card and the blood splats and is told nothing at all.
    /// </summary>
    public static class InjuryNarrator
    {
        /// <summary>
        /// The recovery screen opening, with the companions it is about to
        /// reveal. Called before the cards are built, so the only text on the
        /// panel at this point is the panel's own.
        /// </summary>
        internal static void OnRecoveryOpened(CompanionRecoverSequence sequence, CardData[] recovered)
        {
            if (recovered == null || recovered.Length == 0)
                return;

            var names = new List<string>();
            foreach (CardData card in recovered)
            {
                if (card == null)
                    continue;
                string title;
                try { title = card.title; }
                catch { title = null; }
                names.Add(string.IsNullOrEmpty(title) ? ScreenHandler.CleanName(card.name) : title);
            }
            if (names.Count == 0)
                return;

            var parts = new List<string>();
            string panel = sequence != null ? ReadPanelText(sequence.gameObject) : null;
            if (!string.IsNullOrEmpty(panel))
                parts.Add(panel);
            parts.Add(Loc.Get(names.Count == 1 ? "recover_one" : "recover_many",
                string.Join(", ", names)));
            parts.Add(Loc.Get("overlay_continue_hint"));

            string msg = string.Join(" ", parts);
            ScreenReader.SayEvent(msg, interrupt: true);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "InjuryNarrator", $"Recovery: {msg}");
        }

        /// <summary>
        /// The injured-companion event opening. The card is already in the
        /// container by now — Populate builds it before Run — so it can be
        /// named as part of the framing the game no longer gives.
        /// </summary>
        internal static void OnInjuredEventOpened(EventRoutineInjuredCompanion routine)
        {
            string title = null;
            try
            {
                var container = ReflectionUtil.GetField<CardContainer>(routine, "cardContainer");
                if (container != null && container.Count > 0)
                    title = container[0]?.data?.title;
            }
            catch
            {
                // Card not built — the framing still stands without a name
            }

            string msg = string.IsNullOrEmpty(title)
                ? Loc.Get("injured_event_unnamed")
                : Loc.Get("injured_event", title);

            ScreenReader.SayEvent(msg, interrupt: true);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "InjuryNarrator", $"Injured event: {msg}");
        }

        /// <summary>
        /// A panel's own words, laid out in order. Text belonging to cards on
        /// the panel is skipped: those are card names and stats, read as cards
        /// when the player browses them rather than as part of the heading.
        /// </summary>
        private static string ReadPanelText(GameObject root)
        {
            var parts = new List<string>();
            try
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(includeInactive: true))
                {
                    if (text == null || text.GetComponentInParent<Entity>() != null)
                        continue;

                    string clean = TextProcessor.ProcessForScreenReader(text.text)?.Trim();
                    if (!string.IsNullOrEmpty(clean) && !parts.Contains(clean))
                        parts.Add(clean);
                }
            }
            catch
            {
                // Panel mid-build — the rest of the announcement carries it
            }
            return parts.Count > 0 ? string.Join(". ", parts) : null;
        }
    }

    /// <summary>
    /// FindRecoveries runs once at the top of the sequence and decides whether
    /// the screen shows at all: an empty result means nothing healed and no
    /// panel appears. Its result is also exactly the list the panel is about
    /// to display, before the injuries are cleared off the cards.
    /// </summary>
    [HarmonyPatch(typeof(CompanionRecoverSequence), "FindRecoveries")]
    internal static class CompanionRecoverOpenPatch
    {
        private static void Postfix(CompanionRecoverSequence __instance, CardData[] __result)
        {
            try
            {
                InjuryNarrator.OnRecoveryOpened(__instance, __result);
            }
            catch
            {
                // Inventory mid-teardown — the screen passes unnarrated
            }
        }
    }

    /// <summary>Run is called once when the event takes over the screen.</summary>
    [HarmonyPatch(typeof(EventRoutineInjuredCompanion), "Run")]
    internal static class InjuredCompanionEventPatch
    {
        private static void Prefix(EventRoutineInjuredCompanion __instance)
        {
            try
            {
                InjuryNarrator.OnInjuredEventOpened(__instance);
            }
            catch
            {
                // Event mid-build — the card itself is still browsable
            }
        }
    }
}
