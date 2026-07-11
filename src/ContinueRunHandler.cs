using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the ContinueRun screen — shown after selecting the
    /// Town gate while a journey is in progress. Announces the run summary
    /// (leader, deck contents, start date) and gives the buttons context.
    /// </summary>
    public class ContinueRunHandler : NavigableScreenHandler
    {
        public override string Name => "ContinueRun";

        private ContinueScreen _screen;
        private CardContainer _cardContainer;
        private GameObject _continueButton;
        private GameObject _backButton;
        private GameObject _missingDataDisplay;
        private int _retries;
        private float _nextTry;

        public override void OnEnter()
        {
            base.OnEnter();
            _screen = null;
            _cardContainer = null;
            _continueButton = null;
            _backButton = null;
            _missingDataDisplay = null;
            _retries = 0;
            _nextTry = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            _screen = null;
        }

        protected override bool TryAnnounceScreen()
        {
            if (Time.unscaledTime < _nextTry)
                return false;

            if (_screen == null)
            {
                _screen = Object.FindObjectOfType<ContinueScreen>();
                if (_screen != null)
                {
                    // ContinueScreen keeps everything in private serialized fields
                    _cardContainer = ReflectionUtil.GetField<CardContainer>(_screen, "cardContainer");
                    _continueButton = ReflectionUtil.GetField<GameObject>(_screen, "continueButton");
                    _backButton = ReflectionUtil.GetField<GameObject>(_screen, "backButton");
                    _missingDataDisplay = ReflectionUtil.GetField<GameObject>(_screen, "missingDataDisplay");
                }
            }

            bool missingData = _missingDataDisplay != null && _missingDataDisplay.activeInHierarchy;
            bool deckLoaded = _cardContainer != null && _cardContainer.Count > 0;

            // Wait for the deck cards to load (they're built asynchronously)
            if (_screen == null || (!deckLoaded && !missingData))
            {
                _retries++;
                if (_retries < 10)
                {
                    _nextTry = Time.unscaledTime + 0.5f;
                    return false;
                }
            }

            ScreenReader.Say(BuildAnnouncement(missingData), interrupt: true);
            return true;
        }

        /// <summary>Build the spoken run summary.</summary>
        private string BuildAnnouncement(bool missingData)
        {
            var parts = new List<string> { Loc.Get("screen_continue_run") };

            if (missingData)
            {
                parts.Add(Loc.Get("continue_missing_data"));
                return string.Join(" ", parts);
            }

            // Start date (populated by the game, already localized)
            var dateText = ReflectionUtil.GetField<TMP_Text>(_screen, "dateText");
            if (dateText != null && dateText.gameObject.activeInHierarchy)
            {
                string date = dateText.text?.Trim();
                if (!string.IsNullOrEmpty(date))
                    parts.Add(Loc.Get("continue_started", date));
            }

            // Leader and deck summary
            if (_cardContainer != null && _cardContainer.Count > 0)
            {
                Entity leader = FindLeader();
                if (leader?.data != null)
                {
                    string leaderInfo = leader.data.title;
                    if (leader.hp.max > 0)
                        leaderInfo += ", " + Loc.Get("stat_health", leader.hp.current);
                    if (leader.damage.max > 0)
                        leaderInfo += ", " + Loc.Get("stat_attack", leader.damage.current);
                    parts.Add(Loc.Get("continue_leader", leaderInfo));
                }

                parts.Add(Loc.Get("continue_deck", _cardContainer.Count, ListDeckCards(leader)));
            }

            string hint = HintOnce("continue_hint");
            if (hint != null)
                parts.Add(hint);
            return string.Join(" ", parts);
        }

        /// <summary>The leader is the deck card whose card type is flagged as miniboss.</summary>
        private Entity FindLeader()
        {
            foreach (Entity entity in _cardContainer)
            {
                if (entity?.data?.cardType != null && entity.data.cardType.miniboss)
                    return entity;
            }
            return null;
        }

        /// <summary>List deck card names (excluding the leader), grouping duplicates.</summary>
        private string ListDeckCards(Entity leader)
        {
            var counts = new Dictionary<string, int>();
            var order = new List<string>();

            foreach (Entity entity in _cardContainer)
            {
                if (entity == null || entity == leader || entity.data == null)
                    continue;

                string title = entity.data.title;
                if (string.IsNullOrEmpty(title)) continue;

                if (counts.ContainsKey(title))
                {
                    counts[title]++;
                }
                else
                {
                    counts[title] = 1;
                    order.Add(title);
                }
            }

            var names = new List<string>();
            foreach (string title in order)
            {
                names.Add(counts[title] > 1
                    ? Loc.Get("card_count_multiple", title, counts[title])
                    : title);
            }

            return string.Join(", ", names);
        }

        protected override string GetItemDescription(UINavigationItem item)
        {
            // Give the two main buttons context beyond their labels
            if (IsPartOf(item, _continueButton))
                return $"{GetButtonText(item)}. {Loc.Get("continue_button_desc")}";

            if (IsPartOf(item, _backButton))
                return $"{GetButtonText(item)}. {Loc.Get("continue_back_desc")}";

            return base.GetItemDescription(item);
        }

        /// <summary>Check whether a navigation item belongs to the given GameObject's hierarchy.</summary>
        private static bool IsPartOf(UINavigationItem item, GameObject root)
        {
            if (root == null) return false;
            Transform rootT = root.transform;
            if (item.transform == rootT || item.transform.IsChildOf(rootT))
                return true;
            return item.clickHandler != null
                && (item.clickHandler.transform == rootT || item.clickHandler.transform.IsChildOf(rootT));
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_continue_run");
        }
    }
}
