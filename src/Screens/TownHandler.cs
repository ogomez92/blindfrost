using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Town screen — the game's home base.
    /// Describes each building with its localized name, purpose, and state,
    /// and tells the player what the Gate will do (new run, continue, tutorial).
    /// </summary>
    public partial class TownHandler : NavigableScreenHandler
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

        // The unlock buildings (Tribe Hall, Pet House, Inventor's Hut, companion
        // hut, Icebreaker Hut): their contents are browsed as named entries
        // rather than dumped as loose text. _detailTitle names the entry whose
        // detail panel is open, so the panel does not read out anonymously.
        private List<TownUnlockReader.Entry> _unlockEntries = new List<TownUnlockReader.Entry>();
        private int _unlockIndex;
        private string _detailTitle;

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
            _unlockEntries.Clear();
            _unlockIndex = 0;
            _detailTitle = null;
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
                _unlockEntries.Clear();
                _unlockIndex = 0;
                _detailTitle = null;
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
