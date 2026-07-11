using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Fallback accessibility handler for any screen without a specialized handler.
    /// Announces focused UI elements and provides basic arrow key navigation.
    /// </summary>
    public class GenericScreenHandler : ScreenHandler
    {
        public override string Name => $"Generic({_sceneName})";

        private string _sceneName;
        private UINavigationItem _lastFocused;
        private bool _announcedScreen;
        private float _enterTime;
        private int _titleRetries;
        private UINavigationLayer _lastNavLayer;

        public void SetScene(string sceneName)
        {
            _sceneName = sceneName ?? "Unknown";
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _lastFocused = null;
            _announcedScreen = false;
            _enterTime = Time.unscaledTime;
            _titleRetries = 0;
            _lastNavLayer = UINavigationSystem.ActiveNavigationLayer;
        }

        public override void OnExit()
        {
            base.OnExit();
            _lastFocused = null;
        }

        public override void OnUpdate()
        {
            // Wait for UI to settle, then announce screen title
            // Retry a few times since UI may still be loading
            if (!_announcedScreen)
            {
                float elapsed = Time.unscaledTime - _enterTime;
                float delay = 0.5f + _titleRetries * 0.5f;
                if (elapsed > delay)
                {
                    if (AnnounceScreen())
                    {
                        _announcedScreen = true;
                    }
                    else
                    {
                        _titleRetries++;
                        if (_titleRetries >= 4)
                        {
                            // Give up and announce scene name
                            _announcedScreen = true;
                            string cleanScene = CleanName(_sceneName);
                            ScreenReader.Say(cleanScene, interrupt: true);
                        }
                    }
                }
            }

            // Detect navigation layer changes (popups/panels opening within same scene)
            var currentLayer = UINavigationSystem.ActiveNavigationLayer;
            if (currentLayer != _lastNavLayer)
            {
                _lastNavLayer = currentLayer;
                _lastFocused = null; // Reset focus tracking so new panel items get announced
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name,
                    $"Navigation layer changed: {currentLayer?.name ?? "null"}");
            }

            // Allow navigation even before title is announced
            // (skip only the very first 0.3s to let UI initialize)
            if (Time.unscaledTime - _enterTime < 0.3f) return;

            // Handle arrow key navigation
            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                var items = NavigationHelper.GetNavigableItems();
                if (items.Count > 0)
                {
                    var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
                    UINavigationItem current = navSystem?.currentNavigationItem;

                    // Use vertical for up/down, horizontal for left/right
                    UINavigationItem next = null;
                    if (dir == NavDirection.Up || dir == NavDirection.Down)
                        next = NavigationHelper.NavigateLinear(items, current, dir, vertical: true);
                    else
                        next = NavigateHorizontal(items, current, dir);

                    if (next != null)
                        NavigationHelper.FocusItem(next);
                }
            }

            // Handle Enter to activate
            if (NavigationHelper.IsConfirmPressed())
            {
                var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
                if (navSystem?.currentNavigationItem != null)
                {
                    DebugLogger.LogInput(Name, "Confirm");
                    NavigationHelper.ActivateCurrent();
                }
            }

            // Announce focus changes
            CheckAndAnnounceFocus();
        }

        /// <summary>
        /// Try to find and announce the screen title/header text.
        /// Returns true if a title was found and announced.
        /// </summary>
        private bool AnnounceScreen()
        {
            string title = FindScreenTitle();
            if (title != null)
            {
                ScreenReader.Say(title, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Screen title: {title}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Search the scene for screen title text using multiple strategies.
        /// </summary>
        private string FindScreenTitle()
        {
            // Strategy 1: Find TitleSetter components (the game's standard title system)
            string title = FindTitleFromTitleSetter();
            if (title != null) return title;

            // Strategy 2: Find objects named "title" or "header" with TMP_Text
            title = FindTitleByObjectName();
            if (title != null) return title;

            // Strategy 3: Heuristic - biggest font near top of screen
            title = FindTitleByHeuristic();
            return title;
        }

        /// <summary>Find title from TitleSetter components (game's primary title system).</summary>
        private string FindTitleFromTitleSetter()
        {
            var titleSetters = Object.FindObjectsOfType<TitleSetter>();
            foreach (var setter in titleSetters)
            {
                if (setter == null || !setter.gameObject.activeInHierarchy)
                    continue;

                // TitleSetter has a child TMP_Text via LocalizeStringEvent
                var tmp = setter.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    string text = tmp.text?.Trim();
                    if (!string.IsNullOrEmpty(text) && text.Length >= 3)
                        return text;
                }
            }
            return null;
        }

        /// <summary>Find title by searching for GameObjects named "title", "header", etc.</summary>
        private string FindTitleByObjectName()
        {
            var allTexts = Object.FindObjectsOfType<TMP_Text>();
            if (allTexts == null) return null;

            // Priority list of name patterns to look for (case-insensitive)
            string[] titlePatterns = { "title", "header", "heading", "pagetitle", "screentitle" };

            foreach (var pattern in titlePatterns)
            {
                foreach (var txt in allTexts)
                {
                    if (txt == null || !txt.gameObject.activeInHierarchy)
                        continue;

                    // Skip text that belongs to cards
                    if (IsCardText(txt))
                        continue;

                    // Check this object and its parents for the pattern
                    Transform check = txt.transform;
                    int depth = 0;
                    while (check != null && depth < 3)
                    {
                        if (check.name.ToLowerInvariant().Contains(pattern))
                        {
                            string content = txt.text?.Trim();
                            if (!string.IsNullOrEmpty(content) && content.Length >= 3 && content.Length <= 100)
                                return content;
                        }
                        check = check.parent;
                        depth++;
                    }
                }
            }
            return null;
        }

        /// <summary>Fallback heuristic: find the largest TMP_Text near the top of the screen.</summary>
        private string FindTitleByHeuristic()
        {
            var allTexts = Object.FindObjectsOfType<TMP_Text>();
            if (allTexts == null || allTexts.Length == 0) return null;

            TMP_Text best = null;
            float bestScore = float.MinValue;

            foreach (var txt in allTexts)
            {
                if (txt == null || !txt.gameObject.activeInHierarchy)
                    continue;

                string content = txt.text?.Trim();
                if (string.IsNullOrEmpty(content) || content.Length < 3 || content.Length > 80)
                    continue;

                // Skip text that looks like stats/numbers
                if (content.All(c => char.IsDigit(c) || c == '/' || c == ' '))
                    continue;

                // Skip text that belongs to cards/entities
                if (IsCardText(txt))
                    continue;

                // Score: big font + high on screen + bonus for short text (titles are short)
                float fontSize = txt.fontSize;
                float yPos = txt.transform.position.y;
                float lengthBonus = content.Length <= 30 ? 20f : 0f;

                float score = fontSize * 2f + yPos * 10f + lengthBonus;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = txt;
                }
            }

            return best?.text?.Trim();
        }

        /// <summary>
        /// Navigate left/right among items sorted by X position.
        /// </summary>
        /// <summary>Check if a TMP_Text belongs to a card/entity (not a screen title).</summary>
        private static bool IsCardText(TMP_Text txt)
        {
            return txt.GetComponentInParent<Entity>() != null
                || txt.GetComponentInParent<Card>() != null
                || txt.GetComponentInParent<CardPopUpPanel>() != null;
        }

        private UINavigationItem NavigateHorizontal(List<UINavigationItem> items, UINavigationItem current, NavDirection dir)
        {
            // Sort by X position for horizontal navigation
            items.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            return NavigationHelper.NavigateLinear(items, current, dir, vertical: false);
        }

        private void CheckAndAnnounceFocus()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null) return;

            UINavigationItem current = navSystem.currentNavigationItem;
            if (current == _lastFocused) return;

            _lastFocused = current;
            if (current == null) return;

            string text = GetItemDescription(current);
            if (!string.IsNullOrEmpty(text))
            {
                ScreenReader.Say(text, interrupt: true);
                DebugLogger.Log(DebugLogger.LogCategory.Handler, Name, $"Focused: {text}");
            }
        }

        /// <summary>
        /// Get a rich description of a focused item.
        /// For cards: reads name, stats, and description.
        /// For slots: reads position and occupant.
        /// For buttons: reads the button text.
        /// </summary>
        private string GetItemDescription(UINavigationItem item)
        {
            if (item == null) return null;

            // Check for battlefield card slot (diamond placement slots)
            var slot = item.GetComponent<CardSlot>();
            if (slot == null)
                slot = item.GetComponentInParent<CardSlot>();
            if (slot != null)
            {
                return DescribeSlot(slot);
            }

            // Check for Town building (Gate, ChallengeShrine, PetHut, etc.)
            var building = FindBuilding(item.gameObject);
            if (building != null)
            {
                return DescribeBuilding(building);
            }

            // Try to find an Entity (card) on this item or its parents/children
            var entity = FindEntity(item.gameObject);
            if (entity != null)
            {
                return DescribeEntity(entity);
            }

            // Fall back to standard button text
            return GetButtonText(item);
        }

        /// <summary>
        /// Look for an Entity component on this object or nearby in the hierarchy.
        /// </summary>
        private Entity FindEntity(GameObject obj)
        {
            // Check self and parents
            var entity = obj.GetComponentInParent<Entity>();
            if (entity != null) return entity;

            // Check children
            entity = obj.GetComponentInChildren<Entity>();
            return entity;
        }

        /// <summary>
        /// Look for a Building component on this object or nearby in the hierarchy.
        /// </summary>
        private Building FindBuilding(GameObject obj)
        {
            var building = obj.GetComponentInParent<Building>();
            if (building != null) return building;
            return obj.GetComponentInChildren<Building>();
        }

        /// <summary>
        /// Build a screen-reader-friendly description of a Town building.
        /// Reads the localized building name and any challenge progress.
        /// </summary>
        private string DescribeBuilding(Building building)
        {
            var parts = new List<string>();

            // Building name from localized title
            if (building.type != null)
            {
                try
                {
                    string title = building.type.titleKey.GetLocalizedString();
                    if (!string.IsNullOrEmpty(title))
                        parts.Add(title);
                }
                catch
                {
                    // Localization may not be ready
                }
            }

            // Fallback to cleaned GameObject name if no localized title
            if (parts.Count == 0)
                parts.Add(CleanName(building.gameObject.name));

            // Check for challenge progress display (ChallengeShrine and similar)
            var challengeDisplay = building.GetComponentInChildren<ChallengeProgressDisplay>();
            if (challengeDisplay != null)
            {
                if (challengeDisplay.text != null)
                {
                    string challengeText = challengeDisplay.text.text?.Trim();
                    if (!string.IsNullOrEmpty(challengeText))
                        parts.Add(challengeText);
                }
                if (challengeDisplay.progressText != null)
                {
                    string progress = challengeDisplay.progressText.text?.Trim();
                    if (!string.IsNullOrEmpty(progress))
                        parts.Add(progress);
                }
            }

            // Building state
            if (!building.built && building.buildStarted)
                parts.Add(Loc.Get("building_under_construction"));
            else if (building.HasUncheckedUnlocks)
                parts.Add(Loc.Get("building_new_unlock"));

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Build a screen-reader-friendly description of a battlefield slot.
        /// Announces row, position, and occupant if any.
        /// </summary>
        private string DescribeSlot(CardSlot slot)
        {
            var parts = new List<string>();

            // Determine side (player or enemy)
            string side = "";
            if (slot.owner != null && References.Battle != null)
            {
                if (slot.owner == References.Battle.player)
                    side = Loc.Get("slot_your_side");
                else
                    side = Loc.Get("slot_enemy_side");
            }

            // Find row index
            int rowIndex = -1;
            int slotIndex = -1;
            var lane = slot.GetComponentInParent<CardSlotLane>();
            if (lane != null)
            {
                slotIndex = lane.slots.IndexOf(slot);
                if (References.Battle != null)
                    rowIndex = References.Battle.GetRowIndex(lane);
            }

            // Build position description
            if (!string.IsNullOrEmpty(side))
                parts.Add(side);

            if (rowIndex >= 0)
                parts.Add(Loc.Get("slot_row", rowIndex + 1));

            if (slotIndex >= 0)
                parts.Add(Loc.Get("slot_position", slotIndex + 1));

            // Describe occupant or empty state
            if (slot.Empty)
            {
                parts.Add(Loc.Get("slot_empty"));
            }
            else
            {
                var occupant = slot.GetTop();
                if (occupant?.data != null)
                {
                    parts.Add(occupant.data.title);
                    // Add basic stats
                    if (occupant.hp.max > 0)
                        parts.Add($"{occupant.hp.current} HP");
                    if (occupant.damage.max > 0)
                        parts.Add($"{occupant.damage.current} attack");
                }
                else
                {
                    parts.Add(Loc.Get("slot_occupied"));
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : Loc.Get("slot_empty");
        }

        /// <summary>
        /// Build a screen-reader-friendly description of a card/entity.
        /// Reads name, stats, and fully expanded description with keyword explanations.
        /// </summary>
        private string DescribeEntity(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string>();

            // Name
            string title = entity.data.title;
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            // Attack
            if (entity.damage.max > 0)
                parts.Add($"{entity.damage.current} attack");

            // Health
            if (entity.hp.max > 0)
                parts.Add($"{entity.hp.current} health");

            // Counter
            if (entity.counter.max > 0)
                parts.Add($"{entity.counter.current} counter");

            // Description text — expanded with keyword descriptions
            try
            {
                string rawDesc = Card.GetDescription(entity.data);
                if (!string.IsNullOrEmpty(rawDesc))
                {
                    string processed = TextProcessor.ProcessForScreenReader(rawDesc);
                    if (!string.IsNullOrEmpty(processed))
                        parts.Add(processed);
                }
            }
            catch
            {
                // Card.GetDescription may fail if card isn't fully initialized
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }
}
