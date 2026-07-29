using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WildfrostAccessibility
{
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
    /// The prefix fires when the game calls the deploy coroutine. Patched on
    /// every deployer variant: they are unrelated classes, and the one the
    /// current game ships is not the one whose name says "wave deploy system".
    /// </summary>
    [HarmonyPatch]
    internal static class WaveDeployActivatePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in WaveDeployer.SystemTypes)
            {
                MethodBase method = AccessTools.Method(type, "Activate");
                if (method != null)
                    yield return method;
            }
        }

        private static void Prefix(object __instance)
        {
            try
            {
                VisualNarrator.OnWaveStarting(__instance as Component);
            }
            catch
            {
                // Deployer state unreadable — the arrivals still narrate
            }
        }
    }

    /// <summary>
    /// Which enemy arrives where. Deploy runs once per unit that found a slot,
    /// with the row and column it takes, so each arrival can be placed on the
    /// board by ear. Units the wave could not fit never reach Deploy — the
    /// overflow patch below counts those instead.
    /// </summary>
    [HarmonyPatch]
    internal static class WaveDeployPlacementPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (Type type in WaveDeployer.SystemTypes)
            {
                MethodBase method = AccessTools.Method(type, "Deploy");
                if (method != null)
                    yield return method;
            }
        }

        private static void Postfix(Entity entity, int targetRow, int targetColumn)
        {
            try
            {
                VisualNarrator.OnWaveUnitDeployed(entity, targetRow, targetColumn);
            }
            catch
            {
                // Board state mid-teardown — the arrival passes unnarrated
            }
        }
    }

    /// <summary>
    /// Enemies the board had no room for. The game holds them back for a later
    /// wave, and the only sign of it is a wave that arrives short — so the
    /// count is folded into the arrival announcement. Only the overflow
    /// deployer has this behaviour; on the others a blocked unit simply
    /// never lands.
    /// </summary>
    [HarmonyPatch(typeof(WaveDeploySystemOverflow), "Overflow")]
    internal static class WaveOverflowPatch
    {
        private static void Postfix(IEnumerable<Entity> entities)
        {
            try
            {
                VisualNarrator.OnWaveUnitsBlocked(entities);
            }
            catch
            {
                // Wave list mid-rebuild — the arrivals still narrate
            }
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
