using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The leader and pet card stages, plus the back button every stage shares:
    /// the strict focus cycle over the choosable cards (the back button as its
    /// last stop), the "Leader 1 of 3" position each card is read with, and the
    /// ways out — Enter on the back button is handled with the tribe stage's
    /// Enter, Escape here.
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

        // ---- The back button, and backing out --------------------------------
        // Every stage ends on the back button, so its nav item is looked up here
        // for the tribe list as well as the card cycle.

        /// <summary>The screen's back-button nav item, or null when the game hides
        /// it (a run that cannot be backed out of).</summary>
        private UINavigationItem GetBackButtonNav()
        {
            EnsureRefs();
            if (_screen == null) return null;

            var backButton = ReflectionUtil.GetField<GameObject>(_screen, "backButton");
            if (backButton == null || !backButton.activeInHierarchy) return null;

            return backButton.GetComponent<UINavigationItem>()
                ?? backButton.GetComponentInChildren<UINavigationItem>(true);
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
    }
}
