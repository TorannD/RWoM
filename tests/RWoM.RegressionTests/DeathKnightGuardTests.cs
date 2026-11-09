using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;

internal static class DeathKnightGuardTests
{
    private static bool shielded, launched;
    public static bool TryLaunch(ref bool __result) { __result = launched; return false; }
    public static bool NativeShield(ref bool absorbed) { absorbed = shielded; return false; }
    public static bool NativeHit(DamageInfo dinfo, ref DamageWorker.DamageResult __result)
    {
        __result = new DamageWorker.DamageResult { totalDamageDealt = dinfo.Amount };
        return false;
    }
    internal static void Verify(Pawn knight, bool oath, bool foundation)
    {
        var map = knight.Map;
        map.info = new MapInfo { Size = new IntVec3(40, 1, 40) };
        map.cellIndices = new CellIndices(map);
        map.thingGrid = new ThingGrid(map);
        var oldWorld = Current.Game.World;
        var oldPosition = knight.Position;
        var oldRelations = typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(knight.Faction);
        var oldState = knight.GetComp<CompClassProgression>().State.Find("rwom.deathknight")!;
        var stateField = oldState.GetType().GetField("MartialData",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var priorData = stateField.GetValue(oldState);
        var lib=Assembly.Load("0Harmony");
        var ht=lib.GetType("HarmonyLib.Harmony")!;
        string id="rwom.regression.knight."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(ht,id)!;
        try
        {
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager=new FactionManager(); world.factionManager.AllFactionsListForReading.Add(knight.Faction);
            Set(world.factionManager,"ofPlayer",knight.Faction); Set(Current.Game,"worldInt",world);
            var foe=new Faction{def=new FactionDef()};
            Set(foe,"relations",new List<FactionRelation>{new(){other=knight.Faction,kind=FactionRelationKind.Hostile}});
            Set(knight.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
            void Place(Pawn p, IntVec3 cell, Faction faction)
            {
                p.def.size=new IntVec2(1,1);p.def.category=ThingCategory.Pawn;p.kindDef=new PawnKindDef{race=p.def};
                p.guest=new Pawn_GuestTracker(p);p.mindState=new Pawn_MindState(p);
                Set(p,"positionInt",cell);Set(p,"factionInt",faction);Set(p,"mapIndexOrState",(sbyte)0);
                map.thingGrid.Register(p);
            }
            Place(knight,new IntVec3(21,0,21),knight.Faction);
            var ally=PawnWithComps(false,false).pawn;Place(ally,knight.Position+IntVec3.East,knight.Faction);
            var enemy=PawnWithComps(false,false).pawn;Place(enemy,knight.Position+IntVec3.North,foe);
            var career = typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.DeathKnightMartialPath",true)!;
            const BindingFlags methods = BindingFlags.NonPublic|BindingFlags.Static;
            bool Hate(Thing source) => (bool)career.GetMethod("CanFuelHate",methods)!.Invoke(null,[knight,source])!;
            bool Drain(Pawn target) => (bool)career.GetMethod("CanDrain",methods)!.Invoke(null,[knight,target])!;
            Check.Equal(false,Hate(ally),"friendly fire cannot feed native hate");
            Check.Equal(false,Hate(knight),"self injury cannot feed hate");
            Check.Equal(true,Hate(enemy),"hostile attacker feeds hate");
            Check.Equal(false,Drain(ally),"ally cannot feed lifesteal");
            Check.Equal(!foundation,Drain(enemy),"only a trained knight can drain hostile targets");
            var cls=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.DeathKnightGuard+"+(foundation?"GuardTrial":"OathShield"),true)!;
            var processor=ht.GetMethod("CreateClassProcessor")!.Invoke(harmony,[cls])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor,null);
            var native=foundation?typeof(Thing).GetMethod(nameof(Thing.TakeDamage)):typeof(Pawn).GetMethod(nameof(Pawn.PreApplyDamage));
            var hm=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(DeathKnightGuardTests).GetMethod(foundation?nameof(NativeHit):nameof(NativeShield))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[native,hm,null,null,null]);
            var cut=new DamageDef{defName="RegressionKnightCut",harmsHealth=true};
            var hit=new DamageInfo(cut,30f,0,-1,enemy);
            if(foundation)
            {
                var baseShot = typeof(AbilityUser.Verb_UseAbility).GetMethod("TryCastShot",BindingFlags.Instance | BindingFlags.NonPublic)!;
                var shotPatch = Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(DeathKnightGuardTests).GetMethod(nameof(TryLaunch))!)!;
                ht.GetMethod("Patch")!.Invoke(harmony,[baseShot,shotPatch,null,null,null]);
                var verb = new Verb_DeathKnightSpite { caster = knight };
                var fire = typeof(Verb_DeathKnightSpite).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!;
                bool Fire() => (bool)fire.Invoke(verb,null)!;
                var hate = TM_Calc.GetHateHediff(knight);
                float previous = hate.Severity;
                launched=false;Check.Equal(false,Fire(),"failed projectile rejected");Check.Equal(previous,hate.Severity,"failed projectile free");
                launched=true;hate.Severity=19;Check.Equal(false,Fire(),"hate rechecked after warmup");Check.Equal(19f,hate.Severity,"failed recheck free");
                hate.Severity=35;Check.Equal(true,Fire(),"valid projectile fired");Check.Equal(15f,hate.Severity,"successful spite spends exactly 20");
                hate.Severity=previous;
                stateField.SetValue(oldState,null);
                knight.TakeDamage(new DamageInfo(cut,30f,0,-1,ally));
                Check.Equal(true,stateField.GetValue(oldState)==null,"friendly fire cannot count");
                knight.TakeDamage(hit);
                var data=stateField.GetValue(oldState)!;
                Check.Equal(1,(int)data.GetType().GetField("Battles",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(data)!,"real hostile hit protecting adjacent ally");
            }
            else
            {
                var hate=TM_Calc.GetHateHediff(knight); var previous=hate.Severity;
                shielded=true;ally.PreApplyDamage(ref hit,out bool absorbed);
                Check.Equal(true,absorbed,"native shield respected");Check.Equal(previous,hate.Severity,"no charge on absorbed hit");
                shielded=false;ally.PreApplyDamage(ref hit,out absorbed);
                Check.Equal(oath?5f:30f,hit.Amount,"only oath offsets incoming damage");
                Check.Equal(previous-(oath?10f:0f),hate.Severity,"single payment");
                var next=new DamageInfo(cut,30f,0,-1,enemy);ally.PreApplyDamage(ref next,out absorbed);
                Check.Equal(30f,next.Amount,"cooldown prevents second block");
            }
        }
        finally
        {
            shielded=false;ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);if(foundation)stateField.SetValue(oldState,priorData);
            Set(knight.Faction,"relations",oldRelations);Set(Current.Game,"worldInt",oldWorld);Set(knight,"positionInt",oldPosition);
        }
    }
}
