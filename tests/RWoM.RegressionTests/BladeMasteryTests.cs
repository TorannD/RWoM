using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeMasteryTests
{
    public static bool Peace(ref bool __result) { __result = false; return false; }
    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool peaceful, Action reload)
    {
        var api = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.BladeMastery", true)!;
        const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Static;
        object? Call(string name, params object[] args) => api.GetMethod(name,hidden)!.Invoke(null,args);
        var ability = TorannMagicDefOf.TM_SeismicSlash;
        var oldType = ability.abilityClass;
        try
        {
            ability.abilityClass = typeof(PawnAbility);
            Check.Equal(false,(bool)Call("Claim",pawn)!,"mastery cannot be claimed early");
            if (peaceful) Rescue(pawn, reload, () => (int)Call("Rescues",pawn)!);
            else
            {
                BladeRiposteTests.Verify(f,pawn);
                for (int i=0;i<8;i++)
                {
                    Check.Equal(false,(bool)Call("Ready",pawn)!,"eight replies needed");
                    Call("Riposte",pawn);
                    if (i==3) reload();
                }
            }
            reload();
            Check.Equal(true,(bool)Call("Claim",pawn)!,"one of the two trials grants mastery");
            Check.Equal(false,(bool)Call("Claim",pawn)!,"no repeat receipt");
            reload();
            might.AbilityData.Powers.RemoveAll(a=>a.Def==ability);
            var career = api.Assembly.GetType("TorannMagic.ClassProgression.BladeCareer",true)!;
            career.GetMethod("Restore",hidden)!.Invoke(null,[might]);
            Check.Equal(InitiationStage.Master,comp.State.Find("rwom.bladedancer")!.Stage,"master grade retained");
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==ability),"seismic slash restored at known rank");
            BladeAscensionTests.Verify(f,pawn,might,comp,peaceful,reload);
        }
        finally { ability.abilityClass = oldType; }
    }

    private static void Rescue(Pawn dancer, Action reload, Func<int> count)
    {
        var map = dancer.Map;
        map.info = new MapInfo {Size = new IntVec3(40,1,40)};
        map.cellIndices = new CellIndices(map);
        map.thingGrid = new ThingGrid(map);
        var cell = new IntVec3(7,0,7);
        var victim = PawnWithComps(false,false).pawn;
        victim.kindDef = new PawnKindDef {race=victim.def};
        victim.mindState = new Pawn_MindState(victim);
        victim.guest = new Pawn_GuestTracker(victim);
        victim.thingIDNumber = 9138;
        victim.def.category = ThingCategory.Pawn;
        victim.def.size = new IntVec2(1,1);
        Set(victim,"mapIndexOrState",(sbyte)0); Set(victim,"positionInt",cell); Set(victim,"factionInt",dancer.Faction);
        map.thingGrid.Register(victim);
        var bed = new Building_Bed {def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
        bed.def.size=new IntVec2(1,1);
        Set(bed,"positionInt",cell); Set(bed,"mapIndexOrState",(sbyte)0); map.thingGrid.Register(bed);
        victim.jobs=new Pawn_JobTracker(victim); Set(victim.jobs,"curJob",new Job {def=new JobDef {defName="LayDown"}});
        victim.jobs.posture=PawnPosture.LayingInBed;
        Set(victim.health,"healthState",PawnHealthState.Down);
        var oldRescue = JobDefOf.Rescue;
        var lib = Assembly.Load("0Harmony");
        var ht=lib.GetType("HarmonyLib.Harmony")!;
        string id="rwom.regression.blade.rescue."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(ht,id)!;
        try
        {
            var method=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(BladeMasteryTests).GetMethod(nameof(Peace))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(GenHostility).GetMethod("AnyHostileActiveThreatToPlayer"),method,null,null,null]);
            JobDefOf.Rescue = new JobDef {defName="Rescue"};
            var driver = new JobDriver_TakeToBed { pawn=dancer, job=new Job {def=JobDefOf.Rescue,targetA=victim,targetB=bed} };
            var hook=typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.GladiatorRescueTrial",true)!.GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!;
            void Delivery() => ((IEnumerable<Toil>)hook.Invoke(null,[Array.Empty<Toil>(),driver])!).Single().initAction();
            Delivery(); Check.Equal(0,count(),"healthy casualty does not count");
            victim.health.hediffSet.hediffs.Add(new Hediff_Injury {pawn=victim,def=new HediffDef {defName="BladeRescueWound"}});
            for(int i=0;i<12;i++)
            {
                Set(Find.TickManager,"ticksGameInt",100 + 60000*i);
                Delivery();
                Check.Equal(i+1,count(),"peaceful delivery advances");
                if(i==4) reload();
                Delivery();
                Check.Equal(i+1,count(),"same casualty cannot be farmed by repeated bed moves");
            }
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            JobDefOf.Rescue=oldRescue;
            map.thingGrid.Deregister(victim); map.thingGrid.Deregister(bed);
        }
    }
}
