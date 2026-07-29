using System;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Categorized debug logging. Only active when debug mode is enabled (F12).
    /// Zero overhead when disabled — checks a bool before doing any work.
    /// </summary>
    public static class DebugLogger
    {
        public enum LogCategory
        {
            ScreenReader,
            Input,
            State,
            Handler,
            Game
        }

        private static WildfrostAccessibilityMod _mod;

        public static void Initialize(WildfrostAccessibilityMod mod)
        {
            _mod = mod;
        }

        public static bool IsEnabled => _mod != null && _mod.debugMode;

        /// <summary>
        /// Log a categorized debug message. No-op if debug mode is off.
        /// </summary>
        public static void Log(LogCategory category, string message)
        {
            if (!IsEnabled) return;
            _mod.WriteLine($"[{category}] {message}");
        }

        /// <summary>
        /// Log a message with the source handler name.
        /// </summary>
        public static void Log(LogCategory category, string source, string message)
        {
            if (!IsEnabled) return;
            _mod.WriteLine($"[{category}] [{source}] {message}");
        }

        /// <summary>Log an input event.</summary>
        public static void LogInput(string source, string input)
        {
            Log(LogCategory.Input, source, $"Input: {input}");
        }

        /// <summary>Log a state change.</summary>
        public static void LogState(string source, string from, string to)
        {
            Log(LogCategory.State, source, $"State: {from} -> {to}");
        }

        /// <summary>Log a game value read.</summary>
        public static void LogGameValue(string source, string name, object value)
        {
            Log(LogCategory.Game, source, $"{name} = {value}");
        }
    }
}
