using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Review-buffer sources: the hand, board, resource and wave item
    /// lists the review navigation reads back one entry at a time.
    /// </summary>
    public partial class BattleHandler
    {
        // ---- Review buffer sources -------------------------------------------

        /// <summary>Hand buffer: one item per hand card, as its short read.</summary>
        internal List<string> BuildHandItems()
        {
            var hand = Battle.instance?.player?.handContainer;
            if (hand == null) return null;

            var items = new List<string>();
            foreach (Entity entity in hand)
            {
                string desc = ItemDescriber.DescribeEntityShort(entity);
                if (desc != null)
                    items.Add(desc);
            }
            return items;
        }

        /// <summary>Board buffer: one item per unit with its position, your side first.</summary>
        internal List<string> BuildBoardItems()
        {
            var battle = Battle.instance;
            if (battle == null) return null;

            var items = new List<string>();
            AddSideBufferItems(items, battle.player);
            AddSideBufferItems(items, battle.enemy);
            return items;
        }

        private static void AddSideBufferItems(List<string> items, Character character)
        {
            if (character == null) return;
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null) continue;

                    string summary = ItemDescriber.SummarizeEntity(occupant);
                    if (summary == null) continue;

                    string position = ItemDescriber.GetSlotPosition(slot);
                    items.Add(string.IsNullOrEmpty(position)
                        ? summary
                        : position + ": " + summary);
                }
            }
        }

        /// <summary>Resources buffer: gold, bell, turn, piles, wave counter.</summary>
        internal List<string> BuildResourceItems()
        {
            var battle = Battle.instance;
            if (battle == null) return null;

            var items = new List<string>();

            try
            {
                items.Add(Loc.Get("gold_amount", References.Player.data.inventory.gold.Value));
            }
            catch { }

            var bell = Object.FindObjectOfType<RedrawBellSystem>();
            if (bell != null)
            {
                items.Add(bell.IsCharged
                    ? Loc.Get("battle_bell_charged")
                    : Loc.Get("battle_bell_charging", bell.counter.current));
            }

            items.Add(Loc.Get("battle_turn", battle.turnCount) + ". "
                + (battle.phase == Battle.Phase.Play
                    ? Loc.Get("battle_phase_play")
                    : Loc.Get("battle_phase_other")));

            var draw = battle.player?.drawContainer;
            if (draw != null)
                items.Add(Loc.Get(draw.Count == 1 ? "pocket_draw_one" : "pocket_draw", draw.Count));

            var discard = battle.player?.discardContainer;
            if (discard != null)
                items.Add(Loc.Get(discard.Count == 1 ? "pocket_discard_one" : "pocket_discard", discard.Count));

            string wave = GetWaveCounterText();
            if (wave != null)
                items.Add(wave);

            return items;
        }

        /// <summary>Waves buffer: the counter plus one item per remaining wave.</summary>
        internal List<string> BuildWaveItems()
        {
            var system = WaveDeployer.Find();
            var waves = WaveDeployer.GetWaves(system);
            if (waves == null) return null;

            var items = new List<string>();
            string counterText = GetWaveCounterText();
            if (counterText != null)
                items.Add(counterText);

            // Everything before the current index has already landed. The
            // overflow deployer never sets Wave.spawned, so its index is the
            // only honest marker of what is still coming.
            int remaining = 0;
            for (int index = WaveDeployer.GetCurrentWave(system) + 1; index <= waves.Count; index++)
            {
                var wave = waves[index - 1];
                if (wave == null || wave.spawned) continue;

                var names = new List<string>();
                if (wave.units != null)
                {
                    foreach (CardData unit in wave.units)
                    {
                        if (unit != null)
                            names.Add(unit.title);
                    }
                }

                string desc = Loc.Get("battle_wave_n", index, string.Join(", ", names));
                if (wave.isBossWave)
                    desc += ", " + Loc.Get("battle_boss_wave");
                items.Add(desc);
                remaining++;
            }

            if (remaining == 0)
                items.Add(Loc.Get("battle_all_waves_spawned"));
            return items;
        }

    }
}
