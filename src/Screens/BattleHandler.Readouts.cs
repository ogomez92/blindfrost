using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// On-demand readout keys: hand, board, waves, bell, modifiers, gold
    /// and turn, plus the wave-countdown text they share.
    /// </summary>
    public partial class BattleHandler
    {
        // ---- Readout keys ----------------------------------------------------

        private void AnnounceHand()
        {
            var hand = Battle.instance?.player?.handContainer;
            if (hand == null || hand.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_hand_empty"), interrupt: true);
                return;
            }

            var names = new List<string>();
            foreach (Entity entity in hand)
            {
                if (entity?.data != null)
                    names.Add(entity.data.title);
            }
            ScreenReader.Say(
                Loc.Get("battle_hand_count", hand.Count) + " " + string.Join(", ", names),
                interrupt: true);
        }

        private void AnnounceBoard()
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var parts = new List<string>
            {
                Loc.Get("group_your_board"),
                DescribeSide(battle.player),
                Loc.Get("group_enemy_board"),
                DescribeSide(battle.enemy)
            };
            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        private string DescribeSide(Character character)
        {
            if (character == null) return Loc.Get("slot_empty");

            var rows = new List<string>();
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;

                var cells = new List<string>();
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null)
                    {
                        cells.Add(Loc.Get("slot_empty"));
                        continue;
                    }

                    string cell = occupant.data.title;
                    if (occupant.hp.max > 0)
                        cell += " " + ItemDescriber.DescribeHealth(occupant);
                    if (occupant.damage.max > 0)
                        cell += " " + Loc.Get("stat_attack", ItemDescriber.GetShownAttack(occupant));
                    if (occupant.counter.max > 0)
                        cell += " " + Loc.Get("battle_acts_in", occupant.counter.current);

                    string statuses = ItemDescriber.DescribeStatusEffects(occupant);
                    if (!string.IsNullOrEmpty(statuses))
                        cell += ", " + statuses;

                    cells.Add(cell);
                }

                rows.Add(Loc.Get("slot_row", row + 1) + ": " + string.Join(", ", cells));
            }

            return string.Join(". ", rows);
        }

        private void AnnounceWaves()
        {
            var items = BuildWaveItems();
            if (items == null || items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_no_waves"), interrupt: true);
                return;
            }

            ScreenReader.Say(string.Join(". ", items), interrupt: true);
        }

        /// <summary>
        /// "Next wave in N turns", read from the wave deploy HUD. The deployer
        /// counts down from an action it queues when the turn starts, so at
        /// that moment the field still holds last turn's number and the one
        /// the player is about to see is a turn lower. An empty enemy board
        /// deploys the wave immediately whatever the counter says. Either way,
        /// a wave landing this very turn has no countdown worth speaking — the
        /// arrival announcement follows a second behind it.
        /// </summary>
        private string GetWaveCounterText(bool atTurnStart = false)
        {
            int counter = WaveDeployer.GetCounter(WaveDeployer.Find());
            if (atTurnStart)
            {
                if (EnemyBoardIsEmpty()) return null;
                counter--;
            }

            if (counter <= 0) return null;

            return Loc.Get("battle_next_wave", counter);
        }

        /// <summary>No enemies left standing — the next wave deploys at once.</summary>
        private static bool EnemyBoardIsEmpty()
        {
            try
            {
                var enemy = Battle.instance?.enemy;
                return enemy != null && Battle.GetCardsOnBoard(enemy).Count <= 0;
            }
            catch
            {
                return false;
            }
        }

        private void AnnounceBell()
        {
            var bell = Object.FindObjectOfType<RedrawBellSystem>();
            if (bell == null)
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
                return;
            }

            if (bell.IsCharged)
                ScreenReader.Say(Loc.Get("battle_bell_charged"), interrupt: true);
            else
                ScreenReader.Say(Loc.Get("battle_bell_charging", bell.counter.current), interrupt: true);
        }

        /// <summary>
        /// Read the run modifier bells hanging in the HUD (gauntlet/event rules).
        /// They only explain themselves via hover panels, which keyboard
        /// navigation can't reach.
        /// </summary>
        private void AnnounceModifiers()
        {
            var parts = new List<string>();
            foreach (var icon in Object.FindObjectsOfType<ModifierIcon>())
            {
                if (icon == null || !icon.gameObject.activeInHierarchy)
                    continue;

                string desc = ItemDescriber.DescribeModifierIcon(icon);
                if (!string.IsNullOrEmpty(desc) && !parts.Contains(desc))
                    parts.Add(desc);
            }

            ScreenReader.Say(parts.Count > 0
                ? string.Join(". ", parts)
                : Loc.Get("battle_no_modifiers"), interrupt: true);
        }

        private void AnnounceGold()
        {
            try
            {
                int gold = References.Player.data.inventory.gold.Value;
                ScreenReader.Say(Loc.Get("gold_amount", gold), interrupt: true);
            }
            catch
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
            }
        }

        private void AnnounceTurn()
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var parts = new List<string> { Loc.Get("battle_turn", battle.turnCount) };

            parts.Add(battle.phase == Battle.Phase.Play
                ? Loc.Get("battle_phase_play")
                : Loc.Get("battle_phase_other"));

            string wave = GetWaveCounterText();
            if (wave != null)
                parts.Add(wave);

            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

    }
}
