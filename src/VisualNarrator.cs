using System;
using System.Collections.Generic;
using System.Reflection;
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
            _arrivals.Clear();
            _arrivalDeadline = 0f;
            _bellPending = false;
            _couldNotFit = 0;
        }

        /// <summary>
        /// A wave does not land all at once: the deployer moves one unit, waits
        /// for that move to finish, then moves the next, so the arrivals come a
        /// few tenths of a second apart. Speaking each one the moment it lands
        /// fires a burst of separate announcements and they do not all survive
        /// the trip to the screen reader — one enemy gets read out and its
        /// companion silently does not. Arrivals are collected here instead and
        /// spoken as a single line once the wave has stopped coming.
        /// </summary>
        private const float ArrivalQuietTime = 0.75f;

        private static readonly List<string> _arrivals = new List<string>();
        private static float _arrivalDeadline;
        private static bool _bellPending;
        private static int _couldNotFit;

        /// <summary>Speak a settled wave. Called every frame from the main update loop.</summary>
        internal static void Update()
        {
            if (_arrivalDeadline > 0f && Time.unscaledTime >= _arrivalDeadline)
                SpeakWave();
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

        /// <summary>
        /// A wave starting to deploy. Anything still pending from a previous
        /// wave is spoken before this one starts collecting over it.
        /// </summary>
        internal static void OnWaveStarting(Component system)
        {
            SpeakWave();

            // The opening deploy is not a bell toll: a battle puts its starting
            // enemies on the board through the same countdown path a mid-fight
            // wave uses, and announcing an ambush at every fight open is noise.
            // Wave index 0 is that opening deploy, and the deployer resets the
            // index per battle — nothing to clear between fights.
            _bellPending = WaveDeployer.GetCurrentWave(system) > 0;
            _couldNotFit = 0;
        }

        /// <summary>
        /// A single enemy landing on the board as the wave bell deploys it.
        /// Called per unit, before its move animation runs, so the placement is
        /// read from the row and column the game chose rather than from the
        /// card's transform — which is still in the reserve container here.
        /// </summary>
        internal static void OnWaveUnitDeployed(Entity entity, int row, int column)
        {
            string title = entity?.data?.title;
            if (string.IsNullOrEmpty(title))
                return;

            _arrivals.Add(Loc.Get("narrate_wave_enter", title,
                DescribeDeploySlot(entity, row, column)));
            _arrivalDeadline = Time.unscaledTime + ArrivalQuietTime;
        }

        /// <summary>
        /// Units the board had no room for. They are held back for the next
        /// wave, which is only visible as enemies that never showed up.
        /// </summary>
        internal static void OnWaveUnitsBlocked(IEnumerable<Entity> entities)
        {
            if (entities == null)
                return;

            foreach (Entity entity in entities)
            {
                if (entity != null)
                    _couldNotFit++;
            }

            if (_couldNotFit > 0)
                _arrivalDeadline = Time.unscaledTime + ArrivalQuietTime;
        }

        /// <summary>The bell, every arrival, and whatever did not fit, as one line.</summary>
        private static void SpeakWave()
        {
            _arrivalDeadline = 0f;
            if (_arrivals.Count == 0 && _couldNotFit == 0)
            {
                _bellPending = false;
                return;
            }

            var parts = new List<string>();
            if (_bellPending)
                parts.Add(Loc.Get("narrate_wave"));
            parts.AddRange(_arrivals);
            if (_couldNotFit > 0)
                parts.Add(Loc.Get("narrate_wave_no_room", _couldNotFit));

            _arrivals.Clear();
            _couldNotFit = 0;
            _bellPending = false;

            string line = string.Join(" ", parts);
            ScreenReader.SayEvent(line);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "VisualNarrator", line);
        }

        /// <summary>
        /// Where a deploying unit lands, worded exactly as when browsing that
        /// slot ("Enemy row 1 2"). Resolves the real slot so the side is named;
        /// falls back to the bare indices if the row is not a slot lane or the
        /// slot cannot be read mid-deploy — an arrival is never worth losing
        /// over the difference between naming its slot and numbering it.
        /// </summary>
        private static string DescribeDeploySlot(Entity entity, int row, int column)
        {
            try
            {
                var battle = References.Battle;
                if (battle != null && entity?.owner != null && row >= 0 && column >= 0
                    && battle.GetRow(entity.owner, row) is CardSlotLane lane
                    && column < lane.slots.Count)
                {
                    string position = ItemDescriber.GetSlotPosition(lane.slots[column]);
                    if (!string.IsNullOrEmpty(position))
                        return position;
                }
            }
            catch
            {
                // Board mid-rebuild — the bare indices still place the arrival
            }
            return Loc.Get("slot_enemy_row", row + 1) + " " + (column + 1);
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
