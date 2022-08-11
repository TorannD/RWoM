using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_SpiritDistortion : HediffComp
    {

        private bool initialized = false;
        private bool shouldRemove = false;

        public void Initialize()
        {
            if (this.Pawn.health != null && this.Pawn.health.hediffSet != null)
            {
                if (!this.Pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ArcaneWeakness))
                {
                    HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_ArcaneWeakness, .1f);
                }
            }
            else
            {
                shouldRemove = true;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if(!initialized)
                {
                    initialized = true;
                    Initialize();
                }
                if (Find.TickManager.TicksGame % 240 == 0)  //arcane weakness drops by -12 sev per day => .0002 per tick => .048 per 240 ticks
                {
                    Hediff hd = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ArcaneWeakness);
                    if(hd == null)
                    {
                        HealthUtility.AdjustSeverity(this.Pawn, TorannMagicDefOf.TM_ArcaneWeakness, .1f);
                        this.parent.Severity = .1f;
                    }
                    else
                    {
                        if (hd.Severity < 19.95f)   
                        {
                            hd.Severity += .056f;    //250 events per day, 10 days to reach 20 severity
                        }
                        else if(hd.Severity < 20.5f && Rand.Chance(.001f))
                        {
                            hd.Severity += Rand.Range(4f, 8f);
                        }
                        this.parent.Severity = hd.Severity;
                    }
                    
                }
            }            
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;

    }
}
