using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace TorannMagic.Ideology
{
    public class TM_RitualObligationTrigger_SeverMagic : RitualObligationTrigger_EveryMember
    {
        private static List<Pawn> existingObligations = new List<Pawn>();
        private int nextObligationCheck = 0;

        public override void Tick()
        {
            if(Find.TickManager.TicksGame >= nextObligationCheck)
            {
                bool flag = false;
                foreach (Pawn p in PawnsFinder.AllMaps_SpawnedPawnsInFaction(Faction.OfPlayer))
                {
                    if(p.Ideo?.GetRole(p)?.def == TorannMagicDefOf.TM_IdeoRole_VoidSeeker)
                    {
                        if(p.Ideo.HasPrecept(TorannMagicDefOf.TM_Mages_Abhorrent))
                        {
                            //List<Precept> pList = Traverse.Create(root: p.Ideo).Field(name: "precepts").GetValue<List<Precept>>();
                            //foreach(Precept prec in pList)
                            //{
                            //    if(prec.def == TorannMagicDefOf.TM_Mages_Abhorrent)
                            //    {
                            //        Log.Message("prec name is " + prec.Label + " prec defname is " + prec.def.defName);

                            //    }
                            //}
                            //Log.Message("true");
                            flag = true;
                            break;
                        }
                    }
                }

                if (flag)
                {
                    Recache();
                }
                nextObligationCheck = Find.TickManager.TicksGame + Rand.Range(1200, 2400);
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
                        if (TM_Calc.IsMagicUser(allMapsCaravansAndTravelingTransportPods_Alive_Colonist))
                        {
                            ritual.AddObligation(new RitualObligation(ritual, allMapsCaravansAndTravelingTransportPods_Alive_Colonist, expires: false));
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
