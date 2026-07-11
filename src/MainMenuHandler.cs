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
            ScreenReader.Say(Loc.Get("screen_main_menu"), interrupt: true);
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
