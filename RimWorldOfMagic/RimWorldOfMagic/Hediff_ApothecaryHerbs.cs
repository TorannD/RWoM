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

        private Pawn PhysicalWorker()
        {
            if (!ClassProgression.SpiritRebirthState.TryGet(pawn, out var spirit)) return pawn;
            return !spirit.DepletionPending && spirit.HasValidHost(pawn) &&
                ClassProgression.ApothecaryMartialPath.Owner(spirit.Host) == pawn ? spirit.Host : null;
        }

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
                var fresh = ClassProgression.ApothecaryMartialPath.Path(pawn)?.MartialAlternate == true;
                this.Severity -= .015f * (1f - (.1f * herbEff)) * (fresh ? .9f : 1f);
                if(Find.TickManager.TicksGame % 3000 == 0)
                {
                    Initialize();
                }
                // The original keeps stock, training and decay; only the physical
                // worker can earn pruning/foraging income while possessed.
                var worker = PhysicalWorker();
                if(worker?.CurJob != null && worker.CurJobDef.defName == "PruneGauranlenTree")
                {
                    this.Severity += .4f * (1f + (.1f * herbVer));
                }
                if(worker != null && worker.Map == null && worker.ParentHolder is Caravan car)
                {
                    bool flag;
                    if (!car.NightResting)
                    {
                        this.Severity += (ForagedFoodPerDayCalculator.GetBaseForagedNutritionPerDay(worker, out flag)/50f) * (1f + (.05f * herbVer));
                    }                    
                }
            }
        }        
    }
}
