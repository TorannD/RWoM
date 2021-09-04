using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

/// <summary>
/// UNUSED
/// TRIGGER BY DATE
/// </summary>
namespace TorannMagic.Ideology
{
    public class TM_RitualObligationTrigger_BestowMagicClass : RitualObligationTrigger_EveryMember
    {
        private static List<Pawn> existingObligations = new List<Pawn>();
        private int nextObligationCheck = 0;

        public override void Tick()
        {
            if(Find.TickManager.TicksGame >= nextObligationCheck)
            {
                Recache();
                nextObligationCheck = Find.TickManager.TicksGame + Rand.Range(300, 1200);
            }
        }

        protected override void Recache()
        {
            try
            {
                if (ritual.activeObligations != null)
                {
                    ritual.activeObligations.RemoveAll(delegate (RitualObligation o)
                    {
                        Pawn pawn = o.targetA.Thing as Pawn;
                        if (pawn != null)
                        {
                            return pawn.Ideo != ritual.ideo;
                        }
                        return false;
                    });
                    foreach (RitualObligation activeObligation in ritual.activeObligations)
                    {
                        existingObligations.Add(activeObligation.targetA.Thing as Pawn);
                    }
                }
                foreach (Pawn allMapsCaravansAndTravelingTransportPods_Alive_Colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
                {
                    if (!existingObligations.Contains(allMapsCaravansAndTravelingTransportPods_Alive_Colonist) && allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo != null && allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo == ritual.ideo)
                    {                        
                        if (allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Inspired && allMapsCaravansAndTravelingTransportPods_Alive_Colonist.InspirationDef == TorannMagicDefOf.ID_ArcanePathways)
                        {
                            ritual.AddObligation(new RitualObligation(ritual, allMapsCaravansAndTravelingTransportPods_Alive_Colonist, expires: true));
                        }
                    }
                }
            }
            finally
            {
                existingObligations.Clear();
            }
        }
    }
}
