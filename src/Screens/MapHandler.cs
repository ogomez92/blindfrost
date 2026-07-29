using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the campaign map (scenes "Campaign" and "MapNew").
    /// Left/Right walk the journey path node by node; Up/Down reach the HUD
    /// (deck pockets, displays). Announces each location with its state,
    /// M reads a full map overview, I reads details of the focused location.
    /// </summary>
    public partial class MapHandler : NavigableScreenHandler
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

            // A fork where the branches never meet again is the one map decision
            // that cannot be walked back, so it is flagged before anything else
            if (IsOneWayFork(current))
                parts.Add(Loc.Get("map_fork_here"));

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
