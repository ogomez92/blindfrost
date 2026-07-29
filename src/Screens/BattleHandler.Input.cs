using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Keyboard input for the battle screen: the key handler plus the
    /// Ctrl-key roll calls for one side's counters and health.
    /// </summary>
    public partial class BattleHandler
    {
        // ---- Input ----------------------------------------------------------

        protected override void HandleInput()
        {
            base.HandleInput();

            // Escape puts a picked-up card back (same as the gamepad Back action)
            if (IsTargeting() && NavigationHelper.IsBackPressed())
            {
                DebugLogger.LogInput(Name, "Cancel pickup");
                var controller = Battle.instance?.playerCardController;
                Entity held = controller?.dragging;
                controller?.DragCancel();
                ScreenReader.Say(Loc.Get("battle_pickup_cancelled", held?.data?.title ?? ""));
                return;
            }

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Ctrl+C / Ctrl+E: quick counter status for one side — who acts
            // in how many turns, without wading through the full board read
            if (ctrl && Input.GetKeyDown(KeyCode.C)) { DebugLogger.LogInput(Name, "Ally counters"); AnnounceCounters(allies: true); return; }
            if (ctrl && Input.GetKeyDown(KeyCode.E)) { DebugLogger.LogInput(Name, "Enemy counters"); AnnounceCounters(allies: false); return; }
            // Ctrl+H / Ctrl+Shift+H: who is hurt. Damage is narrated as it
            // lands, but after a few exchanges only a roll call answers
            // "who do I need to pull out?" — Shift flips it to the enemies
            if (ctrl && Input.GetKeyDown(KeyCode.H))
            {
                DebugLogger.LogInput(Name, shift ? "Enemy health" : "Ally health");
                AnnounceHealth(allies: !shift);
                return;
            }
            if (ctrl) return; // don't let Ctrl+combos fall through to the plain letter keys

            if (Input.GetKeyDown(KeyCode.H)) { DebugLogger.LogInput(Name, "Hand"); AnnounceHand(); }
            if (Input.GetKeyDown(KeyCode.B)) { DebugLogger.LogInput(Name, "Board"); AnnounceBoard(); }
            if (Input.GetKeyDown(KeyCode.W)) { DebugLogger.LogInput(Name, "Waves"); AnnounceWaves(); }
            if (Input.GetKeyDown(KeyCode.R)) { DebugLogger.LogInput(Name, "Bell"); AnnounceBell(); }
            if (Input.GetKeyDown(KeyCode.G)) { DebugLogger.LogInput(Name, "Gold"); AnnounceGold(); }
            if (Input.GetKeyDown(KeyCode.T)) { DebugLogger.LogInput(Name, "Turn"); AnnounceTurn(); }
            if (Input.GetKeyDown(KeyCode.M)) { DebugLogger.LogInput(Name, "Modifiers"); AnnounceModifiers(); }
        }

        /// <summary>
        /// One side's counters at a glance: each unit with a counter, its
        /// position, how many turns until it acts, and whether Snow froze it.
        /// </summary>
        private void AnnounceCounters(bool allies)
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var character = allies ? battle.player : battle.enemy;
            var parts = new List<string>();
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null || occupant.counter.max <= 0) continue;

                    string cell = occupant.data.title;
                    string position = ItemDescriber.GetEntitySlotShort(occupant);
                    if (!string.IsNullOrEmpty(position))
                        cell += " " + position;
                    cell += ", " + Loc.Get("battle_acts_in", occupant.counter.current);
                    if (occupant.IsSnowed)
                        cell += ", " + Loc.Get("counter_frozen");
                    parts.Add(cell);
                }
            }

            if (parts.Count == 0)
            {
                ScreenReader.Say(Loc.Get(allies
                    ? "battle_counters_none_ally"
                    : "battle_counters_none_enemy"), interrupt: true);
                return;
            }

            parts.Insert(0, Loc.Get(allies ? "battle_counters_allies" : "battle_counters_enemies"));
            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        /// <summary>
        /// One side's health at a glance: every unit on the board with its
        /// position and current health out of max. Hits are narrated as they
        /// land, but a running tally is impossible to hold across a long
        /// fight — this is the roll call that says who to recall and heal.
        /// </summary>
        private void AnnounceHealth(bool allies)
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var character = allies ? battle.player : battle.enemy;
            var parts = new List<string>();
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null || !occupant.alive) continue;
                    // Boardable cards without health (scenery, some summons)
                    // have nothing to report
                    if (occupant.hp.max <= 0) continue;

                    string cell = occupant.data.title;
                    string position = ItemDescriber.GetEntitySlotShort(occupant);
                    if (!string.IsNullOrEmpty(position))
                        cell += " " + position;
                    cell += ", " + ItemDescriber.DescribeHealth(occupant);
                    parts.Add(cell);
                }
            }

            if (parts.Count == 0)
            {
                ScreenReader.Say(Loc.Get(allies
                    ? "battle_health_none_ally"
                    : "battle_health_none_enemy"), interrupt: true);
                return;
            }

            parts.Insert(0, Loc.Get(allies ? "battle_health_allies" : "battle_health_enemies"));
            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        /// <summary>
        /// I inspects the focused card — but not while holding one: opening
        /// the zoomed inspect view mid-placement would fight the drag state.
        /// </summary>
        protected override void OnInspectKey()
        {
            if (IsTargeting())
            {
                ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
                return;
            }
            base.OnInspectKey();
        }

    }
}
