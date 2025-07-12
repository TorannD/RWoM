using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffComp_Taunt : HediffComp
    {
        public Pawn tauntTarget = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.tauntTarget, "tauntTarget");
        }
    }
}