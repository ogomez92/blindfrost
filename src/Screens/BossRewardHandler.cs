using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the BossReward overlay (after beating a boss): a sealed
    /// "eye" chest opens into a choice of blessings (charms, crowns, run
    /// modifiers). The vanilla flow is mouse/gamepad-cursor driven — the chest
    /// and the reward tokens may carry no navigation items, so this handler
    /// drives the sequence directly: Enter opens the chest
    /// (GainBlessingSequence2.StartOpen), arrows browse the BossRewardSelect
    /// tokens, Enter takes one through its own wired InputAction. The cinema
    /// bar prompt ("Choose N") is read by CinemaBarReader as it updates.
    /// </summary>
    public class BossRewardHandler : NavigableScreenHandler
    {
        public override string Name => "BossReward";

        private GainBlessingSequence2 _sequence;
        private float _nextSequenceSearch;
        private int _barRetries;
        private float _nextBarTry;
        private int _rewardIndex = -1;
        private bool _chooseHintSpoken;

        public override void OnEnter()
        {
            base.OnEnter();
            _sequence = null;
            _nextSequenceSearch = 0f;
            _barRetries = 0;
            _nextBarTry = 0f;
            _rewardIndex = -1;
            _chooseHintSpoken = false;
        }

        public override string GetHelpText() => Loc.Get("help_bossreward");

        protected override bool TryAnnounceScreen()
        {
            // The bar carries the sequence's title prompt; wait briefly for it
            string bar = CinemaBarReader.CurrentText();
            if (bar == null && _barRetries < 4)
            {
                if (Time.unscaledTime < _nextBarTry)
                    return false;
                _barRetries++;
                _nextBarTry = Time.unscaledTime + 0.5f;
                return false;
            }

            string text = Loc.Get("scene_BossReward");
            if (bar != null)
            {
                text += " " + bar;
                CinemaBarReader.SyncAnnounced();
            }
            text += " " + Loc.Get("bossreward_open_hint");
            ScreenReader.SayEvent(text, interrupt: true);
            return true;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Once the chest has opened and the tokens exist, explain the choice
            if (!_chooseHintSpoken && IsOpen() && GetRewards().Count > 0)
            {
                _chooseHintSpoken = true;
                ScreenReader.SayEvent(Loc.Get("bossreward_choose_hint"));
            }
        }

        private GainBlessingSequence2 Sequence()
        {
            if (_sequence == null && Time.unscaledTime >= _nextSequenceSearch)
            {
                _nextSequenceSearch = Time.unscaledTime + 0.5f;
                _sequence = Object.FindObjectOfType<GainBlessingSequence2>();
            }
            return _sequence;
        }

        private bool IsOpen()
        {
            var seq = Sequence();
            return seq != null && ReflectionUtil.GetBoolField(seq, "open", fallback: false);
        }

        private bool CanOpen()
        {
            var seq = Sequence();
            return seq != null && ReflectionUtil.GetBoolField(seq, "canOpen", fallback: false);
        }

        /// <summary>The reward tokens on offer, left to right.</summary>
        private List<BossRewardSelect> GetRewards()
        {
            var rewards = new List<BossRewardSelect>();
            foreach (var reward in Object.FindObjectsOfType<BossRewardSelect>())
            {
                if (reward != null && reward.gameObject.activeInHierarchy)
                    rewards.Add(reward);
            }
            rewards.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            return rewards;
        }

        /// <summary>
        /// Arrows: game navigation items when they exist, otherwise browse the
        /// reward tokens by index.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            if (GetItems().Count > 0)
            {
                base.Navigate(dir);
                return;
            }

            if (!IsOpen())
            {
                // Nothing to browse before the chest opens
                ScreenReader.Say(Loc.Get(CanOpen()
                    ? "bossreward_open_hint"
                    : "bossreward_not_ready"), interrupt: true);
                return;
            }

            var rewards = GetRewards();
            if (rewards.Count == 0)
            {
                ScreenReader.Say(Loc.Get("nav_nothing"), interrupt: true);
                return;
            }

            bool forward = dir == NavDirection.Right || dir == NavDirection.Down;
            if (_rewardIndex < 0)
                _rewardIndex = forward ? 0 : rewards.Count - 1;
            else
                _rewardIndex = ((_rewardIndex + (forward ? 1 : -1))
                    % rewards.Count + rewards.Count) % rewards.Count;

            var chosen = rewards[_rewardIndex];
            string desc = ItemDescriber.DescribeBossReward(chosen)
                ?? CleanName(chosen.gameObject.name);
            ScreenReader.Say(
                Loc.Get("bossreward_option", _rewardIndex + 1, rewards.Count, desc),
                interrupt: true);
        }

        protected override void Confirm()
        {
            var seq = Sequence();

            // Stage 1: open the chest
            if (seq != null && !IsOpen())
            {
                if (CanOpen())
                {
                    DebugLogger.LogInput(Name, "StartOpen");
                    seq.StartOpen();
                    ScreenReader.SayEvent(Loc.Get("bossreward_opening"), interrupt: true);
                }
                else
                {
                    ScreenReader.Say(Loc.Get("bossreward_not_ready"), interrupt: true);
                }
                return;
            }

            // Stage 2: take the browsed reward token
            var rewards = GetRewards();
            if (rewards.Count > 0)
            {
                // Prefer the game's focused item when it resolves to a token
                BossRewardSelect target = FocusedReward();
                if (target == null && _rewardIndex >= 0 && _rewardIndex < rewards.Count)
                    target = rewards[_rewardIndex];

                if (target != null)
                {
                    TakeReward(target);
                    return;
                }

                ScreenReader.Say(Loc.Get("bossreward_pick_first"), interrupt: true);
                return;
            }

            base.Confirm();
        }

        private BossRewardSelect FocusedReward()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem?.currentNavigationItem;
            if (item == null)
                return null;
            var reward = item.GetComponentInParent<BossRewardSelect>();
            if (reward == null && item.clickHandler != null)
                reward = item.clickHandler.GetComponentInParent<BossRewardSelect>();
            return reward;
        }

        /// <summary>
        /// Take a reward through its own wired InputAction (the same UnityEvent
        /// the mouse click fires: GainBlessingSequence2.Select + destroy).
        /// </summary>
        private void TakeReward(BossRewardSelect reward)
        {
            string desc = ItemDescriber.DescribeBossReward(reward)
                ?? CleanName(reward.gameObject.name);
            var inputAction = ReflectionUtil.GetField<InputAction>(reward, "inputAction");
            if (inputAction == null)
            {
                ScreenReader.Say(Loc.Get("bossreward_take_failed"), interrupt: true);
                return;
            }

            DebugLogger.LogInput(Name, $"Take reward: {desc}");
            inputAction.Run();
            _rewardIndex = -1;
            ScreenReader.SayEvent(Loc.Get("bossreward_taken", desc), interrupt: true);
        }

        /// <summary>Focused game items that resolve to tokens read their description.</summary>
        protected override string GetItemDescription(UINavigationItem item)
        {
            var reward = item.GetComponentInParent<BossRewardSelect>();
            if (reward != null)
            {
                string desc = ItemDescriber.DescribeBossReward(reward);
                if (!string.IsNullOrEmpty(desc))
                    return desc;
            }
            return base.GetItemDescription(item);
        }
    }
}
