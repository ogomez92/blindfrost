using System;
using System.Collections.Generic;
using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Review buffers: steppable lists of extra information, separate from
    /// focus navigation. Ctrl+Up reads the next item in the current buffer,
    /// Ctrl+Down moves back toward the start, Ctrl+Left/Right switch between
    /// the buffers that currently have content. Mirrors the Monster Train
    /// accessibility mods' buffer semantics so players of both games share
    /// muscle memory. While Ctrl is held the arrows never move the game's
    /// real selection (see NavigationHelper.GetNavigationInput).
    /// </summary>
    public static class ReviewBuffers
    {
        private class Buffer
        {
            public string NameKey;
            public Func<List<string>> Build;
            public List<string> Items = new List<string>();
            /// <summary>-1 = start point: the first Ctrl+Up reads item 0.</summary>
            public int Position = -1;
            public string Signature = "";
            /// <summary>
            /// Events only: new items arrive at the front, so the position is
            /// shifted to stay on the item being read instead of resetting.
            /// </summary>
            public bool AnchorPosition;
        }

        private const int MaxEvents = 200;

        // Newest first: Ctrl+Up walks back through history, matching the
        // Monster Train buffers ("the first Ctrl+Up reads the newest event")
        private static readonly List<string> _events = new List<string>();

        private static readonly List<Buffer> _buffers = new List<Buffer>
        {
            new Buffer
            {
                NameKey = "buffer_events",
                Build = () => new List<string>(_events),
                AnchorPosition = true,
            },
            new Buffer { NameKey = "buffer_details", Build = BuildDetails },
            new Buffer
            {
                NameKey = "buffer_hand",
                Build = () => (ScreenManager.ActiveHandler as BattleHandler)?.BuildHandItems(),
            },
            new Buffer
            {
                NameKey = "buffer_board",
                Build = () => (ScreenManager.ActiveHandler as BattleHandler)?.BuildBoardItems(),
            },
            new Buffer
            {
                NameKey = "buffer_resources",
                Build = () => (ScreenManager.ActiveHandler as BattleHandler)?.BuildResourceItems(),
            },
            new Buffer
            {
                NameKey = "buffer_waves",
                Build = () => (ScreenManager.ActiveHandler as BattleHandler)?.BuildWaveItems(),
            },
            new Buffer
            {
                NameKey = "buffer_map",
                Build = () => (ScreenManager.ActiveHandler as MapHandler)?.BuildLocationItems(),
            },
        };

        private static int _current;

        /// <summary>Index of the Details buffer, or -1 if it is ever removed.</summary>
        private static readonly int _detailsIndex =
            _buffers.FindIndex(b => b.NameKey == "buffer_details");

        /// <summary>
        /// The game's focused navigation item as of the last frame. When focus
        /// moves, every buffer resets to its start so the next Ctrl+Up reads
        /// from the top again (see DetectFocusChange).
        /// </summary>
        private static UINavigationItem _lastFocused;

        /// <summary>
        /// Record an unsolicited announcement into the Events buffer.
        /// Called by ScreenReader.SayEvent for battle narration, story text,
        /// popups, and screen announcements — never for focus echoes.
        /// </summary>
        public static void RecordEvent(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _events.Insert(0, text);
            if (_events.Count > MaxEvents)
                _events.RemoveAt(_events.Count - 1);

            // Keep a review-in-progress anchored on the item being read
            var events = _buffers[0];
            if (events.Position >= 0)
                events.Position = Math.Min(events.Position + 1, MaxEvents - 1);
        }

        /// <summary>Handle Ctrl+arrow input. Called every frame from Main.OnUpdate.</summary>
        public static void Update()
        {
            DetectFocusChange();

            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                return;
            if (NavigationHelper.IsTextInputFocused())
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                DebugLogger.LogInput("ReviewBuffers", "Step next");
                Step(forward: true);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                DebugLogger.LogInput("ReviewBuffers", "Step back");
                Step(forward: false);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                DebugLogger.LogInput("ReviewBuffers", "Next buffer");
                Switch(1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                DebugLogger.LogInput("ReviewBuffers", "Previous buffer");
                Switch(-1);
            }
        }

        /// <summary>
        /// Refresh a buffer's content. Position resets when the content
        /// changed (MT semantics), except for the anchored Events buffer.
        /// Returns true when the buffer has content.
        /// </summary>
        private static bool Refresh(Buffer buffer)
        {
            List<string> items;
            try
            {
                items = buffer.Build() ?? new List<string>();
            }
            catch (Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Handler, "ReviewBuffers",
                    $"{buffer.NameKey} build failed: {ex.Message}");
                items = new List<string>();
            }
            items.RemoveAll(string.IsNullOrEmpty);

            string signature = string.Join("\n", items);
            if (signature != buffer.Signature)
            {
                buffer.Signature = signature;
                if (!buffer.AnchorPosition)
                    buffer.Position = -1;
            }
            if (buffer.Position >= items.Count)
                buffer.Position = items.Count - 1;

            buffer.Items = items;
            return items.Count > 0;
        }

        /// <summary>
        /// The current buffer if it has content, else the next available one
        /// (announced via the switched flag). Null when nothing has content.
        /// </summary>
        private static Buffer CurrentAvailable(out bool switched)
        {
            switched = false;
            if (Refresh(_buffers[_current]))
                return _buffers[_current];

            for (int i = 1; i < _buffers.Count; i++)
            {
                int idx = (_current + i) % _buffers.Count;
                if (Refresh(_buffers[idx]))
                {
                    _current = idx;
                    _buffers[idx].Position = -1;
                    switched = true;
                    return _buffers[idx];
                }
            }
            return null;
        }

        private static void Step(bool forward)
        {
            var buffer = CurrentAvailable(out bool switched);
            if (buffer == null)
            {
                Speak(Loc.Get("buffer_none"));
                return;
            }

            // Clamp at both ends and re-read the boundary item. No "start/end
            // of list" cue — hearing the same item again is signal enough.
            buffer.Position = forward
                ? Math.Min(buffer.Position + 1, buffer.Items.Count - 1)
                : Math.Max(buffer.Position - 1, 0);

            string text = buffer.Items[buffer.Position];
            if (switched)
                text = Loc.Get(buffer.NameKey) + ". " + text;
            Speak(text);
        }

        private static void Switch(int direction)
        {
            int count = _buffers.Count;
            for (int i = 1; i <= count; i++)
            {
                int idx = ((_current + direction * i) % count + count) % count;
                if (!Refresh(_buffers[idx]))
                    continue;

                _current = idx;
                _buffers[idx].Position = -1;
                AnnounceBuffer(_buffers[idx]);
                return;
            }
            Speak(Loc.Get("buffer_none"));
        }

        private static void AnnounceBuffer(Buffer buffer)
        {
            string name = Loc.Get(buffer.NameKey);
            Speak(buffer.Items.Count == 1
                ? Loc.Get("buffer_switched_one", name)
                : Loc.Get("buffer_switched", name, buffer.Items.Count));
        }

        /// <summary>
        /// Reviewing is deliberate — cut whatever is being spoken so the
        /// buffer item is heard immediately (Say alone never interrupts).
        /// </summary>
        private static void Speak(string text)
        {
            ScreenReader.Silence();
            ScreenReader.Say(text, interrupt: true);
        }

        /// <summary>
        /// When the game's focus moves to a different item, rewind every buffer
        /// to its start so the next Ctrl+Up reads from the top. Runs each frame,
        /// before the Ctrl gate — but focus can't move while Ctrl is held (the
        /// arrows are captured for review), so this only fires between reviews.
        /// </summary>
        private static void DetectFocusChange()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem != null ? navSystem.currentNavigationItem : null;
            if (item == null || ReferenceEquals(item, _lastFocused))
                return;
            _lastFocused = item;
            foreach (var buffer in _buffers)
                buffer.Position = -1;

            // Focusing something with its own details (a card, a map node, a
            // building) makes that the active buffer, so the first Ctrl+Up reads
            // the focused thing, not the event history.
            if (_detailsIndex >= 0 && FocusedItemHasDetails())
                _current = _detailsIndex;
        }

        /// <summary>True when the Details buffer would have something to read for
        /// the currently focused item.</summary>
        private static bool FocusedItemHasDetails()
        {
            try
            {
                var parts = BuildDetails();
                if (parts != null)
                    foreach (var p in parts)
                        if (!string.IsNullOrEmpty(p))
                            return true;
            }
            catch { /* no owner / item mid-transition */ }
            return false;
        }

        /// <summary>Detail parts for whatever navigation item is focused right now.</summary>
        private static List<string> BuildDetails()
        {
            var navSystem = MonoBehaviourSingleton<UINavigationSystem>.instance;
            var item = navSystem != null ? navSystem.currentNavigationItem : null;
            var owner = ScreenManager.ActiveHandler;
            if (item == null || owner == null)
                return null;
            return ItemDescriber.DescribeDetailParts(item, owner);
        }
    }
}
