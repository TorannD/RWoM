using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Hediff_ApothecaryHerbs : HediffWithCompsExtra
    {
        private bool initialized = false;
        private bool removeNow = false;

        private int eventFrequency = 60;

        private int herbPwr = 0;  //increased maximum hediff value
        private int herbVer = 0;  //increased amount of herbs found during harvesting
        private int herbEff = 0;  //reduces expiration rate

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref herbPwr, "herbPwr", 0);
            Scribe_Values.Look<int>(ref herbVer, "herbVer", 0);
            Scribe_Values.Look<int>(ref herbEff, "herbEff", 0);
        }

        public override string GizmoLabel => "TM_ApothecaryHerbsGizmoLabel".Translate();
        public override float MaxSeverity => def.maxSeverity + (10 * herbPwr);
        public override bool ShouldRemove => this.removeNow;

        private void Initialize()
        {
            bool spawned = pawn.Spawned;
            CompAbilityUserMight comp = pawn.GetCompAbilityUserMight();
            if (comp != null)
            {
                herbPwr = comp.MightData.MightPowerSkill_Herbalist.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Herbalist_pwr").level;
                herbVer = comp.MightData.MightPowerSkill_Herbalist.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Herbalist_ver").level;
                herbEff = comp.MightData.MightPowerSkill_Herbalist.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Herbalist_eff").level;
            }
        }

        public override void PostTick()
        {
            base.PostTick();
            if(!initialized)
            {
                this.initialized = true;
                Initialize();
            }
            if(Find.TickManager.TicksGame % 150 == 0)
            {
                this.Severity -= .015f * (1f - (.1f * herbEff));
                if(Find.TickManager.TicksGame % 3000 == 0)
                {
                    Initialize();
                }
                if(this.pawn.CurJob != null && this.pawn.CurJobDef.defName == "PruneGauranlenTree")
                {
                    this.Severity += .4f * (1f + (.1f * herbVer));
                }
                if(this.pawn.Map == null && this.pawn.ParentHolder is Caravan car)
                {
                    bool flag;
                    if (!car.NightResting)
                    {
                        this.Severity += (ForagedFoodPerDayCalculator.GetBaseForagedNutritionPerDay(this.pawn, out flag)/50f) * (1f + (.05f * herbVer));
                    }                    
                }
            }
        }        
    }
}
