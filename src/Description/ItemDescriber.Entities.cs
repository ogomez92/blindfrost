using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Card and unit reads: the verbose and short focus descriptions, card type,
    /// attack/health stat lines, flavour text, and the per-card detail buffer.
    /// </summary>
    public static partial class ItemDescriber
    {
        /// <summary>
        /// Short combat summary of a unit: name, health, counter, statuses.
        /// Used for slot occupants and targeting, where the full card text is too much.
        /// </summary>
        public static string SummarizeEntity(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string> { entity.data.title };

            if (entity.hp.max > 0)
                parts.Add(DescribeHealth(entity));
            if (entity.damage.max > 0)
                parts.Add(Loc.Get("stat_attack", GetShownAttack(entity)));
            if (entity.counter.max > 0)
            {
                parts.Add(Loc.Get("battle_acts_in", entity.counter.current));
                if (entity.IsSnowed)
                    parts.Add(Loc.Get("counter_frozen"));
            }

            string statuses = DescribeStatusEffects(entity);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Describe a card/entity: name, stats, and expanded description with keyword explanations.
        /// </summary>
        public static string DescribeEntity(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string>();

            string title = entity.data.title;
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            string cardType = GetCardTypeName(entity.data);
            if (!string.IsNullOrEmpty(cardType))
                parts.Add(cardType);

            if (entity.damage.max > 0)
                parts.Add(Loc.Get("stat_attack", GetShownAttack(entity)));

            if (entity.hp.max > 0)
                parts.Add(DescribeHealth(entity));

            if (entity.counter.max > 0)
            {
                parts.Add(Loc.Get("battle_acts_in", entity.counter.current));
                if (entity.IsSnowed)
                    parts.Add(Loc.Get("counter_frozen"));
            }

            // Active status effects (Snow, Frost, Shell...)
            var extraKeywords = new List<string>();
            string statuses = DescribeStatusEffects(entity, extraKeywords);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            // Injuries — the game shows red "Injured" text plus a keyword panel
            int injuryCount = 0;
            try { injuryCount = entity.data.injuries?.Count ?? 0; } catch { }
            if (injuryCount > 0)
            {
                parts.Add(injuryCount == 1
                    ? Loc.Get("card_injured_one")
                    : Loc.Get("card_injured", injuryCount));
                extraKeywords.Add("injured");
            }

            // Crown and charms (card upgrades)
            string upgrades = DescribeUpgrades(entity.data);
            if (!string.IsNullOrEmpty(upgrades))
                parts.Add(upgrades);

            // Hidden keywords: extra panels the inspect view pops for effects
            // whose mechanics aren't in the card text
            try
            {
                foreach (var hidden in entity.GetHiddenKeywords())
                {
                    if (hidden == null) continue;
                    TextProcessor.CacheKeyword(hidden);
                    extraKeywords.Add(hidden.name);
                }
            }
            catch
            {
                // Effects may not be initialized yet
            }

            // Description text — expanded with keyword descriptions.
            // Keyword statuses announced above (Frenzy, Snow...) never appear in
            // the description text — the game shows them as icons with a hover
            // panel — so their ids are passed along to get explanations appended.
            string rawDesc = null;
            try
            {
                rawDesc = Card.GetDescription(entity.data);
            }
            catch
            {
                // Card.GetDescription may fail if the card isn't fully initialized
            }

            string processed = TextProcessor.ProcessForScreenReader(rawDesc, extraKeywords);

            // Cards with no ability text show italic flavour text in the
            // description box instead (Card.SetDescription) — read it there
            // too. A description made only of sprite icons processes down to
            // nothing, so that counts as empty as well: without this, such
            // cards were completely silent about what they do or say.
            if (string.IsNullOrWhiteSpace(rawDesc) || string.IsNullOrEmpty(processed))
            {
                string flavour = GetFlavourText(entity.data);
                if (!string.IsNullOrEmpty(flavour))
                    parts.Add(flavour);
            }

            if (!string.IsNullOrEmpty(processed))
                parts.Add(processed);

            // What opaque "apply X when Y" statuses (Wild...) actually do
            foreach (string note in BuildStatusMechanicNotes(entity))
                parts.Add(note);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Focus read for a card: short by default, full when VerboseFocus.</summary>
        public static string DescribeEntityFocus(Entity entity)
        {
            string desc = VerboseFocus ? DescribeEntity(entity) : DescribeEntityShort(entity);

            // For a card on the board, slot its position in right after the name
            // in the short "Row R S" form (e.g. "Row 1 3") so the player can map
            // the battlefield by ear before the stats.
            string slot = GetEntitySlotShort(entity);
            if (string.IsNullOrEmpty(slot))
                return desc;

            if (string.IsNullOrEmpty(desc))
            {
                desc = slot;
            }
            else
            {
                int afterName = desc.IndexOf(", ");
                desc = afterName < 0
                    ? desc + ", " + slot
                    : desc.Substring(0, afterName) + ", " + slot + desc.Substring(afterName);
            }

            // Trailing, so the card itself still reads first
            string opposite = DescribeOpposite(entity.GetComponentInParent<CardSlot>());
            return string.IsNullOrEmpty(opposite) ? desc : desc + ", " + opposite;
        }

        /// <summary>
        /// The game's own localized name for a card's type — "Companion", "Item",
        /// "Leader", "Clunker", "Miniboss", and so on — read straight from
        /// CardType.title, the text the game prints on the card's name tag. Null
        /// when the card has no type or its title isn't set. (Wildfrost has no
        /// card rarity, so there is nothing of that kind to report.)
        /// </summary>
        public static string GetCardTypeName(CardData data)
        {
            try
            {
                string title = data?.cardType?.title;
                return string.IsNullOrEmpty(title) ? null : title;
            }
            catch
            {
                return null; // localization may not be ready
            }
        }

        /// <summary>
        /// Short focus read: name, stats, and effect NAMES with stack counts —
        /// no card text, no keyword explanations. Those wait in the Details
        /// review buffer (Ctrl+Up) and in the game's inspect view (I).
        /// </summary>
        public static string DescribeEntityShort(Entity entity)
        {
            if (entity?.data == null) return null;

            var parts = new List<string>();

            string title = entity.data.title;
            if (!string.IsNullOrEmpty(title))
                parts.Add(title);

            string cardType = GetCardTypeName(entity.data);
            if (!string.IsNullOrEmpty(cardType))
                parts.Add(cardType);

            if (entity.damage.max > 0)
                parts.Add(Loc.Get("stat_attack", GetShownAttack(entity)));

            if (entity.hp.max > 0)
                parts.Add(DescribeHealth(entity));

            if (entity.counter.max > 0)
            {
                parts.Add(Loc.Get("battle_acts_in", entity.counter.current));
                if (entity.IsSnowed)
                    parts.Add(Loc.Get("counter_frozen"));
            }

            string statuses = DescribeStatusEffects(entity);
            if (!string.IsNullOrEmpty(statuses))
                parts.Add(statuses);

            int injuryCount = 0;
            try { injuryCount = entity.data.injuries?.Count ?? 0; } catch { }
            if (injuryCount > 0)
            {
                parts.Add(injuryCount == 1
                    ? Loc.Get("card_injured_one")
                    : Loc.Get("card_injured", injuryCount));
            }

            // Effects from the card text with their amounts ("Shroom 3", "Consume 1"),
            // skipping ones the active statuses above already announced — match on the
            // name alone, since the same effect can stand at a different amount there
            string rawDesc = null;
            try { rawDesc = Card.GetDescription(entity.data); } catch { }
            foreach (string mention in TextProcessor.ExtractKeywordMentions(rawDesc))
            {
                if (statuses == null || !statuses.Contains(TextProcessor.MentionName(mention)))
                    parts.Add(mention);
            }

            // Upgrades by name only — their effect text is a detail
            AddUpgradeNames(parts, entity.data);

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        /// <summary>Detail pieces for a card, in reading order.</summary>
        public static List<string> BuildEntityDetailParts(Entity entity)
        {
            if (entity?.data == null) return null;

            var items = new List<string>();

            string summary = DescribeEntityShort(entity);
            if (!string.IsNullOrEmpty(summary))
                items.Add(summary);

            // Status keyword ids so their explanations are appended below,
            // exactly like the verbose read does
            var extraKeywords = new List<string>();
            DescribeStatusEffects(entity, extraKeywords);

            int injuryCount = 0;
            try { injuryCount = entity.data.injuries?.Count ?? 0; } catch { }
            if (injuryCount > 0)
                extraKeywords.Add("injured");

            try
            {
                foreach (var hidden in entity.GetHiddenKeywords())
                {
                    if (hidden == null) continue;
                    TextProcessor.CacheKeyword(hidden);
                    extraKeywords.Add(hidden.name);
                }
            }
            catch
            {
                // Effects may not be initialized yet
            }

            string rawDesc = null;
            try { rawDesc = Card.GetDescription(entity.data); } catch { }

            var explanations = new List<string>();
            string text = TextProcessor.ProcessDescriptionParts(rawDesc, extraKeywords, explanations);
            if (!string.IsNullOrEmpty(text))
                items.Add(text);

            if (string.IsNullOrWhiteSpace(rawDesc))
            {
                string flavour = GetFlavourText(entity.data);
                if (!string.IsNullOrEmpty(flavour))
                    items.Add(flavour);
            }

            // One item per charm, with its full effect text
            string upgrades = DescribeUpgrades(entity.data);
            if (!string.IsNullOrEmpty(upgrades))
                items.Add(upgrades);

            items.AddRange(explanations);

            // What opaque "apply X when Y" statuses (Wild...) actually do —
            // right after the keyword explanations they complete
            foreach (string note in BuildStatusMechanicNotes(entity))
            {
                if (!items.Contains(note))
                    items.Add(note);
            }

            // What a summon puts on the board, and what "Trigger when ..."
            // does once its condition is met — both are hover-panel knowledge
            // a sighted player gets for free and the card text never states
            foreach (string note in BuildSummonNotes(entity))
            {
                if (!items.Contains(note))
                    items.Add(note);
            }
            foreach (string note in BuildTriggerReactionNotes(entity))
            {
                if (!items.Contains(note))
                    items.Add(note);
            }
            return items;
        }

        /// <summary>Localized flavour text (lore), shown on cards without ability text.</summary>
        public static string GetFlavourText(CardData data)
        {
            try
            {
                var key = data?.flavourKey;
                if (key == null || key.IsEmpty) return null;
                return TextProcessor.ProcessRawText(key.GetLocalizedString());
            }
            catch
            {
                return null; // localization may not be loaded yet
            }
        }

        /// <summary>
        /// Attack value as the card shows it: base damage plus temporary
        /// modifiers (Spice, Frost, ongoing effects). The game's attack icon
        /// displays damage + tempDamage, never below zero.
        /// </summary>
        public static int GetShownAttack(Entity entity)
        {
            int value = entity.damage.current;
            try { value += entity.tempDamage.Value; } catch { }
            return Mathf.Max(0, value);
        }

        /// <summary>
        /// Health as the card shows it: "5 of 10 health". Sighted players read the
        /// damage off the card at a glance, so the current value alone is not enough
        /// — without the max there is no way to tell a hurt unit from a small one.
        /// </summary>
        public static string DescribeHealth(Entity entity)
        {
            return Loc.Get("stat_health_of_max", entity.hp.current, entity.hp.max);
        }
    }
}
