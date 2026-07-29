using UnityEngine;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Handler for the JournalNameHistory overlay: a short, fully automatic
    /// vignette (about two seconds, no input) where the journal either writes
    /// the new leader's name into the history of past leaders, or crosses out
    /// the most recent name after a defeat. Purely visual in vanilla — this
    /// just says which of the two is happening.
    /// </summary>
    public class JournalNameHandler : NavigableScreenHandler
    {
        public override string Name => "JournalNameHistory";

        protected override float AnnounceDelay => 0.1f;

        protected override bool TryAnnounceScreen()
        {
            if (Object.FindObjectOfType<JournalVoidNameSequence>() != null)
            {
                ScreenReader.SayEvent(Loc.Get("journal_name_void"), interrupt: false);
                return true;
            }

            if (Object.FindObjectOfType<JournalAddNameSequence>() != null)
            {
                string leader = null;
                try { leader = References.LeaderData?.title; }
                catch { /* no run data loaded — announce without the name */ }
                ScreenReader.SayEvent(
                    string.IsNullOrEmpty(leader)
                        ? Loc.Get("journal_name_add")
                        : Loc.Get("journal_name_add_named", leader),
                    interrupt: false);
                return true;
            }

            // Neither sequence found yet — retry next frame (scene still loading).
            // Past the cap, at least name the screen: returning true silently
            // would also disarm the base fallback and leave the vignette mute.
            if (Time.unscaledTime - EnterTime > 2f)
            {
                ScreenReader.SayEvent(Loc.Get("scene_JournalNameHistory"), interrupt: false);
                return true;
            }
            return false;
        }
    }
}
