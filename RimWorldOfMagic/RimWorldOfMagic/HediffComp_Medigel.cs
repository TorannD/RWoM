using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_Medigel : HediffComp
    {

        private bool initializing = true;

        public string labelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                return base.Def.label;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (Find.TickManager.TicksGame % 300 == 0)  //occurs 200x per day; hediff lasts 1/4 day - applies 50 times per medigel
                {
                    TickAction();                    
                }
            }            
        }

        public void TickAction()
        {
            Pawn pawn = this.Pawn;

            if (this.Pawn.health.hediffSet.HasHediff(HediffDefOf.WoundInfection))
            {
                foreach (Hediff hediff in Pawn.health.hediffSet.hediffs)
                {
                    if (hediff.def == HediffDefOf.WoundInfection)
                    {
                        hediff.Severity -= .001f;
                    }
                }
            }

            IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.CanHealNaturally());
            foreach (Hediff_Injury injury in injuries)
            {
                injury.Heal(Rand.Range(.1f, .25f));
            }
        }
    }
}
