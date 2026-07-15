using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the campaign map (scenes "Campaign" and "MapNew").
    /// Left/Right walk the journey path node by node; Up/Down reach the HUD
    /// (deck pockets, displays). Announces each location with its state,
    /// M reads a full map overview, I reads details of the focused location.
    /// </summary>
    public class MapHandler : NavigableScreenHandler
    {
        public override string Name => "Map";

        protected override bool TryAnnounceScreen()
        {
            // The "Campaign" scene is just a loader — the map is ready once MapNew is active
            if (SceneManager.ActiveSceneKey != "MapNew")
                return false;

            if (Campaign.instance == null || References.Map == null)
                return false;

            CampaignNode current = GetPlayerNode();
            if (current == null)
                return false;

            var parts = new List<string> { Loc.Get("screen_map") };

            string zone = GetZoneName(current);
            if (!string.IsNullOrEmpty(zone))
                parts.Add(Loc.Get("map_zone", zone));

            MapNode currentMapNode = References.Map.FindNode(current);
            if (currentMapNode != null)
                parts.Add(Loc.Get("map_you_are_at", ItemDescriber.GetMapNodeName(currentMapNode)));

            var destinations = GetDestinationNames(current);
            if (destinations.Count > 0)
                parts.Add(Loc.Get("map_destinations", destinations.Count, string.Join("; ", destinations)));

            string hint = HintOnce("map_hint");
            if (hint != null)
                parts.Add(hint);

            ScreenReader.SayEvent(string.Join(" ", parts), interrupt: true);
            return true;
        }

        protected override void HandleInput()
        {
            base.HandleInput();

            if (Input.GetKeyDown(KeyCode.M))
            {
                DebugLogger.LogInput(Name, "MapOverview");
                AnnounceOverview();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                DebugLogger.LogInput(Name, "Gold");
                AnnounceGold();
            }
        }

        /// <summary>I: full details of the focused map node — nodes have no
        /// card to inspect.</summary>
        protected override void OnInspectKey()
        {
            DebugLogger.LogInput(Name, "Info");
            AnnounceFocusedNodeDetails();
        }

        /// <summary>
        /// Left/Right walk the map nodes in path order; Up/Down cycle the HUD items.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            var all = NavigationHelper.GetNavigableItems();
            var nodeItems = new List<UINavigationItem>();
            var otherItems = new List<UINavigationItem>();

            foreach (var item in all)
            {
                if (GetMapNode(item) != null)
                    nodeItems.Add(item);
                else
                    otherItems.Add(item);
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            UINavigationItem current = navSystem?.currentNavigationItem;

            UINavigationItem next = null;
            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                if (nodeItems.Count == 0)
                {
                    ScreenReader.Say(Loc.Get("map_not_ready"), interrupt: true);
                    return;
                }

                // Order nodes along the journey: by tier (depth), then branch position
                nodeItems.Sort(CompareNodeItems);
                next = NavigationHelper.NavigateLinear(nodeItems, current, dir, vertical: false);

                // Only one location revealed: silence would read as "arrows are broken"
                if (next == current)
                {
                    ScreenReader.Say(Loc.Get("map_only_location"), interrupt: true);
                    return;
                }
            }
            else
            {
                // The battle draw/discard pockets render on the map HUD but are
                // always empty here — announcing "Draw pile, 0 cards" only confuses
                otherItems.RemoveAll(IsEmptyPocket);

                if (otherItems.Count == 0)
                {
                    ScreenReader.Say(Loc.Get("map_no_controls"), interrupt: true);
                    return;
                }

                otherItems.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
                // Treat up/down as prev/next within the HUD list
                var mapped = dir == NavDirection.Up ? NavDirection.Left : NavDirection.Right;
                next = NavigationHelper.NavigateLinear(otherItems, current, mapped, vertical: false);
            }

            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        private static bool IsEmptyPocket(UINavigationItem item)
        {
            var pocket = item.GetComponentInParent<CardPocket>();
            if (pocket == null && item.clickHandler != null)
                pocket = item.clickHandler.GetComponentInParent<CardPocket>();
            return pocket != null && pocket.Count == 0;
        }

        private static int CompareNodeItems(UINavigationItem a, UINavigationItem b)
        {
            var nodeA = GetMapNode(a)?.campaignNode;
            var nodeB = GetMapNode(b)?.campaignNode;
            if (nodeA == null || nodeB == null) return 0;

            int byTier = nodeA.tier.CompareTo(nodeB.tier);
            if (byTier != 0) return byTier;
            return nodeA.positionIndex.CompareTo(nodeB.positionIndex);
        }

        private static MapNode GetMapNode(UINavigationItem item)
        {
            var node = item.GetComponentInParent<MapNode>();
            if (node == null && item.clickHandler != null)
                node = item.clickHandler.GetComponentInParent<MapNode>();
            return node;
        }

        protected override string GetItemDescription(UINavigationItem item)
        {
            var mapNode = GetMapNode(item);
            if (mapNode != null)
                return DescribeNode(mapNode, includeHints: true);

            return base.GetItemDescription(item);
        }

        /// <summary>Describe a map node: name, type, state, and a short battle preview.</summary>
        private string DescribeNode(MapNode mapNode, bool includeHints)
        {
            var node = mapNode.campaignNode;
            if (node == null)
                return ItemDescriber.GetMapNodeName(mapNode);

            string name = ItemDescriber.GetMapNodeName(mapNode);
            var parts = new List<string> { name };

            // Node category (battle, boss, shop...) when the label doesn't already say it
            string category = GetNodeCategory(node);
            if (!string.IsNullOrEmpty(category)
                && name.IndexOf(category, System.StringComparison.OrdinalIgnoreCase) < 0)
                parts.Add(category);

            // State relative to the player
            CampaignNode current = GetPlayerNode();
            if (node == current)
            {
                parts.Add(Loc.Get("map_node_here"));
                // Standing on an uncleared node (run start): Enter is how you begin it
                if (!node.cleared && includeHints)
                    parts.Add(Loc.Get("map_node_enter"));
            }
            else if (node.cleared)
                parts.Add(Loc.Get("map_node_cleared"));
            else if (IsDirectDestination(current, node))
                parts.Add(includeHints ? Loc.Get("map_node_available") : Loc.Get("map_node_available_short"));
            else if (mapNode.reachable)
                parts.Add(Loc.Get("map_node_ahead"));
            else
                parts.Add(Loc.Get("map_node_not_reachable"));

            // Short battle preview: number of waves
            if (node.type != null && node.type.isBattle)
            {
                try
                {
                    var waves = node.data?.GetSaveCollection<BattleWaveManager.WaveData>("waves");
                    if (waves != null && waves.Length > 0)
                        parts.Add(Loc.Get("map_battle_waves", waves.Length));
                }
                catch { /* wave data not present for this node */ }
            }

            return string.Join(", ", parts);
        }

        /// <summary>Category word derived from the CampaignNodeType subclass name.</summary>
        private static string GetNodeCategory(CampaignNode node)
        {
            if (node.type == null) return null;

            if (node.type.isBoss)
                return Loc.Get("node_type_boss");

            string typeName = node.type.GetType().Name.Replace("CampaignNodeType", "");
            if (string.IsNullOrEmpty(typeName)) return null;

            if (Loc.TryGet("node_type_" + typeName.ToLowerInvariant(), out string localized))
                return localized;

            return ScreenHandler.CleanName(typeName);
        }

        /// <summary>Can the player travel directly to this node right now?</summary>
        private static bool IsDirectDestination(CampaignNode current, CampaignNode node)
        {
            if (current == null || node == null) return false;
            bool connected = current.connections != null
                && current.connections.Exists(c => c.otherId == node.id);
            if (!connected) return false;
            return current.cleared || current.type == null || !current.type.mustClear;
        }

        private static CampaignNode GetPlayerNode()
        {
            try
            {
                return Campaign.FindCharacterNode(References.Player);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Names of nodes the player can travel to right now.</summary>
        private List<string> GetDestinationNames(CampaignNode current)
        {
            var names = new List<string>();
            if (current?.connections == null || References.Map == null)
                return names;

            foreach (var connection in current.connections)
            {
                CampaignNode target = SafeGetNode(connection.otherId);
                if (target == null) continue;

                MapNode mapNode = References.Map.FindNode(target);
                names.Add(mapNode != null
                    ? ItemDescriber.GetMapNodeName(mapNode)
                    : ScreenHandler.CleanName(target.name));
            }
            return names;
        }

        private static CampaignNode SafeGetNode(int id)
        {
            var nodes = Campaign.instance?.nodes;
            if (nodes == null || id < 0 || id >= nodes.Count) return null;
            return nodes[id];
        }

        /// <summary>
        /// The localized zone name lives on non-interactable "area" label nodes.
        /// </summary>
        private static string GetZoneName(CampaignNode current)
        {
            var nodes = Campaign.instance?.nodes;
            if (nodes == null || References.Map == null) return null;

            foreach (CampaignNode node in nodes)
            {
                if (node?.type == null || node.type.interactable)
                    continue;
                if (node.type.letter == null || !node.type.letter.StartsWith("area"))
                    continue;
                if (node.areaIndex != current.areaIndex)
                    continue;

                MapNode labelNode = References.Map.FindNode(node);
                var tmp = labelNode != null
                    ? labelNode.GetComponentInChildren<TMPro.TMP_Text>(true)
                    : null;
                string text = tmp != null ? tmp.text?.Trim() : null;
                if (!string.IsNullOrEmpty(text))
                    return TextProcessor.StripRichText(text);
            }
            return null;
        }

        /// <summary>M: read every revealed location in journey order with its state.</summary>
        private void AnnounceOverview()
        {
            var campaign = Campaign.instance;
            if (campaign?.nodes == null || References.Map == null)
            {
                ScreenReader.Say(Loc.Get("map_not_ready"), interrupt: true);
                return;
            }

            var revealed = campaign.nodes
                .Where(n => n != null && n.revealed && n.type != null && n.type.interactable)
                .OrderBy(n => n.tier)
                .ThenBy(n => n.positionIndex)
                .ToList();

            int hidden = campaign.nodes.Count(
                n => n != null && !n.revealed && n.type != null && n.type.interactable);

            var parts = new List<string> { Loc.Get("map_overview", revealed.Count) };

            foreach (CampaignNode node in revealed)
            {
                MapNode mapNode = References.Map.FindNode(node);
                parts.Add(mapNode != null
                    ? DescribeNode(mapNode, includeHints: false)
                    : ScreenHandler.CleanName(node.name));
            }

            if (hidden > 0)
                parts.Add(Loc.Get("map_hidden_nodes", hidden));

            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        /// <summary>
        /// Map buffer: one review item per revealed location in journey order,
        /// plus the count of locations not yet revealed.
        /// </summary>
        internal List<string> BuildLocationItems()
        {
            var campaign = Campaign.instance;
            if (campaign?.nodes == null || References.Map == null)
                return null;

            var revealed = campaign.nodes
                .Where(n => n != null && n.revealed && n.type != null && n.type.interactable)
                .OrderBy(n => n.tier)
                .ThenBy(n => n.positionIndex)
                .ToList();

            var items = new List<string>();
            foreach (CampaignNode node in revealed)
            {
                MapNode mapNode = References.Map.FindNode(node);
                items.Add(mapNode != null
                    ? DescribeNode(mapNode, includeHints: false)
                    : ScreenHandler.CleanName(node.name));
            }

            int hidden = campaign.nodes.Count(
                n => n != null && !n.revealed && n.type != null && n.type.interactable);
            if (hidden > 0)
                items.Add(Loc.Get("map_hidden_nodes", hidden));

            return items;
        }

        /// <summary>I: details of the focused node — enemies per wave, rewards.</summary>
        private void AnnounceFocusedNodeDetails()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            MapNode mapNode = current != null ? GetMapNode(current) : null;

            // No node focused: describe the player's current location
            if (mapNode == null && References.Map != null)
            {
                CampaignNode playerNode = GetPlayerNode();
                if (playerNode != null)
                    mapNode = References.Map.FindNode(playerNode);
            }

            var items = BuildNodeDetailItems(mapNode);
            if (items == null || items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
                return;
            }

            ScreenReader.Say(string.Join(". ", items), interrupt: true);
        }

        /// <summary>
        /// Details buffer for a focused map node: the same waves/enemies/reward
        /// breakdown the I key reads, as steppable items.
        /// </summary>
        public override List<string> GetFocusedDetailParts(UINavigationItem item)
        {
            var mapNode = GetMapNode(item);
            return mapNode != null ? BuildNodeDetailItems(mapNode) : null;
        }

        /// <summary>
        /// Node summary, then one item per battle wave's enemy roster, then the
        /// game's reward tooltip. Null when the node has nothing to describe.
        /// </summary>
        private List<string> BuildNodeDetailItems(MapNode mapNode)
        {
            if (mapNode?.campaignNode == null)
                return null;

            var node = mapNode.campaignNode;
            var parts = new List<string> { DescribeNode(mapNode, includeHints: false) };

            // Enemy roster for battle nodes
            if (node.type != null && node.type.isBattle)
            {
                try
                {
                    var waves = node.data?.GetSaveCollection<BattleWaveManager.WaveData>("waves");
                    if (waves != null)
                    {
                        for (int i = 0; i < waves.Length; i++)
                        {
                            var enemies = new List<string>();
                            for (int j = 0; j < waves[i].Count; j++)
                            {
                                CardData enemy = waves[i].PeekCardData(j);
                                if (enemy != null)
                                    enemies.Add(enemy.title);
                            }
                            if (enemies.Count > 0)
                                parts.Add(Loc.Get("map_wave_enemies", i + 1, string.Join(", ", enemies)));
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    DebugLogger.Log(DebugLogger.LogCategory.Game, Name,
                        $"Wave details failed: {ex.Message}");
                }
            }

            // Reward text (the game's own tooltip string)
            try
            {
                string rewards = node.GetDesc();
                if (!string.IsNullOrEmpty(rewards))
                {
                    string clean = TextProcessor.StripRichText(rewards);
                    if (!string.IsNullOrEmpty(clean))
                        parts.Add(clean);
                }
            }
            catch { /* nodes without reward data */ }

            return parts;
        }

        /// <summary>G: announce the player's gold.</summary>
        private void AnnounceGold()
        {
            try
            {
                int gold = References.Player.data.inventory.gold.Value;
                ScreenReader.Say(Loc.Get("gold_amount", gold), interrupt: true);
            }
            catch
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
            }
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_map");
        }
    }
}
