using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Picking up and placing cards: targeting state, the target-aware
    /// focus description, drop-validity checks and the Confirm action.
    /// </summary>
    public partial class BattleHandler
    {
        // ---- Playing cards ---------------------------------------------------

        /// <summary>Is a card currently picked up (targeting mode)?</summary>
        private bool IsTargeting()
        {
            var controller = Battle.instance?.playerCardController;
            return controller != null && controller.dragging != null;
        }

        /// <summary>
        /// While the game resolves actions (enemy turn, a played card), it moves focus
        /// around on its own; announcing those changes would talk over combat narration.
        /// </summary>
        protected override bool SuppressFocusAnnouncements
        {
            get
            {
                var battle = Battle.instance;
                if (battle == null) return false;
                if (battle.phase == Battle.Phase.Battle) return true;
                try { return !ActionQueue.Empty; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Browsing reads the card itself; only while holding a card do slot
        /// positions matter, so the side, row, slot prefix is targeting-only.
        /// </summary>
        protected override string GetItemDescription(UINavigationItem item)
        {
            if (IsTargeting())
            {
                // The game moves focus on its own too (its default-item system),
                // and then nothing has mirrored the mouse's hover onto it yet —
                // so the drop target is armed before it is described
                NavigationHelper.MirrorCardHoverToFocus(item);

                string target = ItemDescriber.DescribeTarget(item);
                if (string.IsNullOrEmpty(target))
                    target = base.GetItemDescription(item);
                // Naming a cell the held card cannot be played on, with no hint
                // that Enter will not take it, is how a card ends up somewhere
                // the player never chose
                if (!string.IsNullOrEmpty(target) && !TargetAccepted(item))
                    target += " " + Loc.Get("battle_not_a_target");
                return target;
            }
            return base.GetItemDescription(item);
        }

        /// <summary>
        /// Whether releasing the held card here would actually play it.
        /// CardControllerBattle.Release plays onto its hoverEntity / hoverSlot /
        /// hoverContainer, so the drop target having followed our focus is most
        /// of the answer — the game refuses to move hoverEntity or hoverSlot onto
        /// anything the held card cannot take, so those two are self-checking.
        /// Containers are not: HoverContainer accepts any lane it is handed, so a
        /// lane still has to be put to the game's own CanPlayOn.
        ///
        /// Focus is free to sit on a cell that is no target at all — browsing
        /// there still reads out what stands in it — but the readout has to say
        /// so, or a card ends up somewhere the player never chose.
        /// </summary>
        private bool TargetAccepted(UINavigationItem item)
        {
            var controller = Battle.instance?.playerCardController;
            Entity held = controller?.dragging;
            if (held == null || item == null) return true;

            // A card that needs no target plays wherever it is released (bar its
            // own container), so no cell it can be browsed onto is a wrong one
            try
            {
                if (held.data != null && held.data.playType == Card.PlayType.Play
                    && !held.NeedsTarget)
                    return true;
            }
            catch { /* data not ready */ }

            GameObject handler = item.clickHandler != null ? item.clickHandler : item.gameObject;
            if (handler == null) return true;

            var entity = handler.GetComponentInParent<Entity>();
            if (entity != null && controller.hoverEntity == entity) return true;

            CardSlot slot = GetTargetSlot(item);
            if (slot != null && controller.hoverSlot == slot) return true;

            // Lanes and the recall zone hover unconditionally
            var container = handler.GetComponentInParent<CardContainer>();
            if (container != null && controller.hoverContainer == container)
                return CanReleaseOn(held, container);

            return false;
        }

        /// <summary>
        /// The game's verdict on releasing a held card onto a container: the
        /// recall zone takes anything recallable, everything else answers
        /// through Entity.CanPlayOn — the same call Release makes.
        /// </summary>
        private static bool CanReleaseOn(Entity held, CardContainer container)
        {
            try
            {
                var player = Battle.instance?.player;
                if (player != null && container == player.discardContainer)
                    return container.canBePlacedOn && held.owner == player && held.CanRecall();
                return held.CanPlayOn(container);
            }
            catch
            {
                return true; // never invent a warning we cannot stand behind
            }
        }

        /// <summary>
        /// Enter: pick up the focused hand card, or place the held card on the
        /// focused target. Falls back to a regular click for buttons/bell.
        /// </summary>
        protected override void Confirm()
        {
            var battle = Battle.instance;
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;

            // Last stand: the Roll button belongs to no navigation layer, so
            // while the game waits for it, Enter rolls the dice directly.
            if (battle != null && battle.phase == Battle.Phase.LastStand)
            {
                var lastStand = Object.FindObjectOfType<LastStandSystem>();
                var rollButton = ReflectionUtil.GetField<GameObject>(lastStand, "button");
                if (rollButton != null && rollButton.activeInHierarchy)
                {
                    DebugLogger.LogInput(Name, "Last stand roll");
                    lastStand.Roll();
                    ScreenReader.Say(Loc.Get("battle_last_stand_rolling"), interrupt: true);
                    return;
                }
            }

            if (battle == null || current == null)
            {
                base.Confirm();
                return;
            }

            var controller = battle.playerCardController;

            // Holding a card: release it on the focused target
            if (controller != null && controller.dragging != null)
            {
                DebugLogger.LogInput(Name, "Place card");
                Entity held = controller.dragging;
                string title = held?.data?.title ?? "";

                // Captured before Release: they decide what the release means
                bool fromBoard = held != null && Battle.IsOnBoard(held);
                bool toRecall = controller.hoverContainer != null
                    && controller.hoverContainer == battle.player?.discardContainer;
                bool busyBefore = !IsActionQueueEmpty();

                if (ReflectionUtil.InvokeMethod(controller, "Release"))
                {
                    // Every successful release queues actions (move/play + end turn);
                    // an invalid target queues nothing and the card snaps back.
                    bool acted = !busyBefore && !IsActionQueueEmpty();

                    if (acted && toRecall)
                    {
                        string msg = Loc.Get("battle_unit_recalled", title);
                        if (fromBoard)
                            msg += " " + Loc.Get("battle_free_action");
                        ScreenReader.SayEvent(msg);
                    }
                    else if (acted && fromBoard)
                    {
                        // Repositioning a board unit is free — the turn continues
                        ScreenReader.SayEvent(Loc.Get("battle_unit_moved", title)
                            + " " + Loc.Get("battle_free_action"));
                    }
                    else if (acted)
                    {
                        ScreenReader.SayEvent(Loc.Get("battle_card_released", title));
                    }
                    else
                    {
                        ScreenReader.Say(Loc.Get("battle_invalid_target"));
                    }
                }
                return;
            }

            // Focused item is one of our own cards (in hand, or a unit on the
            // board — moving, swapping and recalling units is a free action)
            Entity entity = GetEntityFromItem(current);
            if (entity != null && controller != null && entity.owner == battle.player
                && (entity.InHand() || Battle.IsOnBoard(entity)))
            {
                DebugLogger.LogInput(Name, "Pick up card");
                bool onBoard = Battle.IsOnBoard(entity);
                controller.hoverEntity = entity;
                if (ReflectionUtil.SetField(controller, "pressEntity", entity)
                    && ReflectionUtil.InvokeMethod(controller, "Press")
                    && controller.dragging != null)
                {
                    string msg = Loc.Get(
                        onBoard ? "battle_unit_picked_up" : "battle_card_picked_up",
                        entity.data?.title ?? "");
                    string hint = HintOnce(onBoard ? "battle_move_hint" : "battle_pickup_hint");
                    if (hint != null)
                        msg += " " + hint;
                    ScreenReader.Say(msg);
                }
                else
                {
                    ScreenReader.Say(Loc.Get(onBoard ? "battle_cannot_move" : "battle_cannot_play"));
                }
                return;
            }

            // Redraw bell: call the game API directly
            if (current == RedrawBellSystem.nav)
            {
                var bell = Object.FindObjectOfType<RedrawBellSystem>();
                if (bell != null && bell.interactable)
                {
                    DebugLogger.LogInput(Name, "Ring bell");
                    bell.Activate();
                }
                else
                {
                    ScreenReader.Say(Loc.Get("battle_bell_not_ready"));
                }
                return;
            }

            base.Confirm();
        }

        private static Entity GetEntityFromItem(UINavigationItem item)
        {
            var entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            return entity;
        }

        private static bool IsActionQueueEmpty()
        {
            try { return ActionQueue.Empty; }
            catch { return true; }
        }

        /// <summary>How many cards in the player's hand carry a crown.</summary>
        private static int CountCrownedInHand(Battle battle)
        {
            var hand = battle?.player?.handContainer;
            if (hand == null) return 0;

            int count = 0;
            foreach (Entity entity in hand)
            {
                if (entity?.data != null && entity.data.HasCrown)
                    count++;
            }
            return count;
        }

    }
}
