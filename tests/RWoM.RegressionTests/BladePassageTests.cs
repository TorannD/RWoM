using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.FleshDevelopment;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladePassageTests
{
    private static int hits, teleports;
    private static float amount;
    private static Thing? recipient;
    public static bool TakeDamage(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
    {
        hits++; recipient = __instance; amount = dinfo.Amount;
        __result = new DamageWorker.DamageResult { totalDamageDealt = amount };
        return false;
    }
    public static bool WeaponDamage(ref float __result) { __result = 20; return false; }
    public static bool WeaponPenetration(ref float __result) { __result = .1f; return false; }
    public static bool Notify(bool endCurrentJob) { Check.Equal(false,endCurrentJob,"relocation preserves cast job"); teleports++; return false; }
    public static bool GenericRecovery(ref int __result) { __result = 17; return false; }
    private static bool landingSafe;
    private static IntVec3 evaluated;
    private static int payments;
    public static bool Admit(ref bool __result) { __result=true; return false; }
    public static bool CheckLanding(IntVec3 cell, ref bool __result) { evaluated=cell; __result=landingSafe; return false; }
    public static bool Charge() { payments++; return false; }
    public static bool SkipDrawing() => false;

    internal static void Verify(RuntimeFixture fixture, Pawn actor, CompAbilityUserMight might)
    {
        var path = ClassIdentity.Component(actor).State.Find("rwom.bladedancer")!;
        var countField = path.GetType().GetField("BladePassages",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var oldCount = (int)countField.GetValue(path)!;
        var flagsState=BindingFlags.NonPublic|BindingFlags.Instance;
        var saved=new[]{"BladeRoutes","BladeRouteOpponent","BladeRouteOrigin","BladeRouteUntil","BladeRouteMap","BladePassageUpgrades"}
            .ToDictionary(k=>k,k=>path.GetType().GetField(k,flagsState)!.GetValue(path)!);
        bool master=path.Stage==InitiationStage.Master;
        Set(path,"BladePassageUpgrades",0);
        var oldGear = actor.equipment;
        var oldStances = actor.stances;
        var oldJobs = actor.jobs;
        var oldMap = actor.Map;
        var oldPos = actor.Position;
        var relations = typeof(Faction).GetField("relations",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var originalRelations = relations.GetValue(actor.Faction);
        var lib = Assembly.Load("0Harmony");
        var ht = lib.GetType("HarmonyLib.Harmony")!;
        string id = "rwom.regression.bladepassage."+Guid.NewGuid();
        var h = Activator.CreateInstance(ht,id)!;
        void Patch(MethodInfo target,string name) => ht.GetMethod("Patch")!.Invoke(h,[target,Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(BladePassageTests).GetMethod(name)!),null,null,null]);
        var maps=(List<Map>)typeof(Game).GetField("maps",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Current.Game)!;
        int index=maps.IndexOf(oldMap);
        try
        {
            var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            maps[index]=map;
            map.info=new MapInfo{Size=new IntVec3(32,1,32)};
            map.cellIndices=new CellIndices(map);map.thingGrid=new ThingGrid(map);map.edificeGrid=new EdificeGrid(map);
            map.fogGrid=new FogGrid(map);map.coverGrid=new CoverGrid(map);map.gasGrid=new GasGrid(map);
            map.designationManager=new DesignationManager(map);map.listerMergeables=new ListerMergeables(map);
            map.regionGrid=new RegionGrid(map);map.regionDirtyer=new RegionDirtyer(map);
            map.regionAndRoomUpdater=new RegionAndRoomUpdater(map);Set(map.regionAndRoomUpdater,"initialized",true);
            var grid=new PathGrid(map,new PathGridDef {defName="BladePassagePath"});
            var context=new PathingContext(map,grid);
            map.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));Set(map.pathing,"normal",context);Set(map.pathing,"fenceBlocked",context);
            var origin=new IntVec3(10,0,10);Set(actor,"positionInt",origin);map.thingGrid.Register(actor);
            var hostile=new Faction {def=new FactionDef()};
            Set(hostile,"relations",new List<FactionRelation>{new(){other=actor.Faction,kind=FactionRelationKind.Hostile}});
            Set(actor.Faction,"relations",new List<FactionRelation>{new(){other=hostile,kind=FactionRelationKind.Hostile}});
            var target=PawnWithComps(false,false).pawn;target.kindDef=new PawnKindDef {race=target.def};
            target.thingIDNumber=7501;target.guest=new Pawn_GuestTracker(target);target.mindState=new Pawn_MindState(target);
            Set(target,"factionInt",hostile);Set(target,"mapIndexOrState",(sbyte)index);Set(target,"positionInt",origin+IntVec3.East*6);
            var weaponDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            var tool=new Tool {power=20};weaponDef.category=ThingCategory.Item;weaponDef.equipmentType=EquipmentType.Primary;weaponDef.tools=[tool];weaponDef.comps=[];
            var weapon=new ThingWithComps {def=weaponDef};
            var eq=new CompEquippable {parent=weapon};eq.Initialize(new CompProperties {compClass=typeof(CompEquippable)});
            var melee=new Verb_MeleeAttackDamage{caster=actor,verbTracker=eq.verbTracker,verbProps=new VerbProperties{verbClass=typeof(Verb_MeleeAttackDamage),meleeDamageDef=new DamageDef {defName="PassageCut"}}, tool=tool};
            Set(eq.verbTracker,"verbs",new List<Verb>{melee});Set(weapon,"comps",new List<ThingComp>{eq});
            var verb=new Verb_BladePassage{caster=actor,verbProps=new VerbProperties_Ability{range=6}};
            var cast=typeof(Verb_BladePassage).GetMethod("TryCastShot",BindingFlags.Instance|BindingFlags.NonPublic)!;
            bool Fire(){Set(verb,"currentTarget",new LocalTargetInfo(target));return (bool)cast.Invoke(verb,null)!;}
            hits=teleports=0;
            actor.equipment=new Pawn_EquipmentTracker(actor);
            Check.Equal(false,Fire(),"cannot use unarmed");
            actor.equipment.AddEquipment(weapon);
            Set(target,"positionInt",origin+IntVec3.East*7);
            Check.Equal(false,Fire(),"cannot exceed six cells");
            Set(target,"positionInt",origin+IntVec3.East);Check.Equal(false,Fire(),"no passage in place");
            Set(target,"positionInt",origin+IntVec3.East*6);
            Set(target,"factionInt",actor.Faction);Check.Equal(false,Fire(),"ally rejected");Set(target,"factionInt",hostile);
            var door=new Building_Door{def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
            door.def.size=new IntVec2(1,1);door.def.building=new BuildingProperties();door.def.fillPercent=1;
            Set(door,"positionInt",origin+IntVec3.East*2);Set(door,"mapIndexOrState",(sbyte)index);map.edificeGrid.Register(door);
            Check.Equal(false,Fire(),"closed door blocks cast");map.edificeGrid.DeRegister(door);
            Set(door,"positionInt",origin+IntVec3.East);map.edificeGrid.Register(door);
            var corner = new Building {def=door.def};
            Set(corner,"positionInt",origin+IntVec3.North);Set(corner,"mapIndexOrState",(sbyte)index);map.edificeGrid.Register(corner);
            Set(target,"positionInt",origin+IntVec3.East*3+IntVec3.North*3);
            Check.Equal(false,Fire(),"two solid corner cells block diagonal passage");
            map.edificeGrid.DeRegister(door);map.edificeGrid.DeRegister(corner);
            Set(target,"positionInt",origin+IntVec3.East*6);
            Check.Equal(origin,actor.Position,"refusals preserve position");
            Check.Equal(oldCount,(int)countField.GetValue(path)!,"refusals cannot train");
            Patch(typeof(Thing).GetMethod("DirtyMapMesh",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!,nameof(SkipDrawing));
            Patch(typeof(Pawn).GetMethod(nameof(Pawn.Notify_Teleported))!,nameof(Notify));
            Patch(typeof(VerbProperties).GetMethod(nameof(VerbProperties.AdjustedMeleeDamageAmount),[typeof(Verb),typeof(Pawn)])!,nameof(WeaponDamage));
            Patch(typeof(VerbProperties).GetMethod(nameof(VerbProperties.AdjustedArmorPenetration),[typeof(Verb),typeof(Pawn)])!,nameof(WeaponPenetration));
            Patch(typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,nameof(TakeDamage));
            Check.Same(weapon,actor.equipment.Primary,"equipped weapon");Check.Equal(true,weaponDef.IsMeleeWeapon,"melee weapon");
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            Check.NotNull(typeof(Verb_BladePassage).GetMethod("Strike",flags)!.Invoke(null,[actor]),"fixture supplies melee damage verb");
            var args=new object[]{actor,origin,target,IntVec3.Invalid};
            Check.Equal(true,(bool)typeof(Verb_BladePassage).GetMethod("Landing",flags)!.Invoke(null,args)!,
                $"landing (hostile {target.HostileTo(actor)}, fog {target.Position.Fogged(map)}, LoS {GenSight.LineOfSight(origin,target.Position,map)}, stand {(target.Position-IntVec3.East).StandableBy(map,actor)})");
            var rankField=path.GetType().GetField("BladePassageUpgrades",BindingFlags.Instance|BindingFlags.NonPublic)!;
            for(int rank=0;rank<3;rank++)
            {
                rankField.SetValue(path,rank);
                int range=6+rank*2;
                Check.Equal((float)range,verb.EffectiveRange,"targeting ring uses paid rank");
                Set(target,"positionInt",origin+IntVec3.East*range);
                Check.Equal(true,verb.CanHitTargetFrom(origin,target),"rank boundary valid");
                Set(target,"positionInt",origin+IntVec3.East*(range+1));
                Check.Equal(false,verb.CanHitTargetFrom(origin,target),"beyond rank rejected");
            }
            rankField.SetValue(path,0);Set(target,"positionInt",origin+IntVec3.East*6);
            Set(verb,"currentTarget",new LocalTargetInfo(target));verb.verbTracker=new VerbTracker(actor);
            actor.stances=new Pawn_StanceTracker(actor);actor.jobs=new Pawn_JobTracker(actor);
            Patch(typeof(Pawn).GetMethod(nameof(Pawn.Notify_UsedVerb))!,nameof(SkipDrawing));

            Patch(typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.Notify_UsedVerb))!,nameof(SkipDrawing));
            Patch(typeof(VerbProperties).GetMethod(nameof(VerbProperties.AdjustedCooldownTicks),[typeof(Verb),typeof(Pawn)])!,nameof(GenericRecovery));
            var safety = typeof(TorannMagic.Odyssey.ManualTravelSafety);
            Patch(safety.GetMethod("CanPay")!,nameof(Admit));
            Patch(safety.GetMethod("CanComplete")!,nameof(CheckLanding));
            Patch(safety.GetMethod("Settle")!,nameof(Charge));
            landingSafe=false; payments=0;
            Check.Equal(false,Fire(),"unconfirmed hazardous landing refused");
            Check.Equal(target.Position-IntVec3.East,evaluated,"environment checked at landing, not enemy cell");
            Check.Equal(0,payments,"rejected trip free of charge");
            landingSafe=true;
            verb.WarmupComplete();
            Check.Equal(1,payments,"successful passage settles exactly once");
            Check.Equal(60,((Stance_Cooldown)actor.stances.curStance).ticksLeft,"fixed recovery replaces generic cooldown");
            Check.Equal(target.Position-IntVec3.East,actor.Position,"stop at preceding cell");
            Check.Equal(1,teleports,"one path notification");
            Check.Same(target,recipient!,"chosen hostile receives contact");Check.Equal(18f,amount,"90% weapon damage");
            Check.Equal(false,Fire(),"re-entry now adjacent");
            Check.Equal(1,hits,"no second contact");Check.Equal(master ? oldCount : oldCount+1,(int)countField.GetValue(path)!,"trial stage credit");
            if (master)
            {
                Check.Equal(target.thingIDNumber,(int)path.GetType().GetField("BladeRouteOpponent",flagsState)!.GetValue(path)!,"passage starts master route");
                var second=PawnWithComps(false,false).pawn;
                second.kindDef=new PawnKindDef {race=second.def};second.mindState=new Pawn_MindState(second);
                second.guest=new Pawn_GuestTracker(second);second.thingIDNumber=7502;
                Set(second,"mapIndexOrState",(sbyte)index);Set(second,"positionInt",actor.Position+IntVec3.North);Set(second,"factionInt",hostile);
                void Contact(Pawn onto,float received)
                {
                    FleshCombatPatches.Prefix(melee,onto,out var scope);
                    try
                    {
                        var info=new DamageInfo(melee.verbProps.meleeDamageDef,20f,instigator:actor,weapon:weaponDef);
                        var result=new DamageWorker.DamageResult{totalDamageDealt=received};
                        FleshCombatPatches.Record(onto,info,result);
                        FleshCombatPatches.Postfix(melee,scope,ref result);
                    }
                    finally {FleshCombatPatches.Finalizer(null!,scope);}
                }
                Contact(target,10);Contact(second,0);
                Check.Equal((int)saved["BladeRoutes"],(int)path.GetType().GetField("BladeRoutes",flagsState)!.GetValue(path)!,"same target and blocked packet do not finish");
                Contact(second,10);Contact(second,10);
                Check.Equal((int)saved["BladeRoutes"]+1,(int)path.GetType().GetField("BladeRoutes",flagsState)!.GetValue(path)!,"second enemy finishes once");
            }
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(h,[id]);maps[index]=oldMap;Set(actor,"positionInt",oldPos);
            actor.stances=oldStances;actor.jobs=oldJobs;actor.equipment=oldGear;relations.SetValue(actor.Faction,originalRelations);countField.SetValue(path,oldCount);
            foreach(var entry in saved) Set(path,entry.Key,entry.Value);
            recipient=null;
        }
    }
}
