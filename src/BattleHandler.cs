using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Battle scene.
    /// Navigation model: Up/Down switch groups (hand, your board, enemy board, system items),
    /// Left/Right move within the group. Enter picks up a card, Enter again places it.
    /// Announces turns, phases, waves, and full card descriptions with status effects.
    /// Extra keys: H hand, B board, W waves, R redraw bell, G gold, T turn.
    /// </summary>
    public class BattleHandler : NavigableScreenHandler
    {
        public override string Name => "Battle";

        private enum Group { Hand, PlayerBoard, EnemyBoard, System }

        private Group _group = Group.Hand;
        private Battle.Phase _lastAnnouncedPhase = Battle.Phase.None;
        private bool _subscribed;

        public override void OnEnter()
        {
            base.OnEnter();
            _group = Group.Hand;
            _lastAnnouncedPhase = Battle.Phase.None;
            Subscribe();
        }

        public override void OnExit()
        {
            base.OnExit();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            Events.OnBattlePhaseStart += OnPhaseStart;
            Events.OnBattleTurnStart += OnTurnStart;
            Events.OnRedrawBellHit += OnRedrawBellHit;
            Events.OnEntityPostHit += OnEntityPostHit;
            Events.OnEntityKilled += OnEntityKilled;
            Events.OnStatusEffectApplied += OnStatusApplied;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            Events.OnBattlePhaseStart -= OnPhaseStart;
            Events.OnBattleTurnStart -= OnTurnStart;
            Events.OnRedrawBellHit -= OnRedrawBellHit;
            Events.OnEntityPostHit -= OnEntityPostHit;
            Events.OnEntityKilled -= OnEntityKilled;
            Events.OnStatusEffectApplied -= OnStatusApplied;
        }

        protected override bool TryAnnounceScreen()
        {
            var battle = Battle.instance;
            if (battle == null || battle.player == null)
                return false;

            var parts = new List<string> { Loc.Get("screen_battle") };

            int waveCount = 0;
            var waveManager = GetWaveManager();
            if (waveManager?.list != null)
                waveCount = waveManager.list.Count;
            if (waveCount > 0)
                parts.Add(Loc.Get("battle_wave_total", waveCount));

            int handCount = battle.player.handContainer?.Count ?? 0;
            if (handCount > 0)
                parts.Add(Loc.Get("battle_hand_count", handCount));

            // Navigation instructions only the first time this session; F1 repeats them
            string hint = HintOnce("battle_hint");
            if (hint != null)
                parts.Add(hint);

            ScreenReader.Say(string.Join(" ", parts), interrupt: true);
            return true;
        }

        // ---- Game event announcements -------------------------------------

        private void OnPhaseStart(Battle.Phase phase)
        {
            if (phase == _lastAnnouncedPhase) return;
            _lastAnnouncedPhase = phase;

            switch (phase)
            {
                case Battle.Phase.Play:
                    var battle = Battle.instance;
                    int hand = battle?.player?.handContainer?.Count ?? 0;
                    string wave = GetWaveCounterText();
                    string msg = Loc.Get("battle_your_turn", hand);
                    if (wave != null)
                        msg += " " + wave;
                    ScreenReader.Say(msg);
                    break;
                case Battle.Phase.Battle:
                    ScreenReader.Say(Loc.Get("battle_resolving"));
                    break;
                case Battle.Phase.End:
                    ScreenReader.Say(Loc.Get("battle_over"));
                    break;
            }
        }

        private void OnTurnStart(int turn)
        {
            ScreenReader.Say(Loc.Get("battle_turn", turn));
        }

        private void OnRedrawBellHit(RedrawBellSystem bell)
        {
            ScreenReader.Say(Loc.Get("battle_bell_rung"));
        }

        // ---- Combat narration ------------------------------------------------
        // Queued (non-interrupting) so a resolving turn reads out as a sequence:
        // who hit whom for how much, what was applied, who died.

        /// <summary>Narrate only while the battle is actually running (skips setup/cleanup).</summary>
        private static bool InCombat()
        {
            var battle = Battle.instance;
            return battle != null
                && (battle.phase == Battle.Phase.Play || battle.phase == Battle.Phase.Battle);
        }

        private void OnEntityPostHit(Hit hit)
        {
            if (!InCombat() || hit?.target?.data == null) return;

            string target = hit.target.data.title;

            if (hit.dodged)
            {
                ScreenReader.Say(Loc.Get("battle_dodged", target));
                return;
            }

            if (hit.damageDealt > 0)
            {
                if (hit.attacker?.data != null)
                    ScreenReader.Say(Loc.Get("battle_hit", hit.attacker.data.title, target, hit.damageDealt));
                else
                    ScreenReader.Say(Loc.Get("battle_takes_damage", target, hit.damageDealt));
            }
            else if (hit.damageDealt < 0)
            {
                ScreenReader.Say(Loc.Get("battle_healed", target, -hit.damageDealt));
            }
            // Zero-damage hits (pure status applications) are narrated by OnStatusApplied
        }

        private void OnEntityKilled(Entity entity, DeathType deathType)
        {
            if (!InCombat() || entity?.data == null) return;
            ScreenReader.Say(Loc.Get("battle_destroyed", entity.data.title));
        }

        private void OnStatusApplied(StatusEffectApply apply)
        {
            if (!InCombat()) return;
            if (apply?.effectData == null || apply.target?.data == null) return;
            if (!apply.effectData.visible || apply.count <= 0) return;

            ScreenReader.Say(Loc.Get("battle_status_applied",
                apply.count, ItemDescriber.GetStatusName(apply.effectData), apply.target.data.title));
        }

        // ---- Input ----------------------------------------------------------

        protected override void HandleInput()
        {
            base.HandleInput();

            if (Input.GetKeyDown(KeyCode.H)) { DebugLogger.LogInput(Name, "Hand"); AnnounceHand(); }
            if (Input.GetKeyDown(KeyCode.B)) { DebugLogger.LogInput(Name, "Board"); AnnounceBoard(); }
            if (Input.GetKeyDown(KeyCode.W)) { DebugLogger.LogInput(Name, "Waves"); AnnounceWaves(); }
            if (Input.GetKeyDown(KeyCode.R)) { DebugLogger.LogInput(Name, "Bell"); AnnounceBell(); }
            if (Input.GetKeyDown(KeyCode.G)) { DebugLogger.LogInput(Name, "Gold"); AnnounceGold(); }
            if (Input.GetKeyDown(KeyCode.T)) { DebugLogger.LogInput(Name, "Turn"); AnnounceTurn(); }
        }

        /// <summary>
        /// While holding a card: all arrows move between valid targets (the game
        /// disables everything else). Otherwise: Up/Down switch groups, Left/Right
        /// move within the current group.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            if (IsTargeting())
            {
                base.Navigate(dir);
                return;
            }

            if (dir == NavDirection.Up || dir == NavDirection.Down)
            {
                SwitchGroup(dir == NavDirection.Down);
                return;
            }

            var items = GetGroupItems(_group);
            if (items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_group_empty", GetGroupName(_group)));
                return;
            }

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var next = NavigationHelper.NavigateLinear(
                items, navSystem?.currentNavigationItem, dir, vertical: false);
            if (next != null)
                NavigationHelper.FocusItem(next);
        }

        /// <summary>Move to the next/previous group and focus its first item.</summary>
        private void SwitchGroup(bool forward)
        {
            const int groupCount = 4;
            for (int i = 0; i < groupCount; i++)
            {
                int next = ((int)_group + (forward ? i + 1 : -(i + 1)) + groupCount * 2) % groupCount;
                var candidate = (Group)next;
                var items = GetGroupItems(candidate);
                if (items.Count == 0) continue;

                _group = candidate;
                ScreenReader.Say(GetGroupName(_group), interrupt: true);
                NavigationHelper.FocusItem(items[0]);
                return;
            }

            ScreenReader.Say(Loc.Get("battle_nothing_to_focus"));
        }

        private string GetGroupName(Group group)
        {
            switch (group)
            {
                case Group.Hand: return Loc.Get("group_hand");
                case Group.PlayerBoard: return Loc.Get("group_your_board");
                case Group.EnemyBoard: return Loc.Get("group_enemy_board");
                default: return Loc.Get("group_system");
            }
        }

        /// <summary>Collect the navigation items belonging to a group, in reading order.</summary>
        private List<UINavigationItem> GetGroupItems(Group group)
        {
            var items = new List<UINavigationItem>();
            var battle = Battle.instance;
            if (battle == null) return items;

            switch (group)
            {
                case Group.Hand:
                    AddContainerItems(items, battle.player?.handContainer);
                    break;

                case Group.PlayerBoard:
                    AddBoardItems(items, battle.player);
                    break;

                case Group.EnemyBoard:
                    AddBoardItems(items, battle.enemy);
                    break;

                case Group.System:
                    AddNavItem(items, RedrawBellSystem.nav);
                    AddNavItem(items, WaveDeploySystem.nav);
                    foreach (var item in NavigationHelper.GetNavigableItems())
                    {
                        if (item.GetComponentInParent<CardPocket>() != null
                            || (item.clickHandler != null
                                && item.clickHandler.GetComponentInParent<CardPocket>() != null))
                        {
                            AddNavItem(items, item);
                        }
                    }
                    break;
            }
            return items;
        }

        private static void AddContainerItems(List<UINavigationItem> items, CardContainer container)
        {
            if (container == null) return;
            foreach (Entity entity in container)
                AddNavItem(items, entity != null ? entity.uINavigationItem : null);
        }

        private static void AddBoardItems(List<UINavigationItem> items, Character character)
        {
            if (character == null) return;
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    AddNavItem(items, occupant != null ? occupant.uINavigationItem : null);
                }
            }
        }

        private static CardSlotLane GetLane(Character character, int row)
        {
            try
            {
                return Battle.instance.GetRow(character, row) as CardSlotLane;
            }
            catch
            {
                return null;
            }
        }

        private static void AddNavItem(List<UINavigationItem> items, UINavigationItem item)
        {
            if (item == null || !item.isSelectable || !item.gameObject.activeInHierarchy)
                return;
            if (!item.enabled) return;
            if (items.Contains(item)) return;
            items.Add(item);
        }

        // ---- Playing cards ---------------------------------------------------

        /// <summary>Is a card currently picked up (targeting mode)?</summary>
        private bool IsTargeting()
        {
            var controller = Battle.instance?.playerCardController;
            return controller != null && controller.dragging != null;
        }

        /// <summary>
        /// While the game resolves actions (enemy turn, a played card), it moves focus
        /// around on its own; announcing those changes would talk over combat narration.
        /// </summary>
        protected override bool SuppressFocusAnnouncements
        {
            get
            {
                var battle = Battle.instance;
                if (battle == null) return false;
                if (battle.phase == Battle.Phase.Battle) return true;
                try { return !ActionQueue.Empty; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Browsing reads the card itself; only while holding a card do slot
        /// positions matter, so the side, row, slot prefix is targeting-only.
        /// </summary>
        protected override string GetItemDescription(UINavigationItem item)
        {
            if (IsTargeting())
            {
                string target = ItemDescriber.DescribeTarget(item);
                if (!string.IsNullOrEmpty(target))
                    return target;
            }
            return base.GetItemDescription(item);
        }

        /// <summary>
        /// Enter: pick up the focused hand card, or place the held card on the
        /// focused target. Falls back to a regular click for buttons/bell.
        /// </summary>
        protected override void Confirm()
        {
            var battle = Battle.instance;
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;
            if (battle == null || current == null)
            {
                base.Confirm();
                return;
            }

            var controller = battle.playerCardController;

            // Holding a card: release it on the focused target
            if (controller != null && controller.dragging != null)
            {
                DebugLogger.LogInput(Name, "Place card");
                Entity held = controller.dragging;
                if (ReflectionUtil.InvokeMethod(controller, "Release"))
                {
                    if (controller.dragging == null)
                        ScreenReader.Say(Loc.Get("battle_card_released", held?.data?.title ?? ""));
                    else
                        ScreenReader.Say(Loc.Get("battle_invalid_target"));
                }
                return;
            }

            // Focused item is one of our own hand cards: pick it up
            Entity entity = GetEntityFromItem(current);
            if (entity != null && controller != null
                && entity.owner == battle.player && entity.InHand())
            {
                DebugLogger.LogInput(Name, "Pick up card");
                controller.hoverEntity = entity;
                if (ReflectionUtil.SetField(controller, "pressEntity", entity)
                    && ReflectionUtil.InvokeMethod(controller, "Press")
                    && controller.dragging != null)
                {
                    string msg = Loc.Get("battle_card_picked_up", entity.data?.title ?? "");
                    string hint = HintOnce("battle_pickup_hint");
                    if (hint != null)
                        msg += " " + hint;
                    ScreenReader.Say(msg);
                }
                else
                {
                    ScreenReader.Say(Loc.Get("battle_cannot_play"));
                }
                return;
            }

            // Redraw bell: call the game API directly
            if (current == RedrawBellSystem.nav)
            {
                var bell = Object.FindObjectOfType<RedrawBellSystem>();
                if (bell != null && bell.interactable)
                {
                    DebugLogger.LogInput(Name, "Ring bell");
                    bell.Activate();
                }
                else
                {
                    ScreenReader.Say(Loc.Get("battle_bell_not_ready"));
                }
                return;
            }

            base.Confirm();
        }

        private static Entity GetEntityFromItem(UINavigationItem item)
        {
            var entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            return entity;
        }

        // ---- Readout keys ----------------------------------------------------

        private void AnnounceHand()
        {
            var hand = Battle.instance?.player?.handContainer;
            if (hand == null || hand.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_hand_empty"), interrupt: true);
                return;
            }

            var names = new List<string>();
            foreach (Entity entity in hand)
            {
                if (entity?.data != null)
                    names.Add(entity.data.title);
            }
            ScreenReader.Say(
                Loc.Get("battle_hand_count", hand.Count) + " " + string.Join(", ", names),
                interrupt: true);
        }

        private void AnnounceBoard()
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var parts = new List<string>
            {
                Loc.Get("group_your_board"),
                DescribeSide(battle.player),
                Loc.Get("group_enemy_board"),
                DescribeSide(battle.enemy)
            };
            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        private string DescribeSide(Character character)
        {
            if (character == null) return Loc.Get("slot_empty");

            var rows = new List<string>();
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;

                var cells = new List<string>();
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null)
                    {
                        cells.Add(Loc.Get("slot_empty"));
                        continue;
                    }

                    string cell = occupant.data.title;
                    if (occupant.hp.max > 0)
                        cell += " " + Loc.Get("stat_health", occupant.hp.current);
                    if (occupant.damage.max > 0)
                        cell += " " + Loc.Get("stat_attack", occupant.damage.current);
                    if (occupant.counter.max > 0)
                        cell += " " + Loc.Get("battle_acts_in", occupant.counter.current);

                    string statuses = ItemDescriber.DescribeStatusEffects(occupant);
                    if (!string.IsNullOrEmpty(statuses))
                        cell += ", " + statuses;

                    cells.Add(cell);
                }

                rows.Add(Loc.Get("slot_row", row + 1) + ": " + string.Join(", ", cells));
            }

            return string.Join(". ", rows);
        }

        private void AnnounceWaves()
        {
            var waveManager = GetWaveManager();
            if (waveManager?.list == null || waveManager.list.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_no_waves"), interrupt: true);
                return;
            }

            var parts = new List<string>();
            string counterText = GetWaveCounterText();
            if (counterText != null)
                parts.Add(counterText);

            int index = 0;
            foreach (var wave in waveManager.list)
            {
                index++;
                if (wave == null || wave.spawned) continue;

                var names = new List<string>();
                if (wave.units != null)
                {
                    foreach (CardData unit in wave.units)
                    {
                        if (unit != null)
                            names.Add(unit.title);
                    }
                }

                string desc = Loc.Get("battle_wave_n", index, string.Join(", ", names));
                if (wave.isBossWave)
                    desc += ", " + Loc.Get("battle_boss_wave");
                parts.Add(desc);
            }

            if (parts.Count == 0)
                parts.Add(Loc.Get("battle_all_waves_spawned"));

            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        private BattleWaveManager GetWaveManager()
        {
            var enemy = Battle.instance?.enemy;
            return enemy != null ? enemy.GetComponent<BattleWaveManager>() : null;
        }

        /// <summary>"Next wave in N turns", read from the wave deploy HUD.</summary>
        private string GetWaveCounterText()
        {
            var deploy = Object.FindObjectOfType<WaveDeploySystem>();
            if (deploy == null) return null;

            int counter = ReflectionUtil.GetIntField(deploy, "counter", -1);
            if (counter <= 0) return null;

            return Loc.Get("battle_next_wave", counter);
        }

        private void AnnounceBell()
        {
            var bell = Object.FindObjectOfType<RedrawBellSystem>();
            if (bell == null)
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
                return;
            }

            if (bell.IsCharged)
                ScreenReader.Say(Loc.Get("battle_bell_charged"), interrupt: true);
            else
                ScreenReader.Say(Loc.Get("battle_bell_charging", bell.counter.current), interrupt: true);
        }

        private void AnnounceGold()
        {
            try
            {
                int gold = References.Player.data.inventory.gold.Value;
                ScreenReader.Say(Loc.Get("gold_amount", gold), interrupt: true);
            }
            catch
            {
                ScreenReader.Say(Loc.Get("no_info_available"), interrupt: true);
            }
        }

        private void AnnounceTurn()
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var parts = new List<string> { Loc.Get("battle_turn", battle.turnCount) };

            parts.Add(battle.phase == Battle.Phase.Play
                ? Loc.Get("battle_phase_play")
                : Loc.Get("battle_phase_other"));

            string wave = GetWaveCounterText();
            if (wave != null)
                parts.Add(wave);

            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_battle");
        }
    }
}
