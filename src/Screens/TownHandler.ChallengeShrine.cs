using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The challenge shrine overlay: its stones split into an incomplete and a
    /// completed row, the up/down + left/right browse over them, and the reward
    /// and condition each stone is read with (also used when a stone is focused
    /// as a plain overlay item).
    /// </summary>
    public partial class TownHandler
    {
        /// <summary>A challenge stone as "Reward: condition", or "Reward, hidden".</summary>
        private static string DescribeChallengeStone(ChallengeStone stone)
        {
            string name = null, condition = null;
            // Raw localized text: a challenge condition names its subject in tags
            // ("<Combos> give double <keyword=blings>"), so the tags must expand
            try { name = TextProcessor.ProcessRawText(stone.challenge.titleKey.GetLocalizedString()); }
            catch { /* localization not ready */ }
            if (!stone.challenge.hidden)
            {
                try { condition = TextProcessor.ProcessRawText(stone.challenge.textKey.GetLocalizedString()); }
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
    }
}
