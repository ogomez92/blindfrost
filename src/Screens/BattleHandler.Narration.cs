using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Combat narration: the subscribed game-event handlers that speak hits,
    /// status applications, triggers, kills, injuries, turns and combo gold.
    /// </summary>
    public partial class BattleHandler
    {
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

    }
}
