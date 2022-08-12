using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class HediffComp_Chi : HediffComp
    {

        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 120;
        private int chiFrequency = 4;
        private int lastChiTick = 0;
        private float lastChi = 0;

        public float maxSev = 100f;

        private int pwrVal = 0;
        private int verVal = 0;
        private int effVal = 0;

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
            CompAbilityUserMight comp = this.Pawn.GetCompAbilityUserMight();

            if (comp != null && comp.IsMightUser)
            {
                pwrVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_pwr").level;
                verVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_ver").level;
                effVal = comp.MightData.MightPowerSkill_Chi.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Chi_eff").level;
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
            }
            if(initialized && Find.TickManager.TicksGame % this.eventFrequency == 0)
            {
                if (this.Pawn.IsColonist)
                {
                    severityAdjustment -= (Rand.Range(.03f, .05f) - (.008f * verVal));
                }
                else if(this.Pawn.IsPrisoner)
                {
                    severityAdjustment -= (Rand.Range(.25f, .5f) - (.00375f * verVal));
                }
                else
                {
                    severityAdjustment += 2f;
                }
            }
            this.parent.Severity = Mathf.Clamp(this.parent.Severity, 0, this.maxSev);
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove || this.removeNow;
            }
        }        
    }
}
