using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The map overview and detail readouts: every revealed location in
    /// journey order, and the waves/enemies/rewards behind one node.
    /// </summary>
    public partial class MapHandler
    {
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
                    string clean = TextProcessor.ProcessRawText(rewards);
                    if (!string.IsNullOrEmpty(clean))
                        parts.Add(clean);
                }
            }
            catch { /* nodes without reward data */ }

            return parts;
        }

    }
}
