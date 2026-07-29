using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Keyboard input for the battle screen: the key table that cancels a
    /// pickup and dispatches the on-demand readouts, plus the inspect key's
    /// mid-placement guard.
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

            // Ctrl+C / Ctrl+Shift+C: quick counter status for one side — who
            // acts in how many turns, without wading through the full board
            // read. Shift flips it to the enemies, same as the health keys
            if (ctrl && Input.GetKeyDown(KeyCode.C))
            {
                DebugLogger.LogInput(Name, shift ? "Enemy counters" : "Ally counters");
                AnnounceCounters(allies: !shift);
                return;
            }
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
