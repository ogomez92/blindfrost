using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WildfrostAccessibility
{
    /// <summary>
    /// What each item is read out as: item descriptions, journal tab labels,
    /// and reading the current value of a setting row (dropdowns, sliders)
    /// or falling back to the row texts.
    /// </summary>
    public partial class PauseMenuHandler
    {
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
