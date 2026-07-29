using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Activating the focused item: simulated pointer presses and the
    /// select-card press/release path.
    /// </summary>
    public static partial class NavigationHelper
    {
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

            // The game may refuse the selection (tutorial gates block choosing
            // a companion until a card has been inspected) — and its only
            // refusal feedback is a visual prompt shake. Ask first and voice
            // the refusal instead of pressing into silence.
            bool allowed = true;
            try { allowed = Events.CheckAction(new ActionSelect(entity, delegate { })); }
            catch { /* no listeners / event system not ready */ }
            if (!allowed)
            {
                string reason = PopupReader.ActivePromptText();
                ScreenReader.Say(
                    string.IsNullOrEmpty(reason)
                        ? Loc.Get("select_blocked")
                        : Loc.Get("select_blocked_reason", reason),
                    interrupt: true);
                DebugLogger.LogInput("NavigationHelper",
                    $"Select-card blocked: {entity.data?.title ?? entity.name}");
                return true; // handled — the refusal was spoken
            }

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
    }
}
