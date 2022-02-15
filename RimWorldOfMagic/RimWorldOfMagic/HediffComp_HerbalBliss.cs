using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_HerbalBliss : HediffComp
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
                if (Find.TickManager.TicksGame % 300 == 0)  //occurs 200x per day; hediff lasts .7 to 1 day - applies 140 to 200 times per elixir; 
                {
                    TickAction();                    
                }
            }            
        }

        public void TickAction()
        {
            SkillDef ski = this.Pawn.skills.skills.RandomElement().def;
            this.Pawn.skills.Learn(ski, Rand.Range(-5, -15), true);
        }
    }
}
