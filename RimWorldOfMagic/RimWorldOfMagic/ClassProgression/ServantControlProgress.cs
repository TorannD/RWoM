using System;
using Verse;

namespace TorannMagic.ClassProgression
{
    // Tracks observed control, never elapsed wall time or rewards.
    public sealed class ServantControlProgress : IExposable
    {
        public const int RequiredTicks = 60000;
        private string subjectId;
        private int controlledTicks;
        private int lastTick = -1;
        private bool lastActive;

        public string SubjectId => subjectId;
        public int ControlledTicks => controlledTicks;

        public void Observe(string subjectId, bool active, int now)
        {
            if (string.IsNullOrEmpty(subjectId))
            {
                this.subjectId = null;
                controlledTicks = 0;
                lastTick = -1;
                lastActive = false;
                return;
            }
            if (!string.Equals(this.subjectId, subjectId, StringComparison.Ordinal))
            {
                this.subjectId = subjectId;
                controlledTicks = 0;
                lastTick = now;
                lastActive = active;
                return;
            }
            // Duplicate/backward observations never rewind the clock or revive
            // continuity. A reported pause still breaks an active interval.
            if (now <= lastTick)
            {
                if (!active) lastActive = false;
                return;
            }
            long delta = (long)now - lastTick;
            if (lastTick >= 0 && lastActive && active && delta <= 60)
                controlledTicks = Math.Min(RequiredTicks, controlledTicks + (int)delta);
            lastTick = now;
            lastActive = active;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref subjectId, "subjectId");
            Scribe_Values.Look(ref controlledTicks, "controlledTicks", 0);
            Scribe_Values.Look(ref lastTick, "lastTick", -1);
            Scribe_Values.Look(ref lastActive, "lastActive", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                controlledTicks = Math.Max(0, Math.Min(RequiredTicks, controlledTicks));
                if (string.IsNullOrEmpty(subjectId)) Observe(null, false, 0);
                if (lastTick < 0) lastActive = false;
            }
        }
    }
}
