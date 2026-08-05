namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Main Menu screen.
    /// Vertical arrow key navigation between menu buttons.
    /// </summary>
    public class MainMenuHandler : NavigableScreenHandler
    {
        public override string Name => "MainMenu";

        protected override bool TryAnnounceScreen()
        {
            // The main menu is where a player who cannot read the mod's own
            // language needs to hear that the settings menu exists — it holds
            // the language switch, and nothing else advertises it.
            ScreenReader.SayEvent(
                Loc.Get("screen_main_menu") + " " + Loc.Get("settings_hint"),
                interrupt: true);
            return true;
        }

        protected override void Navigate(NavDirection dir)
        {
            // The main menu is a vertical list — ignore left/right
            if (dir != NavDirection.Up && dir != NavDirection.Down)
                return;

            base.Navigate(dir);
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_main_menu");
        }
    }
}
