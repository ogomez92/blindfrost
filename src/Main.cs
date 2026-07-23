using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Main entry point for the Wildfrost Accessibility mod.
    /// Provides screen reader support for blind players.
    /// </summary>
    public class WildfrostAccessibilityMod : WildfrostMod
    {
        public static WildfrostAccessibilityMod Instance { get; private set; }

        public override string GUID => "accessibility.wildfrost.screenreader";
        public override string[] Depends => new string[] { };
        public override string Title => "Wildfrost Accessibility";
        public override string Description => "Screen reader accessibility mod for blind players.";

        [ConfigItem(false, "Enable debug logging (also toggled with F10 in-game)", forceTitle: "Debug Mode")]
        public bool debugMode;

        private GameObject _updateObj;

        public WildfrostAccessibilityMod(string modDirectory) : base(modDirectory)
        {
            SelfEnableOnFirstRun(modDirectory);
        }

        /// <summary>
        /// Adds this mod to the game's enabled-mods list so blind players never
        /// have to find the Mods menu (which is inaccessible without the mod —
        /// chicken-and-egg).
        ///
        /// The game constructs every mod it finds in StreamingAssets/Mods on
        /// boot, but only activates the ones stored under "lastSavedMods" in
        /// the save file (see Bootstrap.ModsSetup and WildfrostMod.GetLastMods).
        /// This constructor runs before that check, so registering our GUID here
        /// makes the game activate us through its normal path on the same boot.
        ///
        /// Self-enable fires when:
        ///  - the save has no "lastSavedMods" key at all (fresh or wiped save;
        ///    a marker file can't be trusted here because the save state it
        ///    guarded no longer exists), or
        ///  - our GUID is missing and the marker file is absent (first install
        ///    onto an existing modded save).
        /// A save that has "lastSavedMods" without our GUID while the marker
        /// exists means the player disabled us in the Mods menu — respected.
        /// </summary>
        private void SelfEnableOnFirstRun(string modDirectory)
        {
            try
            {
                string marker = Path.Combine(modDirectory, "autoenable.marker");
                string[] enabled = SaveSystem.LoadProgressData<string[]>("lastSavedMods");

                if (enabled == null)
                {
                    SaveSystem.SaveProgressData("lastSavedMods", new[] { GUID });
                    Debug.Log("[WildfrostAccessibility] Fresh save: mod self-enabled, will load this boot");
                }
                else if (!enabled.Contains(GUID) && !File.Exists(marker))
                {
                    SaveSystem.SaveProgressData("lastSavedMods", enabled.Append(GUID).ToArray());
                    Debug.Log("[WildfrostAccessibility] First run: mod self-enabled, will load this boot");
                }

                if (!File.Exists(marker))
                    File.WriteAllText(marker, "This file marks that the mod has auto-enabled itself once. Delete it to make the mod auto-enable again on the next game start.");
            }
            catch (Exception ex)
            {
                // Never let self-enable break mod construction — the game's
                // ModsSetup aborts ALL mods if a constructor throws.
                Debug.LogError($"[WildfrostAccessibility] Self-enable failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Re-adds this mod to "lastSavedMods" if something wiped it while we
        /// are loaded. On a brand-new save, VersionCompatibility.GameStart sees
        /// version 0, runs Reset Progress scripts that delete the whole save
        /// file (and even the profile folder) — including the entry written by
        /// SelfEnableOnFirstRun — so without this the mod silently stays off
        /// from the second boot onward. Called by a Harmony postfix on
        /// VersionCompatibility.GameStart, which only exists while the mod is
        /// loaded; a player who disables the mod in the Mods menu is unloaded
        /// first, so their choice is never overridden.
        /// </summary>
        internal static void EnsureEnabledInSave()
        {
            var mod = Instance;
            if (mod == null)
                return;

            try
            {
                string[] enabled = SaveSystem.LoadProgressData<string[]>("lastSavedMods") ?? new string[0];
                if (!enabled.Contains(mod.GUID))
                {
                    SaveSystem.SaveProgressData("lastSavedMods", enabled.Append(mod.GUID).ToArray());
                    Debug.Log("[WildfrostAccessibility] Enabled-mods list was reset by the game; re-added self");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WildfrostAccessibility] EnsureEnabledInSave failed: {ex.Message}");
            }
        }

        protected override void Load()
        {
            Instance = this;
            Debug.Log("[WildfrostAccessibility] Load() started");

            try
            {
                base.Load();
                Debug.Log("[WildfrostAccessibility] base.Load() completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WildfrostAccessibility] base.Load() failed: {ex.Message}\n{ex.StackTrace}");
            }

            try
            {
                ScreenReader.Initialize();
                Debug.Log("[WildfrostAccessibility] ScreenReader initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WildfrostAccessibility] ScreenReader init failed: {ex.Message}\n{ex.StackTrace}");
            }

            DebugLogger.Initialize(this);
            Loc.Initialize();
            Loc.LoadLanguageOverride(ModDirectory);
            ScreenManager.Initialize();
            VisualNarrator.Initialize();
            LoadVerbosity();
            CreateUpdateHook();

            Debug.Log("[WildfrostAccessibility] All systems initialized, attempting speech");
            ScreenReader.Say(Loc.Get("mod_loaded"), interrupt: true);
            WriteLine("Wildfrost Accessibility mod loaded.");
        }

        protected override void Unload()
        {
            ScreenReader.Say(Loc.Get("mod_unloaded"), interrupt: true);

            VisualNarrator.Shutdown();
            OverlayWatcher.Reset();
            CharmGainNarrator.Reset();
            HelpPanelRouter.Reset();
            ScreenManager.Shutdown();
            DestroyUpdateHook();
            ScreenReader.Shutdown();

            Instance = null;
            base.Unload();
        }

        /// <summary>
        /// Creates a persistent GameObject to receive Unity Update calls.
        /// WildfrostMod has no built-in Update loop, so we create one.
        /// </summary>
        private void CreateUpdateHook()
        {
            _updateObj = new GameObject("WildfrostAccessibility_Update");
            _updateObj.AddComponent<ModUpdateBehaviour>();
            UnityEngine.Object.DontDestroyOnLoad(_updateObj);
        }

        private void DestroyUpdateHook()
        {
            if (_updateObj != null)
            {
                UnityEngine.Object.Destroy(_updateObj);
                _updateObj = null;
            }
        }

        /// <summary>
        /// Called every frame by ModUpdateBehaviour.
        /// Routes input and updates to handlers.
        /// </summary>
        internal void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                debugMode = !debugMode;
                ScreenReader.Say(debugMode ? "Debug mode on" : "Debug mode off", interrupt: true);
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                AnnounceHelp();
            }

            // O opens/closes the game menu (settings, battle log, lore) anywhere.
            // The game only opens it via mouse click or gamepad, so keyboard
            // users need this binding. Inactive while typing into a text field.
            if (Input.GetKeyDown(KeyCode.O) && !NavigationHelper.IsTextInputFocused())
            {
                TogglePauseMenu();
            }

            // V cycles focus verbosity: short reads (name, stats, effect names;
            // details wait in the review buffers) or the full verbose reads
            if (Input.GetKeyDown(KeyCode.V) && !NavigationHelper.IsTextInputFocused())
            {
                ToggleVerbosity();
            }

            // Ctrl+arrows: review buffers (event history, details, hand...)
            ReviewBuffers.Update();

            // F9 (debug mode only): dump the navigation state to the log
            if (debugMode && Input.GetKeyDown(KeyCode.F9))
            {
                NavigationHelper.DumpNavigationState();
                ScreenReader.Say("Navigation state dumped", interrupt: true);
            }

            // Debug: trace every Enter press across all input systems, so
            // phantom activations show up with their source in the log
            if (debugMode && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                NavigationHelper.LogEnterDiagnostic();
            }

            // Keep the game in controller mode permanently. In mouse mode the
            // game hovers — and Select-clicks — whatever sits under the
            // PHYSICAL mouse cursor, which a blind player never aims: that
            // produced phantom clicks on menu buttons behind everything.
            NavigationHelper.EnsureControllerMode();

            // Check for overlay popups (help panels, prompts, etc.)
            PopupReader.Update();

            // Route updates to the active screen handler
            ScreenManager.Update();

            // Cinema bar text (event titles, story lines) — after ScreenManager,
            // so a handler's interrupting screen announcement speaks first and
            // can fold the bar text in via CinemaBarReader.SyncAnnounced()
            CinemaBarReader.Update();
        }

        /// <summary>
        /// Restore the focus verbosity chosen with V on a previous run.
        /// Defaults to short reads (the review buffers carry the details).
        /// </summary>
        private void LoadVerbosity()
        {
            try
            {
                // Stored as a string ("1"/"0"): SaveSystem.LoadProgressData<T>
                // constrains T to a reference type, so bool can't be used directly.
                string stored = SaveSystem.LoadProgressData<string>("accessibilityVerboseFocus");
                ItemDescriber.VerboseFocus = stored == "1";
            }
            catch
            {
                ItemDescriber.VerboseFocus = false;
            }
        }

        private void ToggleVerbosity()
        {
            ItemDescriber.VerboseFocus = !ItemDescriber.VerboseFocus;
            try
            {
                SaveSystem.SaveProgressData("accessibilityVerboseFocus",
                    ItemDescriber.VerboseFocus ? "1" : "0");
            }
            catch
            {
                // Not persisted this run; the toggle still applies until quit
            }
            ScreenReader.Say(Loc.Get(ItemDescriber.VerboseFocus
                ? "verbosity_verbose"
                : "verbosity_short"), interrupt: true);
        }

        private void AnnounceHelp()
        {
            // The inventory overlay sits on top of whatever screen is active —
            // while it is open, its keys are what the player needs to hear
            string help = DeckpackNavigator.IsOpen
                ? Loc.Get("help_deckpack")
                : ScreenManager.ActiveHandler?.GetHelpText() ?? Loc.Get("help_text");

            // The review buffers and verbosity toggle work on every screen, so
            // append them to whatever per-screen help was chosen (unless it is
            // the global help, which already lists them)
            if (help != Loc.Get("help_text"))
                help += " " + Loc.Get("help_buffers");

            ScreenReader.Say(help, interrupt: true);
        }

        /// <summary>
        /// Toggle the game's pause menu (persistent PauseScreen scene).
        /// Opening is announced by PauseMenuHandler once ScreenManager routes to it.
        /// </summary>
        private void TogglePauseMenu()
        {
            var menu = UnityEngine.Object.FindObjectOfType<PauseMenu>(true);
            if (menu == null)
            {
                ScreenReader.Say(Loc.Get("pause_unavailable"), interrupt: true);
                return;
            }

            DebugLogger.LogInput("Main", "Toggle pause menu");
            menu.Toggle();
            if (!menu.gameObject.activeSelf)
                ScreenReader.Say(Loc.Get("pause_closed"), interrupt: true);
        }
    }

    /// <summary>
    /// Runs right after the game's version-compatibility scripts, which wipe
    /// the save file (including the enabled-mods list) on fresh saves. Applied
    /// automatically by base.Load()'s PatchAll, so it only exists while the
    /// mod is actually loaded.
    /// </summary>
    [HarmonyPatch(typeof(VersionCompatibility), "GameStart")]
    internal static class VersionCompatibilityGameStartPatch
    {
        private static void Postfix()
        {
            WildfrostAccessibilityMod.EnsureEnabledInSave();
        }
    }

    /// <summary>
    /// MonoBehaviour that forwards Unity Update calls to the mod.
    /// </summary>
    internal class ModUpdateBehaviour : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                WildfrostAccessibilityMod.Instance?.OnUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WildfrostAccessibility] Update error: {ex.Message}");
            }
        }
    }


}
