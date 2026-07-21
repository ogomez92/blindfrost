using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Character select: tribe, then leader, then optionally a starting pet.
    /// Cards are picked via CardControllerSelectCard (see
    /// NavigationHelper.TryPressSelectCard) and confirmed in the inspect
    /// panel that NavigableScreenHandler drives (Enter/Escape). This screen's
    /// panel confirms through CharacterSelectScreen.Continue() — the base
    /// TakeCard() path has no card selector here — and the handler announces
    /// the stage flow: leaders, pet choice, journey start.
    /// </summary>
    public class CharacterSelectHandler : NavigableScreenHandler
    {
        public override string Name => "CharacterSelect";

        private CharacterSelectScreen _screen;
        private SelectLeader _leaderSelection;
        private SelectStartingPet _petSelection;

        private bool _petWasRunning;
        private bool _leaderWasRunning;
        private bool _startAnnounced;
        private bool _leaderIntroPending;
        private float _leaderIntroSince;

        public override void OnEnter()
        {
            base.OnEnter();
            _screen = null;
            _petWasRunning = false;
            _leaderWasRunning = false;
            _startAnnounced = false;
            _leaderIntroPending = false;
            EnsureRefs();
        }

        /// <summary>
        /// The screen object may not exist yet on the first frames of the
        /// scene — resolve lazily and cache.
        /// </summary>
        private void EnsureRefs()
        {
            if (_screen != null) return;
            _screen = Object.FindObjectOfType<CharacterSelectScreen>();
            if (_screen == null) return;

            _leaderSelection = ReflectionUtil.GetField<SelectLeader>(_screen, "leaderSelection");
            _petSelection = ReflectionUtil.GetField<SelectStartingPet>(_screen, "petSelection");
        }

        protected override bool TryAnnounceScreen()
        {
            EnsureRefs();
            string msg;
            if (_leaderSelection != null && _leaderSelection.running)
            {
                msg = Loc.Get("charselect_leaders");
                _leaderWasRunning = true;
            }
            else if (TribeStageVisible())
            {
                // Normal mode starts on the tribe choice; without this the
                // player only heard the bare screen name and flag focus reads
                msg = Loc.Get("charselect_tribes");
            }
            else if (Time.unscaledTime - EnterTime < 3f)
            {
                // Neither stage is up yet — the flags/leaders may still be
                // loading in. Retry instead of latching onto the bare name.
                return false;
            }
            else
            {
                msg = Loc.Get("scene_CharacterSelect");
            }
            ScreenReader.SayEvent(msg, interrupt: true);
            return true;
        }

        /// <summary>Tribe flags on screen mean the tribe stage is active
        /// (SelectTribe has no running flag to ask).</summary>
        private static bool TribeStageVisible()
        {
            foreach (var flag in Object.FindObjectsOfType<TribeFlagDisplay>())
                if (flag != null && flag.gameObject.activeInHierarchy)
                    return true;
            return false;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            EnsureRefs();
            if (_screen == null) return;

            // Leader stage started (after picking a tribe). The intro is
            // deferred until the candidates finish generating so it can also
            // read the chosen tribe's starting deck.
            bool leaderRunning = _leaderSelection != null && _leaderSelection.running;
            if (leaderRunning && !_leaderWasRunning)
            {
                _leaderIntroPending = true;
                _leaderIntroSince = Time.unscaledTime;
            }
            _leaderWasRunning = leaderRunning;
            if (!leaderRunning)
                _leaderIntroPending = false;
            else if (_leaderIntroPending)
                TryAnnounceLeaderIntro();

            // Pet stage started (after confirming the leader)
            bool petRunning = _petSelection != null && _petSelection.running;
            if (petRunning && !_petWasRunning)
                ScreenReader.SayEvent(Loc.Get("charselect_pets"), interrupt: true);
            _petWasRunning = petRunning;

            // Final confirm pressed: the run is being created
            if (!_startAnnounced && ReflectionUtil.GetBoolField(_screen, "loadingToCampaign", false))
            {
                _startAnnounced = true;
                ScreenReader.SayEvent(Loc.Get("charselect_starting"), interrupt: true);
            }
        }

        /// <summary>
        /// Announce the leader stage: the intro line plus the starting deck of
        /// the chosen tribe (the cards every run with it begins with). Waits
        /// for GenerateLeaders to finish so the candidates' tribe is known;
        /// falls back to the plain intro if the stage never settles.
        /// </summary>
        private void TryAnnounceLeaderIntro()
        {
            var characters = _leaderSelection.generating ? null : GetLeaderCharacters();
            bool ready = characters != null && characters.Count > 0;
            if (!ready && Time.unscaledTime - _leaderIntroSince < 4f)
                return;

            _leaderIntroPending = false;
            string msg = Loc.Get("charselect_leaders");
            if (ready)
            {
                var tribe = characters[0]?.data?.classData;
                string deck = ItemDescriber.DescribeTribeStartingDeck(tribe);
                if (!string.IsNullOrEmpty(deck))
                    msg += " " + deck;
            }
            ScreenReader.SayEvent(msg, interrupt: true);
        }

        /// <summary>The leader stage's candidate list (SelectLeader.characters,
        /// private). Null before the stage has generated anything.</summary>
        private List<SelectLeader.Character> GetLeaderCharacters()
        {
            return _leaderSelection == null
                ? null
                : ReflectionUtil.GetField<List<SelectLeader.Character>>(
                    _leaderSelection, "characters");
        }

        /// <summary>
        /// Exactly what the unreachable "Let's Go!" button does here:
        /// next stage (pet choice) or start the run.
        /// </summary>
        protected override void ConfirmInspectPanel(InspectNewUnitSequence panel)
        {
            if (_screen != null)
            {
                DebugLogger.LogInput(Name, "Continue (Let's Go)");
                _screen.Continue();
                return;
            }
            base.ConfirmInspectPanel(panel);
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_charselect");
        }

        // ---- Tribe-stage navigation ------------------------------------------
        // On the "Select a Tribe" stage the flags are the only tribe content, and
        // the game's spatial navigation lets up/down wander off them. Here the
        // arrows are remapped: up/down move between the tribes (and the back
        // button, as a final stop), left/right read the focused tribe's roster
        // (also in the Details buffer on Ctrl+Up). Escape and the back button run
        // the screen's own Back(). The leader and pet stages keep base navigation.

        private sealed class TribeEntry
        {
            public TribeFlagDisplay Flag;   // null on the back-button entry
            public ClassData Tribe;         // null on the back-button entry
            public UINavigationItem Nav;
        }

        /// <summary>
        /// The tribe flags (with their tribe and nav item, in the game's flag
        /// order) followed by the back button. SelectTribe keeps its flags and
        /// tribes as lock-step lists.
        /// </summary>
        private List<TribeEntry> GetTribeEntries()
        {
            var entries = new List<TribeEntry>();
            var select = Object.FindObjectOfType<SelectTribe>();
            if (select == null) return entries;

            var flags = ReflectionUtil.GetField<List<TribeFlagDisplay>>(select, "flags");
            var tribes = ReflectionUtil.GetField<List<ClassData>>(select, "tribes");
            if (flags == null || tribes == null) return entries;

            int count = Mathf.Min(flags.Count, tribes.Count);
            for (int i = 0; i < count; i++)
            {
                var flag = flags[i];
                if (flag == null || !flag.gameObject.activeInHierarchy) continue;

                var nav = flag.GetComponentInChildren<UINavigationItem>(true);
                if (nav == null) continue;

                entries.Add(new TribeEntry { Flag = flag, Tribe = tribes[i], Nav = nav });
            }

            // The back button as a final up/down stop, so it stays reachable and
            // Enter on it goes back (only when the game offers a back button).
            if (entries.Count > 0)
            {
                var backNav = GetBackButtonNav();
                if (backNav != null)
                    entries.Add(new TribeEntry { Flag = null, Tribe = null, Nav = backNav });
            }
            return entries;
        }

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

        /// <summary>True (with the entries) while the tribe-choice stage is the one
        /// being navigated — not the leader or pet stage.</summary>
        private bool TribeNavActive(out List<TribeEntry> entries)
        {
            entries = GetTribeEntries();
            if (entries.Count == 0) return false;
            if (_leaderSelection != null && _leaderSelection.running) return false;
            if (_petSelection != null && _petSelection.running) return false;
            return true;
        }

        private static int CurrentTribeIndex(List<TribeEntry> entries, UINavigationItem current)
        {
            if (current == null) return -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Nav == current) return i;
                if (entries[i].Flag != null
                    && current.GetComponentInParent<TribeFlagDisplay>() == entries[i].Flag)
                    return i;
            }
            return -1;
        }

        protected override void Navigate(NavDirection dir)
        {
            // While the chosen-card panel is up, Enter/Escape drive it — the
            // arrows must not shuffle focus (and the pending Enter target)
            // silently underneath it.
            if (ActiveInspectPanel != null)
                return;

            if (TribeNavActive(out var entries))
            {
                NavigateTribes(entries, dir);
                return;
            }

            // Leader and pet stages: the arrows cycle strictly through the
            // choosable cards and the back button. The base spatial navigation
            // could wander onto everything else registered in the scene (the
            // off-screen pet hand, help button), the game's default-item
            // system kept snatching focus back to a card the player had moved
            // off, and Enter then chose that stale card.
            if (StageCardNavActive(out var items))
            {
                CycleFocus(items, dir);
                return;
            }

            base.Navigate(dir);
        }

        private void NavigateTribes(List<TribeEntry> entries, NavDirection dir)
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            int index = CurrentTribeIndex(entries, navSystem?.currentNavigationItem);

            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                int next;
                if (index < 0)
                    next = dir == NavDirection.Down ? 0 : entries.Count - 1;
                else
                {
                    next = index + (dir == NavDirection.Down ? 1 : -1);
                    if (next >= entries.Count) next = 0;
                    if (next < 0) next = entries.Count - 1;
                }
                NavigationHelper.FocusItem(entries[next].Nav);
            }
            else // Left or Right: read the focused tribe's roster (nothing on back)
            {
                var tribe = entries[index < 0 ? 0 : index].Tribe;
                if (tribe != null)
                    SpeakTribeRoster(tribe);
            }
        }

        protected override List<UINavigationItem> GetItems()
        {
            if (TribeNavActive(out var entries))
            {
                var navs = new List<UINavigationItem>(entries.Count);
                foreach (var entry in entries)
                    navs.Add(entry.Nav);
                return navs;
            }
            if (StageCardNavActive(out var items))
                return items;
            return base.GetItems();
        }

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
                return entries[0].Nav; // first tribe, never the trailing back button
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
                !string.IsNullOrEmpty(text) ? text : Loc.Get("tribe_no_companions"),
                interrupt: true);
        }
    }
}
