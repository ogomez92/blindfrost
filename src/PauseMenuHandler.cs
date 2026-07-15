using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the pause menu / journal (settings, battle log,
    /// lore pages). The menu lives in the persistent PauseScreen scene and never
    /// changes ActiveSceneKey, so ScreenManager routes here via GameManager.paused.
    ///
    /// Navigation follows the game's active navigation layer exactly: the game's
    /// UINavigationSystem clears any focus that is off-layer every frame and
    /// re-focuses a default item, so offering off-layer items just fights it
    /// (that caused the "stuck cursor" bug). Tabs and pages swap the active
    /// layer; the base class re-announces on layer changes.
    ///
    /// Up/Down move between items, Left/Right change a setting's value (the same
    /// OnHorizontalOverride path the game uses for gamepad input), Enter activates
    /// buttons and tabs. O closes the menu again (global toggle in Main).
    /// </summary>
    public class PauseMenuHandler : NavigableScreenHandler
    {
        public override string Name => "PauseMenu";

        protected override float AnnounceDelay => 0.3f;

        public override void OnEnter()
        {
            base.OnEnter();
            _virtualIndex = -1;
        }

        protected override void OnNavigationLayerChanged(UINavigationLayer layer)
        {
            base.OnNavigationLayerChanged(layer);
            _virtualIndex = -1; // page changed — the virtual rows are new
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // The main menu's buttons stay registered (and clickable) behind the
            // open journal, and the game clicks its hovered object when Enter
            // (Rewired "Select") is pressed. Keep the hover glued to our focus
            // so Enter can never hit anything behind the menu. On a virtual row
            // there is no valid click target at all — clear the hover instead,
            // or a game-side Enter would click the previously focused item.
            if (_virtualIndex >= 0)
                NavigationHelper.ClearHover();
            else
                NavigationHelper.SyncHoverToFocus();

            // If something else moved the real focus, we are no longer standing
            // on a virtual row — Enter must act on the focused item again.
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var focus = navSystem != null ? navSystem.currentNavigationItem : null;
            if (focus != _lastSeenFocus)
            {
                _lastSeenFocus = focus;
                if (focus != null)
                    _virtualIndex = -1;
            }
        }

        private UINavigationItem _lastSeenFocus;

        /// <summary>
        /// Tab / Shift+Tab step through the page; T jumps to the tab strip;
        /// Escape goes back one level (sub-pages like a settings category swap
        /// in their own navigation layer, hiding the tab strip — Back is the
        /// only way out, and the game maps it to gamepad only).
        /// </summary>
        protected override void HandleInput()
        {
            base.HandleInput();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                bool backward = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                Navigate(backward ? NavDirection.Up : NavDirection.Down);
            }

            // T: guaranteed way to the tab strip — a page made up entirely of
            // setting rows has no Left/Right route out (they adjust values).
            if (Input.GetKeyDown(KeyCode.T))
            {
                DebugLogger.LogInput(Name, "Jump to tabs");
                var tabs = GetTabItems();
                if (tabs.Count > 0)
                    NavigationHelper.FocusItem(tabs[0]);
                else
                    ScreenReader.Say(Loc.Get("pause_no_tabs"), interrupt: true);
            }

            if (NavigationHelper.IsBackPressed())
            {
                DebugLogger.LogInput(Name, "Back");
                GoBack();
            }
        }

        /// <summary>
        /// Trigger the same back action the gamepad "Back" button fires:
        /// the BackButtonGamePadController for the active layer. Falls back
        /// to closing the menu so Escape always leads out.
        /// </summary>
        private void GoBack()
        {
            // An opened lore page overlays the journal — close it first
            foreach (var manager in Object.FindObjectsOfType<LorePageManager>())
            {
                var focusLayer = ReflectionUtil.GetField<GameObject>(manager, "focusLayer");
                if (focusLayer != null && focusLayer.activeSelf)
                {
                    manager.DisableFocusLayer();
                    ScreenReader.Say(Loc.Get("pause_lore_closed"), interrupt: true);
                    return;
                }
            }

            var activeLayer = UINavigationSystem.ActiveNavigationLayer;
            BackButtonGamePadController match = null;
            BackButtonGamePadController any = null;

            foreach (var controller in Object.FindObjectsOfType<BackButtonGamePadController>())
            {
                if (controller == null) continue;
                if (any == null) any = controller;
                if (controller.uINavigationLayer != null
                    && controller.uINavigationLayer == activeLayer)
                {
                    match = controller;
                    break;
                }
            }

            var back = match ?? any;
            if (back != null)
            {
                if (back.OnBackButtonOverride != null
                    && back.OnBackButtonOverride.GetPersistentEventCount() > 0)
                {
                    back.OnBackButtonOverride.Invoke();
                    ResetFocusTracking();
                    return;
                }
                if (back.backButton != null)
                {
                    back.backButton.onClick.Invoke();
                    ResetFocusTracking();
                    return;
                }
            }

            // No back controller found: close the whole menu
            var menu = Object.FindObjectOfType<PauseMenu>();
            if (menu != null)
            {
                menu.Close();
                ScreenReader.Say(Loc.Get("pause_closed"), interrupt: true);
            }
        }

        protected override bool TryAnnounceScreen()
        {
            if (GetItems().Count == 0)
                return false; // menu still opening / layer not registered yet

            string msg = Loc.Get("screen_pause");
            string hint = HintOnce("pause_hint");
            if (hint != null)
                msg += " " + hint;
            ScreenReader.SayEvent(msg, interrupt: true);
            return true;
        }

        // GetItems: intentionally NOT overridden. The base implementation returns
        // the items on the game's active navigation layer — the same set the
        // game itself allows focus on while the menu is open. Anything else gets
        // cleared by UINavigationSystem.Update on the next frame.

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

        /// <summary>A page entry without a navigation item: spoken text plus an optional action.</summary>
        private struct VirtualRow
        {
            public string Text;
            public System.Action Activate;
            public Transform Anchor;
            public int Order;
        }

        /// <summary>
        /// Rows on the current page that the game builds without navigation
        /// items: challenges (text + progress), battle log lines, stats, and
        /// lore page buttons (activatable). Entries that do have a navigation
        /// item in our content list are skipped to avoid double announcements.
        /// Ordered column-major like the focusable content.
        /// </summary>
        private List<VirtualRow> GetVirtualRows(List<UINavigationItem> contentItems)
        {
            var rows = new List<VirtualRow>();

            foreach (var entry in Object.FindObjectsOfType<ChallengeEntry>())
                AddVirtualRow(rows, contentItems, entry.transform, DescribeRowTexts(entry), null);

            foreach (var entry in Object.FindObjectsOfType<BattleLogEntry>())
                AddVirtualRow(rows, contentItems, entry.transform, DescribeRowTexts(entry), null);

            foreach (var stat in Object.FindObjectsOfType<StatDisplay>())
                AddVirtualRow(rows, contentItems, stat.transform, DescribeRowTexts(stat), null);

            // The "Overall Statistics" page has no per-row components — the
            // game renders it into a few large text blocks (names and values
            // in parallel columns). Split those into one row per stat.
            foreach (var stats in Object.FindObjectsOfType<OverallStatsDisplay>())
                AddOverallStatRows(rows, contentItems, stats);

            foreach (var page in Object.FindObjectsOfType<LorePage>())
            {
                LorePage captured = page;
                AddVirtualRow(rows, contentItems, page.transform,
                    DescribeLorePage(page), () => OpenLorePage(captured));
            }

            rows.Sort((a, b) =>
            {
                int byPosition = CompareColumnMajorPosition(a.Anchor.position, b.Anchor.position);
                // List.Sort is unstable and stat lines share their block's anchor
                return byPosition != 0 ? byPosition : a.Order.CompareTo(b.Order);
            });
            return rows;
        }

        /// <summary>
        /// Stat rows of the "Overall Statistics" page. OverallStatsDisplay
        /// writes the whole page into a few large TMP texts: names in one
        /// block, values in a parallel block, lines separated by br tags
        /// (centred locales inline the value into the name block instead).
        /// Split the blocks and re-pair the lines into one row per stat.
        /// </summary>
        private static void AddOverallStatRows(List<VirtualRow> rows,
            List<UINavigationItem> contentItems, OverallStatsDisplay display)
        {
            var nameGroups = ReflectionUtil.GetField<TMP_Text[]>(display, "nameGroups");
            var valueGroups = ReflectionUtil.GetField<TMP_Text[]>(display, "valueGroups");
            if (nameGroups == null) return;

            for (int group = 0; group < nameGroups.Length; group++)
            {
                var nameBlock = nameGroups[group];
                if (nameBlock == null) continue;

                var valueBlock = valueGroups != null && group < valueGroups.Length
                    ? valueGroups[group] : null;
                string[] names = SplitBlockLines(nameBlock.text);
                string[] values = valueBlock != null ? SplitBlockLines(valueBlock.text) : new string[0];

                for (int line = 0; line < names.Length; line++)
                {
                    string name = names[line];
                    if (string.IsNullOrEmpty(name)) continue; // blank separator line
                    string value = line < values.Length ? values[line] : null;
                    if (value == "-")
                        value = Loc.Get("stat_no_value");
                    string text = string.IsNullOrEmpty(value) ? name : name + " " + value;
                    AddVirtualRow(rows, contentItems, nameBlock.transform, text, null);
                }
            }
        }

        /// <summary>Split a stats text block into plain-text lines.</summary>
        private static string[] SplitBlockLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string[0];
            string[] lines = text.Split(new[] { "<br>", "\n" }, System.StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = TextProcessor.StripRichText(lines[i]);
            return lines;
        }

        /// <summary>Add a virtual row unless it is already reachable as a focusable item.</summary>
        private static void AddVirtualRow(List<VirtualRow> rows, List<UINavigationItem> contentItems,
            Transform anchor, string text, System.Action activate)
        {
            if (anchor == null || string.IsNullOrEmpty(text)) return;
            if (!anchor.gameObject.activeInHierarchy) return;

            foreach (var item in contentItems)
            {
                if (item != null && (item.transform.IsChildOf(anchor) || anchor.IsChildOf(item.transform)))
                    return; // already navigable the normal way
            }

            rows.Add(new VirtualRow { Text = text, Activate = activate, Anchor = anchor, Order = rows.Count });
        }

        /// <summary>
        /// Lore page entry. The page's whole story canvas doubles as the
        /// button's face, so reading its texts gives every page the same
        /// header — identify pages by number and data asset name instead.
        /// </summary>
        private static string DescribeLorePage(LorePage page)
        {
            // Prefab names identify pages well: LorePageWildfrost -> "Lore Page Wildfrost"
            string text = CleanName(page.gameObject.name);
            if (string.IsNullOrEmpty(text) || text == "unknown")
                text = Loc.Get("pause_lore_page") + " " + (page.transform.GetSiblingIndex() + 1);

            if (!page.isUnlocked)
                return text + ", " + Loc.Get("pause_lore_locked");

            if (page.isNew)
                text += ", " + Loc.Get("pause_lore_new");
            return text + ". " + Loc.Get("pause_lore_open_hint");
        }

        /// <summary>Open a lore page and read its full story text aloud.</summary>
        private static void OpenLorePage(LorePage page)
        {
            if (!page.isUnlocked)
            {
                page.Select(); // plays the game's deny feedback
                ScreenReader.Say(Loc.Get("pause_lore_locked"), interrupt: true);
                return;
            }

            page.Select();

            var root = page.canvas != null ? (Component)page.canvas : page;
            var blocks = new List<TMP_Text>(root.GetComponentsInChildren<TMP_Text>(false));
            // Reading order: top to bottom, left to right. The canvas scales
            // uniformly while opening, so relative positions stay valid.
            blocks.Sort((a, b) =>
            {
                int byHeight = b.transform.position.y.CompareTo(a.transform.position.y);
                return byHeight != 0
                    ? byHeight
                    : a.transform.position.x.CompareTo(b.transform.position.x);
            });

            var parts = new List<string>();
            foreach (var text in blocks)
            {
                if (text == null) continue;
                string value = TextProcessor.StripRichText(text.text?.Trim());
                if (!string.IsNullOrEmpty(value) && !parts.Contains(value))
                    parts.Add(value);
                if (parts.Count >= 40) break;
            }

            if (parts.Count > 0)
                ScreenReader.Say(string.Join(". ", parts) + " " + Loc.Get("pause_lore_close_hint"));
            else
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
        }

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

        protected override string GetItemDescription(UINavigationItem item)
        {
            // Setting rows: label plus current value plus a one-time adjust hint
            string value = GetSettingValue(item);
            if (value != null)
            {
                string label = GetButtonText(item);
                string text = string.IsNullOrEmpty(label) || label == value
                    ? value
                    : label + ", " + value;
                string hint = HintOnce("setting_adjust_hint");
                if (hint != null)
                    text += ". " + hint;
                return text;
            }

            // Journal tabs (battle log, settings, lore pages). Parent-chain only:
            // matching children would misreport container items as tabs.
            var tab = FindInParents<JournalTab>(item);
            if (tab != null)
            {
                string name = GetTabLabel(item, tab);
                return string.IsNullOrEmpty(name)
                    ? Loc.Get("pause_tab")
                    : Loc.Get("pause_tab_named", name);
            }

            // Lore page buttons ARE focusable, so they arrive here — the
            // generic text walk reads every one as "Lore" (the story canvas
            // doubles as the button face). Name them properly instead.
            var lorePage = FindInParents<LorePage>(item);
            if (lorePage != null)
                return DescribeLorePage(lorePage);

            string description = base.GetItemDescription(item);
            if (string.IsNullOrEmpty(description))
                description = DescribeRowTexts(
                    item.clickHandler != null ? item.clickHandler.transform : item.transform);

            // Adjustable rows whose value control we couldn't identify still
            // deserve the left/right hint — the game will adjust them anyway
            if (!string.IsNullOrEmpty(description) && item.overrideHorizontal)
            {
                string hint = HintOnce("setting_adjust_hint");
                if (hint != null)
                    description += ". " + hint;
            }
            return description;
        }

        /// <summary>
        /// Label of a journal tab. Its text may sit on the inactive
        /// selected/unselected sub-group, and the generic hierarchy walk leaks
        /// the book's page title ("Journal") in — so look inside the tab first,
        /// then fall back to the tab's object name (TabCards → "Cards").
        /// </summary>
        private string GetTabLabel(UINavigationItem item, JournalTab tab)
        {
            foreach (var text in tab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;
                string value = TextProcessor.StripRichText(text.text?.Trim());
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            string name = CleanName(tab.gameObject.name);
            if (!string.IsNullOrEmpty(name))
            {
                if (name.StartsWith("Tab "))
                    name = name.Substring(4);
                return name;
            }

            return GetButtonText(item);
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_pause");
        }

        // ---- Setting rows -------------------------------------------------

        /// <summary>Find a component on the item or its click handler, parents only.</summary>
        private static T FindInParents<T>(UINavigationItem item) where T : Component
        {
            var comp = item.GetComponentInParent<T>();
            if (comp == null && item.clickHandler != null)
                comp = item.clickHandler.GetComponentInParent<T>();
            return comp;
        }

        /// <summary>
        /// The value control of a setting row. Looks for the row's Setting
        /// component in the parent chain first (precise), then falls back to
        /// the item's own children — never whole-panel searches, which would
        /// match other rows' controls.
        /// </summary>
        private static T FindValueControl<T>(UINavigationItem item) where T : Component
        {
            Component row = (Component)FindInParents<SettingOptions>(item)
                ?? (Component)FindInParents<SettingSlider>(item);
            if (row != null)
            {
                var comp = row.GetComponentInChildren<T>(true);
                if (comp != null)
                    return comp;
            }
            return item.GetComponentInChildren<T>(true);
        }

        /// <summary>
        /// Current value of a setting row: the dropdown's selected option
        /// (resolution, language, display mode...) or a slider's percentage.
        /// The row's Setting component references its control via a serialized
        /// field — most reliable, since the control need not be a child of the
        /// navigation item (Max FPS and Vsync were missed by child search).
        /// Returns null if the item is not a setting row.
        /// </summary>
        private static string GetSettingValue(UINavigationItem item)
        {
            var options = FindInParents<SettingOptions>(item);
            if (options != null)
            {
                var dropdown = ReflectionUtil.GetField<TMP_Dropdown>(options, "dropdown");
                if (dropdown == null)
                    dropdown = options.GetComponentInChildren<TMP_Dropdown>(true);
                string fromOptions = ReadDropdown(dropdown);
                if (fromOptions != null)
                    return fromOptions;
            }

            var sliderSetting = FindInParents<SettingSlider>(item);
            if (sliderSetting != null)
            {
                var settingSlider = ReflectionUtil.GetField<Slider>(sliderSetting, "slider");
                if (settingSlider == null)
                    settingSlider = sliderSetting.GetComponentInChildren<Slider>(true);
                if (settingSlider != null)
                    return Loc.Get("setting_percent",
                        Mathf.RoundToInt(settingSlider.normalizedValue * 100f));
            }

            // Controls not wrapped in a Setting component
            string fromDropdown = ReadDropdown(FindValueControl<TMP_Dropdown>(item));
            if (fromDropdown != null)
                return fromDropdown;

            var slider = FindValueControl<Slider>(item);
            if (slider != null)
                return Loc.Get("setting_percent",
                    Mathf.RoundToInt(slider.normalizedValue * 100f));

            return null;
        }

        private static string ReadDropdown(TMP_Dropdown dropdown)
        {
            if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
                return null;
            int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
            string text = TextProcessor.StripRichText(dropdown.options[index].text);
            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>
        /// Last resort description: read every distinct text in the row
        /// (label, value, progress). Covers rows built without recognizable
        /// controls, like "Max FPS 60" or a challenge entry with its counter.
        /// </summary>
        private static string DescribeRowTexts(Component root)
        {
            if (root == null) return null;
            var parts = new List<string>();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (text == null) continue;
                string value = TextProcessor.StripRichText(text.text?.Trim());
                if (!string.IsNullOrEmpty(value) && !parts.Contains(value))
                    parts.Add(value);
                if (parts.Count >= 4) break;
            }
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }
    }
}
