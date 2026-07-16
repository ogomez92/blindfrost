using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the Credits scene (from the main menu) and the CreditsEnd
    /// scene (rolls after vanquishing the Frost, before the final victory
    /// screen). The scroll is visual; without this handler the previous
    /// screen's handler stayed active and arrows drove invisible buttons.
    /// Enter or Escape skips by unloading the scene — both callers
    /// (MainMenu.CreditsRoutine, DefeatSequence.Routine) just wait for the
    /// unload, so skipping resumes their flow exactly like the vanilla exit.
    /// </summary>
    public class CreditsHandler : NavigableScreenHandler
    {
        public override string Name => "Credits";

        /// <summary>True when this is the post-victory end credits roll.</summary>
        private bool IsEndCredits => SceneManager.IsLoaded("CreditsEnd");

        protected override bool TryAnnounceScreen()
        {
            string text = IsEndCredits
                ? Loc.Get("creditsend_announce")
                : Loc.Get("scene_Credits");
            ScreenReader.SayEvent(text + " " + Loc.Get("credits_skip_hint"), interrupt: true);
            return true;
        }

        public override string GetHelpText() => Loc.Get("help_overlay_continue");

        protected override void HandleInput()
        {
            if (NavigationHelper.IsConfirmPressed() || NavigationHelper.IsBackPressed())
            {
                Skip();
                return;
            }
            base.HandleInput();
        }

        private void Skip()
        {
            string sceneKey = IsEndCredits ? "CreditsEnd" : "Credits";

            // Prefer the scene's own unloader (plays its outro); fall back to a
            // direct unload — the loader coroutines only wait for the unload.
            var unloader = Object.FindObjectOfType<SceneUnloader>();
            if (unloader != null && unloader.gameObject.scene.name == sceneKey)
            {
                DebugLogger.LogInput(Name, $"Skip credits via SceneUnloader ({sceneKey})");
                unloader.Unload();
            }
            else
            {
                DebugLogger.LogInput(Name, $"Skip credits via direct unload ({sceneKey})");
                new Routine(SceneManager.Unload(sceneKey));
            }
            ScreenReader.Say(Loc.Get("credits_skipped"), interrupt: true);
        }
    }
}
