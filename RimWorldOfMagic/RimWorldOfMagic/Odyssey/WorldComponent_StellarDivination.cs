using System;
using RimWorld.Planet;
using Verse;
namespace TorannMagic.Odyssey
{
    public sealed class WorldComponent_StellarDivination : WorldComponent
    {
        private long nextStartTick;
        private int searchSerial;
        private bool starting;
        public WorldComponent_StellarDivination(World world) : base(world) { }
        public int SearchSerial => searchSerial;
        public long CooldownRemaining(int now) => Math.Max(0L, nextStartTick - now);
        public bool CanStart(int now) => !starting && now >= 0 && now >= nextStartTick;
        public bool TryStart(int now, Func<bool> payment)
        {
            if (!CanStart(now) || payment == null) return false;
            starting = true;
            try
            {
                if (!payment()) return false;
                nextStartTick = (long)now + 1080000;
                searchSerial++;
                return true;
            }
            finally { starting = false; }
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextStartTick, "TM_stellarNextStart", 0L);
            Scribe_Values.Look(ref searchSerial, "TM_stellarSerial", 0);
        }
    }
    public sealed class StellarDivinationSearch : IExposable
    {
        private int id, remainingTicks, lastTick = -1;
        private long receiptEnd = -1;
        public int Id => id;
        public int RemainingTicks => remainingTicks;
        public void Begin(int searchId, int duration)
        {
            id = searchId; remainingTicks = duration; lastTick = -1; receiptEnd = -1;
        }
        public void Advance(int now, int delta, bool active)
        {
            if (!active || id == 0 || delta <= 0 || now < 0 || now <= lastTick || now < receiptEnd) return;
            lastTick = now; receiptEnd = (long)now + delta;
            remainingTicks = Math.Max(0, remainingTicks - Math.Min(remainingTicks, delta));
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref remainingTicks, "remaining", 0);
            Scribe_Values.Look(ref lastTick, "lastTick", -1);
            Scribe_Values.Look(ref receiptEnd, "receiptEnd", -1L);
        }
    }
}
