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
                IEnumerable<Hediff> hds = this.Pawn.health.hediffSet.GetHediffs<Hediff>();
                foreach (Hediff current in hds)
                {
                    if (current.def == HediffDefOf.WoundInfection)
                    {
                        current.Severity -= .001f;
                    }
                }
            }

            using (IEnumerator<BodyPartRecord> enumerator = pawn.health.hediffSet.GetInjuredParts().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    BodyPartRecord rec = enumerator.Current;
                    IEnumerable<Hediff_Injury> arg_BB_0 = pawn.health.hediffSet.GetHediffs<Hediff_Injury>();
                    Func<Hediff_Injury, bool> arg_BB_1;
                    arg_BB_1 = ((Hediff_Injury injury) => injury.Part == rec);
                    foreach (Hediff_Injury current in arg_BB_0.Where(arg_BB_1))
                    {
                        bool flag5 = current.CanHealNaturally() && !current.IsPermanent();
                        if (flag5)
                        {
                            current.Heal(Rand.Range(.1f, .25f));
                        }                        
                    }                    
                }
            }
        }
    }
}
