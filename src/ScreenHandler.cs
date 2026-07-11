using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Base class for screen-specific accessibility handlers.
    /// Each game screen (MainMenu, Battle, Campaign, etc.) gets its own handler.
    /// </summary>
    public abstract class ScreenHandler
    {
        /// <summary>
        /// Known name mappings for icon-only buttons and common GameObjects.
        /// Maps lowercase name fragments to readable labels.
        /// Checked against: GameObject name, parent names, sprite names, component types.
        /// </summary>
        private static readonly Dictionary<string, string> _knownNames = new Dictionary<string, string>
        {
            // Social
            { "discord", "Discord" },
            { "twitter", "X" },
            { "x.com", "X" },
            { "steam", "Steam" },
            { "youtube", "YouTube" },
            { "reddit", "Reddit" },
            { "website", "Website" },
            { "instagram", "Instagram" },
            { "tiktok", "TikTok" },
            { "facebook", "Facebook" },
            { "patreon", "Patreon" },
            // Menu
            { "settings", "Settings" },
            { "options", "Options" },
            { "credits", "Credits" },
            { "quit", "Quit" },
            { "exit", "Exit" },
            { "back", "Back" },
            { "close", "Close" },
            { "play", "Play" },
            { "continue", "Continue" },
            { "mods", "Mods" },
            { "restart", "Restart" },
            { "resume", "Resume" },
            // Game UI
            { "journal", "Journal" },
            { "battlelog", "Battle Log" },
            { "battle log", "Battle Log" },
            { "log", "Log" },
            { "help", "Help" },
            { "inspect", "Inspect" },
            { "info", "Info" },
            { "pause", "Pause" },
            { "inventory", "Inventory" },
            { "backpack", "Backpack" },
            { "deck", "Deck" },
            { "deckview", "Deck View" },
            { "shop", "Shop" },
            { "reward", "Reward" },
            { "charm", "Charms" },
            { "upgrade", "Upgrades" },
            { "lore", "Lore" },
            { "lorepage", "Lore Pages" },
            { "redraw", "Redraw Bell" },
            { "bell", "Bell" },
            { "confirm", "Confirm" },
            { "accept", "Accept" },
            { "cancel", "Cancel" },
            { "deny", "Deny" },
            { "skip", "Skip" },
            { "next", "Next" },
            { "previous", "Previous" },
            { "prev", "Previous" },
            { "endturn", "End Turn" },
            { "end turn", "End Turn" },
            { "undo", "Undo" },
            { "recall", "Recall" },
            { "flee", "Flee" },
            { "abandon", "Abandon Run" },
        };

        /// <summary>Display name for logging.</summary>
        public abstract string Name { get; }

        /// <summary>Called when this screen becomes active.</summary>
        public virtual void OnEnter()
        {
            DebugLogger.LogState("ScreenManager", "->", Name);
        }

        /// <summary>Called when leaving this screen.</summary>
        public virtual void OnExit()
        {
            DebugLogger.LogState("ScreenManager", Name, "->");
        }

        /// <summary>Called every frame while this screen is active.</summary>
        public abstract void OnUpdate();

        /// <summary>
        /// Get a readable text label from a UINavigationItem.
        /// Uses a thorough search: text content, hierarchy names, component types, sprites.
        /// </summary>
        protected string GetButtonText(UINavigationItem item)
        {
            if (item == null) return null;

            GameObject target = item.clickHandler ?? item.gameObject;
            GameObject navObj = item.gameObject;

            // 1. Try TMP_Text — most reliable when present
            string text = FindText(target);
            if (text == null && target != navObj)
                text = FindText(navObj);
            if (text != null)
                return text;

            // 2. Walk hierarchy: check this object, parents, and siblings for known names
            string label = SearchHierarchyForLabel(target);
            if (label == null && target != navObj)
                label = SearchHierarchyForLabel(navObj);
            if (label != null)
                return label;

            // 3. Check component types for identification
            label = IdentifyByComponents(target);
            if (label == null && target != navObj)
                label = IdentifyByComponents(navObj);
            if (label != null)
                return label;

            // 4. Debug log unknown buttons so we can add them to known names
            LogUnknownButton(item, target);

            // 5. Fallback: cleaned GameObject name (skip generic names)
            string cleanedName = CleanName(target.name);
            if (IsGenericName(cleanedName) && target != navObj)
                cleanedName = CleanName(navObj.name);
            if (IsGenericName(cleanedName))
            {
                // Try parent name
                var parent = navObj.transform.parent;
                if (parent != null)
                    cleanedName = CleanName(parent.name);
            }
            return IsGenericName(cleanedName) ? "Button" : cleanedName;
        }

        /// <summary>Find TMP_Text in object and children.</summary>
        private string FindText(GameObject obj)
        {
            var tmp = obj.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp != null)
            {
                string t = tmp.text?.Trim();
                // Skip empty or single-character text (often just icons)
                if (!string.IsNullOrEmpty(t) && t.Length > 1)
                    return t;
            }
            return null;
        }

        /// <summary>
        /// Walk up the hierarchy checking each name against known names.
        /// Also checks sprite names at each level.
        /// </summary>
        private string SearchHierarchyForLabel(GameObject obj)
        {
            Transform current = obj.transform;
            int depth = 0;

            while (current != null && depth < 6)
            {
                // Check GameObject name
                string match = TryMatchKnownName(current.name);
                if (match != null) return match;

                // Check sprite on this object
                var image = current.GetComponent<Image>();
                if (image != null && image.sprite != null)
                {
                    match = TryMatchKnownName(image.sprite.name);
                    if (match != null) return match;
                }

                current = current.parent;
                depth++;
            }

            return null;
        }

        /// <summary>
        /// Try to identify a button by its attached components.
        /// </summary>
        private string IdentifyByComponents(GameObject obj)
        {
            // HelpPanelShower = help/info button
            if (obj.GetComponent<HelpPanelShower>() != null)
                return "Help";
            if (obj.GetComponentInParent<HelpPanelShower>() != null)
                return "Help";

            // OpenURL = external link
            var openUrl = obj.GetComponent<OpenURL>();
            if (openUrl != null)
                return IdentifyUrl(openUrl) ?? "Link";

            // Check for known game components
            if (obj.GetComponent<BattleLogButton>() != null)
                return "Battle Log";

            return null;
        }

        /// <summary>
        /// Try to identify a URL link by its target URL.
        /// Uses reflection since url is private serialized.
        /// </summary>
        private string IdentifyUrl(OpenURL openUrl)
        {
            try
            {
                var field = typeof(OpenURL).GetField("url",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    string url = field.GetValue(openUrl) as string;
                    if (!string.IsNullOrEmpty(url))
                    {
                        string lower = url.ToLowerInvariant();
                        if (lower.Contains("discord")) return "Discord";
                        if (lower.Contains("twitter") || lower.Contains("x.com")) return "X";
                        if (lower.Contains("steam")) return "Steam";
                        if (lower.Contains("youtube")) return "YouTube";
                        if (lower.Contains("reddit")) return "Reddit";
                        if (lower.Contains("wiki")) return "Wiki";
                        return "Link";
                    }
                }
            }
            catch
            {
                // Reflection failed, just return null
            }
            return null;
        }

        /// <summary>
        /// Log details about an unidentified button for debugging.
        /// </summary>
        private void LogUnknownButton(UINavigationItem item, GameObject target)
        {
            if (!DebugLogger.IsEnabled) return;

            var sb = new System.Text.StringBuilder();
            sb.Append($"Unknown button: obj={target.name}");

            if (item.gameObject != target)
                sb.Append($", nav={item.gameObject.name}");

            // Log parent chain
            var parent = target.transform.parent;
            if (parent != null)
                sb.Append($", parent={parent.name}");
            if (parent?.parent != null)
                sb.Append($", grandparent={parent.parent.name}");

            // Log sprite
            var img = target.GetComponent<Image>();
            if (img != null && img.sprite != null)
                sb.Append($", sprite={img.sprite.name}");

            // Log components
            var components = target.GetComponents<Component>();
            sb.Append(", components=[");
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;
                if (i > 0) sb.Append(",");
                sb.Append(components[i].GetType().Name);
            }
            sb.Append("]");

            DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, sb.ToString());
        }

        /// <summary>
        /// Try to match a name against known button name patterns.
        /// </summary>
        private static string TryMatchKnownName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string lower = name.ToLowerInvariant();
            foreach (var kv in _knownNames)
            {
                if (lower.Contains(kv.Key))
                    return kv.Value;
            }
            return null;
        }

        /// <summary>
        /// Check if a name is too generic to be useful.
        /// </summary>
        private static bool IsGenericName(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            string lower = name.ToLowerInvariant().Trim();
            return lower == "button" || lower == "btn" || lower == "image"
                || lower == "icon" || lower == "panel" || lower == "canvas"
                || lower == "content" || lower == "container" || lower == "group"
                || lower.StartsWith("buttonsheet") || lower.StartsWith("button sheet")
                || lower.StartsWith("uinavigation") || lower == "unknown";
        }

        /// <summary>
        /// Clean a GameObject name for speech (remove (Clone), underscores, CamelCase split).
        /// </summary>
        protected string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            name = name.Replace("(Clone)", "").Trim();
            name = name.Replace("_", " ");
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    sb.Append(' ');
                sb.Append(name[i]);
            }
            return sb.ToString().Trim();
        }
    }
}
