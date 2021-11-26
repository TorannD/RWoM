using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_HerbalElixir : HediffComp
    {

        public float verVal = 0;
        public float pwrVal = 0;

        private bool initializing = true;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<float>(ref verVal, "verVal", 0f);
            Scribe_Values.Look<float>(ref pwrVal, "pwrVal", 0f);
        }

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
                if (Find.TickManager.TicksGame % 300 == 0)  //occurs 200x per day; hediff lasts .7 to 1 day - applies 140 to 200 times per elixir; 
                {
                    TickAction();                    
                }
            }            
        }

        public void TickAction()
        {
            Pawn pawn = this.Pawn;


            IEnumerable<Hediff> hds = this.Pawn.health.hediffSet.GetHediffs<Hediff>();
            foreach (Hediff current in hds)
            {
                if (verVal >= 1)
                {
                    if (current.def == HediffDefOf.WoundInfection)
                    {
                        current.Severity -= (.001f * this.parent.Severity * (1f + (.1f * pwrVal)));
                    }
                }
                if (verVal >= 2)
                {
                    HediffComp_Immunizable hc_i = current.TryGetComp<HediffComp_Immunizable>();
                    if (hc_i != null)
                    {
                        current.Severity -= (hc_i.Props.severityPerDayNotImmune / 100f) * this.parent.Severity;
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
                            float healBleedingBonus = (current.Bleeding ? .2f : 0f);                                
                            current.Heal((Rand.Range(.2f, .3f) + healBleedingBonus) * this.parent.Severity * (1f + (.1f * pwrVal))); //140-200 applications = 14 - 39 healing for every wound                            
                        }        
                        else if(verVal >= 3)
                        {
                            current.Heal(Rand.Range(.008f, .012f) * this.parent.Severity * (1f + (.1f * pwrVal))); //.56 - 1.56 permanent injury healing for every wound
                        }
                    }                    
                }
            }
        }
    }
}
