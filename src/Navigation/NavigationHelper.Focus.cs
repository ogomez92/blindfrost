using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Moving focus and keeping the game's own hover state in sync with it,
    /// including the card-drag slot/container mirroring and hover clearing.
    /// </summary>
    public static partial class NavigationHelper
    {
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
                {
                    // Unhover the old object FIRST. CustomEventSystem.Hover only
                    // fires pointerEnter on the new object — it never fires
                    // pointerExit on the one being left, so the old object's
                    // Hover state lives on. That matters while holding a card:
                    // the game plays onto CardController.hoverEntity/hoverSlot,
                    // and it silently refuses to move those onto anything the
                    // held card cannot take. Focus would walk on and be
                    // announced while the PREVIOUS unit stayed armed, so Enter
                    // played the card at a target the player never heard.
                    // Clearing first makes a refused hover fail closed: the
                    // card returns to hand instead of hitting the wrong unit.
                    if (hovered != null)
                        eventSystem.Unhover(hovered);
                    eventSystem.Hover(item.clickHandler);
                }
            }
            else if (hovered != null)
            {
                // No click handler to hover: clear the stale hover so the game's
                // Select cannot click a leftover object (it opened the wrong
                // settings page when Enter hit a previously hovered tab).
                eventSystem.Unhover(hovered);
            }

            MirrorCardHoverToFocus(item);
            ClearUnitySelection();
        }

        /// <summary>
        /// Hover the containers a mouse would have hovered, while a card is held.
        ///
        /// The game's own pointer is Hover3dSystem: it raycasts the cursor and
        /// hovers EVERY collider the ray passes through, so a mouse resting on a
        /// unit that stands in a slot hovers the unit, its slot AND its lane at
        /// once. Our focus goes through CustomEventSystem.Hover, whose
        /// ExecuteHierarchy stops at the first handler it finds — the unit — so
        /// the slot and lane underneath were never hovered.
        ///
        /// CardControllerBattle.Release plays onto hoverSlot / hoverContainer, so
        /// a card that lands on a slot (a summon such as Junjun Mask) could not be
        /// played onto an occupied slot at all: Enter did nothing and the readout
        /// called every occupied slot "not a valid target". Row-target cards had
        /// the same hole whenever focus sat on a unit rather than on bare ground.
        ///
        /// The game's own gates still apply — HoverSlot refuses a slot the held
        /// card cannot play on — so nothing here makes an illegal target legal.
        /// Both are cleared first so a refused hover fails closed rather than
        /// leaving the previously focused slot armed.
        /// </summary>
        public static void MirrorCardHoverToFocus(UINavigationItem item)
        {
            if (item == null) return;

            CardController controller;
            try { controller = Battle.instance?.playerCardController; }
            catch { return; }
            if (controller == null || controller.dragging == null) return;

            GameObject handler = item.clickHandler != null ? item.clickHandler : item.gameObject;
            if (handler == null) return;

            CardSlot slot = handler.GetComponent<CardSlot>() ?? handler.GetComponentInParent<CardSlot>();
            CardContainer group = slot != null
                ? slot.Group
                : handler.GetComponentInParent<CardContainer>();

            // Already matching: re-hovering would fire the game's hover events
            // again every frame for no change
            if (controller.hoverSlot == slot
                && controller.hoverContainer == (group != slot ? group : null))
                return;

            controller.UnHoverSlot();
            controller.UnHoverContainer();

            if (slot != null)
                slot.ForceHover();
            if (group != null && group != slot)
                group.Hover();
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
    }
}
