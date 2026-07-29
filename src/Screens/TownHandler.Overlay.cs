using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The in-town building overlay: reading its text, browsing the banners and
    /// buttons the game keeps off the navigation layer, and the panel/name/text
    /// readers the other overlay parts share.
    /// </summary>
    public partial class TownHandler
    {
        /// <summary>
        /// Read the overlay's visible text (tribe names, unlock challenges and
        /// their progress, tribe lore, the daily challenge card and date, ...),
        /// re-reading when it changes or on I. Escape closes it — these overlays
        /// leave no keyboard way out.
        /// </summary>
        private void HandleBuildingOverlay(BuildingDisplay overlay)
        {
            // A help-panel popup can open over the building overlay (the
            // balloon's no-connection and first-time-help panels) — it owns
            // the keys until it is answered or dismissed.
            if (HelpPanelRouter.RouteInput())
                return;

            if (!_overlayOpen)
            {
                _overlayOpen = true;
                _overlayAnnounced = false;
                _balloonAnnounced = false;
                _balloonLoadingSaid = false;
                _lastOverlayText = null;
                _overlaySelected = null;
                _unlockEntries.Clear();
                _unlockIndex = 0;
                _detailTitle = null;
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
                    _detailTitle = null;
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

            // The unlock buildings (Tribe Hall, Pet House, Inventor's Hut,
            // companion hut, Icebreaker Hut): browsed entry by entry, because
            // the generic text dump reads their challenge progress with nothing
            // attached to it and cannot name a tribe banner at all.
            if (HandleUnlockBuildingOverlay(overlay))
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
            // the Tribe Hall gates the next tribe — say so, so "6 of 100" doesn't
            // dangle without a purpose. Only there: other unlock buildings (the
            // Pet House, ...) gate different rewards with the same display.
            else if (ActiveDetail(overlay) == null
                && overlay.GetComponentInChildren<TribeHutSequence>(includeInactive: false) != null
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

        /// <summary>The detail panel a building has opened over itself — a tribe's
        /// lore page or the Icebreaker's map-node preview — or null. Both are
        /// plain panels the game toggles active, so Escape closes either.</summary>
        private static GameObject ActiveDetail(BuildingDisplay overlay)
        {
            var tribePage = overlay.GetComponentInChildren<TribeDisplaySequence>(includeInactive: false);
            if (tribePage != null)
                return tribePage.gameObject;

            var nodePreview = overlay.GetComponentInChildren<MapInspectSequence>(includeInactive: false);
            return nodePreview != null ? nodePreview.gameObject : null;
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

    }
}
