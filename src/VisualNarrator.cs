using HarmonyLib;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Speaks the game's purely visual story moments — scenes that carry
    /// meaning only through animation: a miniboss slamming onto the board, a
    /// boss transforming between phases, the wave bell washing new enemies in,
    /// the final-boss shade possessing the leader, cards merging into a new
    /// one, and every speech bubble (town greeters, the muncher, the gnome),
    /// whose text exists but is never read anywhere else.
    /// Event subscriptions live here; the moments without events are narrated
    /// from Harmony prefixes at the bottom of this file.
    /// </summary>
    public static class VisualNarrator
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Events.OnMinibossIntro += OnMinibossIntro;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;
            Events.OnMinibossIntro -= OnMinibossIntro;
        }

        /// <summary>
        /// Ambient speech bubbles: town building greeters, the muncher, shop
        /// keepers. The text is real and localized but only ever shown visually.
        /// Called from the display-time patch below — NOT from OnCreate, which
        /// fires at enqueue time: several bubbles queued in one frame would all
        /// be spoken at once, out of sync with what is on screen.
        /// </summary>
        internal static void OnSpeechBubbleShown(SpeechBubbleData data)
        {
            if (data == null || string.IsNullOrEmpty(data.text))
                return;
            string text = TextProcessor.StripRichText(data.text)?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            string line = !string.IsNullOrEmpty(data.targetName)
                ? Loc.Get("speech_bubble", data.targetName, text)
                : text;
            // Queued, not interrupting: bubbles accompany whatever else is
            // being announced and should never cut it off.
            ScreenReader.SayEvent(line);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "VisualNarrator",
                $"Speech bubble: {line}");
        }

        /// <summary>A miniboss lands on the board with a zoom-and-shake cinematic.</summary>
        private static void OnMinibossIntro(Entity entity)
        {
            string title = entity?.data?.title;
            if (string.IsNullOrEmpty(title))
                return;
            ScreenReader.SayEvent(Loc.Get("narrate_miniboss", title), interrupt: true);
        }

        internal static void Narrate(string locKey, params object[] args)
        {
            ScreenReader.SayEvent(Loc.Get(locKey, args));
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "VisualNarrator", locKey);
        }
    }

    /// <summary>
    /// Speech bubbles are narrated when they are actually DISPLAYED:
    /// SpeechBubbleSystem queues bubbles and shows them one at a time, so
    /// CreateBubble (the display step) is the moment the text appears. The
    /// delay guard skips the pre-delay call — the coroutine re-calls with
    /// delay zero when the bubble really shows.
    /// </summary>
    [HarmonyPatch(typeof(SpeechBubbleSystem), "CreateBubble")]
    internal static class SpeechBubbleShownPatch
    {
        private static void Prefix(SpeechBubbleData data)
        {
            if (data != null && data.delay <= 0f)
                VisualNarrator.OnSpeechBubbleShown(data);
        }
    }

    /// <summary>
    /// "The bell tolls" — a new enemy wave physically washes onto the board.
    /// The prefix fires when the game calls the deploy coroutine.
    /// </summary>
    [HarmonyPatch(typeof(WaveDeploySystem), "Activate")]
    internal static class WaveDeployActivatePatch
    {
        private static void Prefix()
        {
            VisualNarrator.Narrate("narrate_wave");
        }
    }

    /// <summary>
    /// Clunker boss phase change: explosions and a rumble are the only cue
    /// that the boss just transformed.
    /// </summary>
    [HarmonyPatch(typeof(CardAnimationClunkerBossChange), "Routine")]
    internal static class BossPhaseChangePatch
    {
        private static void Prefix(object data)
        {
            if (data is Entity entity && entity.data != null)
                VisualNarrator.Narrate("narrate_boss_transform", entity.data.title);
        }
    }

    /// <summary>
    /// Final-boss shade cinematic. After the last guardian falls, a dark wisp
    /// spawns at its corpse: on ordinary boss nodes it flees into the storm;
    /// on the final node it dives into the player's leader (possession) —
    /// unless the leader carries the sealing vase (BlockWisp).
    /// All three are silent, purely animated beats.
    /// </summary>
    [HarmonyPatch(typeof(FinalBossSequenceSystem), "Flee")]
    internal static class ShadeFleePatch
    {
        private static void Prefix()
        {
            VisualNarrator.Narrate("narrate_shade_flee");
        }
    }

    [HarmonyPatch(typeof(FinalBossSequenceSystem), "PossessLeader")]
    internal static class ShadePossessPatch
    {
        private static void Prefix()
        {
            VisualNarrator.Narrate("narrate_shade_possess");
        }
    }

    [HarmonyPatch(typeof(FinalBossSequenceSystem), "BlockWisp")]
    internal static class ShadeBlockedPatch
    {
        private static void Prefix(CardData blockCardData)
        {
            VisualNarrator.Narrate("narrate_shade_blocked",
                blockCardData != null ? blockCardData.title : "");
        }
    }

    /// <summary>
    /// The possession completing: the leader's eyes turn to frost. Create also
    /// runs for enemy units that come with frost eyes, so only the player's
    /// own units are narrated.
    /// </summary>
    [HarmonyPatch(typeof(FrostEyeSystem), nameof(FrostEyeSystem.Create))]
    internal static class FrostEyesPatch
    {
        private static void Postfix(Entity entity)
        {
            try
            {
                if (entity?.data == null || References.Battle == null)
                    return;
                if (entity.owner?.team != References.Battle.player?.team)
                    return;
                VisualNarrator.Narrate("narrate_frost_eyes", entity.data.title);
            }
            catch
            {
                // Battle state mid-teardown — the moment passes unnarrated
            }
        }
    }

    /// <summary>
    /// Card combine: several deck cards fly together and merge. The cinema bar
    /// title only appears at the END of the ~3s animation, so this prefix
    /// explains the sudden takeover when it starts.
    /// </summary>
    [HarmonyPatch(typeof(CombineCardSystem), "CombineSequence")]
    internal static class CombineStartPatch
    {
        private static void Prefix()
        {
            VisualNarrator.Narrate("narrate_combine");
        }
    }
}
