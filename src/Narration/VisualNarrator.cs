using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Speaks the game's purely visual story moments — scenes that carry
    /// meaning only through animation: a miniboss slamming onto the board, a
    /// boss transforming between phases, the wave bell washing new enemies in,
    /// the final-boss shade possessing the leader, cards merging into a new
    /// one, and every speech bubble (town greeters, the muncher, the gnome),
    /// whose text exists but is never read anywhere else.
    /// This part holds the event subscriptions and the moments that arrive
    /// alone: speech bubbles, a miniboss intro, gold picked up outside combat.
    /// The wave bell has a part of its own, and the moments the game raises no
    /// event for are narrated from the Harmony patches in
    /// VisualNarrator.Patches.cs.
    /// </summary>
    public static partial class VisualNarrator
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Events.OnMinibossIntro += OnMinibossIntro;
            Events.OnDropGold += OnDropGold;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;
            Events.OnMinibossIntro -= OnMinibossIntro;
            Events.OnDropGold -= OnDropGold;
            _goldGained = 0;
            _goldDue = false;
            _arrivals.Clear();
            _arrivalDeadline = 0f;
            _bellPending = false;
            _couldNotFit = 0;
        }

        /// <summary>Speak a settled wave and any gold that has landed. Called
        /// every frame from the main update loop.</summary>
        internal static void Update()
        {
            if (_arrivalDeadline > 0f && Time.unscaledTime >= _arrivalDeadline)
                SpeakWave();
            if (_goldDue)
                SpeakGold();
        }

        /// <summary>
        /// Ambient speech bubbles: town building greeters, the muncher, shop
        /// keepers. The text is real and localized but only ever shown visually.
        /// Called from the display-time patch — NOT from OnCreate, which
        /// fires at enqueue time: several bubbles queued in one frame would all
        /// be spoken at once, out of sync with what is on screen.
        /// </summary>
        internal static void OnSpeechBubbleShown(SpeechBubbleData data)
        {
            if (data == null || string.IsNullOrEmpty(data.text))
                return;
            // Bubble text is the raw localized string, tags and all — the shop
            // keeper's own name arrives as "<#7569CF Monchi>"
            string text = TextProcessor.ProcessRawText(data.text)?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            string line = !string.IsNullOrEmpty(data.targetName)
                ? Loc.Get("speech_bubble", data.targetName, text)
                : text;
            // Queued, not interrupting: bubbles accompany whatever else is
            // being announced and should never cut it off.
            ScreenReader.SayEvent(line);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "VisualNarrator",
                $"Speech bubble: {line}");
        }

        private static int _goldGained;
        private static bool _goldDue;

        /// <summary>
        /// Gold gained outside combat: a treasure cave paying out on the map,
        /// or the reward for skipping a card. Both arrive as coins flying into
        /// the purse and say nothing anywhere — the map node never mentions the
        /// amount even after it pays. BattleHandler speaks for gold dropped
        /// during a fight and only then, so this covers the rest without ever
        /// doubling up with it.
        /// </summary>
        private static void OnDropGold(int amount, string source, Character owner, Vector3 position)
        {
            if (amount <= 0 || InCombat())
                return;
            if (owner != null && References.Player != null && owner != References.Player)
                return;

            // Spoken from the next tick, not here: the coins are booked onto
            // the purse by another listener on this same event, and which of
            // us runs first is down to subscription order.
            _goldGained += amount;
            _goldDue = true;
        }

        private static bool InCombat()
        {
            var battle = Battle.instance;
            return battle != null
                && (battle.phase == Battle.Phase.Play || battle.phase == Battle.Phase.Battle);
        }

        private static void SpeakGold()
        {
            _goldDue = false;
            int amount = _goldGained;
            _goldGained = 0;
            if (amount <= 0)
                return;

            string line = Loc.Get("narrate_gold_gained", amount);
            int total = GetPurseTotal();
            if (total > 0)
                line += " " + Loc.Get("gold_amount", total);

            ScreenReader.SayEvent(line);
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "VisualNarrator", line);
        }

        /// <summary>
        /// What the purse holds once the coins land. Dropped gold is booked as
        /// owed straight away and paid in as each coin particle arrives, so the
        /// two added together are the settled total at any point during the
        /// animation — the same sum the game itself writes to the save.
        /// </summary>
        private static int GetPurseTotal()
        {
            try
            {
                var inventory = References.Player?.data?.inventory;
                return inventory != null ? inventory.gold.Value + inventory.goldOwed : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>A miniboss lands on the board with a zoom-and-shake cinematic.</summary>
        private static void OnMinibossIntro(Entity entity)
        {
            string title = entity?.data?.title;
            if (string.IsNullOrEmpty(title))
                return;
            ScreenReader.SayEvent(Loc.Get("narrate_miniboss", title), interrupt: true);
        }

        internal static void Narrate(string locKey, params object[] args)
        {
            ScreenReader.SayEvent(Loc.Get(locKey, args));
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "VisualNarrator", locKey);
        }
    }
}
