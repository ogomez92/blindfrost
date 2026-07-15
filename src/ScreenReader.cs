using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Wrapper for Tolk screen reader library.
    /// Provides text-to-speech output via NVDA, JAWS, or other screen readers.
    /// Degrades gracefully if Tolk is unavailable.
    /// </summary>
    public static class ScreenReader
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Load();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Unload();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_IsLoaded();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_Output(
            [MarshalAs(UnmanagedType.LPWStr)] string str,
            bool interrupt);

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_Speak(
            [MarshalAs(UnmanagedType.LPWStr)] string str,
            bool interrupt);

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_Silence();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        private static extern string Tolk_DetectScreenReader();

        [DllImport("Tolk", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Tolk_HasSpeech();

        private static bool _isAvailable;

        /// <summary>Whether Tolk loaded successfully and a screen reader is detected.</summary>
        public static bool IsAvailable => _isAvailable;

        /// <summary>
        /// Initialize Tolk. Call once during mod load.
        /// Adds the Modded directory to DLL search path so Tolk.dll can be found.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Tolk.dll and nvdaControllerClient64.dll must be in Modded/ (NOT in the mod folder,
                // because the game tries to load all DLLs in the mod folder as .NET assemblies).
                // Add the Modded directory to the DLL search path.
                string moddedDir = Path.GetDirectoryName(Application.dataPath);
                Debug.Log($"[WildfrostAccessibility] Setting DLL directory: {moddedDir}");
                SetDllDirectory(moddedDir);

                Debug.Log("[WildfrostAccessibility] Calling Tolk_Load()...");
                Tolk_Load();
                Debug.Log("[WildfrostAccessibility] Tolk_Load() completed");

                // Skip Tolk_DetectScreenReader — its string return marshalling causes native crashes.
                // Instead, just try to speak. If it works, we're good.
                _isAvailable = Tolk_HasSpeech();
                Debug.Log($"[WildfrostAccessibility] Tolk_HasSpeech: {_isAvailable}");

                if (!_isAvailable)
                {
                    Debug.LogWarning("[WildfrostAccessibility] No screen reader detected. Speech output disabled.");
                }
            }
            catch (DllNotFoundException ex)
            {
                _isAvailable = false;
                Debug.LogError($"[WildfrostAccessibility] Tolk.dll not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                _isAvailable = false;
                Debug.LogError($"[WildfrostAccessibility] Tolk initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Shut down Tolk. Call during mod unload.
        /// </summary>
        public static void Shutdown()
        {
            if (_isAvailable)
            {
                try
                {
                    Tolk_Unload();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WildfrostAccessibility] Tolk shutdown error: {ex.Message}");
                }
                _isAvailable = false;
            }
        }

        /// <summary>
        /// Speak text via the screen reader.
        /// </summary>
        /// <param name="text">Text to speak.</param>
        /// <param name="interrupt">Ignored — never interrupts. Speech is always queued.</param>
        public static void Say(string text, bool interrupt = false)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!_isAvailable)
            {
                DebugLogger.Log(DebugLogger.LogCategory.ScreenReader, $"[NO SR] {text}");
                return;
            }

            try
            {
                Tolk_Output(text, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WildfrostAccessibility] Speech error: {ex.Message}");
            }
        }

        /// <summary>
        /// Speak an unsolicited announcement — battle narration, story text,
        /// popups, screen changes — and record it in the Events review buffer
        /// so it can be re-read with Ctrl+Up later. Focus echoes and
        /// on-demand readouts use Say instead: recording those would bury
        /// the events under navigation chatter.
        /// </summary>
        public static void SayEvent(string text, bool interrupt = false)
        {
            ReviewBuffers.RecordEvent(text);
            Say(text, interrupt);
        }

        /// <summary>
        /// Stop all current speech output.
        /// </summary>
        public static void Silence()
        {
            if (!_isAvailable)
                return;

            try
            {
                Tolk_Silence();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WildfrostAccessibility] Silence error: {ex.Message}");
            }
        }
    }
}
