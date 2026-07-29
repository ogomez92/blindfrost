using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Manages screen handlers based on active game scene.
    /// Detects scene changes and routes Update calls to the appropriate handler.
    /// </summary>
    public static class ScreenManager
    {
        private static readonly Dictionary<string, ScreenHandler> _handlers
            = new Dictionary<string, ScreenHandler>();

        private static ScreenHandler _activeHandler;
        private static string _lastSceneKey;
        private static GenericScreenHandler _fallback;

        /// <summary>Currently active screen handler, or null.</summary>
        public static ScreenHandler ActiveHandler => _activeHandler;

        /// <summary>
        /// Register all screen handlers. Called once during mod init.
        /// </summary>
        public static void Initialize()
        {
            _handlers.Clear();
            _activeHandler = null;
            _lastSceneKey = null;
            _fallback = new GenericScreenHandler();

            // Register handlers for each game screen
            Register("MainMenu", new MainMenuHandler());
            Register("Town", new TownHandler());
            Register("ContinueRun", new ContinueRunHandler());
            Register("Battle", new BattleHandler());
            Register("BattleWin", new BattleWinHandler());
            Register("CampaignEnd", new CampaignEndHandler());
            Register("CharacterSelect", new CharacterSelectHandler());
            Register("Event", new EventScreenHandler());
            Register("BossReward", new BossRewardHandler());
            Register("TownUnlocks", new TownUnlocksHandler());
            Register("CardFramesUnlocked", new CardFramesUnlockedHandler());
            Register("NewFrostGuardian", new FrostoscopeHandler());
            Register("JournalNameHistory", new JournalNameHandler());

            // Both credits scenes share one handler; it tells them apart itself
            var creditsHandler = new CreditsHandler();
            Register("Credits", creditsHandler);
            Register("CreditsEnd", creditsHandler);

            // The campaign map spans two scenes: "Campaign" loads the systems,
            // "MapNew" is the visible map. One handler serves both.
            var mapHandler = new MapHandler();
            Register("Campaign", mapHandler);
            Register("MapNew", mapHandler);

            // Pause menu (settings, battle log, lore) — routed via GameManager.paused,
            // not by scene: it lives in the always-loaded PauseScreen scene.
            Register("PauseMenu", new PauseMenuHandler());
        }

        /// <summary>
        /// Shut down all handlers.
        /// </summary>
        public static void Shutdown()
        {
            _activeHandler?.OnExit();
            _activeHandler = null;
            _lastSceneKey = null;
            _handlers.Clear();
        }

        /// <summary>
        /// Register a handler for a specific scene name.
        /// </summary>
        public static void Register(string sceneKey, ScreenHandler handler)
        {
            _handlers[sceneKey] = handler;
            DebugLogger.Log(DebugLogger.LogCategory.Handler, "ScreenManager",
                $"Registered handler: {sceneKey} -> {handler.Name}");
        }

        /// <summary>
        /// Called every frame from the mod's Update loop.
        /// Detects scene changes and dispatches to active handler.
        /// </summary>
        public static void Update()
        {
            string currentScene = GetCurrentSceneKey();

            // Detect scene change
            if (currentScene != _lastSceneKey)
            {
                OnSceneChanged(_lastSceneKey, currentScene);
                _lastSceneKey = currentScene;
            }

            // Update active handler
            _activeHandler?.OnUpdate();
        }

        private static void OnSceneChanged(string from, string to)
        {
            DebugLogger.LogState("ScreenManager", from ?? "null", to ?? "null");

            // Deactivate old handler
            _activeHandler?.OnExit();
            _activeHandler = null;

            // Activate new handler — dedicated if available, otherwise generic fallback
            if (to != null && _handlers.TryGetValue(to, out var handler))
            {
                _activeHandler = handler;
                _activeHandler.OnEnter();
            }
            else if (to != null)
            {
                // Use generic fallback for any screen without a dedicated handler
                _fallback.SetScene(to);
                _activeHandler = _fallback;
                _activeHandler.OnEnter();
            }
        }

        /// <summary>
        /// Temporary scenes that overlay the active scene without changing
        /// ActiveSceneKey (e.g. ContinueRun opens on top of Town).
        /// When one is loaded, it takes priority for handler routing.
        /// </summary>
        private static readonly string[] _overlayScenes =
        {
            // Ordered: the credits/journal/frame celebrations can stack on top
            // of another overlay (CreditsEnd loads over CampaignEnd), so the
            // ones that appear on top are checked first.
            "CreditsEnd", "Credits", "JournalNameHistory", "CardFramesUnlocked",
            "NewFrostGuardian", "TownUnlocks", "ContinueRun", "BossReward",
            "BattleWin", "CampaignEnd", "Mods", "DemoEnd"
        };

        private static string GetCurrentSceneKey()
        {
            try
            {
                // The pause menu overlays everything and never changes the
                // active scene — PauseMenu.OnEnable/OnDisable toggle this flag.
                if (GameManager.paused)
                    return "PauseMenu";

                foreach (string overlay in _overlayScenes)
                {
                    if (SceneManager.IsLoaded(overlay))
                        return overlay;
                }
                return SceneManager.ActiveSceneKey;
            }
            catch
            {
                return null;
            }
        }
    }
}
