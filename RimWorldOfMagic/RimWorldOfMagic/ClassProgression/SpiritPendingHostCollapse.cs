using Verse;

namespace TorannMagic.ClassProgression
{
    // References and authorization only. The keeper/stone are the sole owners.
    internal sealed class SpiritPendingHostCollapse : IExposable
    {
        internal readonly MapComponent_SpiritCollapse Keeper;
        internal int Id, SourceHostId, RetryAt;
        internal Pawn Original;
        internal Thing_SoulStone Stone;
        internal IntVec3 Anchor;
        internal bool WarningIssued, Forced;
        private int version = 1;
        public SpiritPendingHostCollapse(MapComponent_SpiritCollapse keeper) { Keeper = keeper; }
        internal bool Registered => version == 1 && Id > 0 && Keeper.Registered && Keeper.ContainsRecord(this);
        internal bool OwnsOriginal => Registered && Keeper.OwnsOriginal(this);
        public void ExposeData()
        {
            Scribe_Values.Look(ref version, "version", 1);
            Scribe_Values.Look(ref Id, "id"); Scribe_Values.Look(ref SourceHostId, "sourceHostId");
            Scribe_Values.Look(ref Anchor, "anchor"); Scribe_Values.Look(ref RetryAt, "retryAt");
            Scribe_Values.Look(ref Forced, "forcedRemoval");
            Scribe_Values.Look(ref WarningIssued, "warningIssued");
            Scribe_References.Look(ref Original, "original"); Scribe_References.Look(ref Stone, "stone");
        }
    }
}
