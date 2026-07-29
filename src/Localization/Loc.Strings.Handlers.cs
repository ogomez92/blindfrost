namespace WildfrostAccessibility
{
    /// <summary>
    /// The screen handler string table is large enough that it lives one file
    /// per language — Loc.Strings.Handlers.En/De/Es/Fr.cs. This part is the
    /// fan-out that registers every one of them.
    /// </summary>
    public static partial class Loc
    {
        /// <summary>
        /// Strings for the dedicated screen handlers — Town and its unlock buildings,
        /// ContinueRun, the campaign map, Battle, CharacterSelect and its tribes,
        /// MainMenu, BattleWin, CampaignEnd, the Daily Voyage balloon, the deckpack
        /// inventory, the map node categories, the pause menu and the story events —
        /// plus the shared item descriptions. One method per language, called in the
        /// order below. English, German, Spanish and French; other locales fall back
        /// to English until translated.
        /// </summary>
        private static void RegisterHandlerStrings()
        {
            RegisterHandlerStringsEnglish();
            RegisterHandlerStringsGerman();
            RegisterHandlerStringsSpanish();
            RegisterHandlerStringsFrench();
        }
    }
}
