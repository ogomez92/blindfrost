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
        /// Adds this mod to the game's enabled-mods list on the first run after
        /// installation, so blind players never have to find the Mods menu
        /// (which is inaccessible without the mod — chicken-and-egg).
        ///
        /// The game constructs every mod it finds in StreamingAssets/Mods on
        /// boot, but only activates the ones stored under "lastSavedMods" in
        /// the save file (see Bootstrap.ModsSetup and WildfrostMod.GetLastMods).
        /// This constructor runs before that check, so registering our GUID here
        /// makes the game activate us through its normal path on the same boot.
        ///
        /// A marker file remembers that self-enable already happened, so a
        /// player who later disables the mod in the Mods menu stays disabled.
        /// </summary>
        private void SelfEnableOnFirstRun(string modDirectory)
        {
            try
            {
                string marker = Path.Combine(modDirectory, "autoenable.marker");
                if (File.Exists(marker))
                    return;

                string[] enabled = SaveSystem.LoadProgressData<string[]>("lastSavedMods") ?? new string[0];
                if (!enabled.Contains(GUID))
                {
                    SaveSystem.SaveProgressData("lastSavedMods", enabled.Append(GUID).ToArray());
                    Debug.Log("[WildfrostAccessibility] First run: mod self-enabled, will load this boot");
                }
                File.WriteAllText(marker, "This file marks that the mod has auto-enabled itself once. Delete it to make the mod auto-enable again on the next game start.");
            }
            catch (Exception ex)
            {
                // Never let self-enable break mod construction — the game's
                // ModsSetup aborts ALL mods if a constructor throws.
                Debug.LogError($"[WildfrostAccessibility] Self-enable failed: {ex.Message}");
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
            ScreenManager.Initialize();
            CreateUpdateHook();

            Debug.Log("[WildfrostAccessibility] All systems initialized, attempting speech");
            ScreenReader.Say(Loc.Get("mod_loaded"), interrupt: true);
            WriteLine("Wildfrost Accessibility mod loaded.");
        }

        protected override void Unload()
        {
            ScreenReader.Say(Loc.Get("mod_unloaded"), interrupt: true);

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
        }

        private void AnnounceHelp()
        {
            string help = ScreenManager.ActiveHandler?.GetHelpText() ?? Loc.Get("help_text");
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
