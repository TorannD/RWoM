using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_Blood : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 60;

        private int bloodPwr = 0;  //increased amount blood levels affect ability power
        private int bloodVer = 0;  //increased blood per bleed rate and blood gift use
        private int bloodEff = 0;  //reduces ability blood costs
        private float arcaneDmg = 1f;

        public override void CompExposeData()
        {
            base.CompExposeData();
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

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
            if (spawned && comp != null && comp.IsMagicUser)
            {
                //bloodPwr = comp.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_pwr").level;
                //bloodVer = comp.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_ver").level;
                //bloodEff = comp.MagicData.MagicPowerSkill_BloodGift.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_BloodGift_eff").level;
                bloodPwr = TM_Calc.GetSkillPowerLevel(Pawn, TorannMagicDefOf.TM_BloodGift, false);
                bloodVer = TM_Calc.GetSkillVersatilityLevel(Pawn, TorannMagicDefOf.TM_BloodGift, false);
                bloodEff = TM_Calc.GetSkillEfficiencyLevel(Pawn, TorannMagicDefOf.TM_BloodGift, false);
                this.arcaneDmg = comp.arcaneDmg;
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
                    if(this.Pawn.health.hediffSet.BleedRateTotal != 0)
                    {
                        //.06 bleed rate per 1 dmg "cut"
                        //.1 bleed rate per 1 dmg sacrificial cut
                        //Log.Message("current bleed rate is " + this.Pawn.health.hediffSet.BleedRateTotal);
                        severityAdjustment += (this.Pawn.health.hediffSet.BleedRateTotal * (1.25f + (.125f *this.bloodVer))) * this.arcaneDmg;
                    }
                    else if(!this.Pawn.IsColonist)
                    {
                        severityAdjustment += 5;
                    }
                    else
                    {
                        severityAdjustment -= Rand.Range(.04f, .1f);
                    }

                    Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_BloodBoostHD);
                    float maxSev = 100;
                    if(hediff != null)
                    {
                        maxSev += hediff.Severity;
                    }
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
