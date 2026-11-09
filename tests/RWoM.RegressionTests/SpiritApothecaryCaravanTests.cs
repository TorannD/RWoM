using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static partial class SpiritMapCollapseTests
{
    private static bool apothecaryNight;
    // The map fixture has no planet clock; control only this environmental input.
    private static bool ApothecaryNightRest(ref bool __result) {__result=apothecaryNight;return false;}
    private static IEnumerable<TestCase> ApothecaryCaravanCases()
    {
        foreach(string mode in new[]{"Day","Night","Depleted","NativeCarrier","DownedCarrier"})
        yield return new("ApothecaryProjection.CaravanStock."+mode,r=>SpiritRebirthRitualTransactionTests.Run(r,c=>{
            using var fixture=new Fixture(r,c);using var setup=ApothecaryCareerTests.Setup(r);
            using var rest=new Patch(typeof(Caravan).GetProperty(nameof(Caravan.NightResting))!.GetMethod!,nameof(ApothecaryNightRest));
            var priorStat=StatDefOf.ForagedNutritionPerDay;var tick=Find.TickManager.TicksGame;
            var priorDegrees=TorannMagicDefOf.TM_Apothecary.degreeDatas;
            try {
                TorannMagicDefOf.TM_Apothecary.degreeDatas=new List<TraitDegreeData>{new(){degree=0,label="Apothecary"}};
                StatDefOf.ForagedNutritionPerDay=new StatDef{defName="ForagedNutritionPerDay",defaultBaseValue=10,workerClass=typeof(StatWorker)};
                var path=new ClassProgressionState(Vocation.Warrior);path.TryAddFoundation("rwom.apothecary",DisciplineDomain.Might);Set(ClassIdentity.Component(c.Original),"state",path);
                foreach(var skill in c.Original.GetCompAbilityUserMight().MightData.MightPowerSkill_Herbalist)skill.level=3;
                var stock=new Hediff_ApothecaryHerbs{def=TorannMagicDefOf.TM_ApothecaryHerbsHD,pawn=c.Original};stock.Severity=50;c.Original.health.hediffSet.hediffs.Add(stock);
                ApothecaryPatient(c.Creator);
                Set(c.Creator.jobs,"curJob",null!);Set(c.Creator.jobs,"curDriver",null!);
                var caravan=new Caravan{ID=69991,def=new WorldObjectDef{defName="ApothecaryCaravan"}};
                Set(((UIRoot_Play)Current.Root.uiRoot).mapUI,"colonistBar",System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ColonistBar)));
                Set(caravan,"factionInt",c.Creator.Faction);Set(caravan,"babies",null);
                c.Map.spawnedThings.Remove(c.Creator);Set(c.Creator,"mapIndexOrState",(sbyte)-1);
                Check.Equal(true,caravan.pawns.TryAdd(c.Creator,false),"native caravan owner receives physical carrier");
                Check.Same(caravan,c.Creator.ParentHolder,"physical carrier belongs to caravan");
                if(mode=="Depleted")Set(ClassIdentity.Component(c.Original).SpiritRebirth,"depletionPending",true);
                if(mode=="NativeCarrier")c.Creator.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Apothecary));
                if(mode=="DownedCarrier")Set(c.Creator.health,"healthState",PawnHealthState.Down);
                apothecaryNight=mode=="Night";Set(Find.TickManager,"ticksGameInt",150);
                float native=ForagedFoodPerDayCalculator.GetBaseForagedNutritionPerDay(c.Creator,out bool skip);
                Check.Equal(mode=="DownedCarrier"?0f:10f,native,"native foraging observes physical worker health and stat");
                stock.PostTick();
                float expected=50-.015f*.7f+(mode=="Day"?10f/50f*1.15f:0);
                Check.Equal(true,Math.Abs(stock.Severity-expected)<.0001f,"only active daytime physical foraging funds soul stock");
                Check.Equal(130f,stock.MaxSeverity,"foraging retains soul training");
                Check.Equal(false,c.Creator.health.hediffSet.HasHediff(stock.def),"caravan does not duplicate herbs on carrier");
            } finally {TorannMagicDefOf.TM_Apothecary.degreeDatas=priorDegrees;StatDefOf.ForagedNutritionPerDay=priorStat;Set(Find.TickManager,"ticksGameInt",tick);apothecaryNight=false;}
        }));
    }
}
