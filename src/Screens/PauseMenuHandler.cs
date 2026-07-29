using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the pause menu / journal (settings, battle log,
    /// lore pages). The menu lives in the persistent PauseScreen scene and never
    /// changes ActiveSceneKey, so ScreenManager routes here via GameManager.paused.
    ///
    /// Navigation follows the game's active navigation layer exactly: the game's
    /// UINavigationSystem clears any focus that is off-layer every frame and
    /// re-focuses a default item, so offering off-layer items just fights it
    /// (that caused the "stuck cursor" bug). Tabs and pages swap the active
    /// layer; the base class re-announces on layer changes.
    ///
    /// Up/Down move between items, Left/Right change a setting's value (the same
    /// OnHorizontalOverride path the game uses for gamepad input), Enter activates
    /// buttons and tabs. O closes the menu again (global toggle in Main).
    /// </summary>
    public partial class PauseMenuHandler : NavigableScreenHandler
    {
        public override string Name => "PauseMenu";

        protected override float AnnounceDelay => 0.3f;

        public override void OnEnter()
        {
            base.OnEnter();
            _virtualIndex = -1;
        }

        protected override void OnNavigationLayerChanged(UINavigationLayer layer)
        {
            base.OnNavigationLayerChanged(layer);
            _virtualIndex = -1; // page changed — the virtual rows are new
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // The main menu's buttons stay registered (and clickable) behind the
            // open journal, and the game clicks its hovered object when Enter
            // (Rewired "Select") is pressed. Keep the hover glued to our focus
            // so Enter can never hit anything behind the menu. On a virtual row
            // there is no valid click target at all — clear the hover instead,
            // or a game-side Enter would click the previously focused item.
            if (_virtualIndex >= 0)
                NavigationHelper.ClearHover();
            else
                NavigationHelper.SyncHoverToFocus();

            // If something else moved the real focus, we are no longer standing
            // on a virtual row — Enter must act on the focused item again.
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var focus = navSystem != null ? navSystem.currentNavigationItem : null;
            if (focus != _lastSeenFocus)
            {
                _lastSeenFocus = focus;
                if (focus != null)
                    _virtualIndex = -1;
            }
        }

        private UINavigationItem _lastSeenFocus;

        /// <summary>
        /// Tab / Shift+Tab step through the page; T jumps to the tab strip;
        /// Escape goes back one level (sub-pages like a settings category swap
        /// in their own navigation layer, hiding the tab strip — Back is the
        /// only way out, and the game maps it to gamepad only).
        /// </summary>
        protected override void HandleInput()
        {
            base.HandleInput();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool backward = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                Navigate(backward ? NavDirection.Up : NavDirection.Down);
            }

            // T: guaranteed way to the tab strip — a page made up entirely of
            // setting rows has no Left/Right route out (they adjust values).
            if (Input.GetKeyDown(KeyCode.T))
            {
                DebugLogger.LogInput(Name, "Jump to tabs");
                var tabs = GetTabItems();
                if (tabs.Count > 0)
                    NavigationHelper.FocusItem(tabs[0]);
                else
                    ScreenReader.Say(Loc.Get("pause_no_tabs"), interrupt: true);
            }

            if (NavigationHelper.IsBackPressed())
            {
                DebugLogger.LogInput(Name, "Back");
                GoBack();
            }
        }

        /// <summary>
        /// Trigger the same back action the gamepad "Back" button fires:
        /// the BackButtonGamePadController for the active layer. Falls back
        /// to closing the menu so Escape always leads out.
        /// </summary>
        private void GoBack()
        {
            // An opened lore page overlays the journal — close it first
            foreach (var manager in Object.FindObjectsOfType<LorePageManager>())
            {
                var focusLayer = ReflectionUtil.GetField<GameObject>(manager, "focusLayer");
                if (focusLayer != null && focusLayer.activeSelf)
                {
                    manager.DisableFocusLayer();
                    ScreenReader.Say(Loc.Get("pause_lore_closed"), interrupt: true);
                    return;
                }
            }

            var activeLayer = UINavigationSystem.ActiveNavigationLayer;
            BackButtonGamePadController match = null;
            BackButtonGamePadController any = null;

            foreach (var controller in Object.FindObjectsOfType<BackButtonGamePadController>())
            {
                if (controller == null) continue;
                if (any == null) any = controller;
                if (controller.uINavigationLayer != null
                    && controller.uINavigationLayer == activeLayer)
                {
                    match = controller;
                    break;
                }
            }

            var back = match ?? any;
            if (back != null)
            {
                if (back.OnBackButtonOverride != null
                    && back.OnBackButtonOverride.GetPersistentEventCount() > 0)
                {
                    back.OnBackButtonOverride.Invoke();
                    ResetFocusTracking();
                    return;
                }
                if (back.backButton != null)
                {
                    back.backButton.onClick.Invoke();
                    ResetFocusTracking();
                    return;
                }
            }

            // No back controller found: close the whole menu
            var menu = Object.FindObjectOfType<PauseMenu>();
            if (menu != null)
            {
                menu.Close();
                ScreenReader.Say(Loc.Get("pause_closed"), interrupt: true);
            }
        }

        protected override bool TryAnnounceScreen()
        {
            if (GetItems().Count == 0)
                return false; // menu still opening / layer not registered yet

            string msg = Loc.Get("screen_pause");
            string hint = HintOnce("pause_hint");
            if (hint != null)
                msg += " " + hint;
            ScreenReader.SayEvent(msg, interrupt: true);
            return true;
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_pause");
        }

        // GetItems: intentionally NOT overridden. The base implementation returns
        // the items on the game's active navigation layer — the same set the
        // game itself allows focus on while the menu is open. Anything else gets
        // cleared by UINavigationSystem.Update on the next frame.
    }
}
