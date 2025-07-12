using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_Nightshade : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 240;

        private int pwrVal = 0;  //increased amount blood levels affect ability power
        private int verVal = 0;  //increased blood per bleed rate and blood gift use

        public float GetApplicationSeverity
        {
            get
            {
                return 1f + (.2f * pwrVal);
            }
        }

        public int GetDoseCount
        {
            get
            {
                return (int)(this.parent.Severity / GetApplicationSeverity);
            }
        }

        public override string CompLabelInBracketsExtra => "" + GetDoseCount + " doses";

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

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();
            if (spawned && comp != null && comp.IsMightUser)
            {
                pwrVal = comp.MightData.MightPowerSkill_Nightshade.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Nightshade_pwr").level;
                verVal = comp.MightData.MightPowerSkill_Nightshade.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Nightshade_ver").level;
            }
            else
            {
                this.removeNow = true;
            }
        }        

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }

                if (Find.TickManager.TicksGame % this.eventFrequency == 0)
                {
                    Initialize();
                    severityAdjustment += Rand.Range(.2f, .3f);                    

                    float maxSev = 10 + (2*verVal);
                    this.parent.Severity = Mathf.Clamp(this.parent.Severity, 0, maxSev);
                } 
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return this.removeNow || base.CompShouldRemove;
            }
        }        
    }
}
