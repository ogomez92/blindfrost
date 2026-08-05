using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The mod's own settings screen, opened with F2 from anywhere and driven
    /// entirely by speech — there is no visual UI, because everything it
    /// configures exists for players who cannot see one. Up and down move
    /// between settings, left and right change the focused setting's value,
    /// Enter cycles it forward, Escape or F2 closes.
    ///
    /// Every change applies the moment it is made and is written to disk, so
    /// there is nothing to confirm and no way to lose a setting by closing.
    ///
    /// While the menu is open the game's own input is switched off (the same
    /// InputSystem lock the game uses for its console and its cutscenes) so
    /// arrow keys cannot walk the menu underneath and Enter cannot press the
    /// button that happens to be focused there. The option table itself lives
    /// in SettingsMenu.Options.cs.
    /// </summary>
    public static partial class SettingsMenu
    {
        private static bool _isOpen;
        private static int _index;

        // The game's input locks, captured on open so closing restores exactly
        // what was there rather than force-enabling input the game had disabled.
        private static bool _restoreInputSystem;
        private static RewiredControllerManager _lockedRewired;

        /// <summary>True while the settings menu owns the keyboard.</summary>
        public static bool IsOpen => _isOpen;

        /// <summary>
        /// Open the settings menu, announcing how it works and the first
        /// setting. Called from the F2 binding in Main.OnUpdate.
        /// </summary>
        public static void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            _index = 0;
            LockGameInput();

            DebugLogger.LogInput("Settings", "Open");
            ScreenReader.SayEvent(Loc.Get("settings_opened"), interrupt: true);
            AnnounceCurrent();
        }

        /// <summary>Close the settings menu and hand the keyboard back to the game.</summary>
        public static void Close()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            UnlockGameInput();

            DebugLogger.LogInput("Settings", "Close");
            ScreenReader.Say(Loc.Get("settings_closed"), interrupt: true);
        }

        /// <summary>
        /// Handle the settings keys while the menu is open. Returns true when
        /// the menu owns this frame's input, which stops Main.OnUpdate from
        /// routing anything else — the menu is modal.
        /// </summary>
        public static bool RouteInput()
        {
            if (!_isOpen)
                return false;

            // The lock has to be re-asserted: scene loads and the game's own
            // sequences call InputSystem.Enable() while we are open
            HoldGameInput();

            if (Input.GetKeyDown(KeyCode.F2) || NavigationHelper.IsBackPressed())
            {
                Close();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                ScreenReader.Say(Loc.Get("settings_help"), interrupt: true);
                return true;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            switch (dir)
            {
                case NavDirection.Up:
                    Move(-1);
                    return true;
                case NavDirection.Down:
                    Move(1);
                    return true;
                case NavDirection.Left:
                    ChangeCurrent(-1);
                    return true;
                case NavDirection.Right:
                    ChangeCurrent(1);
                    return true;
            }

            if (NavigationHelper.IsConfirmPressed())
            {
                ChangeCurrent(1);
                return true;
            }

            return true; // modal: swallow everything else
        }

        /// <summary>Move between settings. The list does not wrap — the ends are edges you can feel.</summary>
        private static void Move(int delta)
        {
            var options = Options;
            int next = Mathf.Clamp(_index + delta, 0, options.Count - 1);
            if (next == _index)
            {
                // Silence at the end of a list reads as a dead key
                ScreenReader.Say(Loc.Get(delta < 0 ? "settings_first" : "settings_last"), interrupt: true);
                return;
            }

            _index = next;
            AnnounceCurrent();
        }

        /// <summary>Change the focused setting and speak its new value.</summary>
        private static void ChangeCurrent(int delta)
        {
            var option = Options[_index];
            option.Change(delta);

            // Changing the language changes the language the menu speaks in, so
            // the setting names itself again — otherwise the player hears one
            // bare word in a language that just switched under them
            if (option.ReannounceOnChange)
                AnnounceCurrent();
            else
                ScreenReader.Say(option.DescribeValue(), interrupt: true);
        }

        /// <summary>Speak the focused setting: its name, then its value.</summary>
        private static void AnnounceCurrent()
        {
            var option = Options[_index];
            ScreenReader.Say(option.DescribeName() + ": " + option.DescribeValue(), interrupt: true);
        }

        // ---- Game input lock ---------------------------------------------------

        /// <summary>
        /// Switch the game's input off for the duration of the menu. Two locks
        /// are needed: InputSystem.Disable() stops UINavigationSystem walking
        /// the focus underneath (it polls InputSystem for Move Horizontal and
        /// Move Vertical), and disabling RewiredControllerManager stops
        /// VirtualInputModule turning an Enter press into a click on whatever
        /// the game currently has focused — which would start a new run from
        /// inside the settings menu.
        /// </summary>
        private static void LockGameInput()
        {
            _restoreInputSystem = false;
            _lockedRewired = null;

            try
            {
                // Only restore input on close if the game actually had it on;
                // opening the menu during a cutscene must not turn it back on.
                // The raw field, not InputSystem.Enabled — that property also
                // reports false during any screen transition, which would strand
                // the game with its input switched off after we close.
                _restoreInputSystem = RawInputSystemEnabled();
                InputSystem.Disable();
            }
            catch
            {
                // Input system not ready — the menu still works, the game
                // underneath just keeps its keys
            }

            try
            {
                var rewired = Object.FindObjectOfType<RewiredControllerManager>();
                if (rewired != null && rewired.enabled)
                {
                    rewired.enabled = false;
                    _lockedRewired = rewired;
                }
            }
            catch
            {
            }

            // A uGUI object left selected by an earlier click would take our
            // Enter presses as Submit even with the locks above
            try
            {
                NavigationHelper.ClearUnitySelection();
            }
            catch
            {
            }
        }

        /// <summary>
        /// InputSystem's own backing field. The public Enabled property folds in
        /// Transition.Running, so it cannot tell "the game switched input off"
        /// from "a screen is fading" — and only the former must survive our
        /// close. Defaults to true, matching the field's own initial value, if
        /// the field ever goes missing.
        /// </summary>
        private static bool RawInputSystemEnabled()
        {
            var field = HarmonyLib.AccessTools.Field(typeof(InputSystem), "enabled");
            return field == null || (bool)field.GetValue(null);
        }

        /// <summary>Re-assert the lock; the game re-enables input on scene loads and sequences.</summary>
        private static void HoldGameInput()
        {
            try
            {
                if (RawInputSystemEnabled())
                    InputSystem.Disable();
            }
            catch
            {
            }

            if (_lockedRewired != null && _lockedRewired.enabled)
                _lockedRewired.enabled = false;
        }

        /// <summary>Give the game back exactly the input state it had when the menu opened.</summary>
        private static void UnlockGameInput()
        {
            if (_lockedRewired != null)
            {
                _lockedRewired.enabled = true;
                _lockedRewired = null;
            }

            if (_restoreInputSystem)
            {
                try
                {
                    InputSystem.Enable();
                }
                catch
                {
                }
                _restoreInputSystem = false;
            }
        }
    }
}
