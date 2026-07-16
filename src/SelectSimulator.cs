using HarmonyLib;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Makes keyboard Enter satisfy the game's polled "Select" waits.
    /// Several cinematics stop on <c>while (!InputSystem.IsButtonPressed("Select"))</c>
    /// (the card-combine reveal, the final-boss possession prompts) — Rewired
    /// "Select" is gamepad/mouse only, so a keyboard player is hard-stuck there.
    /// Arm() answers the next few Select polls with true. Arming is deliberate
    /// and short-lived: only code that knows the game is waiting on such a poll
    /// calls it (see OverlayWatcher), because a true Select also clicks the
    /// game's hovered object — Arm() clears the hover first so nothing else
    /// can be clicked by the simulated press.
    /// </summary>
    public static class SelectSimulator
    {
        private static int _armedUntilFrame = -1;

        /// <summary>True while an armed Enter press should answer Select polls.</summary>
        public static bool Armed => Time.frameCount <= _armedUntilFrame;

        /// <summary>
        /// Answer Select polls with true for the next few frames. Clears the
        /// game hover AND every card controller's hover first, so the
        /// simulated Select cannot click UI leftovers or pick up a card that
        /// happens to sit under the (virtual) pointer beneath the cinematic —
        /// CustomEventSystem clicks its hovered object and CardController
        /// presses its hoverEntity whenever Select fires.
        /// </summary>
        public static void Arm(int frames = 2)
        {
            NavigationHelper.ClearHover();
            foreach (var controller in Object.FindObjectsOfType<CardController>())
                controller.hoverEntity = null;
            _armedUntilFrame = Time.frameCount + frames;
            DebugLogger.LogInput("SelectSimulator", $"Armed for {frames} frames");
        }
    }

    /// <summary>
    /// The polled waits all funnel through InputSystem.IsButtonPressed
    /// (IsSelectPressed is a wrapper around it), so one postfix covers them.
    /// </summary>
    [HarmonyPatch(typeof(InputSystem), nameof(InputSystem.IsButtonPressed))]
    internal static class InputSystemIsButtonPressedPatch
    {
        private static void Postfix(string input, bool positive, ref bool __result)
        {
            // Armed first: it is false outside a ~2-frame window, and this
            // postfix runs on every input poll in the game. The Enabled check
            // keeps a deliberate game-side input lock (transitions) intact.
            if (SelectSimulator.Armed && !__result && positive && input == "Select"
                && InputSystem.Enabled)
                __result = true;
        }
    }
}
