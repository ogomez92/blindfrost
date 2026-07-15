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

        // The Daily Voyage balloon: its deck cards carry navigation items that
        // do nothing here, so focus must land only on the real buttons. Cache
        // the two the game keeps in private fields.
        private GameObject _balloonPlay;
        private GameObject _balloonScores;
        private bool _balloonAnnounced;
        private bool _balloonLoadingSaid;

        // Scroll-aware town navigation. The town's buildings sit inside a
        // Scroller; when a building scrolls out of the camera view it leaves the
        // navigation registry (the game's CheckLayer requires an item to be
        // on-screen). Focus-only navigation can then never return to it — most
        // painfully the Gate (start/continue run), which starts in view but goes
        // off-screen the moment you move to the front buildings. We keep our own
        // ring of every building seen and scroll the town to a target before
        // focusing it, so all of them — the Gate included — stay reachable.
        private Scroller _scroller;
        private float _nextScrollerSearch;
        private readonly List<Building> _knownBuildings = new List<Building>();
        private Building _ringCurrent;   // last building the ring focused
        private Building _pendingBuilding; // scrolled toward, awaiting focus
        private float _pendingDeadline;

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
            _balloonPlay = null;
            _balloonScores = null;
            _balloonAnnounced = false;
            _balloonLoadingSaid = false;
            _scroller = null;
            _nextScrollerSearch = 0f;
            _knownBuildings.Clear();
            _ringCurrent = null;
            _pendingBuilding = null;
            _pendingDeadline = 0f;
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
                _balloonAnnounced = false;
                _balloonLoadingSaid = false;
                _lastOverlayText = null;
                _overlayItems.Clear();
                _overlaySelected = null;
            }

            // Land focus on a building we scrolled toward last frame, once it has
            // re-entered the view and re-registered. Runs before base.OnUpdate so
            // the focus announcement fires this same frame.
            TryFocusPending();

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

        // ---- Scroll-aware building navigation ----

        /// <summary>
        /// Arrow keys walk a ring of every building seen since entering the town,
        /// scrolling the town to bring each target into view before focusing it.
        /// The ring keeps two kinds of building reachable that the plain
        /// navigation strands: the Gate (start/continue run), which the mod's
        /// help-panel filter drops because the tutorial town wraps it in a
        /// HelpPanelShower, and any building that has scrolled off-screen (the
        /// game's CheckLayer then treats it as non-navigable). Falls back to base
        /// navigation when the town has no scroller (nothing to strand).
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            var scroller = GetTownScroller();
            if (scroller == null || !scroller.ContentLargerThanBounds())
            {
                // Nothing scrolls off-screen — every building stays in view and
                // reachable, so the plain spatial navigation is fine.
                base.Navigate(dir);
                return;
            }

            var ring = TownRing(scroller);
            if (ring.Count == 0)
            {
                base.Navigate(dir);
                return;
            }

            Building current = CurrentBuilding() ?? _ringCurrent;
            int index = current != null ? ring.IndexOf(current) : -1;

            bool forward = dir == NavDirection.Down || dir == NavDirection.Right;
            int next;
            if (index < 0)
                next = forward ? 0 : ring.Count - 1;
            else
            {
                next = forward ? index + 1 : index - 1;
                if (next >= ring.Count) next = 0;
                if (next < 0) next = ring.Count - 1;
            }

            GoToBuilding(scroller, ring[next]);
        }

        /// <summary>The Scroller whose subtree holds the town buildings, or null.</summary>
        private Scroller GetTownScroller()
        {
            if (_scroller == null && Time.unscaledTime >= _nextScrollerSearch)
            {
                _nextScrollerSearch = Time.unscaledTime + 1f;
                foreach (var s in Object.FindObjectsOfType<Scroller>())
                {
                    if (s != null && s.GetComponentInChildren<Building>(includeInactive: true) != null)
                    {
                        _scroller = s;
                        break;
                    }
                }
            }
            return _scroller;
        }

        /// <summary>
        /// Every building known this town session, in reading order (top rows
        /// first, then left to right). Grows as buildings come into view; a
        /// building that later scrolls off-screen stays in the ring so we can
        /// scroll back to it. Sorted by world position — a uniform scroll shifts
        /// them all together, so their relative order is stable.
        /// </summary>
        private List<Building> TownRing(Scroller scroller)
        {
            // Seed from the raw registered navigation items, NOT GetNavigableItems:
            // its help-panel filter drops the Gate, which the tutorial town wraps
            // in a HelpPanelShower (Town.tutorialPrompt). That filter is exactly
            // why the Gate never appeared in the focus list. The registered set
            // still excludes locked buildings (they are unregistered) and the
            // Back Button (not a Building).
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem != null)
            {
                foreach (var item in navSystem.AvailableNavigationItems)
                {
                    if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                        continue;
                    var b = BuildingOf(item);
                    if (b == null || !b.transform.IsChildOf(scroller.transform))
                        continue;
                    if (!_knownBuildings.Contains(b))
                        _knownBuildings.Add(b);
                }
            }
            _knownBuildings.RemoveAll(b => b == null);

            var ring = new List<Building>(_knownBuildings);
            ring.Sort((a, b) =>
            {
                Vector3 pa = a.transform.position, pb = b.transform.position;
                return Mathf.Abs(pa.y - pb.y) > 0.05f
                    ? pb.y.CompareTo(pa.y)
                    : pa.x.CompareTo(pb.x);
            });
            return ring;
        }

        /// <summary>The building the game currently has focused, or null.</summary>
        private Building CurrentBuilding()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem != null ? navSystem.currentNavigationItem : null;
            return item != null ? BuildingOf(item) : null;
        }

        /// <summary>The Building a navigation item belongs to (item or its click handler).</summary>
        private static Building BuildingOf(UINavigationItem item)
        {
            if (item == null)
                return null;
            var building = item.GetComponentInParent<Building>();
            if (building == null && item.clickHandler != null)
                building = item.clickHandler.GetComponentInParent<Building>();
            return building;
        }

        /// <summary>
        /// Scroll the town so <paramref name="target"/> is in view, then queue it
        /// for focus. The item may still be re-registering after the scroll, so
        /// TryFocusPending retries over a short window.
        /// </summary>
        private void GoToBuilding(Scroller scroller, Building target)
        {
            ScrollScrollerTo(scroller, target.transform);
            _pendingBuilding = target;
            _pendingDeadline = Time.unscaledTime + 1.5f;
            TryFocusPending(); // usually ready at once (item already in view)
        }

        /// <summary>
        /// Mirror ScrollToNavigation: move the scroller content so the target is
        /// centred, then snap to it immediately. Snapping matters — an off-screen
        /// item fails the game's CheckLayer, and UINavigationSystem.Update would
        /// null our focus before a smooth scroll finished bringing it on-screen.
        /// </summary>
        private static void ScrollScrollerTo(Scroller scroller, Transform target)
        {
            if (scroller.horizontal)
            {
                float value = scroller.transform.position.x - target.position.x;
                scroller.ScrollTo(new Vector2(value, scroller.targetPos.y));
            }
            else
            {
                float value = scroller.transform.position.y - target.position.y;
                scroller.ScrollTo(new Vector2(scroller.targetPos.x, value));
            }
            scroller.rectTransform.anchoredPosition = scroller.targetPos;
        }

        /// <summary>
        /// Focus the pending building once its navigation item is active and
        /// registered again (a building that scrolled off-screen re-registers a
        /// frame or two after it re-enters view). Gives up after the deadline.
        /// </summary>
        private void TryFocusPending()
        {
            if (_pendingBuilding == null)
                return;
            if (Time.unscaledTime > _pendingDeadline)
            {
                _pendingBuilding = null;
                return;
            }

            var navItem = _pendingBuilding.GetComponentInChildren<UINavigationItem>(includeInactive: false);
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navItem == null || navSystem == null)
                return;
            if (!navItem.isSelectable || !navItem.gameObject.activeInHierarchy
                || !navSystem.AvailableNavigationItems.Contains(navItem))
                return; // still (re)activating after the scroll — try again next frame

            NavigationHelper.FocusItem(navItem);
            _ringCurrent = _pendingBuilding;
            _pendingBuilding = null;
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
                _balloonAnnounced = false;
                _balloonLoadingSaid = false;
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

            // Daily Voyage balloon: a fixed-deck preview whose deck cards carry
            // nav items too. Surface only the real buttons (Let's Go, Scores)
            // and give the date, deck and modifiers as a one-shot summary.
            if (HandleBalloonOverlay(overlay))
                return;

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

        /// <summary>
        /// The Daily Voyage balloon preview. Its deck cards and modifier icons
        /// carry navigation items but do nothing here, so focus must skip them:
        /// only the Let's Go and Scores buttons are real. Summarize the daily
        /// (date, deck, modifiers) once, then browse just the buttons. Returns
        /// false when this overlay is not a balloon, or has no buttons yet
        /// (still loading, or no connection) so the generic text read speaks.
        /// </summary>
        private bool HandleBalloonOverlay(BuildingDisplay overlay)
        {
            var balloon = overlay.GetComponentInChildren<BalloonSequence>(includeInactive: false);
            if (balloon == null)
                return false;

            RefreshBalloonButtons(overlay, balloon);

            // Still fetching the date and building the deck: own the overlay so
            // focus can't land on half-loaded cards, and say so once. The
            // no-connection and first-time-help panels are HelpPanelSystem
            // popups that PopupReader reads independently of this handler.
            if (_overlayItems.Count == 0)
            {
                if (!_balloonLoadingSaid)
                {
                    _balloonLoadingSaid = true;
                    ScreenReader.SayEvent(Loc.Get("balloon_loading"), interrupt: true);
                }
                return true;
            }

            if (NavigationHelper.IsConfirmPressed())
            {
                var item = _overlaySelected ?? _overlayItems[0];
                // Let's Go fires a wired button; call the game's own method
                // directly so keyboard confirm can't miss it. Scores has no
                // public entry point, so drive it through the generic press.
                if (IsPartOf(item, _balloonPlay))
                {
                    DebugLogger.LogInput(Name, "Balloon: Let's Go");
                    balloon.Continue();
                }
                else
                {
                    ActivateOverlayItem();
                }
                return true;
            }

            NavDirection dir = NavigationHelper.GetNavigationInput();
            if (dir != NavDirection.None)
            {
                bool vertical = dir == NavDirection.Up || dir == NavDirection.Down;
                var next = NavigationHelper.NavigateLinear(_overlayItems, _overlaySelected, dir, vertical)
                    ?? _overlayItems[0];
                _overlaySelected = next;
                int index = _overlayItems.IndexOf(next);
                ScreenReader.Say(DescribeBalloonButton(next, index, _overlayItems.Count), interrupt: true);
                return true;
            }

            // Summarize once the buttons exist, and re-read on I.
            bool reRead = Input.GetKeyDown(KeyCode.I) && !NavigationHelper.IsTextInputFocused();
            if (_balloonAnnounced && !reRead)
                return true;

            _balloonAnnounced = true;
            ScreenReader.SayEvent(BuildBalloonSummary(overlay, balloon), interrupt: true);
            return true;
        }

        /// <summary>Collect only the balloon's real buttons (Let's Go, Scores),
        /// skipping the deck cards' navigation items.</summary>
        private void RefreshBalloonButtons(BuildingDisplay overlay, BalloonSequence balloon)
        {
            _balloonPlay = ReflectionUtil.GetField<GameObject>(balloon, "playButton");
            _balloonScores = ReflectionUtil.GetField<GameObject>(balloon, "scoresButton");

            _overlayItems.Clear();
            foreach (var item in overlay.GetComponentsInChildren<UINavigationItem>(includeInactive: false))
            {
                if (item == null || !item.isSelectable || !item.enabled
                    || !item.gameObject.activeInHierarchy || item.clickHandler == null)
                    continue;
                if (!IsPartOf(item, _balloonPlay) && !IsPartOf(item, _balloonScores))
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

        /// <summary>A balloon button as "label. what it does. N of M".</summary>
        private string DescribeBalloonButton(UINavigationItem item, int index, int total)
        {
            string label = null;
            var tmp = item.GetComponentInChildren<TMP_Text>(includeInactive: false);
            if (tmp != null)
            {
                string s = TextProcessor.StripRichText(tmp.text);
                if (!string.IsNullOrEmpty(s))
                    label = s;
            }
            if (string.IsNullOrEmpty(label))
                label = ScreenHandler.CleanName(item.gameObject.name);

            string desc = IsPartOf(item, _balloonPlay) ? Loc.Get("balloon_play_desc")
                : IsPartOf(item, _balloonScores) ? Loc.Get("balloon_scores_desc")
                : null;

            return string.IsNullOrEmpty(desc)
                ? Loc.Get("overlay_item", label, index + 1, total)
                : label + ". " + desc + " " + Loc.Get("overlay_position", index + 1, total);
        }

        /// <summary>Title, date, fixed deck, modifiers, and the button hint.</summary>
        private string BuildBalloonSummary(BuildingDisplay overlay, BalloonSequence balloon)
        {
            var parts = new List<string>();

            var titleT = ReflectionUtil.GetField<TMP_Text>(balloon, "title");
            string title = titleT != null ? TextProcessor.StripRichText(titleT.text) : null;
            if (string.IsNullOrEmpty(title))
                title = OverlayBuildingName(overlay);
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            var dateT = ReflectionUtil.GetField<TMP_Text>(balloon, "date");
            if (dateT != null && dateT.gameObject.activeInHierarchy)
            {
                string d = TextProcessor.StripRichText(dateT.text);
                if (!string.IsNullOrEmpty(d))
                    parts.Add(d);
            }

            string deck = ListBalloonDeck(overlay);
            if (!string.IsNullOrEmpty(deck))
                parts.Add(deck);

            string mods = ListBalloonModifiers();
            if (!string.IsNullOrEmpty(mods))
                parts.Add(mods);

            parts.Add(Loc.Get("balloon_buttons_hint"));
            return string.Join(". ", parts);
        }

        /// <summary>The daily's fixed deck, grouping duplicate cards.</summary>
        private static string ListBalloonDeck(BuildingDisplay overlay)
        {
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            int total = 0;
            foreach (var card in overlay.GetComponentsInChildren<Card>(includeInactive: false))
            {
                string title = card?.entity?.data?.title;
                if (string.IsNullOrEmpty(title))
                    continue;
                total++;
                if (counts.ContainsKey(title))
                    counts[title]++;
                else { counts[title] = 1; order.Add(title); }
            }
            if (order.Count == 0)
                return null;

            var names = new List<string>();
            foreach (string title in order)
                names.Add(counts[title] > 1
                    ? Loc.Get("card_count_multiple", title, counts[title])
                    : title);
            return Loc.Get("balloon_deck", total, string.Join(", ", names));
        }

        /// <summary>The daily's visible modifiers by name (the bell icons, ...).</summary>
        private static string ListBalloonModifiers()
        {
            List<GameModifierData> mods = null;
            try { mods = Campaign.Data?.Modifiers; }
            catch { /* campaign not ready */ }
            if (mods == null || mods.Count == 0)
                return null;

            var names = new List<string>();
            foreach (var m in mods)
            {
                if (m == null || !m.visible)
                    continue;
                string n = null;
                try { n = TextProcessor.StripRichText(m.titleKey.GetLocalizedString()); }
                catch { /* localization not ready */ }
                if (string.IsNullOrEmpty(n))
                    n = ScreenHandler.CleanName(m.name);
                if (!string.IsNullOrEmpty(n))
                    names.Add(n);
            }
            if (names.Count == 0)
                return null;
            return Loc.Get("balloon_modifiers", names.Count, string.Join(", ", names));
        }

        /// <summary>Whether a navigation item belongs to the given GameObject's subtree.</summary>
        private static bool IsPartOf(UINavigationItem item, GameObject root)
        {
            if (root == null || item == null)
                return false;
            Transform rootT = root.transform;
            if (item.transform == rootT || item.transform.IsChildOf(rootT))
                return true;
            return item.clickHandler != null
                && (item.clickHandler.transform == rootT || item.clickHandler.transform.IsChildOf(rootT));
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

                // The Gate starts or continues the journey — say which.
                // The Daily Voyage balloon does the same for the daily run.
                if (IsGate(building))
                    desc += ". " + GetGateAction();
                else if (IsBalloon(building))
                    desc += ". " + GetBalloonAction();

                // Verbose focus folds in the building's help text (what I reads).
                // In short focus we still fold it in when the name alone says
                // nothing (e.g. "Balloon"); named-and-stated buildings keep the
                // help in the Details buffer (Ctrl+Up) / on I. The Gate and
                // balloon already carry their own action line, so skip them.
                if (ItemDescriber.VerboseFocus
                    || (!IsGate(building) && !IsBalloon(building)
                        && ItemDescriber.BuildingFocusIsBareName(building)))
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
            else if (IsBalloon(building))
                summary += ". " + GetBalloonAction();
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

        /// <summary>The Daily Voyage balloon carries a BuildingBalloon component.</summary>
        private static bool IsBalloon(Building building)
        {
            return building.GetComponent<BuildingBalloon>() != null;
        }

        /// <summary>
        /// Mirror BuildingBalloon.Select: a daily run in progress continues,
        /// otherwise the balloon opens the preview to start a fresh daily.
        /// </summary>
        private static string GetBalloonAction()
        {
            try
            {
                var dailyMode = AddressableLoader.Get<GameMode>("GameMode", "GameModeDaily");
                if (dailyMode != null && Campaign.CheckContinue(dailyMode))
                    return Loc.Get("balloon_continue_run");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TownHandler",
                    $"Balloon state check failed: {ex.Message}");
            }

            return Loc.Get("balloon_start_run");
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
