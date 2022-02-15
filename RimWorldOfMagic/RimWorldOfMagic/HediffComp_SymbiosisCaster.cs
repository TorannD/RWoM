using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffComp_SymbiosisCaster : HediffComp
    {
        private bool initializing = true;
        private bool shouldRemove = false;
        public Pawn symbiosisHost = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.symbiosisHost, "symbiosisHost");
        }

        public override string CompLabelInBracketsExtra => symbiosisHost != null ? symbiosisHost.LabelShort + "[-]" + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if (symbiosisHost != null)
                {
                    return base.Def.LabelCap + "(" + symbiosisHost.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (symbiosisHost != null)
                {
                    return base.Def.label + "(" + symbiosisHost.LabelShort + ")";
                }
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            if (spawned && base.Pawn.Map != null)
            {
                FleckMaker.ThrowHeatGlow(base.Pawn.Position, base.Pawn.Map, 2f);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {
                if (initializing)
                {
                    initializing = false;
                    this.Initialize();
                }
                if(Find.TickManager.TicksGame % 200 == 0)
                {
                    if(symbiosisHost.DestroyedOrNull())
                    {
                        this.shouldRemove = true;
                    }
                    else if(symbiosisHost.Dead)
                    {
                        DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_SymbiosisDD, 1f);
                        base.Pawn.Kill(dinfo, null);
                        this.shouldRemove = true;
                    }

                    if (this.Pawn.needs != null && this.Pawn.needs.mood != null)
                    {
                        if (this.Pawn.needs.mood.CurLevel < .01f)
                        {
                            this.shouldRemove = true;
                            RemoveHostHediff();
                        }
                        this.Pawn.needs.mood.CurLevel -= .002f;
                    }
                    else
                    {
                        RemoveHostHediff();
                        this.shouldRemove = true;
                    }
                }                
            }
        }

        public void RemoveHostHediff()
        {
            if(this.symbiosisHost != null && this.symbiosisHost.health != null && this.symbiosisHost.health.hediffSet != null)
            {
                Hediff hd = this.symbiosisHost.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD);
                if(hd != null)
                {
                    this.symbiosisHost.health.RemoveHediff(hd);
                }
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;
    }
}
