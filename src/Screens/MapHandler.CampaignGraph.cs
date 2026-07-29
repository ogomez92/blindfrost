using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Reading the campaign graph: what a fork branch gives up for good,
    /// forward reachability, and the node/zone lookups those rest on.
    /// </summary>
    public partial class MapHandler
    {
        /// <summary>How many forfeited locations are named before falling back to a count.</summary>
        private const int ForkNamesMax = 5;

        /// <summary>
        /// The cost of taking one branch of a fork, as a single phrase: either
        /// the locations it gives up for good, or the reassurance that the
        /// routes rejoin. Null when this node is not one of several options,
        /// so a straight path stays silent.
        /// </summary>
        private static string DescribeForkConsequence(CampaignNode current, CampaignNode destination)
        {
            var lost = GetForfeitedNodes(current, destination);
            if (lost == null)
                return null;
            if (lost.Count == 0)
                return Loc.Get("map_fork_rejoins");

            // Locations the player has actually seen can be named; the rest,
            // still hidden further down the branch, can only be counted
            var names = new List<string>();
            foreach (CampaignNode node in lost)
            {
                if (node.revealed && names.Count < ForkNamesMax)
                    names.Add(DescribeForfeitedNode(node));
            }
            int extra = lost.Count - names.Count;

            if (names.Count == 0)
                return Loc.Get("map_fork_gives_up_unseen", lost.Count);

            string listed = string.Join(", ", names);
            return extra > 0
                ? Loc.Get("map_fork_gives_up_more", listed, extra)
                : Loc.Get("map_fork_gives_up", listed);
        }

        /// <summary>
        /// Whether any branch out of this node closes off locations the others
        /// still reach — the difference between "pick an order" and "pick one".
        /// </summary>
        private static bool IsOneWayFork(CampaignNode current)
        {
            if (current?.connections == null || current.connections.Count < 2)
                return false;

            foreach (var connection in current.connections)
            {
                var lost = GetForfeitedNodes(current, SafeGetNode(connection.otherId));
                if (lost != null && lost.Count > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Locations that choosing <paramref name="destination"/> gives up for
        /// good. Map connections only ever lead onward, so everything still
        /// attainable after a move is what is forward-reachable from the node
        /// moved to; anything reachable through the fork's other branches but
        /// not through this one is gone the moment the choice is made.
        /// Empty when the branches rejoin and nothing is actually lost, null
        /// when there is no choice to make.
        /// </summary>
        private static List<CampaignNode> GetForfeitedNodes(CampaignNode current, CampaignNode destination)
        {
            if (current?.connections == null || current.connections.Count < 2 || destination == null)
                return null;

            HashSet<int> kept = ReachableFrom(destination);
            var lost = new List<CampaignNode>();
            var seen = new HashSet<int>();

            foreach (var connection in current.connections)
            {
                CampaignNode sibling = SafeGetNode(connection.otherId);
                if (sibling == null || sibling == destination)
                    continue;

                foreach (int id in ReachableFrom(sibling))
                {
                    if (kept.Contains(id) || !seen.Add(id))
                        continue;

                    CampaignNode node = SafeGetNode(id);
                    // Area labels are not places you travel to, and a cleared
                    // node is spent whichever way the player goes
                    if (node == null || node.type == null || !node.type.interactable || node.cleared)
                        continue;
                    lost.Add(node);
                }
            }

            lost.Sort((a, b) => a.tier != b.tier
                ? a.tier.CompareTo(b.tier)
                : a.positionIndex.CompareTo(b.positionIndex));
            return lost;
        }

        /// <summary>Every node reachable by travelling onward, the start included.</summary>
        private static HashSet<int> ReachableFrom(CampaignNode start)
        {
            var seen = new HashSet<int>();
            if (start == null)
                return seen;

            var pending = new Queue<CampaignNode>();
            seen.Add(start.id);
            pending.Enqueue(start);

            while (pending.Count > 0)
            {
                CampaignNode node = pending.Dequeue();
                if (node.connections == null)
                    continue;
                foreach (var connection in node.connections)
                {
                    CampaignNode next = SafeGetNode(connection.otherId);
                    if (next != null && seen.Add(next.id))
                        pending.Enqueue(next);
                }
            }
            return seen;
        }

        /// <summary>
        /// A forfeited location named by its kind ("treasure", "charm event"),
        /// which is what the choice actually turns on — the banner label would
        /// add a squad name nobody is weighing up.
        /// </summary>
        private static string DescribeForfeitedNode(CampaignNode node)
        {
            string category = GetNodeCategory(node);
            if (!string.IsNullOrEmpty(category))
                return category;

            MapNode mapNode = References.Map != null ? References.Map.FindNode(node) : null;
            return mapNode != null
                ? ItemDescriber.GetMapNodeName(mapNode)
                : ScreenHandler.CleanName(node.name);
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

    }
}
