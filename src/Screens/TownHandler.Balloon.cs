using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The Daily Voyage balloon overlay: its real buttons, and the summary of the
    /// daily's date, fixed deck and modifiers.
    /// </summary>
    public partial class TownHandler
    {
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
                try { n = TextProcessor.ProcessRawText(m.titleKey.GetLocalizedString()); }
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

    }
}
