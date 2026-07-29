using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// What the deckpack contributes to ItemDescriber: the card menu's buttons
    /// read as their role names, and while an upgrade is held the eligible
    /// cards read as assignment targets (charm slots first).
    /// </summary>
    public static partial class DeckpackNavigator
    {
        /// <summary>
        /// Deckpack-specific item descriptions, hooked into ItemDescriber:
        /// menu buttons get their role names, and while an upgrade is held the
        /// eligible cards read as assignment targets (charm slots first).
        /// Returns null when the deckpack has nothing special to say.
        /// </summary>
        public static string DescribeItem(UINavigationItem item, ScreenHandler owner)
        {
            if (item == null || !IsOpen) return null;

            var menu = _menuWasOpen ? ActiveMenu() : null;
            if (menu != null
                && (item.GetComponentInParent<DeckSelectSequence>() != null
                    || (item.clickHandler != null
                        && item.clickHandler.GetComponentInParent<DeckSelectSequence>() != null))
                && item.GetComponentInParent<Entity>() == null)
            {
                return MenuLabel(menu, item, owner);
            }

            var dragHandler = _dragHandler;
            if (dragHandler != null && dragHandler.IsDragging)
            {
                Entity entity = item.GetComponentInParent<Entity>();
                if (entity == null && item.clickHandler != null)
                    entity = item.clickHandler.GetComponentInParent<Entity>();
                if (entity != null && entity.data != null)
                    return DescribeAssignTarget(entity);
            }

            return null;
        }

        /// <summary>"Snow Fox, 1 of 3 charm slots used, Charm: ..." — what matters when placing.</summary>
        private static string DescribeAssignTarget(Entity entity)
        {
            var parts = new List<string> { entity.data.title };

            // Slot usage — only meaningful for charms (crowns have their own slot)
            var dragData = _dragDisplay != null ? _dragDisplay.data : null;
            if (dragData == null || dragData.type == CardUpgradeData.Type.Charm)
            {
                try
                {
                    int total = entity.data.charmSlots;
                    try { total += entity.data.customData?.Get("extraCharmSlots", 0) ?? 0; }
                    catch { /* cards without custom data */ }
                    int used = 0;
                    foreach (var upgrade in entity.data.upgrades)
                    {
                        if (upgrade != null && upgrade.type == CardUpgradeData.Type.Charm
                            && upgrade.takeSlot)
                            used++;
                    }
                    parts.Add(Loc.Get("deckpack_target_slots", used, total));
                }
                catch { /* slots stay unspoken if the data is unreadable */ }
            }

            string upgrades = ItemDescriber.DescribeUpgrades(entity.data);
            if (!string.IsNullOrEmpty(upgrades))
                parts.Add(upgrades);

            return string.Join(", ", parts);
        }

    }
}
