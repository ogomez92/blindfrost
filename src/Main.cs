using System;
using System.Collections.Generic;
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
