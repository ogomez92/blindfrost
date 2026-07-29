using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Shop economics: the price suffix appended to every focused ware, whether
    /// its tag is still live, and the player's purse.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>
        /// Append the focused item's shop price and affordability when it is a
        /// priced ware. Every shop type (gnome, charm, clunk, the main merchant)
        /// tags its cards, shelf charms, the charm machine and the crown holder
        /// with a <see cref="ShopItem"/>; nothing outside a shop carries one, so
        /// this stays silent everywhere else.
        /// </summary>
        private static string WithShopPrice(UINavigationItem item, string desc)
        {
            string price = ShopPriceSuffix(item);
            if (string.IsNullOrEmpty(price))
                return desc;
            return string.IsNullOrEmpty(desc) ? price : desc + ". " + price;
        }

        /// <summary>
        /// The price tag for a focused shop ware: "Costs 50 gold" plus the
        /// player's current gold and whether it covers the price. Returns null
        /// when the item is not a shop ware or its price tag is no longer live
        /// (a purchased item's ShopPrice is destroyed, but the ShopItem lingers).
        /// </summary>
        private static string ShopPriceSuffix(UINavigationItem item)
        {
            // Parent-only: a ware's ShopItem sits on the card/display root the
            // nav item descends from. A child search could pin one card's price
            // onto a container that happens to hold several wares.
            var shopItem = item.GetComponentInParent<ShopItem>();
            if (shopItem == null && item.clickHandler != null)
                shopItem = item.clickHandler.GetComponentInParent<ShopItem>();
            if (shopItem == null)
                return null;

            // Only announce a price that is still on the shelf. When a ware is
            // bought the game destroys its ShopPrice (ShopPriceManager.Remove)
            // but leaves the ShopItem on the pooled card — without this check a
            // sold-out crown holder would still quote its old price.
            bool live = false;
            foreach (var p in Object.FindObjectsOfType<ShopPrice>())
            {
                if (p != null && p.target == shopItem) { live = true; break; }
            }
            if (!live)
                return null;

            int price = shopItem.GetPrice();
            if (price <= 0)
                return null;

            int gold;
            if (!TryGetPlayerGold(out gold))
                return Loc.Get("shop_price", price);

            return gold >= price
                ? Loc.Get("shop_price_afford", price, gold)
                : Loc.Get("shop_price_cant_afford", price, gold);
        }

        /// <summary>The player's current gold (Bling), or false when unavailable.</summary>
        public static bool TryGetPlayerGold(out int gold)
        {
            gold = 0;
            try
            {
                gold = References.Player.data.inventory.gold.Value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
