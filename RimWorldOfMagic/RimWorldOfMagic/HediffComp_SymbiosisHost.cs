using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffComp_SymbiosisHost : HediffComp
    {
        private bool initializing = true;
        private bool shouldRemove = false;
        public Pawn symbiote = null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Pawn>(ref this.symbiote, "symbiote");
        }

        public override string CompLabelInBracketsExtra => symbiote != null ? symbiote.LabelShort + "[+]" + base.CompLabelInBracketsExtra : base.CompLabelInBracketsExtra;

        public string labelCap
        {
            get
            {
                if (symbiote != null)
                {
                    return base.Def.LabelCap + "(" + symbiote.LabelShort + ")";
                }
                return base.Def.LabelCap;
            }
        }

        public string label
        {
            get
            {
                if (symbiote != null)
                {
                    return base.Def.label + "(" + symbiote.LabelShort + ")";
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
                if (Find.TickManager.TicksGame % 201 == 0)
                {
                    if (symbiote.DestroyedOrNull())
                    {
                        this.shouldRemove = true;
                    }
                    else if (symbiote.Dead || !symbiote.Downed)
                    {
                        this.shouldRemove = true;
                    }                    
                    if(this.symbiote.health != null && this.symbiote.health.hediffSet != null)
                    {
                        if(this.symbiote.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD) == null)
                        {
                            this.shouldRemove = true;
                        }
                    }
                    else
                    {
                        this.shouldRemove = true;
                    }
                }
                if (this.Pawn.Dead && this.symbiote != null && !this.symbiote.Dead)
                {
                    this.shouldRemove = true;
                    DamageInfo dinfo = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_SymbiosisDD, 1f);
                    this.symbiote.Kill(dinfo, null);
                    RemoveSymbioteHediff();                    
                }
            }
        }

        public void RemoveSymbioteHediff()
        {
            if (this.symbiote != null && this.symbiote.health != null && this.symbiote.health.hediffSet != null)
            {
                Hediff hd = this.symbiote.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD);
                if (hd != null)
                {
                    this.symbiote.health.RemoveHediff(hd);
                }
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;

    }
}
