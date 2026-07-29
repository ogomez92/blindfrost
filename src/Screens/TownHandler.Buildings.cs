using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// How a town building is read: the focus summary, the Details buffer
    /// (Ctrl+Up) and the in-game help text behind I — plus the line that says
    /// what the Gate and the Daily Voyage balloon will do when pressed.
    /// </summary>
    public partial class TownHandler
    {
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
    }
}
