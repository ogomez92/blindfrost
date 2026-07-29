using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The wave bell: enemies washing onto the board part-way through a fight.
    /// The units land one at a time, so the arrivals are collected here and
    /// spoken as a single line — the bell, who came in where, and how many the
    /// board had no room for.
    /// </summary>
    public static partial class VisualNarrator
    {
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
    }
}
