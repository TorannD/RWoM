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

internal static class RangerTrailTests
{
    private static readonly BindingFlags Impl=BindingFlags.NonPublic|BindingFlags.Instance;
    private sealed class SpringProbe : Building_PoisonTrap
    {
        protected override float SpringChance(Pawn victim) => 1;
        public override void Spring(Pawn victim) => Set(this,"triggered",true); // isolate gas explosion and graphics
        public void Approach(Pawn victim) => CheckSpring(victim);
    }
    public static bool Skip()=>false;
    internal static void Trap(Pawn ranger,Pawn victim,CompAbilityUserMight might)
    {
        var path=ClassIdentity.Component(might.Pawn).State.Find("rwom.ranger")!;
        var field=path.GetType().GetField("RangerTrapVictims",Impl)!;var before=field.GetValue(path);
        var map=ranger.Map;var oldPawns=map.mapPawns;var oldFog=map.fogGrid;var oldPos=victim.Position;
        var main=typeof(UnityData).GetField("mainThreadId",BindingFlags.Static|BindingFlags.NonPublic)!;
        var oldThread=main.GetValue(null);
        var oldId=victim.thingIDNumber;var def=victim.def;var oldFaction=victim.Faction;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;var id="rwom.ranger.trap."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(ht,id)!;
        var trap=new SpringProbe();
        try
        {
            main.SetValue(null,Environment.CurrentManagedThreadId);
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(MapPawns).GetMethod("EnsureFactionsListsInit",Impl),Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(RangerTrailTests).GetMethod(nameof(Skip))!),null,null,null]);
            var threatMethod=typeof(GenHostility).GetMethods().Single(m=>m.Name=="IsActiveThreatTo"&&m.GetParameters()[0].ParameterType==typeof(IAttackTarget));
            ht.GetMethod("Patch")!.Invoke(harmony,[threatMethod,Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(RangerTrailTests).GetMethod(nameof(Threat))!),null,null,null]);
            map.mapPawns=new MapPawns(map);map.fogGrid=new FogGrid(map);
            Set(victim,"positionInt",ranger.Position+IntVec3.East);KnownThreat=victim;
            trap.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));trap.def.category=ThingCategory.Building;
            trap.thingIDNumber=19930;Set(trap,"factionInt",ranger.Faction);Set(trap,"mapIndexOrState",(sbyte)0);
            might.combatItems.Add(trap);
            victim.thingIDNumber=19931;
            victim.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));victim.def.race=new RaceProperties{intelligence=Intelligence.Humanlike};
            trap.Approach(victim);
            Check.Equal(null,typeof(Building_PoisonTrap).GetField("planter",Impl)!.GetValue(trap),"absent old owner stays unresolved");
            Set(trap,"triggered",false);map.mapPawns.SpawnedPawnsInFaction(ranger.Faction).Add(ranger);
            trap.Approach(victim);
            Check.Same(might.Pawn,typeof(Building_PoisonTrap).GetField("planter",Impl)!.GetValue(trap)!,"legacy trap finds actual stored owner on first trigger");
            typeof(Building_PoisonTrap).GetMethod("CheckForAgent",Impl)!.Invoke(trap,null);
            Check.Equal(false,trap.destroyAfterUse,"trap rearms while its owner is still present");
            Check.Equal(1,((List<int>)field.GetValue(path)!).Count,"first genuine spring");
            victim.thingIDNumber=19932;trap.Approach(victim);
            Check.Equal(1,((List<int>)field.GetValue(path)!).Count,"active gas cannot train a second target");
            Set(trap,"triggered",false);Set(victim,"factionInt",ranger.Faction);trap.Approach(victim);
            Check.Equal(1,((List<int>)field.GetValue(path)!).Count,"friendly trip ignored");
            Set(trap,"triggered",false);Set(victim,"factionInt",oldFaction);
            might.combatItems.Remove(trap);trap.Approach(victim);
            Check.Equal(1,((List<int>)field.GetValue(path)!).Count,"released trap cannot train former owner");
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);main.SetValue(null,oldThread);
            KnownThreat=null;Set(victim,"positionInt",oldPos);map.fogGrid=oldFog;
            field.SetValue(path,before);map.mapPawns=oldPawns;victim.thingIDNumber=oldId;victim.def=def;
            Set(victim,"factionInt",oldFaction);might.combatItems.Remove(trap);
        }
    }

    public static Pawn? KnownThreat;
    public static bool Threat(IAttackTarget target,ref bool __result) {__result=target.Thing==KnownThreat;return false;}
    internal static void Trail(RuntimeFixture f,Pawn ranger,Pawn companion,Pawn victim,MightAbility? power=null)
    {
        var path=ClassIdentity.Component(power?.Pawn??ranger).State.Find("rwom.ranger")!;
        var fields=new[]{"RangerRetreats","RangerRetreatTimes"}.ToDictionary(k=>k,k=>path.GetType().GetField(k,Impl)!.GetValue(path));
        var map=ranger.Map;var oldPawns=map.mapPawns;var oldFog=map.fogGrid;var oldPos=companion.Position;var oldEnemyPos=victim.Position;
        var oldMind=companion.mindState;var oldPather=companion.pather;
        var components=map.components;map.components=new List<MapComponent>(components??new List<MapComponent>());
        var trails=new RangerSafeTrails(map);map.components.RemoveAll(c=>c is RangerSafeTrails);map.components.Add(trails);
        var ticks=Find.TickManager.TicksGame;var enemyID=victim.thingIDNumber;var oldRangerID=ranger.thingIDNumber;
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;var id="rwom.ranger.trail."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        var factor=typeof(RangerSafeTrails).GetMethod("Speed",BindingFlags.NonPublic|BindingFlags.Static)!;
        float Speed(Pawn p)=>(float)factor.Invoke(null,[p])!;
        try
        {
            companion.mindState=new Pawn_MindState(companion);map.fogGrid=new FogGrid(map);map.mapPawns=new MapPawns(map);var start=ranger.Position;var end=start+IntVec3.East*8;
            ranger.thingIDNumber=21001;victim.thingIDNumber=21002;Set(victim,"positionInt",start-IntVec3.East*4);
            KnownThreat=victim;
            var threatMethod=typeof(GenHostility).GetMethods().Single(m=>m.Name=="IsActiveThreatTo"&&m.GetParameters()[0].ParameterType==typeof(IAttackTarget));
            ht.GetMethod("Patch")!.Invoke(harmony,[threatMethod,Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(RangerTrailTests).GetMethod(nameof(Threat))!),null,null,null]);
            ((List<Pawn>)map.mapPawns.AllPawnsSpawned).Add(victim);
            Set(companion,"positionInt",start+IntVec3.East);map.thingGrid.Register(companion);companion.pather=new Pawn_PathFollower(companion);
            Pawn SecondTraveler(int number,int offset)
            {
                var ally=PawnWithComps(false,false).pawn;ally.kindDef=new PawnKindDef{race=ally.def};ally.guest=new Pawn_GuestTracker(ally);
                ally.def.category=ThingCategory.Pawn;ally.def.size=new IntVec2(1,1);ally.mindState=new Pawn_MindState(ally);
                ally.thingIDNumber=number;Set(ally,"mapIndexOrState",(sbyte)0);Set(ally,"positionInt",start+IntVec3.East*offset);
                Set(ally,"factionInt",ranger.Faction);map.thingGrid.Register(ally);return ally;
            }
            var second=SecondTraveler(21923,6);var third=SecondTraveler(21924,7);
            var verb=new Verb_RangerSafeTrail{caster=ranger,verbProps=new VerbProperties_Ability{range=8}};
            if(power!=null)verb.Ability=power;
            Set(verb,"currentTarget",new LocalTargetInfo(end));var cast=typeof(Verb_RangerSafeTrail).GetMethod("TryCastShot",Impl)!;
            Check.Equal(false,verb.CanHitTargetFrom(start,start),"no zero-cell trail");
            Check.Equal(false,verb.CanHitTargetFrom(start,end+IntVec3.East),"eight actual cells maximum");
            var door=new Building_Door{def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
            door.def.size=new IntVec2(1,1);door.def.building=new BuildingProperties();door.def.fillPercent=1;
            Set(door,"positionInt",start+IntVec3.East*4);Set(door,"mapIndexOrState",(sbyte)0);map.edificeGrid.Register(door);
            Check.Equal(false,(bool)cast.Invoke(verb,null)!,"closed doorway blocks route");map.edificeGrid.DeRegister(door);
            Check.Equal(true,(bool)cast.Invoke(verb,null)!,"route created via actual ability");
            Check.Equal(1.15f,Speed(companion),"bonded ally on route");Check.Equal(1f,Speed(ranger),"ranger not one of the beneficiaries");
            Check.Equal(1.15f,Speed(second),"second ally");Check.Equal(1f,Speed(third),"third ally receives no bonus");
            var file=Path.Combine(f.WorkingDirectory,"ranger-trail.xml");
            try {Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref trails,"mapComp",map);Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            trails.MapRemoved();trails=null!;
            try {Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref trails,"mapComp",map);
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",Impl)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(ranger);directory.RegisterLoaded(companion);directory.RegisterLoaded(victim);directory.RegisterLoaded(second);
                if(power!=null)directory.RegisterLoaded(power.Pawn);
                Scribe.loader.FinalizeLoading();
            }finally{Scribe.ForceStop();}
            Check.Equal(1.15f,Speed(companion),"loaded route and participant restored");
            if(power!=null)
            {
                var mark=ranger.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD);
                ranger.health.hediffSet.hediffs.Remove(mark);
                try { Check.Equal(1f,Speed(companion),"former host cannot sustain a loaded route"); }
                finally { ranger.health.hediffSet.hediffs.Add(mark); }
            }
            void Walk(IntVec3 pos)
            {
                map.thingGrid.Deregister(companion);Set(companion,"positionInt",pos);map.thingGrid.Register(companion);
                Set(companion.pather,"lastMovedTick",Find.TickManager.TicksGame);trails.MapComponentTick();
            }
            Walk(start+IntVec3.East*2);Walk(start+IntVec3.East*3);
            Check.Equal(1,(int)path.GetType().GetField("RangerRetreats",Impl)!.GetValue(path)!,"two walking steps away from active attacker count once");
            Walk(start+IntVec3.East*4);
            Check.Equal(1,(int)path.GetType().GetField("RangerRetreats",Impl)!.GetValue(path)!,"cast receipt consumed");
            Walk(start+IntVec3.North);
            Check.Equal(1f,Speed(companion),"no off-route speed");
            Walk(start+IntVec3.East);Set(Find.TickManager,"ticksGameInt",ticks+480);
            Check.Equal(1f,Speed(companion),"expiry exact even before component runs");
            trails.MapComponentTick();
            map.thingGrid.Deregister(companion);map.thingGrid.Deregister(second);map.thingGrid.Deregister(third);
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);KnownThreat=null;
            trails?.MapRemoved();map.components=components;map.mapPawns=oldPawns;map.fogGrid=oldFog;
            Set(companion,"positionInt",oldPos);companion.pather=oldPather;companion.mindState=oldMind;Set(victim,"positionInt",oldEnemyPos);
            ranger.thingIDNumber=oldRangerID;victim.thingIDNumber=enemyID;Set(Find.TickManager,"ticksGameInt",ticks);
            foreach(var saved in fields)Set(path,saved.Key,saved.Value);
        }
    }
}
