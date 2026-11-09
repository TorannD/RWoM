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

        private int bloodVer = 0;  //increased blood per bleed rate and blood gift use
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
            var body = Pawn;
            var comp = body.GetCompAbilityUserMagic();
            if (ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body, out var soul))
            {
                var borrowed = ClassProgression.SpiritBloodSacrifice.Find(body, soul);
                if (borrowed != null) comp = borrowed.MagicUser;
            }
            if (!body.Spawned || comp == null || (comp.Pawn == body && !comp.IsMagicUser))
            {
                removeNow = true;
                return;
            }
            removeNow = false;
            bloodVer = comp.Pawn == body ? TM_Calc.GetSkillVersatilityLevel(body, TorannMagicDefOf.TM_BloodGift, false)
                : ClassProgression.SpiritRebirthRuntime.SavedRank(comp.ExistingMagicData, "TM_BloodGift_ver");
            arcaneDmg = comp.arcaneDmg;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            bool flag = base.Pawn != null && base.Pawn.Map != null;
            if (flag)
            {
                if (!initialized || Find.TickManager.TicksGame % eventFrequency == 0)
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
