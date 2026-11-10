using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeServiceTests
{
    private static Thing? subject;
    public static bool MaxHP(Thing __instance, ref int __result)
    {
        if (__instance != subject) return true;
        __result = 100;
        return false;
    }

    public static bool NoFireStat(Thing __instance) => __instance != subject;

    internal static void Verify(RuntimeFixture fixture, Pawn actor, Pawn? source=null, bool soldier=false)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.category = ThingCategory.Item; def.useHitPoints = true; def.stackLimit = 1;
        def.size = new IntVec2(1,1); def.thingClass = typeof(Thing); def.tools = [new Tool()];
        var blade = new Thing {def = def, thingIDNumber = 87346, HitPoints = 100};
        Set(blade, "positionInt", actor.Position); Set(blade, "mapIndexOrState",(sbyte)0);
        object state = ClassIdentity.Component(source ?? actor).State.Find(soldier?"rwom.supersoldier":"rwom.bladedancer")!;
        if(soldier) state=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.SuperSoldierMartialPath")!.GetMethod("Training",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[state])!;
        var backing = state.GetType().GetField(soldier?"Weapons":"BladeWeapons",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var oldIds = backing.GetValue(state);
        var oldWorld = Current.Game.World;
        var oldLords = actor.Map.lordManager;
        var oldGrid = actor.Map.thingGrid; var oldIndices = actor.Map.cellIndices; var oldInfo = actor.Map.info;
        var origin = actor.Position;
        var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
        world.factionManager = new FactionManager();
        world.factionManager.AllFactionsListForReading.Add(actor.Faction);
        Set(world.factionManager, "ofPlayer", actor.Faction);
        var harmonyLib=Assembly.Load("0Harmony");
        var ht=harmonyLib.GetType("HarmonyLib.Harmony")!;
        var id="rwom.test.bladeservice."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var wear=typeof(JobDriver_BladeWeaponService).Assembly.GetType("TorannMagic.ClassProgression.BladeWeaponWear")!;
        var save=wear.GetNestedType("SavedWear",BindingFlags.NonPublic)!.GetMethod("Postfix",flags)!;
        int Available()=>(int)wear.GetMethod("Repairable",flags)!.Invoke(null,[subject,100])!;
        try
        {
            subject=blade;
            var prefix=Activator.CreateInstance(harmonyLib.GetType("HarmonyLib.HarmonyMethod")!,typeof(BladeServiceTests).GetMethod(nameof(MaxHP))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(Thing).GetProperty("MaxHitPoints")!.GetMethod,prefix,null,null,null]);
            prefix=Activator.CreateInstance(harmonyLib.GetType("HarmonyLib.HarmonyMethod")!,typeof(BladeServiceTests).GetMethod(nameof(NoFireStat))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(Thing).GetProperty("FlammableNow")!.GetMethod,prefix,null,null,null]);
            foreach(var name in new[]{"PlayerWeaponDamage","RepairedWeapon","SavedWear"})
            {
                var proc=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[wear.GetNestedType(name,BindingFlags.NonPublic)])!;
                proc.GetType().GetMethod("Patch")!.Invoke(proc,null);
            }
            actor.Map.lordManager=(Verse.AI.Group.LordManager)RuntimeHelpers.GetUninitializedObject(typeof(Verse.AI.Group.LordManager));
            actor.Map.lordManager.lords=[];
            actor.Map.info = new MapInfo {Size = new IntVec3(40,1,40)};
            actor.Map.cellIndices = new CellIndices(actor.Map); actor.Map.thingGrid = new ThingGrid(actor.Map);
            Set(actor,"positionInt",new IntVec3(5,0,5));
            Set(Current.Game,"worldInt",world);
            backing.SetValue(state,new List<int>());
            var testDamage=new DamageDef {harmsHealth=true,workerClass=typeof(DamageWorker)};
            Set(testDamage,"externalViolence",false);
            var damage=new DamageInfo(testDamage,20f,instigator:actor);
            Set(blade,"mapIndexOrState",(sbyte)-1);
            blade.TakeDamage(damage);
            Check.Equal(80,blade.HitPoints,"normal item damage uses actual HP");
            Check.Equal(0,Available(),"player-inflicted damage is never eligible");
            var path=Path.Combine(fixture.WorkingDirectory,"blade-wear.xml");
            try {Scribe.saver.InitSaving(path,"test");save.Invoke(null,[blade]);Scribe.saver.FinalizeSaving();}
            finally {Scribe.ForceStop();}
            subject=new Thing{def=def,HitPoints=80,thingIDNumber=blade.thingIDNumber};
            try {Scribe.loader.InitLoading(path);save.Invoke(null,[subject]);Scribe.loader.FinalizeLoading();}
            finally {Scribe.ForceStop();}
            Check.Equal(0,Available(),"deliberate wear survives loading on replacement instance");
            blade=subject;Set(blade,"positionInt",actor.Position);Set(blade,"mapIndexOrState",(sbyte)0);
            blade.HitPoints=90;Check.Equal(0,Available(),"other repairs reduce ineligible balance");
            blade.HitPoints=70;Check.Equal(20,Available(),"subsequent normal wear remains eligible");
            var job=new Job{def=new JobDef{defName="TM_BladeWeaponService"},targetA=blade};
            if (source!=null) { job.targetB=source; job.targetC=actor.Position; }
            var driver=new JobDriver_BladeWeaponService{pawn=actor,job=job};
            var steps=((IEnumerable<Toil>)typeof(JobDriver_BladeWeaponService).GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null)!).ToArray();
            Check.Equal(600,steps[1].defaultDuration,"repair work duration");
            Check.Equal(0,((List<int>)backing.GetValue(state)!).Count,"interruption before completion grants no progress");
            if (source!=null)
            {
                var possession=actor.health.hediffSet.GetFirstHediffOfDef(TorannMagic.TorannMagicDefOf.TM_SpiritPossessionHD);
                actor.health.hediffSet.hediffs.Remove(possession);
                try
                {
                    Check.Equal(false,(bool)typeof(JobDriver_BladeWeaponService).GetMethod("Valid",flags)!.Invoke(null,[actor,blade,source])!,
                        "former host cannot finish a stored order for the departed soul");
                }
                finally { actor.health.hediffSet.hediffs.Add(possession); }
            }
            steps[2].initAction();
            Check.Equal(80,blade.HitPoints,"job repairs 10 percent durability");
            Check.Equal(1,((List<int>)backing.GetValue(state)!).Count,"one distinct weapon serviced");
            Check.Equal(false,(bool)typeof(JobDriver_BladeWeaponService).GetMethod("Valid",flags)!.Invoke(null,[actor,blade,source??actor])!,"repeat service refused");
            blade.HitPoints=100;blade.HitPoints=86;
            Check.Equal(14,Available(),"full outside repair resets penalty for later natural wear");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            backing.SetValue(state,oldIds);subject=null;actor.Map.lordManager=oldLords;actor.Map.thingGrid=oldGrid;actor.Map.cellIndices=oldIndices;actor.Map.info=oldInfo;Set(actor,"positionInt",origin);
            Set(Current.Game,"worldInt",oldWorld);
        }
    }
}
