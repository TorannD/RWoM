using Verse;
using RimWorld;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorannMagic.Enchantment
{
    public class HediffComp_ShroudOfUndeath : HediffComp_EnchantedItem
    {

        public override void CompExposeData()
        {            
            base.CompExposeData();
        }

        public override void PostInitialize()
        {
            this.hediffActionRate = 150;
        }

        public override void HediffActionTick()
        {
            if (!this.Pawn.DeadOrDowned && this.Pawn.Spawned && this.Pawn.Map != null)
            {
                IEnumerable<Pawn> pList = from pawns in this.Pawn.Map.mapPawns.AllPawnsSpawned
                                          where (pawns.IsColonistPlayerControlled && TM_Calc.IsUndead(pawns) && pawns.Position.DistanceTo(this.Pawn.Position) <= (this.parent.Severity * 40))
                                          select pawns;
                if (pList != null && pList.Count() > 0)
                {
                    foreach (Pawn p in pList)
                    {
                        Hediff hd = null;
                        HediffComp_SetDuration hdc_sd = null;
                        if (p.health.hediffSet.HasHediff(TorannMagicDefOf.TM_UndeadShroudHD))
                        {
                            hd = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_UndeadShroudHD);
                            hdc_sd = hd.TryGetComp<HediffComp_SetDuration>();
                        }
                        if (hd != null && hdc_sd != null)
                        {
                            if (hd.Severity < this.parent.Severity)
                            {
                                hd.Severity = this.parent.Severity;
                            }
                            hdc_sd.duration += 3;
                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_UndeadShroudHD, this.parent.Severity / 2f);
                        }
                    }
                }
            }
        }
    }
}
