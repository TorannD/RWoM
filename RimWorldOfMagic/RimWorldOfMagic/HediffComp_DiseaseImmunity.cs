using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class HediffComp_DiseaseImmunity : HediffComp
    {
        private bool initialized = false;
        public int verVal = 0;

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

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null;
            if (flag)
            {

                if(!base.Pawn.Dead && base.Pawn.Spawned)
                {
                    if (Find.TickManager.TicksGame % 2500 == 0)
                    {
                        if(this.verVal >= 3)
                        {
                            IEnumerable<Hediff> hdEnum = this.Pawn.health.hediffSet.GetHediffs<Hediff>();
                            foreach (Hediff hd in hdEnum)
                            {
                                if (hd.def.defName == "BloodRot")
                                {
                                    int pwrDef = 2;
                                    if (this.parent.def == TorannMagicDefOf.TM_DiseaseImmunity2HD)
                                    {
                                        pwrDef = 3;
                                    }
                                    hd.Severity -= (.005f * pwrDef);
                                    break;
                                }
                            }
                        }
                    }
                }                
            }
        }
    }
}
