using System.Collections.Generic;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Base class for screen-specific accessibility handlers.
    /// Each game screen (MainMenu, Battle, Campaign, etc.) gets its own handler.
    /// This part is the handler contract the ScreenManager drives — lifecycle,
    /// help text, focused-item detail parts, and once-per-session hints. The
    /// button-label engine lives in ScreenHandler.ButtonText.cs.
    /// </summary>
    public abstract partial class ScreenHandler
    {
        /// <summary>Display name for logging.</summary>
        public abstract string Name { get; }

        /// <summary>Called when this screen becomes active.</summary>
        public virtual void OnEnter()
        {
            DebugLogger.LogState("ScreenManager", "->", Name);
        }

        /// <summary>Called when leaving this screen.</summary>
        public virtual void OnExit()
        {
            DebugLogger.LogState("ScreenManager", Name, "->");
        }

        /// <summary>Called every frame while this screen is active.</summary>
        public abstract void OnUpdate();

        /// <summary>
        /// Help text announced when the user presses F1 on this screen.
        /// Override to explain what the screen is for and which controls work here.
        /// </summary>
        public virtual string GetHelpText()
        {
            return Loc.Get("help_text");
        }

        /// <summary>
        /// Detail-buffer parts for the focused item when this screen knows how to
        /// describe it in depth — the same information the I key reads (town
        /// building help, campaign map node waves/rewards). Returns null when the
        /// screen has nothing extra; the Details buffer then falls back to
        /// splitting the focus read into sentences.
        /// </summary>
        public virtual List<string> GetFocusedDetailParts(UINavigationItem item)
        {
            return null;
        }

        // Hint keys already spoken this game session (hints repeat only via F1)
        private static readonly HashSet<string> _spokenHints = new HashSet<string>();

        /// <summary>
        /// Returns the localized hint the first time it is requested this session,
        /// null afterwards. Keeps navigation instructions from repeating on every
        /// screen entry — F1 always has the full help.
        /// </summary>
        protected static string HintOnce(string locKey)
        {
            if (!_spokenHints.Add(locKey))
                return null;
            return Loc.Get(locKey);
        }
    }
}
