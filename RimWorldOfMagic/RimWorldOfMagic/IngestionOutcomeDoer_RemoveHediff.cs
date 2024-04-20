using Verse;
using RimWorld;

namespace TorannMagic
{
    public class IngestionOutcomeDoer_RemoveHediff : IngestionOutcomeDoer
    {
        public HediffDef hediffDef;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef, false);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}
