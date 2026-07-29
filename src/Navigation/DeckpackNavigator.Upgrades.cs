using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Charm and crown assignment: picking an upgrade up (including the
    /// automatic pickup after the charm-gain popup), walking the cards that can
    /// take it, applying or cancelling, and voicing drags begun elsewhere.
    /// </summary>
    public static partial class DeckpackNavigator
    {
        // ---- Charm/crown assignment -------------------------------------------

        /// <summary>
        /// Pack opened out of the charm-gain popup's Assign button: pick the
        /// new charm up as soon as its display exists so the player lands
        /// directly in "arrows choose a card, Enter equips". Retries every
        /// frame while the holder is still populating, then gives up quietly —
        /// the pack works normally either way.
        /// </summary>
        private static void TryAutoPickup(NavigableScreenHandler owner)
        {
            if (Time.unscaledTime > _autoPickupDeadline)
            {
                _autoPickup = null;
                return;
            }

            var dragHandler = GetDragHandler();
            if (dragHandler == null)
                return;
            if (dragHandler.IsDragging)
            {
                // Something else (the mouse) already started a drag
                _autoPickup = null;
                return;
            }

            var holder = ReflectionUtil.GetField<UpgradeHolder>(GetSequence(), "charmHolder");
            var list = ReflectionUtil.GetField<List<UpgradeDisplay>>(holder, "list");
            if (list == null)
                return;

            UpgradeDisplay display = null;
            foreach (var candidate in list)
            {
                if (candidate == null || candidate.data == null) continue;
                if (candidate.data == _autoPickup)
                {
                    display = candidate;
                    break;
                }
                // The clone added to the inventory is normally the displayed
                // instance too; the name match is a safety net
                if (display == null && candidate.data.name == _autoPickup.name)
                    display = candidate;
            }
            if (display == null)
                return;

            DebugLogger.Log(DebugLogger.LogCategory.Handler, "Deckpack",
                $"Auto pickup gained charm: {_autoPickup.name}");
            _autoPickup = null;
            // The pickup instructions replace the overview announcement
            _openAnnounced = true;
            _group = Group.Charms;
            PickUp(display, owner);
        }

        private static void PickUp(UpgradeDisplay upgrade, NavigableScreenHandler owner)
        {
            var interaction = upgrade.GetComponent<CardCharmInteraction>();
            var dragHandler = interaction != null && interaction.dragHandler != null
                ? interaction.dragHandler
                : GetDragHandler();
            if (dragHandler == null || dragHandler.IsDragging)
                return;

            // The game refuses charm drags mid-battle — its only feedback is a sound
            if (References.Battle != null && !References.Battle.ended
                && !ReflectionUtil.GetBoolField(dragHandler, "canDragMidBattle", true))
            {
                ScreenReader.Say(Loc.Get("deckpack_battle_blocked"), interrupt: true);
                return;
            }

            // If the game also reads this Enter as its Rewired Select while the
            // charm is hovered, CardCharmInteraction would start a second drag in
            // LateUpdate — disarm it for a frame so exactly one drag begins.
            if (interaction != null)
            {
                interaction.canDrag = false;
                CoroutineManager.Start(RestoreCanDrag(interaction));
            }

            string name = UpgradeName(upgrade.data);
            DebugLogger.LogInput("Deckpack", $"Pick up: {upgrade.data.name}");
            dragHandler.Drag(upgrade);

            var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
            int count = eligible?.Count ?? 0;
            if (count == 0)
            {
                // Don't leave the player inside a drag that can go nowhere
                _endAnnounced = true;
                _wasDragging = true; // the end transition must not re-announce
                dragHandler.CancelDrag();
                ScreenReader.Say(Loc.Get("deckpack_pickup_none", name), interrupt: true);
                return;
            }

            _dragDisplay = upgrade;
            _dragName = name;
            _pickupAnnounced = true;
            ScreenReader.Say(count == 1
                ? Loc.Get("deckpack_pickup_one", name)
                : Loc.Get("deckpack_pickup", name, count), interrupt: true);
            // The game focuses an eligible card on its own — the pickup
            // instructions matter more than that card's name right now
            owner?.SuppressFocusFor(1.5f);
        }

        private static System.Collections.IEnumerator RestoreCanDrag(CardCharmInteraction interaction)
        {
            yield return null;
            yield return null;
            if (interaction != null)
                interaction.canDrag = true;
        }

        /// <summary>All arrows walk the cards that can take the held upgrade.</summary>
        private static void NavigateEligible(CardCharmDragHandler dragHandler, NavDirection dir)
        {
            var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
            var items = new List<UINavigationItem>();
            if (eligible != null)
            {
                foreach (var entity in eligible)
                    AddNavItem(items, entity != null ? entity.uINavigationItem : null);
            }
            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

            bool forward = dir == NavDirection.Right || dir == NavDirection.Down;
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var next = NavigationHelper.NavigateLinear(
                items, navSystem?.currentNavigationItem,
                forward ? NavDirection.Right : NavDirection.Left, vertical: false);
            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        /// <summary>Enter while holding an upgrade: attach it to the focused card.</summary>
        private static void ApplyToFocused(CardCharmDragHandler dragHandler, NavigableScreenHandler owner)
        {
            var dragging = ReflectionUtil.GetField<UpgradeDisplay>(dragHandler, "dragging");
            var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
            Entity entity = FocusedEntity();

            if (dragging == null || entity == null || eligible == null || !eligible.Contains(entity))
            {
                ScreenReader.Say(
                    Loc.Get("deckpack_not_eligible", _dragName ?? Loc.Get("upgrade_charm")),
                    interrupt: true);
                return;
            }

            DebugLogger.LogInput("Deckpack",
                $"Assign {dragging.data?.name} -> {entity.data?.title ?? entity.name}");
            _endAnnounced = true;

            // Keyboard focus never establishes the game's hover — set it directly,
            // Release() assigns to whatever it believes is hovered
            ReflectionUtil.SetField(dragHandler, "hoverEntity", entity);
            dragHandler.Release(dragging);

            ScreenReader.Say(
                Loc.Get("deckpack_applying",
                    _dragName ?? UpgradeName(dragging.data),
                    entity.data?.title ?? ScreenHandler.CleanName(entity.name)),
                interrupt: true);
            owner?.SuppressFocusFor(2f);
            _group = Group.Charms;
        }

        private static void CancelDrag(CardCharmDragHandler dragHandler, NavigableScreenHandler owner)
        {
            string name = _dragName
                ?? UpgradeName(ReflectionUtil.GetField<UpgradeDisplay>(dragHandler, "dragging")?.data);
            DebugLogger.LogInput("Deckpack", "Cancel drag");
            _endAnnounced = true;
            dragHandler.CancelDrag();
            ScreenReader.Say(Loc.Get("deckpack_returned", name), interrupt: true);
            owner?.SuppressFocusFor(1.5f);
        }

        /// <summary>
        /// Watch the drag handler for pickups and drops that didn't come from our
        /// own key handling (mouse or the game's native Select), and voice them.
        /// </summary>
        private static void PollDragTransitions(NavigableScreenHandler owner)
        {
            var dragHandler = GetDragHandler();
            bool dragging = dragHandler != null && dragHandler.IsDragging;
            if (dragging == _wasDragging) return;
            _wasDragging = dragging;

            if (dragging)
            {
                if (_pickupAnnounced) return;
                var display = ReflectionUtil.GetField<UpgradeDisplay>(dragHandler, "dragging");
                _dragDisplay = display;
                _dragName = UpgradeName(display != null ? display.data : null);
                var eligible = ReflectionUtil.GetField<List<Entity>>(dragHandler, "eligibleCards");
                int count = eligible?.Count ?? 0;
                _pickupAnnounced = true;
                ScreenReader.Say(count == 1
                    ? Loc.Get("deckpack_pickup_one", _dragName)
                    : Loc.Get("deckpack_pickup", _dragName, count), interrupt: true);
                owner?.SuppressFocusFor(1.5f);
            }
            else
            {
                if (!_endAnnounced && _dragName != null)
                {
                    // The game destroys the display when the upgrade was attached;
                    // a surviving display means it went back to the holder.
                    // Recorded as an event: "which charm went where" is worth
                    // replaying with Ctrl+Up.
                    ScreenReader.SayEvent(_dragDisplay == null
                        ? Loc.Get("deckpack_applied", _dragName)
                        : Loc.Get("deckpack_returned", _dragName), interrupt: true);
                }
                ResetDragState();
            }
        }

    }
}
