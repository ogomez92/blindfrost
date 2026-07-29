using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Movement and activation inside the journal: Up/Down through the page's
    /// column-major content (focusable items then virtual rows), Left/Right for
    /// tabs or setting values, and Enter to activate tabs, lore pages and rows.
    /// </summary>
    public partial class PauseMenuHandler
    {
        /// <summary>
        /// The journal's tabs form a vertical strip on the book's edge, so
        /// spatial navigation kept trapping focus there. Instead: Up/Down move
        /// through the PAGE content only (left page top to bottom, then right
        /// page — column-major). Left/Right switch tabs — except on a setting
        /// row, where they adjust its value via OnHorizontalOverride, exactly
        /// what the game invokes for gamepad left/right.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;

            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                if (current != null && current.overrideHorizontal
                    && current.OnHorizontalOverride != null)
                {
                    DebugLogger.LogInput(Name, "Adjust setting");
                    current.OnHorizontalOverride.Invoke(dir == NavDirection.Right ? 1f : -1f);
                    string value = GetSettingValue(current)
                        ?? DescribeRowTexts(current.clickHandler != null
                            ? current.clickHandler.transform : current.transform);
                    ScreenReader.Say(value ?? Loc.Get("no_info_available"), interrupt: true);
                    return;
                }

                var tabs = GetTabItems();
                if (tabs.Count == 0)
                {
                    ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                    return;
                }
                tabs.Sort(CompareColumnMajor);
                var nextTab = NavigationHelper.NavigateLinear(tabs, current, dir, vertical: false);
                if (nextTab != null)
                    NavigationHelper.FocusItem(nextTab);
                return;
            }

            // Up/Down walk the page: focusable content items first, then
            // "virtual rows" — entries like the challenges list, battle log,
            // stats and lore pages, which the game builds without nav items.
            var content = GetContentItems();
            content.Sort(CompareColumnMajor);
            _virtualRows = GetVirtualRows(content);
            if (content.Count == 0 && _virtualRows.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

            int total = content.Count + _virtualRows.Count;
            int index = current != null ? content.IndexOf(current) : -1;
            if (index < 0 && _virtualIndex >= 0 && _virtualIndex < _virtualRows.Count)
                index = content.Count + _virtualIndex;

            bool forward = dir == NavDirection.Down;
            if (index < 0)
                index = forward ? 0 : total - 1;
            else
            {
                index += forward ? 1 : -1;
                if (index >= total) index = 0;
                if (index < 0) index = total - 1;
            }

            if (index < content.Count)
            {
                _virtualIndex = -1;
                NavigationHelper.FocusItem(content[index]);
            }
            else
            {
                _virtualIndex = index - content.Count;
                ScreenReader.Say(_virtualRows[_virtualIndex].Text, interrupt: true);
            }
        }

        /// <summary>Position within the current page's virtual rows.</summary>
        private int _virtualIndex = -1;

        /// <summary>The virtual rows as last built (for Enter activation).</summary>
        private List<VirtualRow> _virtualRows = new List<VirtualRow>();

        /// <summary>
        /// Column-major reading order: leftmost column first, top to bottom.
        /// Positions are bucketed so the book's two pages become two columns.
        /// </summary>
        private static int CompareColumnMajor(UINavigationItem a, UINavigationItem b)
        {
            return CompareColumnMajorPosition(a.Position, b.Position);
        }

        private static int CompareColumnMajorPosition(Vector3 a, Vector3 b)
        {
            int columnA = Mathf.RoundToInt(a.x / 2f);
            int columnB = Mathf.RoundToInt(b.x / 2f);
            if (columnA != columnB)
                return columnA.CompareTo(columnB);
            return b.y.CompareTo(a.y);
        }

        /// <summary>Items that belong to the tab strip.</summary>
        private List<UINavigationItem> GetTabItems()
        {
            var tabs = new List<UINavigationItem>();
            foreach (var item in GetItems())
            {
                if (FindInParents<JournalTab>(item) != null)
                    tabs.Add(item);
            }
            tabs.Sort(CompareColumnMajor);
            return tabs;
        }

        /// <summary>Page content: everything on the layer that is not a tab.</summary>
        private List<UINavigationItem> GetContentItems()
        {
            var content = new List<UINavigationItem>();
            foreach (var item in GetItems())
            {
                if (FindInParents<JournalTab>(item) == null)
                    content.Add(item);
            }
            return content;
        }

        /// <summary>
        /// Enter activates the focused item. When the game maps Enter to its
        /// Rewired "Select" action, its CustomEventSystem clicks the hovered
        /// object itself (kept in sync with our focus) — clicking again here
        /// would double-activate, so we only click manually when the game
        /// did not see this press.
        /// </summary>
        protected override void Confirm()
        {
            // Standing on a virtual row: activate it if it has an action
            // (lore pages open); read-only entries say so.
            if (_virtualIndex >= 0 && _virtualIndex < _virtualRows.Count)
            {
                var row = _virtualRows[_virtualIndex];
                if (row.Activate != null)
                {
                    DebugLogger.LogInput(Name, "Activate virtual row");
                    try { row.Activate(); }
                    catch { /* row's object may be gone after a page switch */ }
                    _virtualIndex = -1;
                    ResetFocusTracking();
                }
                else
                {
                    ScreenReader.Say(Loc.Get("row_not_interactive"), interrupt: true);
                }
                return;
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            if (current == null) return;

            bool gameClicks = false;
            try { gameClicks = InputSystem.IsSelectPressed() && current.clickHandler != null; }
            catch { /* Rewired not ready */ }

            var tab = FindInParents<JournalTab>(current);
            if (tab != null)
            {
                // Always run the manual select: it deterministically targets THIS
                // tab. If the game's Select also clicks (hover is synced to the
                // same tab), the duplicate select of the same tab is harmless.
                DebugLogger.LogInput(Name, "Select tab: " + tab.gameObject.name);
                tab.Hover();
                tab.Press();
                tab.Release(); // fires Select() while hovered
                tab.UnHover();

                string name = GetTabLabel(current, tab);
                if (!string.IsNullOrEmpty(name))
                    ScreenReader.Say(Loc.Get("pause_tab_opened", name), interrupt: true);
                ResetFocusTracking();
                return;
            }

            // Lore pages: open AND read the story (a plain click only opens)
            var lorePage = FindInParents<LorePage>(current);
            if (lorePage != null)
            {
                DebugLogger.LogInput(Name, "Open lore page: " + lorePage.gameObject.name);
                OpenLorePage(lorePage);
                return;
            }

            if (gameClicks)
            {
                DebugLogger.LogInput(Name, "Confirm (game click)");
                return;
            }
            base.Confirm();
        }
    }
}
