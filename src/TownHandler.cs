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

        protected override bool TryAnnounceScreen()
        {
            string msg = Loc.Get("screen_town");
            string hint = HintOnce("town_hint");
            if (hint != null)
                msg += " " + hint;
            ScreenReader.Say(msg, interrupt: true);
            return true;
        }

        protected override void HandleInput()
        {
            base.HandleInput();

            // I: read the focused building's in-game help text
            if (Input.GetKeyDown(KeyCode.I))
            {
                DebugLogger.LogInput(Name, "Info");
                AnnounceFocusedBuildingHelp();
            }
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

                return desc;
            }

            return base.GetItemDescription(item);
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

            try
            {
                if (building.type.helpKey.IsEmpty)
                {
                    // No in-game help — the Gate description is all there is
                    ScreenReader.Say(GetItemDescription(current), interrupt: true);
                    return;
                }

                // helpKey is packed as "title|body|note"
                string packed = building.type.helpKey.GetLocalizedString();
                string[] segments = packed.Split('|');
                var parts = new System.Collections.Generic.List<string>();
                foreach (string segment in segments)
                {
                    string clean = TextProcessor.StripRichText(segment)?.Trim();
                    if (!string.IsNullOrEmpty(clean))
                        parts.Add(clean);
                }

                ScreenReader.Say(parts.Count > 0
                    ? string.Join(". ", parts)
                    : Loc.Get("no_info_available"), interrupt: true);
            }
            catch (System.Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TownHandler",
                    $"Building help failed: {ex.Message}");
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
            }
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_town");
        }
    }
}
