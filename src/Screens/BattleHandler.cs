using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Accessibility handler for the Battle scene.
    /// Navigation model: Up/Down switch groups (hand, your board, enemy board, system items),
    /// Left/Right move within the group. Enter picks up a card, Enter again places it.
    /// Announces turns, phases, waves, and full card descriptions with status effects.
    /// Extra keys: H hand, B board, W waves, R redraw bell, G gold, T turn, M modifiers.
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
            Events.OnEntityTrigger += OnEntityTrigger;
            Events.OnKillCombo += OnKillCombo;
            Events.OnDropGold += OnDropGold;
            Events.OnCardInjured += OnCardInjured;
            Events.OnStatusIconChanged += OnStatusIconChanged;
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
            Events.OnEntityTrigger -= OnEntityTrigger;
            Events.OnKillCombo -= OnKillCombo;
            Events.OnDropGold -= OnDropGold;
            Events.OnCardInjured -= OnCardInjured;
            Events.OnStatusIconChanged -= OnStatusIconChanged;
        }

        protected override bool TryAnnounceScreen()
        {
            var battle = Battle.instance;
            if (battle == null || battle.player == null)
                return false;

            var parts = new List<string> { Loc.Get("screen_battle") };

            int waveCount = WaveDeployer.GetWaves(WaveDeployer.Find())?.Count ?? 0;
            if (waveCount > 0)
                parts.Add(Loc.Get("battle_wave_total", waveCount));

            int handCount = battle.player.handContainer?.Count ?? 0;
            if (handCount > 0)
                parts.Add(Loc.Get("battle_hand_count", handCount));

            // Crowned cards deploy before the battle starts — tell the player now
            int crowned = CountCrownedInHand(battle);
            if (crowned == 1)
                parts.Add(Loc.Get("battle_crown_deploy_one"));
            else if (crowned > 1)
                parts.Add(Loc.Get("battle_crown_deploy", crowned));

            // Navigation instructions only the first time this session; F1 repeats them
            string hint = HintOnce("battle_hint");
            if (hint != null)
                parts.Add(hint);

            ScreenReader.SayEvent(string.Join(" ", parts), interrupt: true);
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
                    string wave = GetWaveCounterText(atTurnStart: true);
                    string msg = Loc.Get("battle_your_turn", hand);
                    if (wave != null)
                        msg += " " + wave;
                    ScreenReader.SayEvent(msg);
                    break;
                case Battle.Phase.Battle:
                    ScreenReader.SayEvent(Loc.Get("battle_resolving"));
                    break;
                case Battle.Phase.End:
                    ScreenReader.SayEvent(Loc.Get("battle_over"));
                    break;
                case Battle.Phase.LastStand:
                    // A dice standoff: the game blocks until Roll is pressed
                    _lastStandResultSeen = -1;
                    string subject = LastStandSystem.subject?.data?.title;
                    ScreenReader.SayEvent(subject != null
                        ? Loc.Get("battle_last_stand", subject)
                        : Loc.Get("battle_last_stand_generic"), interrupt: true);
                    break;
            }
        }

        /// <summary>Outcome of the last dice roll we already announced.</summary>
        private int _lastStandResultSeen = -1;

        public override void OnUpdate()
        {
            RedirectFromUseOnHandAnchor();
            base.OnUpdate();
            WatchLastStand();
        }

        /// <summary>
        /// At battle start the game parks focus on the "use on hand" targeting
        /// anchor, which reads as the meaningless "Use On Hand Anchor". It only
        /// matters while holding a self-target card, so when nothing is held move
        /// focus to the hand instead.
        /// </summary>
        private void RedirectFromUseOnHandAnchor()
        {
            var controller = Battle.instance?.playerCardController as CardControllerBattle;
            var anchor = controller?.useOnHandAnchor;
            if (anchor == null || controller.dragging != null)
                return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            if (navSystem == null || navSystem.currentNavigationItem != anchor)
                return;

            var hand = GetGroupItems(Group.Hand);
            if (hand.Count > 0)
            {
                _group = Group.Hand;
                NavigationHelper.FocusItem(hand[0]);
            }
        }

        /// <summary>
        /// The anchor is a drop zone, not a place to browse. All through battle
        /// setup the game re-parks default focus on it while the hand is still
        /// being drawn, so the redirect above has nowhere to send focus yet and
        /// it read as a spurious "Play without a target" before the first turn.
        /// It only means anything while a card is held — and then the targeting
        /// description names it properly — so stay silent otherwise.
        /// </summary>
        protected override bool ShouldAnnounceFocus(UINavigationItem item)
        {
            if (IsTargeting())
                return true;

            var controller = Battle.instance?.playerCardController as CardControllerBattle;
            return controller == null || item != controller.useOnHandAnchor;
        }

        /// <summary>
        /// The last stand dice roll resolves inside a coroutine with no event
        /// hook — watch the system's private result field for the outcome.
        /// </summary>
        private void WatchLastStand()
        {
            var battle = Battle.instance;
            if (battle == null || battle.phase != Battle.Phase.LastStand)
                return;

            var system = Object.FindObjectOfType<LastStandSystem>();
            if (system == null) return;

            int result = ReflectionUtil.GetIntField(system, "result", -1);
            if (result != -1 && result != _lastStandResultSeen)
            {
                _lastStandResultSeen = result;
                ScreenReader.SayEvent(Loc.Get(result == 0
                    ? "battle_last_stand_won"
                    : "battle_last_stand_lost"), interrupt: true);
            }
        }

        /// <summary>A companion survived defeat but takes a lasting injury.</summary>
        private void OnCardInjured(CardData card)
        {
            if (card == null) return;
            ScreenReader.SayEvent(Loc.Get("battle_companion_injured", card.title));
        }

        private void OnTurnStart(int turn)
        {
            // Combo gold is consumed by the combo event that follows it immediately;
            // drop any that somehow wasn't, so it can't attach to a later turn's combo
            _pendingComboGold = 0;

            // The battle loop still fires a turn-start event after the last
            // enemy dies — announcing "Turn 13" over the victory reads as if
            // the fight went on
            var battle = Battle.instance;
            if (battle == null || battle.ended)
                return;

            ScreenReader.SayEvent(Loc.Get("battle_turn", turn));
        }

        private void OnRedrawBellHit(RedrawBellSystem bell)
        {
            ScreenReader.SayEvent(Loc.Get("battle_bell_rung"));
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

        /// <summary>
        /// A unit's name qualified by whose side it is on — "your Snowball" /
        /// "enemy Snowball" — so combat narration tells apart two units that
        /// share a name. Falls back to the bare title when the side is unknown.
        /// </summary>
        private static string QualifyOwner(Entity entity)
        {
            string title = entity?.data?.title;
            if (string.IsNullOrEmpty(title))
                return title;

            var battle = Battle.instance;
            if (battle != null && entity.owner != null)
            {
                if (entity.owner == battle.player)
                    return Loc.Get("battle_your_unit", title);
                if (entity.owner == battle.enemy)
                    return Loc.Get("battle_enemy_unit", title);
            }
            return title;
        }

        private void OnEntityPostHit(Hit hit)
        {
            if (!InCombat() || hit?.target?.data == null) return;

            string target = QualifyOwner(hit.target);

            if (hit.dodged)
            {
                ScreenReader.SayEvent(Loc.Get("battle_dodged", target));
                return;
            }

            // Shell soaks damage before it lands, silently shrinking (or
            // erasing) the number that gets narrated — say what it blocked.
            // On a full absorb no hit line follows, so name the attacker here.
            if (hit.damageBlocked > 0)
            {
                if (hit.damageDealt <= 0 && hit.attacker?.data != null && hit.BasicHit)
                    ScreenReader.SayEvent(Loc.Get("battle_shell_blocked_all",
                        target, hit.damageBlocked, hit.attacker.data.title));
                else
                    ScreenReader.SayEvent(Loc.Get("battle_shell_blocked", target, hit.damageBlocked));
            }

            if (hit.damageDealt > 0)
            {
                if (!hit.BasicHit)
                {
                    // Status damage (Shroom, Teeth, Overload...): the "attacker"
                    // is only whoever applied the status, possibly long gone —
                    // "Hongo's Hammer hits Snowbo" for a shroom tick reads as a
                    // phantom attack, so name the status instead. Summon decay
                    // is skipped: its kill announcement is the whole story.
                    if (hit.damageType != "summoned")
                        ScreenReader.SayEvent(Loc.Get("battle_status_damage",
                            target, hit.damageDealt, ItemDescriber.GetDamageTypeName(hit)));
                }
                else if (hit.attacker?.data != null)
                    ScreenReader.SayEvent(Loc.Get("battle_hit", hit.attacker.data.title, target, hit.damageDealt));
                else
                    ScreenReader.SayEvent(Loc.Get("battle_takes_damage", target, hit.damageDealt));
            }
            else if (hit.damageDealt < 0)
            {
                ScreenReader.SayEvent(Loc.Get("battle_healed", target, -hit.damageDealt));
            }
            // Zero-damage hits (pure status applications) are narrated by OnStatusApplied

            // Counter-down effects tick an enemy toward acting sooner — the
            // number only ever changed silently on the counter icon. The
            // routine end-of-turn tick is a counter-reducing hit too
            // (Battle.CardCountDown, attacker null), and narrating that for
            // every unit every turn drowns the battle in chatter — so only
            // speak reductions somebody actually caused.
            if (hit.counterReduction > 0 && hit.attacker != null && hit.target.counter.max > 0)
                ScreenReader.SayEvent(Loc.Get("battle_counter_reduced",
                    target, hit.counterReduction, hit.target.counter.current));
        }

        /// <summary>
        /// Narrate stat changes on the cards themselves: attack gains/losses
        /// (Spice, Frost, "gain attack when hit" units...) and counter
        /// increases. Health changes are already narrated per hit, and normal
        /// counter ticking each turn would be noise, so both are skipped.
        /// </summary>
        private void OnStatusIconChanged(StatusIcon icon, Stat previous, Stat current)
        {
            if (!InCombat() || icon == null || icon.target?.data == null) return;
            if (previous.current == current.current) return;
            // A fresh icon jumping from zero is the card being dealt in, not a buff
            if (previous.current == 0 && previous.max == 0) return;

            string name = QualifyOwner(icon.target);
            int delta = current.current - previous.current;

            switch (icon.type)
            {
                case "damage":
                    ScreenReader.SayEvent(delta > 0
                        ? Loc.Get("battle_attack_gain", name, delta, current.current)
                        : Loc.Get("battle_attack_lose", name, -delta, current.current));
                    break;

                case "counter":
                    // Increases only: something pushed the action further away
                    // (or a counter-down was blocked); decreases are either the
                    // per-turn tick or hit.counterReduction, both handled elsewhere.
                    // A unit that just acted has its counter snapped back to max
                    // (Battle.CheckUnitsTakeTurns) — that is the rhythm of the
                    // game, not news, and saying it for every unit every turn
                    // buries the lines that matter.
                    if (delta > 0 && !(previous.current == 0 && current.current == current.max))
                        ScreenReader.SayEvent(Loc.Get("battle_counter_gain",
                            name, delta, current.current));
                    break;
            }
        }

        private void OnEntityKilled(Entity entity, DeathType deathType)
        {
            if (!InCombat() || entity?.data == null) return;
            ScreenReader.SayEvent(Loc.Get("battle_destroyed", QualifyOwner(entity)));
        }

        private void OnStatusApplied(StatusEffectApply apply)
        {
            if (!InCombat()) return;
            if (apply?.effectData == null || apply.target?.data == null) return;
            if (!apply.effectData.visible || apply.count <= 0) return;

            ScreenReader.SayEvent(Loc.Get("battle_status_applied",
                apply.count, ItemDescriber.GetStatusName(apply.effectData), QualifyOwner(apply.target)));
        }

        /// <summary>
        /// Narrate every trigger so the player knows who acts and why:
        /// snowed units skipping their action, nullified triggers, Smackback
        /// retaliation, Last Stand, and reaction chains ("triggered by").
        /// </summary>
        private void OnEntityTrigger(ref Trigger trigger)
        {
            if (!InCombat() || trigger?.entity?.data == null) return;

            string name = trigger.entity.data.title;

            // The game skips a snowed unit's trigger entirely (ActionProcessTrigger)
            if (trigger.entity.IsSnowed)
            {
                ScreenReader.SayEvent(Loc.Get("battle_trigger_snowed", name));
                return;
            }

            if (trigger.nullified)
            {
                ScreenReader.SayEvent(Loc.Get("battle_trigger_nullified", name));
                return;
            }

            switch (trigger.type)
            {
                case "smackback":
                    string attacker = trigger.triggeredBy?.data?.title;
                    ScreenReader.SayEvent(attacker != null
                        ? Loc.Get("battle_trigger_smackback", name, attacker)
                        : Loc.Get("battle_trigger_acts", name));
                    break;

                case "laststand":
                    ScreenReader.SayEvent(Loc.Get("battle_trigger_laststand", name));
                    break;

                default:
                    // Reaction chains: another unit set this trigger off. A card the
                    // player just played is "triggered by" the leader — treat as acting.
                    Entity by = trigger.triggeredBy;
                    if (by != null && by != trigger.entity && by.data != null
                        && by != trigger.entity.owner?.entity)
                        ScreenReader.SayEvent(Loc.Get("battle_trigger_chain", name, by.data.title));
                    else
                        ScreenReader.SayEvent(Loc.Get("battle_trigger_acts", name));
                    break;
            }
        }

        /// <summary>
        /// A kill combo's gold, held between the two events that make it up.
        /// Zero when no combo is mid-announcement.
        /// </summary>
        private int _pendingComboGold;

        /// <summary>
        /// "Combo x2" is the second enemy killed this turn — KillComboSystem counts
        /// kills per turn and resets each turn, and the whole reward is bonus gold.
        /// The bare multiplier names neither fact, so spell both out: the number on
        /// its own reads as a damage or card combo, which Wildfrost has no such thing.
        /// </summary>
        private void OnKillCombo(int combo)
        {
            if (!InCombat()) return;

            int gold = _pendingComboGold;
            _pendingComboGold = 0;

            ScreenReader.SayEvent(gold > 0
                ? Loc.Get("battle_kill_combo_gold", combo, gold)
                : Loc.Get("battle_kill_combo", combo));
        }

        /// <summary>Announce gold earned during battle (combo bonuses, bounties).</summary>
        private void OnDropGold(int amount, string source, Character owner, Vector3 position)
        {
            if (!InCombat() || amount <= 0) return;
            if (owner != null && References.Player != null && owner != References.Player) return;

            // A combo's gold is dropped just before the combo event that explains it,
            // and the two are one event to the player — the game fuses them into a
            // single "x2 / Combo / +5" popup over the dying enemy. Announcing it here
            // would be an unattributed "5 gold." followed by a detached "Combo x2!",
            // so hand it to OnKillCombo instead.
            if (source == ComboGoldSource)
            {
                _pendingComboGold = amount;
                return;
            }

            ScreenReader.SayEvent(Loc.Get("battle_gold_dropped", amount));
        }

        /// <summary>KillComboSystem's literal source tag on the gold it drops.</summary>
        private const string ComboGoldSource = "Combo";

        // ---- Input ----------------------------------------------------------

        protected override void HandleInput()
        {
            base.HandleInput();

            // Escape puts a picked-up card back (same as the gamepad Back action)
            if (IsTargeting() && NavigationHelper.IsBackPressed())
            {
                DebugLogger.LogInput(Name, "Cancel pickup");
                var controller = Battle.instance?.playerCardController;
                Entity held = controller?.dragging;
                controller?.DragCancel();
                ScreenReader.Say(Loc.Get("battle_pickup_cancelled", held?.data?.title ?? ""));
                return;
            }

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Ctrl+C / Ctrl+E: quick counter status for one side — who acts
            // in how many turns, without wading through the full board read
            if (ctrl && Input.GetKeyDown(KeyCode.C)) { DebugLogger.LogInput(Name, "Ally counters"); AnnounceCounters(allies: true); return; }
            if (ctrl && Input.GetKeyDown(KeyCode.E)) { DebugLogger.LogInput(Name, "Enemy counters"); AnnounceCounters(allies: false); return; }
            // Ctrl+H / Ctrl+Shift+H: who is hurt. Damage is narrated as it
            // lands, but after a few exchanges only a roll call answers
            // "who do I need to pull out?" — Shift flips it to the enemies
            if (ctrl && Input.GetKeyDown(KeyCode.H))
            {
                DebugLogger.LogInput(Name, shift ? "Enemy health" : "Ally health");
                AnnounceHealth(allies: !shift);
                return;
            }
            if (ctrl) return; // don't let Ctrl+combos fall through to the plain letter keys

            if (Input.GetKeyDown(KeyCode.H)) { DebugLogger.LogInput(Name, "Hand"); AnnounceHand(); }
            if (Input.GetKeyDown(KeyCode.B)) { DebugLogger.LogInput(Name, "Board"); AnnounceBoard(); }
            if (Input.GetKeyDown(KeyCode.W)) { DebugLogger.LogInput(Name, "Waves"); AnnounceWaves(); }
            if (Input.GetKeyDown(KeyCode.R)) { DebugLogger.LogInput(Name, "Bell"); AnnounceBell(); }
            if (Input.GetKeyDown(KeyCode.G)) { DebugLogger.LogInput(Name, "Gold"); AnnounceGold(); }
            if (Input.GetKeyDown(KeyCode.T)) { DebugLogger.LogInput(Name, "Turn"); AnnounceTurn(); }
            if (Input.GetKeyDown(KeyCode.M)) { DebugLogger.LogInput(Name, "Modifiers"); AnnounceModifiers(); }
        }

        /// <summary>
        /// One side's counters at a glance: each unit with a counter, its
        /// position, how many turns until it acts, and whether Snow froze it.
        /// </summary>
        private void AnnounceCounters(bool allies)
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var character = allies ? battle.player : battle.enemy;
            var parts = new List<string>();
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null || occupant.counter.max <= 0) continue;

                    string cell = occupant.data.title;
                    string position = ItemDescriber.GetEntitySlotShort(occupant);
                    if (!string.IsNullOrEmpty(position))
                        cell += " " + position;
                    cell += ", " + Loc.Get("battle_acts_in", occupant.counter.current);
                    if (occupant.IsSnowed)
                        cell += ", " + Loc.Get("counter_frozen");
                    parts.Add(cell);
                }
            }

            if (parts.Count == 0)
            {
                ScreenReader.Say(Loc.Get(allies
                    ? "battle_counters_none_ally"
                    : "battle_counters_none_enemy"), interrupt: true);
                return;
            }

            parts.Insert(0, Loc.Get(allies ? "battle_counters_allies" : "battle_counters_enemies"));
            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        /// <summary>
        /// One side's health at a glance: every unit on the board with its
        /// position and current health out of max. Hits are narrated as they
        /// land, but a running tally is impossible to hold across a long
        /// fight — this is the roll call that says who to recall and heal.
        /// </summary>
        private void AnnounceHealth(bool allies)
        {
            var battle = Battle.instance;
            if (battle == null) return;

            var character = allies ? battle.player : battle.enemy;
            var parts = new List<string>();
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null || !occupant.alive) continue;
                    // Boardable cards without health (scenery, some summons)
                    // have nothing to report
                    if (occupant.hp.max <= 0) continue;

                    string cell = occupant.data.title;
                    string position = ItemDescriber.GetEntitySlotShort(occupant);
                    if (!string.IsNullOrEmpty(position))
                        cell += " " + position;
                    cell += ", " + ItemDescriber.DescribeHealth(occupant);
                    parts.Add(cell);
                }
            }

            if (parts.Count == 0)
            {
                ScreenReader.Say(Loc.Get(allies
                    ? "battle_health_none_ally"
                    : "battle_health_none_enemy"), interrupt: true);
                return;
            }

            parts.Insert(0, Loc.Get(allies ? "battle_health_allies" : "battle_health_enemies"));
            ScreenReader.Say(string.Join(". ", parts), interrupt: true);
        }

        /// <summary>
        /// I inspects the focused card — but not while holding one: opening
        /// the zoomed inspect view mid-placement would fight the drag state.
        /// </summary>
        protected override void OnInspectKey()
        {
            if (IsTargeting())
            {
                ScreenReader.Say(Loc.Get("select_blocked"), interrupt: true);
                return;
            }
            base.OnInspectKey();
        }

        /// <summary>
        /// While holding a card: the valid targets form a grid — Up/Down move
        /// between rows (staying in the same column), Left/Right move along the
        /// row, and nothing wraps: the edge announces itself instead of jumping
        /// to the far side. Otherwise: Up/Down switch groups, Left/Right move
        /// within the current group.
        /// </summary>
        protected override void Navigate(NavDirection dir)
        {
            if (IsTargeting())
            {
                NavigateTargeting(dir);
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

        /// <summary>
        /// Grid navigation over the valid targets while holding a card.
        /// Battlefield slots group into their lanes (a lane spans both sides,
        /// so Left/Right can cross from your slots to the enemy's); anything
        /// else (the recall zone, the play-without-target anchor, hand cards
        /// offered as targets) groups by vertical position. Up/Down change row
        /// landing on the horizontally closest target; Left/Right stay in the
        /// row; edges say so instead of wrapping around.
        /// </summary>
        private void NavigateTargeting(NavDirection dir)
        {
            var items = NavigationHelper.GetNavigableItems();
            if (items.Count == 0)
                return;

            var rows = BuildTargetRows(items);
            if (rows.Count == 0)
                return;

            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var current = navSystem?.currentNavigationItem;

            int rowIdx = -1, colIdx = -1;
            for (int r = 0; r < rows.Count && rowIdx < 0; r++)
            {
                int c = rows[r].IndexOf(current);
                if (c >= 0) { rowIdx = r; colIdx = c; }
            }
            if (rowIdx < 0)
            {
                NavigationHelper.FocusItem(rows[0][0]);
                return;
            }

            if (dir == NavDirection.Left || dir == NavDirection.Right)
            {
                int nextCol = colIdx + (dir == NavDirection.Right ? 1 : -1);
                if (nextCol < 0 || nextCol >= rows[rowIdx].Count)
                {
                    ScreenReader.Say(Loc.Get("nav_edge"), interrupt: true);
                    return;
                }
                NavigationHelper.FocusItem(rows[rowIdx][nextCol]);
                return;
            }

            int nextRow = rowIdx + (dir == NavDirection.Down ? 1 : -1);
            if (nextRow < 0 || nextRow >= rows.Count)
            {
                ScreenReader.Say(Loc.Get("nav_edge"), interrupt: true);
                return;
            }

            // Hold your place in the line: land on the same column when both
            // rows are battlefield lanes, and on the closest target otherwise
            var from = rows[rowIdx][colIdx];
            UINavigationItem best = null;
            int columnKey = GetTargetColumnKey(from);
            if (columnKey != int.MaxValue)
            {
                foreach (var item in rows[nextRow])
                {
                    if (GetTargetColumnKey(item) == columnKey)
                    {
                        best = item;
                        break;
                    }
                }
            }

            if (best == null)
            {
                float x = from.Position.x;
                float bestDistance = float.MaxValue;
                foreach (var item in rows[nextRow])
                {
                    float distance = Mathf.Abs(item.Position.x - x);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = item;
                    }
                }
            }
            NavigationHelper.FocusItem(best);
        }

        /// <summary>
        /// Arrange targeting items into visual rows, top to bottom, each row
        /// left to right. Slot-bound targets key on their lane index; loose
        /// items (recall zone, hand anchor) cluster by Y position.
        /// </summary>
        private static List<List<UINavigationItem>> BuildTargetRows(List<UINavigationItem> items)
        {
            var laneRows = new Dictionary<int, List<UINavigationItem>>();
            var loose = new List<UINavigationItem>();

            foreach (var item in items)
            {
                int lane = GetTargetLaneIndex(item);
                if (lane >= 0)
                {
                    if (!laneRows.TryGetValue(lane, out var row))
                        laneRows[lane] = row = new List<UINavigationItem>();
                    row.Add(item);
                }
                else
                {
                    loose.Add(item);
                }
            }

            var rows = new List<List<UINavigationItem>>();
            foreach (var row in laneRows.Values)
            {
                // Lanes walk in spoken-column order, so Right always moves to
                // the next column number
                row.Sort((a, b) => GetTargetColumnKey(a).CompareTo(GetTargetColumnKey(b)));
                rows.Add(row);
            }

            // Loose items whose Y positions are close enough share a row
            var looseRows = new List<List<UINavigationItem>>();
            const float rowTolerance = 1.25f;
            loose.Sort((a, b) => b.Position.y.CompareTo(a.Position.y));
            List<UINavigationItem> currentRow = null;
            float currentY = 0f;
            foreach (var item in loose)
            {
                if (currentRow == null || Mathf.Abs(item.Position.y - currentY) > rowTolerance)
                {
                    currentRow = new List<UINavigationItem>();
                    currentY = item.Position.y;
                    rows.Add(currentRow);
                    looseRows.Add(currentRow);
                }
                currentRow.Add(item);
            }

            // The recall zone, the play anchor and hand cards offered as targets
            // have no column, so screen order is all they have
            foreach (var row in looseRows)
                row.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            rows.Sort((a, b) => AverageY(b).CompareTo(AverageY(a)));
            return rows;
        }

        private static float AverageY(List<UINavigationItem> row)
        {
            float sum = 0f;
            foreach (var item in row)
                sum += item.Position.y;
            return row.Count > 0 ? sum / row.Count : 0f;
        }

        /// <summary>
        /// The battlefield lane a targeting item belongs to (same index for
        /// both sides — they are halves of the same visual row), or -1 for
        /// anything not sitting in a lane.
        /// </summary>
        private static int GetTargetLaneIndex(UINavigationItem item)
        {
            var slot = GetTargetSlot(item);
            var lane = slot != null ? slot.GetComponentInParent<CardSlotLane>() : null;
            if (lane == null)
                return -1;
            try
            {
                return References.Battle != null ? References.Battle.GetRowIndex(lane) : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>The battlefield slot a targeting item stands for, or null.</summary>
        private static CardSlot GetTargetSlot(UINavigationItem item)
        {
            var slot = item.GetComponent<CardSlot>() ?? item.GetComponentInParent<CardSlot>();
            if (slot != null)
                return slot;

            var entity = item.GetComponentInParent<Entity>();
            if (entity == null && item.clickHandler != null)
                entity = item.clickHandler.GetComponentInParent<Entity>();
            return entity != null ? entity.GetComponentInParent<CardSlot>() : null;
        }

        /// <summary>Sorts the enemy's columns after all of yours.</summary>
        private const int EnemyColumnOffset = 1000;

        /// <summary>
        /// A lane item's place in the spoken order: your columns counting up
        /// from your front line, then the enemy's counting up from theirs.
        /// Both sides number columns from the front, and the game renders your
        /// half mirrored, so your column 1 sits at the right edge of your side —
        /// ordering by screen position would count columns DOWN as you move
        /// right. Items with no column sort last.
        /// </summary>
        private static int GetTargetColumnKey(UINavigationItem item)
        {
            var slot = GetTargetSlot(item);
            var lane = slot != null ? slot.GetComponentInParent<CardSlotLane>() : null;
            if (lane?.slots == null)
                return int.MaxValue;

            int column = lane.slots.IndexOf(slot);
            if (column < 0)
                return int.MaxValue;

            bool isEnemy = References.Battle != null
                && slot.owner != null
                && slot.owner != References.Battle.player;
            return (isEnemy ? EnemyColumnOffset : 0) + column;
        }

        /// <summary>
        /// After an overlay (the inventory) closes, land focus on the first hand
        /// card rather than wherever the game left it. Falls back to any group
        /// with cards if the hand is empty.
        /// </summary>
        protected override UINavigationItem DefaultFocusItem()
        {
            var hand = GetGroupItems(Group.Hand);
            if (hand.Count > 0)
            {
                _group = Group.Hand;
                return hand[0];
            }
            foreach (Group group in new[] { Group.PlayerBoard, Group.EnemyBoard, Group.System })
            {
                var items = GetGroupItems(group);
                if (items.Count > 0)
                {
                    _group = group;
                    return items[0];
                }
            }
            return null;
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
                // The game moves focus on its own too (its default-item system),
                // and then nothing has mirrored the mouse's hover onto it yet —
                // so the drop target is armed before it is described
                NavigationHelper.MirrorCardHoverToFocus(item);

                string target = ItemDescriber.DescribeTarget(item);
                if (string.IsNullOrEmpty(target))
                    target = base.GetItemDescription(item);
                // Naming a cell the held card cannot be played on, with no hint
                // that Enter will not take it, is how a card ends up somewhere
                // the player never chose
                if (!string.IsNullOrEmpty(target) && !TargetAccepted(item))
                    target += " " + Loc.Get("battle_not_a_target");
                return target;
            }
            return base.GetItemDescription(item);
        }

        /// <summary>
        /// Whether releasing the held card here would actually play it.
        /// CardControllerBattle.Release plays onto its hoverEntity / hoverSlot /
        /// hoverContainer, so the drop target having followed our focus is most
        /// of the answer — the game refuses to move hoverEntity or hoverSlot onto
        /// anything the held card cannot take, so those two are self-checking.
        /// Containers are not: HoverContainer accepts any lane it is handed, so a
        /// lane still has to be put to the game's own CanPlayOn.
        ///
        /// Focus is free to sit on a cell that is no target at all — browsing
        /// there still reads out what stands in it — but the readout has to say
        /// so, or a card ends up somewhere the player never chose.
        /// </summary>
        private bool TargetAccepted(UINavigationItem item)
        {
            var controller = Battle.instance?.playerCardController;
            Entity held = controller?.dragging;
            if (held == null || item == null) return true;

            // A card that needs no target plays wherever it is released (bar its
            // own container), so no cell it can be browsed onto is a wrong one
            try
            {
                if (held.data != null && held.data.playType == Card.PlayType.Play
                    && !held.NeedsTarget)
                    return true;
            }
            catch { /* data not ready */ }

            GameObject handler = item.clickHandler != null ? item.clickHandler : item.gameObject;
            if (handler == null) return true;

            var entity = handler.GetComponentInParent<Entity>();
            if (entity != null && controller.hoverEntity == entity) return true;

            CardSlot slot = GetTargetSlot(item);
            if (slot != null && controller.hoverSlot == slot) return true;

            // Lanes and the recall zone hover unconditionally
            var container = handler.GetComponentInParent<CardContainer>();
            if (container != null && controller.hoverContainer == container)
                return CanReleaseOn(held, container);

            return false;
        }

        /// <summary>
        /// The game's verdict on releasing a held card onto a container: the
        /// recall zone takes anything recallable, everything else answers
        /// through Entity.CanPlayOn — the same call Release makes.
        /// </summary>
        private static bool CanReleaseOn(Entity held, CardContainer container)
        {
            try
            {
                var player = Battle.instance?.player;
                if (player != null && container == player.discardContainer)
                    return container.canBePlacedOn && held.owner == player && held.CanRecall();
                return held.CanPlayOn(container);
            }
            catch
            {
                return true; // never invent a warning we cannot stand behind
            }
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

            // Last stand: the Roll button belongs to no navigation layer, so
            // while the game waits for it, Enter rolls the dice directly.
            if (battle != null && battle.phase == Battle.Phase.LastStand)
            {
                var lastStand = Object.FindObjectOfType<LastStandSystem>();
                var rollButton = ReflectionUtil.GetField<GameObject>(lastStand, "button");
                if (rollButton != null && rollButton.activeInHierarchy)
                {
                    DebugLogger.LogInput(Name, "Last stand roll");
                    lastStand.Roll();
                    ScreenReader.Say(Loc.Get("battle_last_stand_rolling"), interrupt: true);
                    return;
                }
            }

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
                string title = held?.data?.title ?? "";

                // Captured before Release: they decide what the release means
                bool fromBoard = held != null && Battle.IsOnBoard(held);
                bool toRecall = controller.hoverContainer != null
                    && controller.hoverContainer == battle.player?.discardContainer;
                bool busyBefore = !IsActionQueueEmpty();

                if (ReflectionUtil.InvokeMethod(controller, "Release"))
                {
                    // Every successful release queues actions (move/play + end turn);
                    // an invalid target queues nothing and the card snaps back.
                    bool acted = !busyBefore && !IsActionQueueEmpty();

                    if (acted && toRecall)
                    {
                        string msg = Loc.Get("battle_unit_recalled", title);
                        if (fromBoard)
                            msg += " " + Loc.Get("battle_free_action");
                        ScreenReader.SayEvent(msg);
                    }
                    else if (acted && fromBoard)
                    {
                        // Repositioning a board unit is free — the turn continues
                        ScreenReader.SayEvent(Loc.Get("battle_unit_moved", title)
                            + " " + Loc.Get("battle_free_action"));
                    }
                    else if (acted)
                    {
                        ScreenReader.SayEvent(Loc.Get("battle_card_released", title));
                    }
                    else
                    {
                        ScreenReader.Say(Loc.Get("battle_invalid_target"));
                    }
                }
                return;
            }

            // Focused item is one of our own cards (in hand, or a unit on the
            // board — moving, swapping and recalling units is a free action)
            Entity entity = GetEntityFromItem(current);
            if (entity != null && controller != null && entity.owner == battle.player
                && (entity.InHand() || Battle.IsOnBoard(entity)))
            {
                DebugLogger.LogInput(Name, "Pick up card");
                bool onBoard = Battle.IsOnBoard(entity);
                controller.hoverEntity = entity;
                if (ReflectionUtil.SetField(controller, "pressEntity", entity)
                    && ReflectionUtil.InvokeMethod(controller, "Press")
                    && controller.dragging != null)
                {
                    string msg = Loc.Get(
                        onBoard ? "battle_unit_picked_up" : "battle_card_picked_up",
                        entity.data?.title ?? "");
                    string hint = HintOnce(onBoard ? "battle_move_hint" : "battle_pickup_hint");
                    if (hint != null)
                        msg += " " + hint;
                    ScreenReader.Say(msg);
                }
                else
                {
                    ScreenReader.Say(Loc.Get(onBoard ? "battle_cannot_move" : "battle_cannot_play"));
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

        private static bool IsActionQueueEmpty()
        {
            try { return ActionQueue.Empty; }
            catch { return true; }
        }

        /// <summary>How many cards in the player's hand carry a crown.</summary>
        private static int CountCrownedInHand(Battle battle)
        {
            var hand = battle?.player?.handContainer;
            if (hand == null) return 0;

            int count = 0;
            foreach (Entity entity in hand)
            {
                if (entity?.data != null && entity.data.HasCrown)
                    count++;
            }
            return count;
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
                        cell += " " + ItemDescriber.DescribeHealth(occupant);
                    if (occupant.damage.max > 0)
                        cell += " " + Loc.Get("stat_attack", ItemDescriber.GetShownAttack(occupant));
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
            var items = BuildWaveItems();
            if (items == null || items.Count == 0)
            {
                ScreenReader.Say(Loc.Get("battle_no_waves"), interrupt: true);
                return;
            }

            ScreenReader.Say(string.Join(". ", items), interrupt: true);
        }

        /// <summary>
        /// "Next wave in N turns", read from the wave deploy HUD. The deployer
        /// counts down from an action it queues when the turn starts, so at
        /// that moment the field still holds last turn's number and the one
        /// the player is about to see is a turn lower. An empty enemy board
        /// deploys the wave immediately whatever the counter says. Either way,
        /// a wave landing this very turn has no countdown worth speaking — the
        /// arrival announcement follows a second behind it.
        /// </summary>
        private string GetWaveCounterText(bool atTurnStart = false)
        {
            int counter = WaveDeployer.GetCounter(WaveDeployer.Find());
            if (atTurnStart)
            {
                if (EnemyBoardIsEmpty()) return null;
                counter--;
            }

            if (counter <= 0) return null;

            return Loc.Get("battle_next_wave", counter);
        }

        /// <summary>No enemies left standing — the next wave deploys at once.</summary>
        private static bool EnemyBoardIsEmpty()
        {
            try
            {
                var enemy = Battle.instance?.enemy;
                return enemy != null && Battle.GetCardsOnBoard(enemy).Count <= 0;
            }
            catch
            {
                return false;
            }
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

        /// <summary>
        /// Read the run modifier bells hanging in the HUD (gauntlet/event rules).
        /// They only explain themselves via hover panels, which keyboard
        /// navigation can't reach.
        /// </summary>
        private void AnnounceModifiers()
        {
            var parts = new List<string>();
            foreach (var icon in Object.FindObjectsOfType<ModifierIcon>())
            {
                if (icon == null || !icon.gameObject.activeInHierarchy)
                    continue;

                string desc = ItemDescriber.DescribeModifierIcon(icon);
                if (!string.IsNullOrEmpty(desc) && !parts.Contains(desc))
                    parts.Add(desc);
            }

            ScreenReader.Say(parts.Count > 0
                ? string.Join(". ", parts)
                : Loc.Get("battle_no_modifiers"), interrupt: true);
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

        // ---- Review buffer sources -------------------------------------------

        /// <summary>Hand buffer: one item per hand card, as its short read.</summary>
        internal List<string> BuildHandItems()
        {
            var hand = Battle.instance?.player?.handContainer;
            if (hand == null) return null;

            var items = new List<string>();
            foreach (Entity entity in hand)
            {
                string desc = ItemDescriber.DescribeEntityShort(entity);
                if (desc != null)
                    items.Add(desc);
            }
            return items;
        }

        /// <summary>Board buffer: one item per unit with its position, your side first.</summary>
        internal List<string> BuildBoardItems()
        {
            var battle = Battle.instance;
            if (battle == null) return null;

            var items = new List<string>();
            AddSideBufferItems(items, battle.player);
            AddSideBufferItems(items, battle.enemy);
            return items;
        }

        private static void AddSideBufferItems(List<string> items, Character character)
        {
            if (character == null) return;
            for (int row = 0; row < 2; row++)
            {
                CardSlotLane lane = GetLane(character, row);
                if (lane?.slots == null) continue;
                foreach (CardSlot slot in lane.slots)
                {
                    Entity occupant = slot != null ? slot.GetTop() : null;
                    if (occupant?.data == null) continue;

                    string summary = ItemDescriber.SummarizeEntity(occupant);
                    if (summary == null) continue;

                    string position = ItemDescriber.GetSlotPosition(slot);
                    items.Add(string.IsNullOrEmpty(position)
                        ? summary
                        : position + ": " + summary);
                }
            }
        }

        /// <summary>Resources buffer: gold, bell, turn, piles, wave counter.</summary>
        internal List<string> BuildResourceItems()
        {
            var battle = Battle.instance;
            if (battle == null) return null;

            var items = new List<string>();

            try
            {
                items.Add(Loc.Get("gold_amount", References.Player.data.inventory.gold.Value));
            }
            catch { }

            var bell = Object.FindObjectOfType<RedrawBellSystem>();
            if (bell != null)
            {
                items.Add(bell.IsCharged
                    ? Loc.Get("battle_bell_charged")
                    : Loc.Get("battle_bell_charging", bell.counter.current));
            }

            items.Add(Loc.Get("battle_turn", battle.turnCount) + ". "
                + (battle.phase == Battle.Phase.Play
                    ? Loc.Get("battle_phase_play")
                    : Loc.Get("battle_phase_other")));

            var draw = battle.player?.drawContainer;
            if (draw != null)
                items.Add(Loc.Get(draw.Count == 1 ? "pocket_draw_one" : "pocket_draw", draw.Count));

            var discard = battle.player?.discardContainer;
            if (discard != null)
                items.Add(Loc.Get(discard.Count == 1 ? "pocket_discard_one" : "pocket_discard", discard.Count));

            string wave = GetWaveCounterText();
            if (wave != null)
                items.Add(wave);

            return items;
        }

        /// <summary>Waves buffer: the counter plus one item per remaining wave.</summary>
        internal List<string> BuildWaveItems()
        {
            var system = WaveDeployer.Find();
            var waves = WaveDeployer.GetWaves(system);
            if (waves == null) return null;

            var items = new List<string>();
            string counterText = GetWaveCounterText();
            if (counterText != null)
                items.Add(counterText);

            // Everything before the current index has already landed. The
            // overflow deployer never sets Wave.spawned, so its index is the
            // only honest marker of what is still coming.
            int remaining = 0;
            for (int index = WaveDeployer.GetCurrentWave(system) + 1; index <= waves.Count; index++)
            {
                var wave = waves[index - 1];
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
                items.Add(desc);
                remaining++;
            }

            if (remaining == 0)
                items.Add(Loc.Get("battle_all_waves_spawned"));
            return items;
        }

        public override string GetHelpText()
        {
            return Loc.Get("help_battle");
        }
    }
}
