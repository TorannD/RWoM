using RimWorld;
using Verse;

namespace TorannMagic
{
    public class HediffComp_SymbiosisHost : HediffComp
    {
        private bool initializing = true;
        private bool shouldRemove = false;
        public Pawn symbiote = null;
        public int lastDamageTick = 0;

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

        internal Pawn SkillOwner
        {
            get
            {
                var source = symbiote?.health?.hediffSet?.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD)?.TryGetComp<HediffComp_SymbiosisCaster>();
                return source?.symbiosisHost == Pawn ? source.SkillOwner : null;
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo,culprit);
            if (Pawn.Dead && symbiote?.Dead == false && SkillOwner != null)
                symbiote.Kill(new DamageInfo(TMDamageDefOf.DamageDefOf.TM_SymbiosisDD, 1f),null);
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
                    var owner = SkillOwner;
                    if (symbiote.DestroyedOrNull() || symbiote.Dead || !symbiote.Downed || owner == null)
                    {
                        shouldRemove = true;
                        return;
                    }
                    float effVal = TM_Calc.GetSkillPowerLevel(owner, TorannMagicDefOf.TM_Symbiosis);
                    if(base.Pawn.needs != null)
                    {
                        if (base.Pawn.needs.mood != null)
                        {
                            base.Pawn.needs.mood.CurLevel += (.001f * effVal);
                        }
                        if(base.Pawn.needs.rest != null)
                        {
                            base.Pawn.needs.rest.CurLevel += (.001f * effVal);
                        }
                        if(base.Pawn.needs.food != null)
                        {
                            base.Pawn.needs.food.CurLevel += (.001f * effVal);
                        }
                        if(base.Pawn.needs.joy != null)
                        {
                            base.Pawn.needs.joy.CurLevel += (.001f * effVal);
                        }
                    }
                }

            }
        }

        public void RemoveSymbioteHediff()
        {
            if (this.symbiote != null && this.symbiote.health != null && this.symbiote.health.hediffSet != null)
            {
                Hediff hd = this.symbiote.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD);
                if (hd?.TryGetComp<HediffComp_SymbiosisCaster>()?.symbiosisHost == Pawn)
                {
                    this.symbiote.health.RemoveHediff(hd);
                }
            }
        }

        public override bool CompShouldRemove => base.CompShouldRemove || this.shouldRemove;

    }
}
