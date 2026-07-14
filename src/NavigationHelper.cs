using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Shared keyboard navigation utilities.
    /// Provides arrow key input that works with the game's UINavigationSystem.
    /// </summary>
    public static class NavigationHelper
    {
        private static float _lastNavTime;
        private static float _navRepeatDelay = 0.3f;
        private static float _navRepeatRate = 0.1f;
        private static bool _navHeld;

        /// <summary>
        /// Check for arrow key navigation input.
        /// Returns the navigation direction pressed, or None.
        /// Handles initial press and hold-to-repeat.
        /// </summary>
        public static NavDirection GetNavigationInput()
        {
            NavDirection dir = NavDirection.None;

            if (Input.GetKey(KeyCode.UpArrow)) dir = NavDirection.Up;
            else if (Input.GetKey(KeyCode.DownArrow)) dir = NavDirection.Down;
            else if (Input.GetKey(KeyCode.LeftArrow)) dir = NavDirection.Left;
            else if (Input.GetKey(KeyCode.RightArrow)) dir = NavDirection.Right;

            if (dir == NavDirection.None)
            {
                _navHeld = false;
                return NavDirection.None;
            }

            float now = Time.unscaledTime;

            // First press
            if (!_navHeld)
            {
                _navHeld = true;
                _lastNavTime = now;
                return dir;
            }

            // Hold-to-repeat
            float delay = (now - _lastNavTime > _navRepeatDelay + _navRepeatRate)
                ? _navRepeatRate
                : _navRepeatDelay;

            if (now - _lastNavTime >= delay)
            {
                _lastNavTime = now;
                return dir;
            }

            return NavDirection.None;
        }

        /// <summary>
        /// Check if Enter/Return was pressed this frame (confirm/activate).
        /// </summary>
        public static bool IsConfirmPressed()
        {
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        /// <summary>
        /// Check if Escape was pressed this frame (back/cancel).
        /// Note: Only use in contexts where we won't conflict with game's Escape handling.
        /// </summary>
        public static bool IsBackPressed()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }

        /// <summary>
        /// True while a text input field has focus (console, run naming).
        /// Letter-key mod bindings must stay inactive then.
        /// </summary>
        public static bool IsTextInputFocused()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == null) return false;
            return selected.GetComponent<TMPro.TMP_InputField>() != null
                || selected.GetComponent<UnityEngine.UI.InputField>() != null;
        }

        /// <summary>
        /// Get all active UINavigationItems in the current layer, sorted spatially.
        /// Filters to items on the active navigation layer that are selectable.
        /// </summary>
        public static List<UINavigationItem> GetNavigableItems()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return new List<UINavigationItem>();

            var activeLayer = UINavigationSystem.ActiveNavigationLayer;
            var items = new List<UINavigationItem>();

            foreach (var item in navSystem.AvailableNavigationItems)
            {
                if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                    continue;

                // Match the layer check logic from CheckLayer (which is internal):
                // Item is valid if it ignores layers OR is on the active layer
                if (!item.ignoreLayers && item.navigationLayer != activeLayer)
                    continue;

                // Skip HelpPanelShower items — help is accessible via F1,
                // and these often have ignoreLayers=true which pollutes navigation
                if (IsHelpItem(item))
                    continue;

                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Navigate to the next/previous item in a list based on direction.
        /// For vertical menus: Up = previous, Down = next.
        /// For horizontal menus: Left = previous, Right = next.
        /// </summary>
        public static UINavigationItem NavigateLinear(
            List<UINavigationItem> items, UINavigationItem current, NavDirection dir, bool vertical = true)
        {
            if (items == null || items.Count == 0) return null;

            int currentIndex = current != null ? items.IndexOf(current) : -1;

            bool forward = vertical ? (dir == NavDirection.Down) : (dir == NavDirection.Right);
            bool backward = vertical ? (dir == NavDirection.Up) : (dir == NavDirection.Left);

            if (!forward && !backward) return current;

            int newIndex;
            if (currentIndex < 0)
            {
                // Nothing selected, pick first or last
                newIndex = forward ? 0 : items.Count - 1;
            }
            else
            {
                newIndex = forward ? currentIndex + 1 : currentIndex - 1;
                // Wrap around
                if (newIndex >= items.Count) newIndex = 0;
                if (newIndex < 0) newIndex = items.Count - 1;
            }

            return items[newIndex];
        }

        /// <summary>
        /// Force the game's navigation system to focus on a specific item.
        /// Also positions the virtual cursor on it.
        /// </summary>
        public static void FocusItem(UINavigationItem item)
        {
            if (item == null) return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            // Switch to controller mode so navigation is active
            EnsureControllerMode();

            navSystem.SetCurrentNavigationItem(item);
            SyncHoverToFocus();
        }

        /// <summary>
        /// Force the game's hover (CustomEventSystem.current) onto the focused item.
        /// SetCurrentNavigationItem only hovers when the active layer has forceHover,
        /// so inside layers without it (the pause journal) the hover goes stale — and
        /// the game clicks its HOVERED object when Rewired "Select" (Enter) fires.
        /// A stale hover then clicks UI behind the open menu: this is what loaded
        /// the Credits screen from inside the pause menu. Idempotent; call freely.
        /// </summary>
        public static void SyncHoverToFocus()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem != null ? navSystem.currentNavigationItem : null;
            if (item == null) return;

            var eventSystem = ReflectionUtil.GetField<CustomEventSystem>(navSystem, "eventSystem");
            if (eventSystem == null) return;

            var hovered = ReflectionUtil.GetField<GameObject>(eventSystem, "current");
            if (item.clickHandler != null)
            {
                if (hovered != item.clickHandler)
                    eventSystem.Hover(item.clickHandler);
            }
            else if (hovered != null)
            {
                // No click handler to hover: clear the stale hover so the game's
                // Select cannot click a leftover object (it opened the wrong
                // settings page when Enter hit a previously hovered tab).
                eventSystem.Unhover(hovered);
            }

            ClearUnitySelection();
        }

        /// <summary>
        /// Clear the game's hover entirely, so a game-side Select (Enter)
        /// cannot click anything. Used while browsing virtual rows.
        /// </summary>
        public static void ClearHover()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;
            var eventSystem = ReflectionUtil.GetField<CustomEventSystem>(navSystem, "eventSystem");
            if (eventSystem == null) return;
            var hovered = ReflectionUtil.GetField<GameObject>(eventSystem, "current");
            if (hovered != null)
                eventSystem.Unhover(hovered);
            ClearUnitySelection();
        }

        /// <summary>
        /// Disarm Unity's uGUI "Submit": StandaloneInputModule fires Submit on
        /// Enter at the EventSystem's SELECTED object, and a Button stays
        /// selected forever after any pointer-down — so every later Enter
        /// re-clicked an old button invisibly (loaded Credits/Mods from inside
        /// the pause menu). We never use uGUI selection for navigation; keep it
        /// empty except while a text field is being edited.
        /// </summary>
        public static void ClearUnitySelection()
        {
            var unityEventSystem = EventSystem.current;
            var selected = unityEventSystem != null ? unityEventSystem.currentSelectedGameObject : null;
            if (selected == null) return;
            if (selected.GetComponent<TMPro.TMP_InputField>() != null
                || selected.GetComponent<UnityEngine.UI.InputField>() != null)
                return;
            unityEventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Debug: log every input path that could react to this Enter press —
        /// Rewired Select/Back, our nav focus, the game's hover, and Unity's
        /// uGUI selection. Identifies which system performs phantom clicks.
        /// </summary>
        public static void LogEnterDiagnostic()
        {
            bool select = false, back = false;
            try
            {
                select = InputSystem.IsSelectPressed();
                back = InputSystem.IsButtonPressed("Back");
            }
            catch { /* Rewired not ready */ }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem != null ? navSystem.currentNavigationItem : null;

            GameObject hover = null;
            var gameEventSystem = ReflectionUtil.GetField<CustomEventSystem>(navSystem, "eventSystem");
            if (gameEventSystem != null)
                hover = ReflectionUtil.GetField<GameObject>(gameEventSystem, "current");

            var unitySelected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;

            DebugLogger.Log(DebugLogger.LogCategory.State, "EnterDiag",
                $"rewiredSelect={select} rewiredBack={back}"
                + $" | navFocus={(current != null ? GetPath(current.transform) : "null")}"
                + $" | gameHover={(hover != null ? GetPath(hover.transform) : "null")}"
                + $" | unitySelected={(unitySelected != null ? GetPath(unitySelected.transform) : "null")}");
        }

        /// <summary>
        /// Simulate a click/press on the currently focused navigation item.
        /// Uses ExecuteEvents directly since CustomEventSystem.Press is private.
        /// </summary>
        public static void ActivateCurrent()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            var current = navSystem.currentNavigationItem;
            if (current == null) return;

            // Cards first: CardHover implements only pointer enter/exit, so the
            // pointer-event path below cannot press a card entity.
            if (TryPressSelectCard(current)) return;

            if (current.clickHandler == null) return;

            PressObject(current.clickHandler);
        }

        /// <summary>
        /// Simulate a full pointer press on a click handler: down now, up + click
        /// next frame. Also used for buttons that have no UINavigationItem at all
        /// (the BattleWin Continue button — that screen expects the free-moving
        /// controller cursor, which a blind player never aims).
        /// </summary>
        public static void PressObject(GameObject clickHandler)
        {
            if (clickHandler == null) return;

            var pointerData = new PointerEventData(EventSystem.current);

            // Simulate full click sequence: down -> up -> click
            ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerDownHandler);
            CoroutineManager.Start(ReleaseNextFrame(clickHandler, pointerData));
        }

        private static System.Collections.IEnumerator ReleaseNextFrame(GameObject clickHandler, PointerEventData pointerData)
        {
            yield return null;
            ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(clickHandler, pointerData, ExecuteEvents.pointerClickHandler);
            // The pointer-down made this Button the uGUI "selected" object;
            // left armed, every later Enter would re-click it via Unity Submit
            ClearUnitySelection();
        }

        /// <summary>
        /// Press a focused card through its CardControllerSelectCard (leader,
        /// pet and card-reward choices). The game presses the HOVERED entity
        /// when Rewired "Select" fires, but keyboard focus never establishes
        /// that hover, so Enter on a card died in two dead ends: the game saw
        /// hoverEntity == null, and our pointer events hit CardHover, which
        /// handles no pointer-down/click. Mirrors CardController.Update:
        /// press now, release next frame — Release() is what fires pressEvent.
        /// </summary>
        private static bool TryPressSelectCard(UINavigationItem item)
        {
            Entity entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            if (entity == null || entity.display == null || entity.display.hover == null)
                return false;

            var controller = entity.display.hover.controller as CardControllerSelectCard;
            if (controller == null || !controller.enabled || !controller.canPress)
                return false;
            if (entity.flipper != null && entity.flipper.flipped)
                return false;

            DebugLogger.LogInput("NavigationHelper",
                $"Select-card press: {entity.data?.title ?? entity.name}");

            controller.hoverEntity = entity;
            if (!ReflectionUtil.SetField(controller, "pressEntity", entity))
                return false;
            ReflectionUtil.InvokeMethod(controller, "Press");
            CoroutineManager.Start(ReleaseSelectCardNextFrame(controller, entity));
            return true;
        }

        private static System.Collections.IEnumerator ReleaseSelectCardNextFrame(
            CardControllerSelectCard controller, Entity entity)
        {
            yield return null;
            if (controller == null || !controller.enabled) yield break;
            // If Enter is also bound to Rewired "Select", the game's own polling
            // may have released already (pressEntity gone) — don't fire twice.
            if (ReflectionUtil.GetField<Entity>(controller, "pressEntity") != entity)
                yield break;
            // Release() only fires pressEvent while the hover still matches
            controller.hoverEntity = entity;
            ReflectionUtil.InvokeMethod(controller, "Release");
            ReflectionUtil.SetField(controller, "pressEntity", null);
        }

        /// <summary>
        /// Check if an item is a help panel trigger (HelpPanelShower).
        /// These are excluded from navigation since help is on F1.
        /// </summary>
        private static bool IsHelpItem(UINavigationItem item)
        {
            GameObject obj = item.clickHandler ?? item.gameObject;
            return IsUnderHelpShower(obj) || IsUnderHelpShower(item.gameObject);
        }

        /// <summary>
        /// True when the nearest HelpPanelShower above this object marks a help
        /// button. Some screens (BattleWin) put a HelpPanelShower on the root
        /// canvas that also hosts the navigation layer — that one describes the
        /// whole screen, and treating it as a help button filtered out every
        /// item on the victory screen, including Continue.
        /// </summary>
        private static bool IsUnderHelpShower(GameObject obj)
        {
            if (obj == null) return false;
            var shower = obj.GetComponentInParent<HelpPanelShower>();
            if (shower == null) return false;
            return shower.GetComponent<UINavigationLayer>() == null
                && shower.GetComponent<Canvas>() == null;
        }

        /// <summary>
        /// Dump the full navigation state to the debug log: active layer, current
        /// item, and every registered item with its layer, flags, and notable
        /// components. For diagnosing screens whose items aren't where we expect.
        /// </summary>
        public static void DumpNavigationState()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump", "No UINavigationSystem");
                return;
            }

            var layer = UINavigationSystem.ActiveNavigationLayer;
            DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                $"ActiveLayer: {(layer != null ? GetPath(layer.transform) + "#" + layer.GetInstanceID() : "null")}");
            var current = navSystem.currentNavigationItem;
            DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                $"Current: {(current != null ? GetPath(current.transform) : "null")}");
            DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                $"Registered items: {navSystem.AvailableNavigationItems.Count}");

            foreach (var item in navSystem.AvailableNavigationItems)
            {
                if (item == null) continue;

                var flags = new List<string>();
                if (!item.enabled) flags.Add("disabled");
                if (!item.isSelectable) flags.Add("notSelectable");
                if (!item.gameObject.activeInHierarchy) flags.Add("inactive");
                if (item.ignoreLayers) flags.Add("ignoreLayers");
                if (item.overrideHorizontal) flags.Add("overrideH");
                if (item.overrideVertical) flags.Add("overrideV");
                if (item.clickHandler == null) flags.Add("noClick");

                var comps = new List<string>();
                var journalTab = item.GetComponentInParent<JournalTab>();
                if (journalTab != null) comps.Add("JournalTab:" + journalTab.gameObject.name);
                if (item.GetComponentInParent<SettingOptions>() != null) comps.Add("SettingOptions");
                if (item.GetComponentInParent<SettingSlider>() != null) comps.Add("SettingSlider");
                if (item.GetComponentInChildren<TMPro.TMP_Dropdown>(true) != null) comps.Add("Dropdown");
                if (item.GetComponentInChildren<UnityEngine.UI.Slider>(true) != null) comps.Add("Slider");

                DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                    $"{GetPath(item.transform)}"
                    + $" | layer={(item.navigationLayer != null ? item.navigationLayer.name + "#" + item.navigationLayer.GetInstanceID() : "null")}"
                    + (flags.Count > 0 ? " | " + string.Join(",", flags) : "")
                    + (comps.Count > 0 ? " | " + string.Join(",", comps) : ""));
            }
        }

        /// <summary>Hierarchy path of a transform, up to 6 ancestors deep.</summary>
        private static string GetPath(Transform t)
        {
            var parts = new List<string>();
            int depth = 0;
            while (t != null && depth < 6)
            {
                parts.Insert(0, t.name);
                t = t.parent;
                depth++;
            }
            if (t != null) parts.Insert(0, "...");
            return string.Join("/", parts);
        }

        /// <summary>
        /// Ensure the game thinks we're in controller/gamepad mode,
        /// so UINavigationSystem processes navigation instead of clearing it.
        /// </summary>
        public static void EnsureControllerMode()
        {
            var cursor = Cursor3d.instance;
            if (cursor != null && cursor.usingMouse)
            {
                cursor.usingMouse = false;
                cursor.usingTouch = false;
                VirtualPointer.Show();
            }
        }
    }

    /// <summary>Navigation direction for arrow key input.</summary>
    public enum NavDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}
