using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Keyboard access to the game's inventory overlay (the Deckpack: deck and
    /// reserve cards, collected charms and crowns). P toggles it from any screen
    /// that has one (map, battle, events). While open: Up/Down switch groups,
    /// Left/Right move within a group, Enter on a charm or crown picks it up
    /// and drives the game's CardCharmDragHandler so arrows walk the cards that
    /// can take it and Enter attaches it; Enter on a card opens the game's own
    /// options menu (rename, take crown, move between deck and reserve);
    /// Escape backs out one level at a time.
    /// The overlay never changes the scene, so this runs inside every
    /// NavigableScreenHandler instead of being a screen handler itself.
    /// </summary>
    public static partial class DeckpackNavigator
    {
        private enum Group { Deck, Reserve, Charms, Crowns, Controls }

        private static bool _wasOpen;
        private static float _openTime;
        private static bool _openAnnounced;
        private static bool _hintSpoken;
        private static Group _group;

        // Scene objects, cached while the pack is open
        private static DeckDisplaySequence _sequence;
        private static CardCharmDragHandler _dragHandler;
        private static DeckSelectSequence _menu;

        // Charm/crown drag state. The display object is destroyed by the game
        // when the upgrade is successfully attached — that's how a drag that
        // ended outside our own key handling is classified.
        private static bool _wasDragging;
        private static UpgradeDisplay _dragDisplay;
        private static string _dragName;
        private static bool _pickupAnnounced;
        private static bool _endAnnounced;

        private static bool _menuWasOpen;

        // A charm just gained from the charm-gain popup (CharmGainNarrator):
        // when the pack opens out of that popup's Assign button, pick the new
        // charm up automatically so arrows go straight to the eligible cards.
        // The holder displays build over several frames, hence the retry window.
        private static CardUpgradeData _autoPickup;
        private static float _autoPickupDeadline;

        /// <summary>True while the inventory overlay is open.</summary>
        public static bool IsOpen
        {
            get
            {
                try
                {
                    return MonoBehaviourSingleton<Deckpack>.instance != null && Deckpack.IsOpen;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Forget everything on screen changes — no announcements.</summary>
        public static void Reset()
        {
            _wasOpen = false;
            _openAnnounced = false;
            _sequence = null;
            _dragHandler = null;
            _menu = null;
            _menuWasOpen = false;
            _autoPickup = null;
            ResetDragState();
        }

        private static void ResetDragState()
        {
            _wasDragging = false;
            _dragDisplay = null;
            _dragName = null;
            _pickupAnnounced = false;
            _endAnnounced = false;
        }

        /// <summary>
        /// Called every frame from NavigableScreenHandler.OnUpdate. Returns true
        /// while the inventory is open — the screen's own input handling must
        /// stay out of the way then.
        /// </summary>
        public static bool RouteInput(NavigableScreenHandler owner)
        {
            // The pause menu overlays the inventory — its handler owns the keys then
            if (GameManager.paused)
                return false;

            bool open = IsOpen;
            if (open != _wasOpen)
            {
                _wasOpen = open;
                if (open) OnOpened();
                else OnClosed(owner);
            }
            if (!open)
                return false;

            if (_autoPickup != null)
                TryAutoPickup(owner);

            // Overview after the game has placed its own initial focus,
            // so the focus announcement doesn't talk over it
            if (!_openAnnounced && Time.unscaledTime - _openTime >= 0.6f)
                AnnounceOverview();

            PollDragTransitions(owner);
            PollMenu(owner);

            // While the rename input has focus, every key belongs to it
            if (NavigationHelper.IsTextInputFocused())
                return true;

            HandleKeys(owner);
            return true;
        }

        private static void OnOpened()
        {
            DebugLogger.LogState("Deckpack", "closed", "open");
            _openTime = Time.unscaledTime;
            _openAnnounced = false;
            _group = Group.Deck;
            _sequence = null;
            _dragHandler = null;
            _menu = null;
            _menuWasOpen = false;
            ResetDragState();
            _autoPickup = CharmGainNarrator.TakePendingCharm();
            _autoPickupDeadline = Time.unscaledTime + 5f;
        }

        private static void OnClosed(NavigableScreenHandler owner)
        {
            DebugLogger.LogState("Deckpack", "open", "closed");
            _autoPickup = null;
            ResetDragState();
            ScreenReader.Say(Loc.Get("deckpack_closed"), interrupt: true);
            // The game often leaves focus in limbo here — put it back on a real
            // item (the first hand card in battle). Stay quiet through the close
            // transition; RequestRefocus announces the item it lands on.
            owner?.SuppressFocusFor(1.5f);
            owner?.RequestRefocus();
        }

        /// <summary>Inventory contents: deck, reserve, charm and crown counts.</summary>
        private static void AnnounceOverview()
        {
            _openAnnounced = true;

            var parts = new List<string> { Loc.Get("deckpack_open") };
            try
            {
                var inventory = References.PlayerData.inventory;
                int deck = inventory.deck?.Count ?? 0;
                int reserve = inventory.reserve?.Count ?? 0;
                int charms = 0, crowns = 0;
                if (inventory.upgrades != null)
                {
                    foreach (var upgrade in inventory.upgrades)
                    {
                        if (upgrade == null) continue;
                        if (upgrade.type == CardUpgradeData.Type.Charm) charms++;
                        else if (upgrade.type == CardUpgradeData.Type.Crown) crowns++;
                    }
                }

                var counts = new List<string> { Loc.Get("deckpack_part_deck", deck) };
                if (reserve > 0)
                    counts.Add(Loc.Get("deckpack_part_reserve", reserve));
                counts.Add(charms == 1
                    ? Loc.Get("deckpack_part_charm_one")
                    : Loc.Get("deckpack_part_charms", charms));
                if (crowns > 0)
                    counts.Add(crowns == 1
                        ? Loc.Get("deckpack_part_crown_one")
                        : Loc.Get("deckpack_part_crowns", crowns));
                parts.Add(string.Join(", ", counts) + ".");
            }
            catch
            {
                // No run inventory to summarize — the group navigation still works
            }

            if (!_hintSpoken)
            {
                _hintSpoken = true;
                parts.Add(Loc.Get("deckpack_hint"));
            }

            ScreenReader.Say(string.Join(" ", parts), interrupt: true);
        }

        // ---- Open / close --------------------------------------------------------

        /// <summary>P pressed on a screen: open the inventory, or close it if open.</summary>
        public static void ToggleInventory()
        {
            if (IsOpen)
            {
                CloseInventory();
                return;
            }

            // Not from inside the pause menu, and not while holding a battle card
            if (GameManager.paused)
            {
                ScreenReader.Say(Loc.Get("deckpack_unavailable"), interrupt: true);
                return;
            }
            try
            {
                var battleController = Battle.instance?.playerCardController;
                if (battleController != null && battleController.dragging != null)
                {
                    ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
                    return;
                }
            }
            catch { /* no battle */ }

            var characterDisplay = FindCharacterDisplay();
            if (characterDisplay == null || characterDisplay.deckDisplay == null
                || characterDisplay.deckDisplay.displaySequence == null
                || MonoBehaviourSingleton<Deckpack>.instance == null)
            {
                ScreenReader.Say(Loc.Get("deckpack_unavailable"), interrupt: true);
                return;
            }

            // The game disables the backpack button during sequences — respect that
            var button = ReflectionUtil.GetField<UnityEngine.UI.Button>(
                MonoBehaviourSingleton<Deckpack>.instance, "button");
            if (button != null && (!button.interactable || !button.gameObject.activeInHierarchy))
            {
                ScreenReader.Say(Loc.Get("deckpack_blocked"), interrupt: true);
                return;
            }

            DebugLogger.LogInput("Deckpack", "Open");
            characterDisplay.OpenInventory();
            // "Inventory open" plus contents comes from the open transition
        }

        private static void CloseInventory()
        {
            var characterDisplay = FindCharacterDisplay();
            if (characterDisplay != null)
            {
                characterDisplay.CloseInventory();
            }
            else if (IsOpen)
            {
                Deckpack.Close();
                GetSequence()?.End();
            }
            // "Inventory closed" comes from the close transition
        }

        // ---- Scene lookups ---------------------------------------------------------

        private static CharacterDisplay FindCharacterDisplay()
        {
            try
            {
                if (References.Player != null && References.Player.entity != null
                    && References.Player.entity.display is CharacterDisplay display
                    && display.deckDisplay != null)
                    return display;
            }
            catch { /* no run */ }

            foreach (var display in Object.FindObjectsOfType<CharacterDisplay>())
            {
                if (display != null && display.deckDisplay != null)
                    return display;
            }
            return null;
        }

        private static DeckDisplaySequence GetSequence()
        {
            if (_sequence == null)
            {
                var characterDisplay = FindCharacterDisplay();
                _sequence = characterDisplay != null && characterDisplay.deckDisplay != null
                    ? characterDisplay.deckDisplay.displaySequence
                    : Object.FindObjectOfType<DeckDisplaySequence>(true);
            }
            return _sequence;
        }

        private static CardCharmDragHandler GetDragHandler()
        {
            if (_dragHandler == null)
            {
                _dragHandler = ReflectionUtil.GetField<CardCharmDragHandler>(GetSequence(), "charmDragHandler")
                    ?? Object.FindObjectOfType<CardCharmDragHandler>();
            }
            return _dragHandler;
        }

        private static Entity FocusedEntity()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            if (item == null) return null;
            Entity entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            return entity;
        }

        private static string UpgradeName(CardUpgradeData data)
        {
            if (data == null) return Loc.Get("upgrade_charm");
            string title;
            try { title = data.title; }
            catch { title = null; }
            return string.IsNullOrEmpty(title) ? ScreenHandler.CleanName(data.name) : title;
        }
    }
}
