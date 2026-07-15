using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Town screen — the game's home base.
    /// Describes each building with its localized name, purpose, and state,
    /// and tells the player what the Gate will do (new run, continue, tutorial).
    /// </summary>
    public class TownHandler : NavigableScreenHandler
    {
        public override string Name => "Town";

        // Info buildings (the Tribe Hall, the Daily Challenge balloon, ...) open
        // as an in-town overlay (a BuildingSequence, not a new scene), and their
        // contents are not UINavigationItems — so the normal navigation finds
        // nothing and the player gets trapped with no way out. While one of these
        // overlays is up we read its text and let Escape close it.
        private BuildingDisplay _buildingDisplay;
        private float _nextBuildingSearch;
        private bool _overlayOpen;
        private bool _overlayAnnounced;
        private string _lastOverlayText;
        private readonly List<UINavigationItem> _overlayItems = new List<UINavigationItem>();
        private UINavigationItem _overlaySelected;

        // Challenge shrine: two rows browsed with up/down (incomplete / completed),
        // stones within a row with left/right.
        private readonly List<ChallengeStone> _shrineIncomplete = new List<ChallengeStone>();
        private readonly List<ChallengeStone> _shrineComplete = new List<ChallengeStone>();
        private int _shrineRow; // 0 = incomplete, 1 = completed
        private ChallengeStone _shrineStone;
        private bool _shrineAnnounced;

        public override void OnEnter()
        {
            base.OnEnter();
            _buildingDisplay = null;
            _nextBuildingSearch = 0f;
            _overlayOpen = false;
            _overlayAnnounced = false;
            _lastOverlayText = null;
            _overlayItems.Clear();
            _overlaySelected = null;
            _shrineRow = 0;
            _shrineStone = null;
            _shrineAnnounced = false;
        }

        public override void OnUpdate()
        {
            var overlay = ActiveBuildingOverlay();
            if (overlay != null)
            {
                HandleBuildingOverlay(overlay);
                return;
            }
            if (_overlayOpen)
            {
                _overlayOpen = false;
                _overlayAnnounced = false;
                _lastOverlayText = null;
                _overlayItems.Clear();
                _overlaySelected = null;
            }
            base.OnUpdate();
        }

        /// <summary>The building-sequence overlay if one is open, else null.</summary>
        private BuildingDisplay ActiveBuildingOverlay()
        {
            // The BuildingDisplay is reused (toggled active), so cache it and just
            // check its state each frame; only pay the scene search occasionally.
            if (_buildingDisplay == null && Time.unscaledTime >= _nextBuildingSearch)
            {
                _nextBuildingSearch = Time.unscaledTime + 1f;
                _buildingDisplay = Object.FindObjectOfType<BuildingDisplay>(includeInactive: true);
            }
            if (_buildingDisplay == null || !_buildingDisplay.gameObject.activeInHierarchy)
                return null;
            return _buildingDisplay.GetComponentInChildren<BuildingSequence>(includeInactive: false) != null
                ? _buildingDisplay
                : null;
        }

        /// <summary>
        /// Read the overlay's visible text (tribe names, unlock challenges and
        /// their progress, tribe lore, the daily challenge card and date, ...),
        /// re-reading when it changes or on I. Escape closes it — these overlays
        /// leave no keyboard way out.
        /// </summary>
        private void HandleBuildingOverlay(BuildingDisplay overlay)
        {
            if (!_overlayOpen)
            {
                _overlayOpen = true;
                _overlayAnnounced = false;
                _lastOverlayText = null;
                _overlaySelected = null;
                _shrineRow = 0;
                _shrineStone = null;
            }

            // Escape: step out of an open tribe detail first, else leave the building.
            if (NavigationHelper.IsBackPressed())
            {
                var detail = ActiveDetail(overlay);
                if (detail != null)
                {
                    detail.SetActive(false);
                    _lastOverlayText = null; // re-read the hall behind it
                    _overlaySelected = null;
                    ScreenReader.Say(Loc.Get("building_back"), interrupt: true);
                    return;
                }
                DebugLogger.LogInput(Name, "Close building overlay");
                overlay.End();
                _overlayOpen = false;
                _lastOverlayText = null;
                _overlayItems.Clear();
                _overlaySelected = null;
                ScreenReader.Say(Loc.Get("building_closed"), interrupt: true);
                RequestRefocus(); // land back on a town building, not in limbo
                return;
            }

            // Challenge shrine: dozens of stones split into completed / incomplete
            // rows — its own up/down + left/right browse, not the generic dump.
            if (RefreshShrine(overlay))
            {
                HandleShrineNav(overlay);
                return;
            }

            // The banners/buttons are UINavigationItems the game keeps off the
            // active layer, so the normal navigation skips them — gather them
            // straight from the overlay subtree and drive them ourselves.
            RefreshOverlayItems(overlay);

            if (_overlayItems.Count > 0 && NavigationHelper.IsConfirmPressed())
            {
                ActivateOverlayItem();
                return;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None && _overlayItems.Count > 0)
            {
                MoveOverlaySelection(dir);
                return;
            }

            // Read the overlay's text (unlock challenge, tribe lore, daily card)
            // on entry, whenever it changes, or on I.
            bool reRead = Input.GetKeyDown(KeyCode.I) && !NavigationHelper.IsTextInputFocused();
            string text = ReadVisibleText(overlay.transform);
            if (string.IsNullOrEmpty(text) || (text == _lastOverlayText && !reRead))
                return;
            _lastOverlayText = text;

            // A very long dump (the challenge shrine's dozens of stones, all
            // names then all conditions, out of order) is unusable — summarize
            // and let the player browse them one at a time with the arrows.
            if (text.Length > 400 && _overlayItems.Count > 0)
                text = Loc.Get("overlay_browse", _overlayItems.Count);
            // A visible unlock challenge (and no tribe detail open over it) means
            // this hall gates the next tribe — say so, so "6 of 100" doesn't
            // dangle without a purpose.
            else if (ActiveDetail(overlay) == null
                && overlay.GetComponentInChildren<ChallengeProgressDisplay>(includeInactive: false) != null)
                text = Loc.Get("tribe_unlock_intro") + " " + text;

            string name = OverlayBuildingName(overlay);
            string prefix = (!_overlayAnnounced && !string.IsNullOrEmpty(name)) ? name + ". " : "";
            string suffix = _overlayAnnounced ? "" : (HintOnce("building_overlay_hint") is string h ? " " + h : "");
            _overlayAnnounced = true;
            ScreenReader.SayEvent(prefix + text + suffix, interrupt: true);
        }

        /// <summary>Collect the overlay's interactable navigation items in reading order.</summary>
        private void RefreshOverlayItems(BuildingDisplay overlay)
        {
            _overlayItems.Clear();
            foreach (var item in overlay.GetComponentsInChildren<UINavigationItem>(includeInactive: false))
            {
                if (item == null || !item.isSelectable || !item.enabled
                    || !item.gameObject.activeInHierarchy || item.clickHandler == null)
                    continue;
                _overlayItems.Add(item);
            }
            _overlayItems.Sort((a, b) =>
                Mathf.Abs(a.Position.y - b.Position.y) > 0.05f
                    ? b.Position.y.CompareTo(a.Position.y)
                    : a.Position.x.CompareTo(b.Position.x));

            if (_overlaySelected != null && !_overlayItems.Contains(_overlaySelected))
                _overlaySelected = null;
        }

        private void MoveOverlaySelection(NavDirection dir)
        {
            bool vertical = dir == NavDirection.Up || dir == NavDirection.Down;
            var next = NavigationHelper.NavigateLinear(_overlayItems, _overlaySelected, dir, vertical)
                ?? _overlayItems[0];
            _overlaySelected = next;
            int index = _overlayItems.IndexOf(next);
            ScreenReader.Say(DescribeOverlayItem(next, index, _overlayItems.Count), interrupt: true);
        }

        private void ActivateOverlayItem()
        {
            var item = _overlaySelected ?? _overlayItems[0];
            DebugLogger.LogInput(Name, "Activate overlay item");
            _lastOverlayText = null; // whatever it opens should read out fresh

            // These banners fire a wired InputAction on the game's Select, not a
            // raw pointer click — a simulated click just clears the hover and
            // nothing opens (confirmed in the log). Trigger the action directly,
            // falling back to a click for plain buttons.
            var action = FindInputAction(item);
            if (action != null)
                action.Run();
            else if (item.clickHandler != null)
                NavigationHelper.PressObject(item.clickHandler);
        }

        /// <summary>The InputAction a banner/button fires when chosen, or null.</summary>
        private static InputAction FindInputAction(UINavigationItem item)
        {
            var action = item.GetComponentInParent<InputAction>();
            if (action != null)
                return action;
            // The action often sits elsewhere in the flag/button root's subtree
            var flag = item.GetComponentInParent<TribeFlagDisplay>();
            if (flag != null)
                return flag.GetComponentInChildren<InputAction>(includeInactive: true);
            return null;
        }

        /// <summary>Label a focused overlay item: its button text, else a cleaned
        /// name, with its position in the list.</summary>
        private static string DescribeOverlayItem(UINavigationItem item, int index, int total)
        {
            string label = null;

            // A challenge stone: read its reward name and the condition to earn it.
            var stone = item.GetComponentInParent<ChallengeStone>();
            if (stone != null && stone.challenge != null)
                label = DescribeChallengeStone(stone);

            // A tribe banner carries no text label — name it, and say Enter opens it.
            if (string.IsNullOrEmpty(label) && item.GetComponentInParent<TribeFlagDisplay>() != null)
                label = Loc.Get("tribe_banner");

            if (string.IsNullOrEmpty(label))
            {
                var tmp = item.GetComponentInChildren<TMP_Text>(includeInactive: false);
                if (tmp != null)
                {
                    string s = TextProcessor.StripRichText(tmp.text);
                    if (!string.IsNullOrEmpty(s))
                        label = s;
                }
            }
            if (string.IsNullOrEmpty(label))
                label = ScreenHandler.CleanName(item.gameObject.name);
            return Loc.Get("overlay_item", label, index + 1, total);
        }

        /// <summary>A challenge stone as "Reward: condition", or "Reward, hidden".</summary>
        private static string DescribeChallengeStone(ChallengeStone stone)
        {
            string name = null, condition = null;
            try { name = TextProcessor.StripRichText(stone.challenge.titleKey.GetLocalizedString()); }
            catch { /* localization not ready */ }
            if (!stone.challenge.hidden)
            {
                try { condition = TextProcessor.StripRichText(stone.challenge.textKey.GetLocalizedString()); }
                catch { /* localization not ready */ }
            }

            if (string.IsNullOrEmpty(name))
                name = Loc.Get("challenge_stone");
            if (stone.challenge.hidden || string.IsNullOrEmpty(condition))
                return Loc.Get("challenge_hidden", name);
            return name + ": " + condition;
        }

        /// <summary>
        /// Split the shrine's stones into incomplete and completed rows.
        /// Returns false when this overlay is not a challenge shrine.
        /// </summary>
        private bool RefreshShrine(BuildingDisplay overlay)
        {
            var stones = overlay.GetComponentsInChildren<ChallengeStone>(includeInactive: false);
            if (stones.Length == 0)
                return false;

            List<string> unlocked = null;
            try { unlocked = MetaprogressionSystem.GetUnlockedList(); }
            catch { /* metaprogression not ready */ }

            _shrineIncomplete.Clear();
            _shrineComplete.Clear();
            foreach (var stone in stones)
            {
                if (stone == null || stone.challenge == null || !stone.gameObject.activeInHierarchy)
                    continue;
                bool completed = unlocked != null && stone.challenge.reward != null
                    && unlocked.Contains(stone.challenge.reward.name);
                (completed ? _shrineComplete : _shrineIncomplete).Add(stone);
            }
            _shrineIncomplete.Sort(CompareStonePosition);
            _shrineComplete.Sort(CompareStonePosition);

            var row = _shrineRow == 1 ? _shrineComplete : _shrineIncomplete;
            if (_shrineStone != null && !row.Contains(_shrineStone))
                _shrineStone = null;
            return true;
        }

        private void HandleShrineNav(BuildingDisplay overlay)
        {
            if (!_shrineAnnounced)
            {
                _shrineAnnounced = true;
                string name = OverlayBuildingName(overlay);
                string hint = HintOnce("shrine_hint");
                ScreenReader.SayEvent(
                    (string.IsNullOrEmpty(name) ? "" : name + ". ")
                    + Loc.Get("shrine_summary", _shrineIncomplete.Count, _shrineComplete.Count)
                    + (hint != null ? " " + hint : ""),
                    interrupt: true);
                return;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir == NavDirection.None)
                return;

            // Up / Down switch between the incomplete and completed rows.
            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                int newRow = dir == NavDirection.Down ? 1 : 0;
                var target = newRow == 1 ? _shrineComplete : _shrineIncomplete;
                if (target.Count == 0)
                {
                    ScreenReader.Say(Loc.Get(newRow == 1
                        ? "shrine_none_completed" : "shrine_none_incomplete"), interrupt: true);
                    return;
                }
                _shrineRow = newRow;
                _shrineStone = target[0];
                ScreenReader.Say(
                    Loc.Get(newRow == 1 ? "shrine_row_completed" : "shrine_row_incomplete")
                    + ". " + DescribeChallengeStone(_shrineStone)
                    + " " + Loc.Get("overlay_position", 1, target.Count),
                    interrupt: true);
                return;
            }

            // Left / Right browse within the current row.
            var rowList = _shrineRow == 1 ? _shrineComplete : _shrineIncomplete;
            if (rowList.Count == 0)
                return;
            int idx = _shrineStone != null ? rowList.IndexOf(_shrineStone) : -1;
            bool forward = dir == NavDirection.Right;
            int newIdx = idx < 0 ? (forward ? 0 : rowList.Count - 1) : (forward ? idx + 1 : idx - 1);
            newIdx = Mathf.Clamp(newIdx, 0, rowList.Count - 1);
            _shrineStone = rowList[newIdx];
            ScreenReader.Say(
                DescribeChallengeStone(_shrineStone)
                + " " + Loc.Get("overlay_position", newIdx + 1, rowList.Count),
                interrupt: true);
        }

        private static int CompareStonePosition(ChallengeStone a, ChallengeStone b)
        {
            Vector3 pa = a.transform.position, pb = b.transform.position;
            return Mathf.Abs(pa.y - pb.y) > 0.05f
                ? pb.y.CompareTo(pa.y)
                : pa.x.CompareTo(pb.x);
        }

        /// <summary>The open tribe-lore detail panel, or null.</summary>
        private static GameObject ActiveDetail(BuildingDisplay overlay)
        {
            var detail = overlay.GetComponentInChildren<TribeDisplaySequence>(includeInactive: false);
            return detail != null ? detail.gameObject : null;
        }

        /// <summary>Localized name of the building whose overlay is open, or null.</summary>
        private static string OverlayBuildingName(BuildingDisplay overlay)
        {
            var seq = overlay.GetComponentInChildren<BuildingSequence>(includeInactive: false);
            var building = seq != null ? seq.building : null;
            if (building == null)
                return null;
            try
            {
                string title = building.type?.titleKey.GetLocalizedString();
                if (!string.IsNullOrEmpty(title))
                    return title;
            }
            catch { /* localization not ready */ }
            return ScreenHandler.CleanName(building.gameObject.name);
        }

        /// <summary>All visible TMP text under a root, in reading order (top rows
        /// first, then left to right), de-duplicated and joined.</summary>
        private static string ReadVisibleText(Transform root)
        {
            var entries = new List<(float y, float x, string s)>();
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(includeInactive: false))
            {
                if (t == null || !t.isActiveAndEnabled || !t.gameObject.activeInHierarchy)
                    continue;
                string s = TextProcessor.StripRichText(t.text);
                if (string.IsNullOrEmpty(s))
                    continue;
                var p = t.transform.position;
                entries.Add((p.y, p.x, s));
            }

            entries.Sort((a, b) =>
                Mathf.Abs(a.y - b.y) > 0.05f ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));

            var seen = new HashSet<string>();
            var parts = new List<string>();
            foreach (var e in entries)
                if (seen.Add(e.s))
                    parts.Add(e.s);
            return parts.Count > 0 ? string.Join(". ", parts) : null;
        }

        protected override bool TryAnnounceScreen()
        {
            string msg = Loc.Get("screen_town");
            string hint = HintOnce("town_hint");
            if (hint != null)
                msg += " " + hint;
            ScreenReader.SayEvent(msg, interrupt: true);
            return true;
        }

        /// <summary>I: the focused building's in-game help text — buildings
        /// have no card to inspect.</summary>
        protected override void OnInspectKey()
        {
            DebugLogger.LogInput(Name, "Info");
            AnnounceFocusedBuildingHelp();
        }

        protected override string GetItemDescription(UINavigationItem item)
        {
            var building = item.GetComponentInParent<Building>();
            if (building == null && item.clickHandler != null)
                building = item.clickHandler.GetComponentInParent<Building>();

            if (building != null)
            {
                string desc = ItemDescriber.DescribeBuilding(building);

                // The Gate starts or continues the journey — say which
                if (IsGate(building))
                    desc += ". " + GetGateAction();

                // Verbose focus folds in the building's help text (what I reads).
                // In short focus we still fold it in when the name alone says
                // nothing (e.g. "Balloon"); named-and-stated buildings keep the
                // help in the Details buffer (Ctrl+Up) / on I.
                if (ItemDescriber.VerboseFocus
                    || (!IsGate(building) && ItemDescriber.BuildingFocusIsBareName(building)))
                {
                    var help = ItemDescriber.GetBuildingHelpParts(building);
                    if (help.Count > 0)
                        desc += ". " + string.Join(". ", help);
                }

                return desc;
            }

            return base.GetItemDescription(item);
        }

        /// <summary>
        /// Details buffer for a focused building: the focus summary followed by
        /// the building's help text, so Ctrl+Up steps through what I reads.
        /// </summary>
        public override List<string> GetFocusedDetailParts(UINavigationItem item)
        {
            var building = item.GetComponentInParent<Building>();
            if (building == null && item.clickHandler != null)
                building = item.clickHandler.GetComponentInParent<Building>();
            if (building == null)
                return null;

            var parts = new List<string>();

            string summary = ItemDescriber.DescribeBuilding(building);
            if (IsGate(building))
                summary += ". " + GetGateAction();
            if (!string.IsNullOrEmpty(summary))
                parts.Add(summary);

            parts.AddRange(ItemDescriber.GetBuildingHelpParts(building));
            return parts;
        }

        /// <summary>The main gate prefab is named "Gate".</summary>
        private static bool IsGate(Building building)
        {
            return building.name.ToLowerInvariant().Contains("gate");
        }

        /// <summary>
        /// Mirror Menu.StartGameOrContinue: tutorial run in progress > tutorial offer >
        /// normal run in progress > new run.
        /// </summary>
        private static string GetGateAction()
        {
            try
            {
                var tutorialMode = AddressableLoader.Get<GameMode>("GameMode", "GameModeTutorial");
                if (tutorialMode != null && Campaign.CheckContinue(tutorialMode))
                    return Loc.Get("gate_continue_tutorial");

                if (SaveSystem.LoadProgressData("tutorialProgress", 0) <= 1)
                    return Loc.Get("gate_start_tutorial");

                var normalMode = AddressableLoader.Get<GameMode>("GameMode", "GameModeNormal");
                if (normalMode != null && Campaign.CheckContinue(normalMode))
                    return Loc.Get("gate_continue_run");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TownHandler",
                    $"Gate state check failed: {ex.Message}");
            }

            return Loc.Get("gate_start_run");
        }

        /// <summary>Read the focused building's help text (BuildingType.helpKey: title|body|note).</summary>
        private void AnnounceFocusedBuildingHelp()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            if (current == null)
            {
                ScreenReader.Say(Loc.Get("no_item_focused"), interrupt: true);
                return;
            }

            var building = current.GetComponentInParent<Building>();
            if (building == null && current.clickHandler != null)
                building = current.clickHandler.GetComponentInParent<Building>();

            if (building?.type == null)
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
                return;
            }

            var parts = ItemDescriber.GetBuildingHelpParts(building);
            if (parts.Count == 0)
            {
                // No in-game help — the building summary is all there is
                ScreenReader.Say(GetItemDescription(current), interrupt: true);
                return;
            }

            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_town");
        }
    }
}
