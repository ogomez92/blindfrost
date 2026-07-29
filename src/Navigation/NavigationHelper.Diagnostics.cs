using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Debug logging helpers: Enter-press input diagnostics and the full
    /// navigation-state dump, with the view test and path formatting they use.
    /// </summary>
    public static partial class NavigationHelper
    {
        /// <summary>
        /// Debug: log every input path that could react to this Enter press —
        /// Rewired Select/Back, our nav focus, the game's hover, and Unity's
        /// uGUI selection. Identifies which system performs phantom clicks.
        /// </summary>
        public static void LogEnterDiagnostic()
        {
            bool select = false, back = false;
            try
            {
                select = InputSystem.IsSelectPressed();
                back = InputSystem.IsButtonPressed("Back");
            }
            catch { /* Rewired not ready */ }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem != null ? navSystem.currentNavigationItem : null;

            GameObject hover = null;
            var gameEventSystem = ReflectionUtil.GetField<CustomEventSystem>(navSystem, "eventSystem");
            if (gameEventSystem != null)
                hover = ReflectionUtil.GetField<GameObject>(gameEventSystem, "current");

            var unitySelected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;

            DebugLogger.Log(DebugLogger.LogCategory.State, "EnterDiag",
                $"rewiredSelect={select} rewiredBack={back}"
                + $" | navFocus={(current != null ? GetPath(current.transform) : "null")}"
                + $" | gameHover={(hover != null ? GetPath(hover.transform) : "null")}"
                + $" | unitySelected={(unitySelected != null ? GetPath(unitySelected.transform) : "null")}");
        }

        /// <summary>
        /// Dump the full navigation state to the debug log: active layer, current
        /// item, and every registered item with its layer, flags, and notable
        /// components. For diagnosing screens whose items aren't where we expect.
        /// </summary>
        public static void DumpNavigationState()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null)
            {
                DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump", "No UINavigationSystem");
                return;
            }

            var layer = UINavigationSystem.ActiveNavigationLayer;
            DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                $"ActiveLayer: {(layer != null ? GetPath(layer.transform) + "#" + layer.GetInstanceID() : "null")}");
            var current = navSystem.currentNavigationItem;
            DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                $"Current: {(current != null ? GetPath(current.transform) : "null")}");
            DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                $"Registered items: {navSystem.AvailableNavigationItems.Count}");

            foreach (var item in navSystem.AvailableNavigationItems)
            {
                if (item == null) continue;

                var flags = new List<string>();
                if (!item.enabled) flags.Add("disabled");
                if (!item.isSelectable) flags.Add("notSelectable");
                if (!item.gameObject.activeInHierarchy) flags.Add("inactive");
                if (item.ignoreLayers) flags.Add("ignoreLayers");
                if (item.overrideHorizontal) flags.Add("overrideH");
                if (item.overrideVertical) flags.Add("overrideV");
                if (item.clickHandler == null) flags.Add("noClick");

                // On-screen state: the game's CheckLayer treats an off-screen
                // item as non-navigable, so this reveals items that have scrolled
                // out of reach (e.g. the town Gate after moving into town).
                if (!IsInView(item)) flags.Add("offScreen");

                var comps = new List<string>();
                var journalTab = item.GetComponentInParent<JournalTab>();
                if (journalTab != null) comps.Add("JournalTab:" + journalTab.gameObject.name);
                if (item.GetComponentInParent<SettingOptions>() != null) comps.Add("SettingOptions");
                if (item.GetComponentInParent<SettingSlider>() != null) comps.Add("SettingSlider");
                if (item.GetComponentInChildren<TMPro.TMP_Dropdown>(true) != null) comps.Add("Dropdown");
                if (item.GetComponentInChildren<UnityEngine.UI.Slider>(true) != null) comps.Add("Slider");

                DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                    $"{GetPath(item.transform)}"
                    + $" | layer={(item.navigationLayer != null ? item.navigationLayer.name + "#" + item.navigationLayer.GetInstanceID() : "null")}"
                    + (flags.Count > 0 ? " | " + string.Join(",", flags) : "")
                    + (comps.Count > 0 ? " | " + string.Join(",", comps) : ""));
            }

            // Buildings including inactive ones, to tell "scrolled off-screen but
            // still registered" apart from "deactivated and unregistered".
            var buildings = Object.FindObjectsOfType<Building>(includeInactive: true);
            if (buildings.Length > 0)
            {
                DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                    $"Buildings (incl. inactive): {buildings.Length}");
                foreach (var b in buildings)
                {
                    if (b == null) continue;
                    var nav = b.GetComponentInChildren<UINavigationItem>(includeInactive: true);
                    bool registered = nav != null && navSystem.AvailableNavigationItems.Contains(nav);
                    DebugLogger.Log(DebugLogger.LogCategory.State, "NavDump",
                        $"{GetPath(b.transform)}"
                        + $" | active={b.gameObject.activeInHierarchy}"
                        + $" | navItem={(nav != null ? "yes" : "none")}"
                        + $" | registered={registered}"
                        + (nav != null ? $" | inView={IsInView(nav)}" : ""));
                }
            }
        }

        /// <summary>
        /// Whether an item's position is inside the main camera's view — the same
        /// test the game's CheckLayer uses to decide if an item is navigable.
        /// </summary>
        private static bool IsInView(UINavigationItem item)
        {
            var cam = Camera.main;
            if (cam == null) return true;
            Vector3 v = cam.WorldToViewportPoint(item.Position);
            return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        }

        /// <summary>Hierarchy path of a transform, up to 6 ancestors deep.</summary>
        private static string GetPath(Transform t)
        {
            var parts = new List<string>();
            int depth = 0;
            while (t != null && depth < 6)
            {
                parts.Insert(0, t.name);
                t = t.parent;
                depth++;
            }
            if (t != null) parts.Insert(0, "...");
            return string.Join("/", parts);
        }
    }
}
