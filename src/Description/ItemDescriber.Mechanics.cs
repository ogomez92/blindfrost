using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Status effects and the plain-language notes that explain them: ApplyX triggers
    /// and targets, summons, trigger reactions, damage type and status names.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>
        /// List an entity's visible status effects as "Name amount" pairs.
        /// Optionally collects the effects' keyword ids so their explanations
        /// can be appended to the full card readout.
        /// </summary>
        public static string DescribeStatusEffects(Entity entity, List<string> keywordIds = null)
        {
            if (entity?.statusEffects == null || entity.statusEffects.Count == 0)
                return null;

            var parts = new List<string>();
            foreach (var effect in entity.statusEffects)
            {
                if (effect == null || !effect.visible)
                    continue;

                int amount;
                try { amount = effect.GetAmount(); }
                catch { amount = effect.count; }
                if (amount <= 0) continue;

                string name = GetStatusName(effect);
                parts.Add($"{name} {amount}");

                if (keywordIds != null && !string.IsNullOrEmpty(effect.keyword))
                    keywordIds.Add(effect.keyword);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// Plain-language notes for statuses whose keyword panel does not say
        /// what they actually do. The data-driven "apply X when Y" statuses
        /// (Wild: "Gain when other cards are killed" — gain what?) carry the
        /// answer in their asset fields: which effect they apply, how much,
        /// and to whom. One note per such status, e.g.
        /// "Wild: when any other card is destroyed, applies 1 Wild to itself."
        /// </summary>
        public static List<string> BuildStatusMechanicNotes(Entity entity)
        {
            var notes = new List<string>();
            if (entity?.statusEffects == null)
                return notes;

            foreach (var effect in entity.statusEffects)
            {
                if (effect == null || !effect.visible)
                    continue;
                string note = DescribeApplyXMechanics(effect);
                if (!string.IsNullOrEmpty(note) && !notes.Contains(note))
                    notes.Add(note);
            }
            return notes;
        }

        /// <summary>
        /// Every status effect a card carries, gathered from the four places
        /// the game keeps them: the live statuses on the entity, the effects
        /// its attack applies, the effects it starts play with, and the passive
        /// effects behind its traits. A card sitting in hand has not had its
        /// starting effects applied yet, so on those the data-side lists are
        /// the only ones with anything in them — reading only entity
        /// statusEffects would find nothing on the very cards being browsed.
        /// </summary>
        private static List<StatusEffectData> CollectCardEffects(Entity entity)
        {
            var found = new List<StatusEffectData>();
            if (entity == null)
                return found;

            void Take(StatusEffectData effect)
            {
                if (effect != null && !found.Contains(effect))
                    found.Add(effect);
            }

            try
            {
                if (entity.statusEffects != null)
                    foreach (var effect in entity.statusEffects)
                        Take(effect);

                if (entity.attackEffects != null)
                    foreach (var stack in entity.attackEffects)
                        Take(stack?.data);

                foreach (var trait in entity.traits)
                {
                    if (trait?.data?.effects != null)
                        foreach (var effect in trait.data.effects)
                            Take(effect);
                }

                CardData data = entity.data;
                if (data?.attackEffects != null)
                    foreach (var stack in data.attackEffects)
                        Take(stack?.data);
                if (data?.startWithEffects != null)
                    foreach (var stack in data.startWithEffects)
                        Take(stack?.data);
                if (data?.traits != null)
                {
                    foreach (var trait in data.traits)
                    {
                        if (trait?.data?.effects != null)
                            foreach (var effect in trait.data.effects)
                                Take(effect);
                    }
                }
            }
            catch { /* effects not initialized yet */ }

            return found;
        }

        /// <summary>
        /// What the cards a summon puts on the board actually are. "Summon
        /// Leech" names the card and stops; a sighted player hovers the summon
        /// keyword and reads Leech's stats off the panel, so leaving them out
        /// keeps the one number the decision turns on a secret. One note per
        /// distinct summoned card, in the same wording a card focus uses.
        /// </summary>
        public static List<string> BuildSummonNotes(Entity entity)
        {
            var notes = new List<string>();
            var described = new List<CardData>();

            foreach (var effect in CollectCardEffects(entity))
            {
                CardData summoned = GetSummonedCard(effect);
                if (summoned == null || described.Contains(summoned))
                    continue;

                // A card that summons a copy of itself (Split and friends) says
                // nothing the player is not already reading
                if (entity.data != null && summoned.name == entity.data.name)
                    continue;

                described.Add(summoned);
                string line = DescribeSummonedCard(summoned);
                if (!string.IsNullOrEmpty(line) && !notes.Contains(line))
                    notes.Add(line);
            }
            return notes;
        }

        /// <summary>
        /// The card a summon effect brings in, whether the effect is the summon
        /// itself or an instant that delegates to one.
        /// </summary>
        private static CardData GetSummonedCard(StatusEffectData effect)
        {
            try
            {
                if (effect is StatusEffectSummon summon)
                    return summon.summonCard;
                if (effect is StatusEffectInstantSummon instant)
                    return instant.targetSummon?.summonCard;
            }
            catch { /* asset not loaded */ }
            return null;
        }

        /// <summary>
        /// A summoned card as "Summons Leech: Companion, 2 attack, 3 health,
        /// acts every 3, Bloodsucker" — its stats read from card data, since a
        /// card that has not been summoned yet has no entity to read from.
        /// </summary>
        private static string DescribeSummonedCard(CardData data)
        {
            string title = SafeTitle(data);
            if (string.IsNullOrEmpty(title))
                return null;

            var parts = new List<string>();
            try
            {
                string cardType = GetCardTypeName(data);
                if (!string.IsNullOrEmpty(cardType))
                    parts.Add(cardType);

                if (data.hasAttack && data.damage > 0)
                    parts.Add(Loc.Get("stat_attack", data.damage));
                if (data.hasHealth && data.hp > 0)
                    parts.Add(Loc.Get("stat_health", data.hp));
                if (data.counter > 0)
                    parts.Add(Loc.Get("mech_summon_counter", data.counter));

                // What the summoned card itself does, by keyword name and amount
                string summonedDesc = null;
                try { summonedDesc = Card.GetDescription(data); } catch { }
                foreach (string mention in TextProcessor.ExtractKeywordMentions(summonedDesc))
                    parts.Add(mention);
            }
            catch { /* data not ready — the name alone still beats silence */ }

            return parts.Count > 0
                ? Loc.Get("mech_summons", title, string.Join(", ", parts))
                : Loc.Get("mech_summons_bare", title);
        }

        /// <summary>
        /// What "Trigger when ..." actually does. The card text names the
        /// condition and stops there, so the part that decides whether the card
        /// is worth playing — that triggering means attacking, right then, on
        /// top of its own turn — goes unsaid. One note per card however many
        /// reaction effects it carries: the conditions differ, the consequence
        /// does not.
        /// </summary>
        public static List<string> BuildTriggerReactionNotes(Entity entity)
        {
            var notes = new List<string>();
            bool retaliates = false;
            bool reacts = false;

            foreach (var effect in CollectCardEffects(entity))
            {
                if (effect == null) continue;
                switch (effect.GetType().Name)
                {
                    case "StatusEffectTriggerAgainstAttackerWhenHit":
                        retaliates = true;
                        break;
                    case "StatusEffectTriggerWhenAllyAttacks":
                    case "StatusEffectTriggerWhenCardTypeUsedOnAlly":
                    case "StatusEffectTriggerWhenStatusApplied":
                    case "StatusEffectTriggerWhenDeployed":
                        reacts = true;
                        break;
                }
            }

            // ActionTrigger runs the card's attack at its own targets and never
            // touches counter.current — only the turn loop's ActionTriggerByCounter
            // resets that — so a reaction really is an attack for free
            if (reacts)
                notes.Add(Loc.Get("mech_trigger_meaning"));
            if (retaliates)
                notes.Add(Loc.Get("mech_trigger_retaliate"));

            return notes;
        }

        /// <summary>
        /// "{Status}: {when it triggers}, {what it does} {to whom}." for an
        /// ApplyX-family status, or null when the effect is not of that family
        /// or its trigger class is not recognized.
        /// </summary>
        private static string DescribeApplyXMechanics(StatusEffectData effect)
        {
            var applyX = effect as StatusEffectApplyX;
            if (applyX == null)
                return null;

            string trigger = GetApplyXTriggerPhrase(effect.GetType().Name);
            if (trigger == null)
                return null;

            int amount;
            try { amount = effect.GetAmount(); }
            catch { amount = effect.count; }
            if (amount <= 0)
                amount = effect.count;

            string core;
            if (applyX.dealDamage)
            {
                core = Loc.Get("status_mech_damage", amount);
            }
            else
            {
                if (applyX.effectToApply == null)
                    return null;
                core = Loc.Get("status_mech_applies", amount, GetStatusName(applyX.effectToApply));
            }

            string text = $"{GetStatusName(effect)}: {trigger}, {core}";
            string target = GetApplyXTargetPhrase(applyX.applyToFlags);
            if (!string.IsNullOrEmpty(target))
                text += " " + target;
            return text + ".";
        }

        /// <summary>The trigger condition encoded in an ApplyX subclass's name.</summary>
        private static string GetApplyXTriggerPhrase(string typeName)
        {
            switch (typeName)
            {
                case "StatusEffectApplyXWhenCardDestroyed": return Loc.Get("mech_when_card_destroyed");
                case "StatusEffectApplyXWhenAllyIsKilled": return Loc.Get("mech_when_ally_killed");
                case "StatusEffectApplyXWhenUnitIsKilled": return Loc.Get("mech_when_unit_killed");
                case "StatusEffectApplyXWhenClunkerDestroyed": return Loc.Get("mech_when_clunker_destroyed");
                case "StatusEffectApplyXWhenHit": return Loc.Get("mech_when_hit");
                case "StatusEffectApplyXWhenUnitIsHit": return Loc.Get("mech_when_unit_hit");
                case "StatusEffectApplyXWhenDamageTaken": return Loc.Get("mech_when_damage_taken");
                case "StatusEffectApplyXWhenHealed": return Loc.Get("mech_when_healed");
                case "StatusEffectApplyXWhenAllyHealed": return Loc.Get("mech_when_ally_healed");
                case "StatusEffectApplyXWhenDeployed": return Loc.Get("mech_when_deployed");
                case "StatusEffectApplyXWhenDestroyed": return Loc.Get("mech_when_destroyed");
                case "StatusEffectApplyXWhenDrawn": return Loc.Get("mech_when_drawn");
                case "StatusEffectApplyXOnKill": return Loc.Get("mech_on_kill");
                case "StatusEffectApplyXOnTurn": return Loc.Get("mech_on_turn");
                case "StatusEffectApplyXEveryTurn": return Loc.Get("mech_every_turn");
                case "StatusEffectApplyXPostAttack": return Loc.Get("mech_after_attack");
                case "StatusEffectApplyXOnHit": return Loc.Get("mech_on_hit");
                default: return null;
            }
        }

        /// <summary>Who an ApplyX status hands its effect to, as a spoken phrase.</summary>
        private static string GetApplyXTargetPhrase(StatusEffectApplyX.ApplyToFlags flags)
        {
            var parts = new List<string>();
            void AddFlag(StatusEffectApplyX.ApplyToFlags flag, string key)
            {
                if ((flags & flag) != 0)
                    parts.Add(Loc.Get(key));
            }

            AddFlag(StatusEffectApplyX.ApplyToFlags.Self, "mech_to_self");
            AddFlag(StatusEffectApplyX.ApplyToFlags.Allies, "mech_to_allies");
            AddFlag(StatusEffectApplyX.ApplyToFlags.AlliesInRow, "mech_to_allies_row");
            AddFlag(StatusEffectApplyX.ApplyToFlags.FrontAlly, "mech_to_front_ally");
            AddFlag(StatusEffectApplyX.ApplyToFlags.Enemies, "mech_to_enemies");
            AddFlag(StatusEffectApplyX.ApplyToFlags.FrontEnemy, "mech_to_front_enemy");
            AddFlag(StatusEffectApplyX.ApplyToFlags.Attacker, "mech_to_attacker");
            AddFlag(StatusEffectApplyX.ApplyToFlags.Target, "mech_to_target");
            AddFlag(StatusEffectApplyX.ApplyToFlags.RandomAlly, "mech_to_random_ally");
            AddFlag(StatusEffectApplyX.ApplyToFlags.RandomEnemy, "mech_to_random_enemy");
            AddFlag(StatusEffectApplyX.ApplyToFlags.Hand, "mech_to_hand");

            return string.Join(", ", parts);
        }

        /// <summary>
        /// The player-facing name of a non-basic damage type ("shroom" →
        /// "Shroom", "spikes" → "Teeth"). Prefers the matching status carried
        /// by either combatant — its keyword holds the localized title — then
        /// a direct keyword lookup, then the raw type word capitalized.
        /// </summary>
        public static string GetDamageTypeName(Hit hit)
        {
            string type = hit?.damageType;
            if (string.IsNullOrEmpty(type))
                return null;

            foreach (var entity in new[] { hit.target, hit.attacker })
            {
                if (entity == null || entity.statusEffects == null)
                    continue;
                foreach (var effect in entity.statusEffects)
                {
                    if (effect != null && effect.type == type)
                        return GetStatusName(effect);
                }
            }

            try
            {
                var keyword = AddressableLoader.Get<KeywordData>("KeywordData", type);
                if (keyword != null && !string.IsNullOrEmpty(keyword.title))
                    return keyword.title;
            }
            catch { /* keyword lookup can fail during load */ }

            return char.ToUpperInvariant(type[0]) + type.Substring(1);
        }

        /// <summary>Human-readable name of a status effect via its keyword.</summary>
        public static string GetStatusName(StatusEffectData effect)
        {
            try
            {
                if (!string.IsNullOrEmpty(effect.keyword))
                {
                    var keyword = AddressableLoader.Get<KeywordData>("KeywordData", effect.keyword);
                    if (keyword != null && !string.IsNullOrEmpty(keyword.title))
                        return keyword.title;
                }
            }
            catch { /* keyword lookup can fail during load */ }

            return ScreenHandler.CleanName(effect.name);
        }
    }
}
