namespace WildfrostAccessibility
{
    /// <summary>
    /// The keyboard navigation layer the whole mod runs on.
    ///
    /// The game has no keyboard navigation of its own — its UINavigationSystem
    /// only ever sees gamepad input — so this helper supplies it: arrow keys
    /// become directions, those directions pick the next UINavigationItem, and
    /// the game is forced into controller mode so it keeps the focus we set.
    ///
    /// The parts:
    /// <list type="bullet">
    /// <item>Input — reading the keyboard, hold-to-repeat, the typing guard</item>
    /// <item>Items — collecting and spatially sorting the navigable items</item>
    /// <item>Focus — moving focus and keeping the game's idea of it in sync</item>
    /// <item>Activation — confirm/back, simulating the click the game expects</item>
    /// <item>Diagnostics — debug dumps of the navigation state</item>
    /// </list>
    /// </summary>
    public static partial class NavigationHelper
    {
        // Hold-to-repeat state, shared by the input and focus parts.
        private static float _lastNavTime;
        private static float _navRepeatDelay = 0.3f;
        private static float _navRepeatRate = 0.1f;
        private static bool _navHeld;
    }
}
