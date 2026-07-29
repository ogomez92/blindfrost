using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The leader and pet card stages: the strict focus cycle over the choosable
    /// cards plus the back button, the focus and description overrides it feeds,
    /// and the Enter/Escape handling (locked-tribe refusal, going back).
    /// </summary>
    public partial class CharacterSelectHandler
    {
        // ---- Leader/pet-stage navigation -------------------------------------

        /// <summary>
        /// The focusable items of the current card-choice stage: the leader
        /// candidates or the pets, left to right, then the back button. False
        /// on the tribe stage or when the stage has no cards yet.
        /// </summary>
        private bool StageCardNavActive(out List<UINavigationItem> items)
        {
            items = null;

            List<Entity> cards = null;
            if (_petSelection != null && _petSelection.running)
                cards = _petSelection.pets;
            else if (_leaderSelection != null && _leaderSelection.running)
            {
                var characters = GetLeaderCharacters();
                if (characters != null)
                {
                    cards = new List<Entity>(characters.Count);
                    foreach (var character in characters)
                        if (character?.entity != null)
                            cards.Add(character.entity);
                }
            }
            if (cards == null)
                return false;

            var navs = new List<UINavigationItem>();
            foreach (var entity in cards)
            {
                if (entity == null || !entity.gameObject.activeInHierarchy) continue;
                var nav = entity.GetComponentInChildren<UINavigationItem>(true);
                if (nav != null && !navs.Contains(nav))
                    navs.Add(nav);
            }
            if (navs.Count == 0)
                return false;

            navs.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));

            var backNav = GetBackButtonNav();
            if (backNav != null)
                navs.Add(backNav);

            items = navs;
            return true;
        }

        /// <summary>Move focus one step through the list, any arrow direction,
        /// wrapping at the ends. Nothing outside the list is reachable.</summary>
        private static void CycleFocus(List<UINavigationItem> items, NavDirection dir)
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            int index = IndexOfNav(items, navSystem?.currentNavigationItem);

            bool forward = dir == NavDirection.Down || dir == NavDirection.Right;
            int next;
            if (index < 0)
                next = forward ? 0 : items.Count - 1;
            else
                next = (index + (forward ? 1 : -1) + items.Count) % items.Count;
            NavigationHelper.FocusItem(items[next]);
        }

        /// <summary>Find the focused item in the list, matching through the
        /// owning card entity when the nav item instance differs.</summary>
        private static int IndexOfNav(List<UINavigationItem> items, UINavigationItem current)
        {
            if (current == null) return -1;
            int index = items.IndexOf(current);
            if (index >= 0) return index;

            var entity = current.GetComponentInParent<Entity>();
            if (entity == null) return -1;
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && items[i].GetComponentInParent<Entity>() == entity)
                    return i;
            return -1;
        }

        protected override UINavigationItem DefaultFocusItem()
        {
            if (TribeNavActive(out var entries))
            {
                // First tribe the player can pick — never a locked one, and
                // never the trailing back button
                var playable = FirstPlayableEntry(entries);
                return playable != null ? playable.Nav : entries[0].Nav;
            }
            return base.DefaultFocusItem();
        }

        public override List<string> GetFocusedDetailParts(UINavigationItem item)
        {
            if (item != null)
            {
                var flag = item.GetComponentInParent<TribeFlagDisplay>();
                if (flag == null && item.clickHandler != null)
                    flag = item.clickHandler.GetComponentInParent<TribeFlagDisplay>();
                if (flag != null)
                {
                    var parts = ItemDescriber.BuildTribeDetailParts(flag);
                    if (parts != null && parts.Count > 0)
                        return parts;
                }
            }
            return base.GetFocusedDetailParts(item);
        }

        protected override string GetItemDescription(UINavigationItem item)
        {
            var backNav = GetBackButtonNav();
            if (backNav != null && item == backNav)
                return Loc.Get("charselect_back");

            // Position the card in its stage ("Leader 1 of 3: ...") so the
            // player always knows which option they are on and how many exist
            if (StageCardNavActive(out var items))
            {
                if (backNav != null)
                    items.Remove(backNav);
                int index = IndexOfNav(items, item);
                if (index >= 0)
                {
                    string desc = base.GetItemDescription(item);
                    if (string.IsNullOrEmpty(desc))
                        desc = CleanName(item.gameObject.name);
                    bool pets = _petSelection != null && _petSelection.running;
                    return Loc.Get(pets ? "charselect_pet_pos" : "charselect_leader_pos",
                        index + 1, items.Count, desc);
                }
            }
            return base.GetItemDescription(item);
        }

        protected override void Confirm()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            var backNav = GetBackButtonNav();
            if (backNav != null && current == backNav && _screen != null)
            {
                DebugLogger.LogInput(Name, "Back (Enter on back button)");
                _screen.Back();
                return;
            }

            // A locked tribe is still fully pressable — the game's own filter
            // never removed it (see the tribe-lock notes in ItemDescriber), and
            // SelectTribe.Run cleared the padlock on every flag it was handed.
            // Refuse it here and say what it takes to earn it instead of
            // starting a run with a tribe this save never unlocked.
            if (TribeNavActive(out var entries))
            {
                int index = CurrentTribeIndex(entries, current);
                if (index >= 0 && entries[index].Locked)
                {
                    var tribe = entries[index].Tribe;
                    DebugLogger.LogInput(Name, $"Blocked locked tribe: {tribe?.name}");
                    ScreenReader.Say(
                        Loc.Get("tribe_locked_blocked",
                            ItemDescriber.GetTribeName(tribe),
                            ItemDescriber.GetTribeLockReason(tribe)),
                        interrupt: true);
                    return;
                }
            }

            base.Confirm();
        }

        protected override void HandleInput()
        {
            base.HandleInput();

            // Escape returns. The base only wired Escape to the chosen-card panel,
            // so there was no keyboard way out of the tribe/leader/pet stages.
            if (NavigationHelper.IsBackPressed()
                && ActiveInspectPanel == null
                && !NavigationHelper.IsTextInputFocused())
                TryGoBack();
        }

        /// <summary>
        /// Run the screen's own Back(): the tribe stage returns to the menu (only
        /// when the game shows a back button), the leader and pet stages step back
        /// one stage.
        /// </summary>
        private void TryGoBack()
        {
            EnsureRefs();
            if (_screen == null) return;

            bool onLaterStage = (_leaderSelection != null && _leaderSelection.running)
                             || (_petSelection != null && _petSelection.running);

            var backButton = ReflectionUtil.GetField<GameObject>(_screen, "backButton");
            bool canReturn = backButton != null && backButton.activeInHierarchy;

            if (onLaterStage || canReturn)
            {
                DebugLogger.LogInput(Name, "Back (Escape)");
                _screen.Back();
            }
        }

        private void SpeakTribeRoster(ClassData tribe)
        {
            string text = ItemDescriber.DescribeTribeRoster(tribe);
            ScreenReader.Say(
                !string.IsNullOrEmpty(text) ? text : Loc.Get("tribe_no_roster"),
                interrupt: true);
        }
    }
}
