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

internal static class RangerHuntTests
{
    private static Thing? testGun;
    public static bool WeaponStat(Thing thing, ref float __result)
    {
        if (thing != testGun && thing != probeShooter) return true;
        __result = 1f;
        return false;
    }
    private static Thing? probeShooter;
    public static bool DrawPos(Pawn __instance, ref UnityEngine.Vector3 __result)
    {
        if (__instance != probeShooter) return true;
        __result=__instance.Position.ToVector3Shifted();
        return false;
    }
    public static bool Spawn(ThingDef def, IntVec3 loc, Map map, ref Thing __result)
    {
        if (def.defName != "TM_RangerHuntArrow") return true;
        var shot=(RangerHuntArrow)ThingMaker.MakeThing(def);
        Set(shot,"mapIndexOrState",(sbyte)0);
        Set(shot,"positionInt",loc);
        launches.Add(shot);__result=shot;
        return false;
    }
    private static List<RangerHuntArrow> launches=[];
    private static int shots => launches.Count;


    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight comp)
    {
        var path = ClassIdentity.Component(pawn).State.Find("rwom.ranger")!;
        var impl=BindingFlags.NonPublic|BindingFlags.Instance;
        var ability = DefDatabase<TMAbilityDef>.GetNamed("TM_RangerBreakthrough");
        var pet=comp.bondedPet;
        var petPos=pet.Position;
        var oldType=ability.MainVerb.verbClass;
        var oldMarkerDefs=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var oldRecords=DefDatabase<RecordDef>.AllDefsListForReading.ToArray();
        var oldShotRecord=RecordDefOf.ShotsFired;
        var oldTracker=pawn.records;
        var oldReady=(int)path.GetType().GetField("RangerHuntReadyAt",impl)!.GetValue(path)!;
        var oldGear=pawn.equipment;
        var oldWeather=pawn.Map.weatherManager;
        var oldDraft=pawn.jobs;
        var oldStaminaDef=TorannMagicDefOf.TM_Stamina;
        var oldJobs=DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        var harmonyType=Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        string id="rwom.regression.hunt."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(harmonyType,id)!;
        var pos=pawn.Position;
        var oldFactions=(List<FactionRelation>)typeof(Faction).GetField("relations",impl)!.GetValue(pawn.Faction)!;
        try
        {
            Set(Current.Game, "questManager", new QuestManager());
            ability.MainVerb.verbClass=typeof(Verb_RangerBreakthrough);
            var owner=comp.AbilityData.Powers.OfType<RangerHuntAbility>().Single();
            Set(owner,"verb",null!);
            var gunDef=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            gunDef.defName="TestBow";gunDef.tools=[new Tool{power=3}];gunDef.category=ThingCategory.Item;gunDef.equipmentType=EquipmentType.Primary;gunDef.comps=[];
            var gun=new ThingWithComps{def=gunDef};
            var eq=new CompEquippable{parent=gun};
            eq.Initialize(new CompProperties{compClass=typeof(CompEquippable)});
            var ammo=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            ammo.comps=[];ammo.thingClass=typeof(Bullet);ammo.projectile=new ProjectileProperties{damageDef=new DamageDef{defName="Arrow"},speed=55};
            Set(ammo.projectile,"damageAmountBase",20);
            var shot=new Verb_Shoot{caster=pawn,verbProps=new VerbProperties{range=50,verbClass=typeof(Verb_Shoot),defaultProjectile=ammo},verbTracker=eq.verbTracker};
            Set(eq.verbTracker,"verbs",new List<Verb>{shot});Set(gun,"comps",new List<ThingComp>{eq});
            pawn.equipment=new Pawn_EquipmentTracker(pawn);pawn.equipment.AddEquipment(gun);
            pawn.Map.coverGrid=new CoverGrid(pawn.Map);
            pawn.Map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
            pawn.Map.weatherManager.curWeather=new WeatherDef{maxRangeCap=-1};
            Set(pawn,"positionInt",new IntVec3(10,0,10));Set(pet,"positionInt",pawn.Position+IntVec3.North);
            var faction=new Faction{def=new FactionDef()};
            Set(faction,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=faction,kind=FactionRelationKind.Hostile}});
            var enemy=PawnWithComps(false,false).pawn;
            enemy.kindDef=new PawnKindDef{race=enemy.def};enemy.mindState=new Pawn_MindState(enemy);
            enemy.guest=new Pawn_GuestTracker(enemy);enemy.def.category=ThingCategory.Pawn;enemy.def.size=new IntVec2(1,1);
            Set(enemy,"mapIndexOrState",(sbyte)0);Set(enemy,"factionInt",faction);Set(enemy,"positionInt",pawn.Position+IntVec3.East*12);
            enemy.thingIDNumber=37214;pawn.Map.thingGrid.Register(enemy);
            var alternate=PawnWithComps(false,false).pawn;
            alternate.kindDef=new PawnKindDef{race=alternate.def};alternate.mindState=new Pawn_MindState(alternate);
            alternate.guest=new Pawn_GuestTracker(alternate);alternate.def.category=ThingCategory.Pawn;alternate.def.size=new IntVec2(1,1);
            alternate.thingIDNumber=37215;Set(alternate,"mapIndexOrState",(sbyte)0);Set(alternate,"factionInt",faction);
            Set(alternate,"positionInt",enemy.Position+IntVec3.East);pawn.Map.thingGrid.Register(alternate);
            var bullet=new RangerHuntArrow{def=ammo};
            bullet.Launch(pawn,pawn.Position.ToVector3Shifted(),enemy,enemy,ProjectileHitFlags.IntendedTarget);
            Check.Equal(14,bullet.DamageAmount,"70% bow damage");
            testGun=gun;probeShooter=pawn;
            var harmonyMethod=Assembly.Load("0Harmony").GetType("HarmonyLib.HarmonyMethod")!;
            var stat=typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!;
            harmonyType.GetMethod("Patch")!.Invoke(harmony,[stat,Activator.CreateInstance(harmonyMethod,typeof(RangerHuntTests).GetMethod(nameof(WeaponStat))!),null,null,null]);
            var gunVerb=(Verb_RangerBreakthrough)owner.Verb;
            ammo.projectile.damageDef.defName="Gunshot";
            Check.Equal(false,gunVerb.CanHitTargetFrom(pawn.Position,enemy),"ordinary rifle bullet cannot start hunt");
            ammo.projectile.damageDef.defName="Arrow";
            Check.Equal(30f,gunVerb.EffectiveRange,"gun range capped");
            Set(enemy,"positionInt",pawn.Position+IntVec3.East*31);
            Check.Equal(false,gunVerb.CanHitTargetFrom(pawn.Position,enemy),"beyond 30 blocked");
            Set(enemy,"positionInt",pawn.Position+IntVec3.East*12);
            if(DefDatabase<JobDef>.GetNamedSilentFail("TM_RangerBreakthroughCast")==null)
                DefDatabase<JobDef>.Add(new JobDef{defName="TM_RangerBreakthroughCast",driverClass=typeof(JobDriver_RangerBreakthrough)});
            if(comp.Stamina==null)
            {
                TorannMagicDefOf.TM_Stamina=new NeedDef{defName="OpeningStamina",needClass=typeof(Need_Stamina)};
                pawn.needs.AllNeeds.Add(new Need_Stamina(pawn){def=TorannMagicDefOf.TM_Stamina});
            }
            Set(pawn.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
            comp.Stamina.CurLevelPercentage=1;
            float oldMax=comp.maxSP;
            var settings=TorannMagic.ModOptions.Settings.Instance;
            var startKind=pawn.kindDef;
            pawn.kindDef??=new PawnKindDef{race=pawn.def};
            TorannMagic.ModOptions.Settings.Instance??=new TorannMagic.ModOptions.Settings();
            comp.maxSP=.7f;
            try
            {
                comp.Stamina.GainNeed(1f);
                Check.Equal(comp.Stamina.MaxLevel,comp.Stamina.CurLevel,"bond upkeep must clamp excess stamina only once");
            }
            finally
            {
                pawn.kindDef=startKind;
                TorannMagic.ModOptions.Settings.Instance=settings;
                comp.maxSP=oldMax; comp.Stamina.CurLevelPercentage=1;
            }
            var job=owner.GetJob(AbilityContext.Player,enemy);
            var driver=new JobDriver_RangerBreakthrough{pawn=pawn,job=job};
            Check.Equal(true,driver.TryMakePreToilReservations(false),"normal readiness with ordinary gun and intended target");
            object? Call(string method,params object[] args)=>typeof(JobDriver_RangerBreakthrough).GetMethod(method,impl)!.Invoke(driver,args);
            Call("Begin");
            pawn.jobs=new Pawn_JobTracker(pawn);
            Set(pawn.jobs,"curDriver",driver);
            float Damage(float amount)
            {
                var hit=new DamageInfo(new DamageDef{defName="HuntHit"},amount,instigator:enemy,weapon:gunDef);
                ClassIdentity.Component(pawn).PostPreApplyDamage(ref hit,out _);
                return hit.Amount;
            }
            Check.Equal(30f,Damage(30),"no protection during unpaid preparation");
            int now=Find.TickManager.TicksGame;
            Call("Interrupt");Call("Interrupt");
            Check.Equal(true,Math.Abs(comp.Stamina.CurLevelPercentage-.94f)<.001f,"interrupted preparation costs 6% once");
            Check.Equal(now+6000,(int)path.GetType().GetField("RangerHuntReadyAt",impl)!.GetValue(path)!,"short cooldown");
            Set(path,"RangerHuntReadyAt",0);owner.CooldownTicksLeft=0;comp.Stamina.CurLevelPercentage=1;
            driver=new JobDriver_RangerBreakthrough{pawn=pawn,job=job};
            Set(pawn.jobs,"curDriver",driver);Call("Begin");
            var marker=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            marker.defName="TM_RangerHuntArrow";marker.thingClass=typeof(RangerHuntArrow);marker.projectile=ammo.projectile;
            DefDatabase<ThingDef>.Add(marker);
            pawn.Map.gasGrid=new GasGrid(pawn.Map);pawn.Map.roofGrid=new RoofGrid(pawn.Map);
            pawn.Map.weatherManager.lastWeather=pawn.Map.weatherManager.curWeather;
            enemy.ageTracker=new Pawn_AgeTracker(enemy);
            enemy.def.race.lifeStageAges=[new LifeStageAge{minAge=0,def=new LifeStageDef{bodySizeFactor=1f}}];
            Set(enemy.ageTracker,"cachedLifeStageIndex",0);
            alternate.ageTracker=new Pawn_AgeTracker(alternate);alternate.def.race.lifeStageAges=enemy.def.race.lifeStageAges;Set(alternate.ageTracker,"cachedLifeStageIndex",0);
            RecordDefOf.ShotsFired = new RecordDef{defName="ShotsFired",type=RecordType.Int};
            DefDatabase<RecordDef>.Add(RecordDefOf.ShotsFired);
            pawn.records=new Pawn_RecordsTracker(pawn);
            var hm=Assembly.Load("0Harmony").GetType("HarmonyLib.HarmonyMethod")!;
            var prefix=Activator.CreateInstance(hm,typeof(RangerHuntTests).GetMethod(nameof(Spawn))!)!;
            harmonyType.GetMethod("Patch")!.Invoke(harmony,[typeof(GenSpawn).GetMethod("Spawn",[typeof(ThingDef),typeof(IntVec3),typeof(Map),typeof(WipeMode)]),prefix,null,null,null]);
            harmonyType.GetMethod("Patch")!.Invoke(harmony,[typeof(Pawn).GetProperty("DrawPos")!.GetMethod!,Activator.CreateInstance(hm,typeof(RangerHuntTests).GetMethod(nameof(DrawPos))!),null,null,null]);
            launches.Clear();
            Call("Shoot",0);Call("Shoot",0);
            Check.Equal(1,shots,"duplicate first entry cannot fire twice");
            Check.Equal(now+120000,(int)path.GetType().GetField("RangerHuntReadyAt",impl)!.GetValue(path)!,"paid cooldown");
            Check.Equal(true,Math.Abs(comp.Stamina.CurLevelPercentage-.4f)<.001f,"full payment once");
            Check.Equal(5f,Damage(30),"first weapon hit capped at 25");
            Check.Equal(.8f,ClassPassiveEffects.StatFactor(pawn,StatDefOf.MoveSpeed),"active movement penalty");
            Set(pet,"positionInt",pawn.Position+IntVec3.North*13);
            Check.Equal(false,(bool)Call("Together")!,"pet separation breaks sequence");
            Check.Equal(30f,Damage(30),"separated pet cannot protect");
            Set(pet,"positionInt",pawn.Position+IntVec3.North);
            Set(pet.health,"healthState",PawnHealthState.Down);
            Check.Equal(false,(bool)Call("Together")!,"downed pet breaks sequence");
            Set(pet.health,"healthState",PawnHealthState.Mobile);
            var wall=new Building{def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))};
            wall.def.fillPercent=1f;wall.def.size=new IntVec2(1,1);wall.def.building=new BuildingProperties();
            Set(wall,"positionInt",pawn.Position+IntVec3.East*6);Set(wall,"mapIndexOrState",(sbyte)0);
            pawn.Map.edificeGrid.Register(wall);
            Check.Equal(false,gunVerb.CanHitTargetFrom(pawn.Position,enemy),"native vision obstruction blocks the next arrow");
            pawn.Map.edificeGrid.DeRegister(wall);
            var file=Path.Combine(f.WorkingDirectory,"ranger-hunt-job.xml");
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref driver,"driver");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            driver=null!;
            try
            {
                Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref driver,"driver");driver.pawn=pawn;driver.job=job;
                var directory=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",impl)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(enemy);directory.RegisterLoaded(alternate);Scribe.loader.FinalizeLoading();
            }
            finally{Scribe.ForceStop();}
            Set(pawn.jobs,"curDriver",driver);
            Check.Equal(0f,Damage(10),"one remaining interception after load");
            Check.Equal(30f,Damage(30),"only two blocks");
            Call("Shoot",0);Call("Shoot",1);
            Set(alternate.health,"healthState",PawnHealthState.Down);
            for(int i=2;i<6;i++) Call("Shoot",i);
            Call("Shoot",5);Call("Interrupt");
            Check.Same(enemy,launches.First().intendedTarget.Thing,"player target first");
            Check.Same(alternate,launches[1].intendedTarget.Thing,"second marked target saved");
            Check.Equal(true,launches.Skip(2).All(r=>r.intendedTarget.Thing==enemy),"downed marks skipped");
            Check.Equal(true,launches.All(r=>r.DamageAmount==14 && r.def==ammo),"all native arrows retain bow ammunition and damage");
            Check.Equal(6,shots,"saved sequence emits six individual arrows");
            Check.Equal(true,Math.Abs(comp.Stamina.CurLevelPercentage-.4f)<.001f,"stamina debited once");
        }
        finally
        {
            testGun=null;probeShooter=null;launches.Clear();
            Set(pet,"positionInt",petPos);Set(path,"RangerHuntReadyAt",oldReady);
            pawn.records=oldTracker;RecordDefOf.ShotsFired=oldShotRecord;
            DefDatabase<RecordDef>.Clear();foreach(var d in oldRecords) DefDatabase<RecordDef>.Add(d);
            DefDatabase<ThingDef>.Clear();foreach(var d in oldMarkerDefs) DefDatabase<ThingDef>.Add(d);
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            DefDatabase<JobDef>.Clear();foreach(var d in oldJobs) DefDatabase<JobDef>.Add(d);
            Set(pawn.Faction,"relations",oldFactions);Set(pawn,"positionInt",pos);
            ability.MainVerb.verbClass=oldType;pawn.equipment=oldGear;pawn.jobs=oldDraft;
            pawn.Map.weatherManager=oldWeather;TorannMagicDefOf.TM_Stamina=oldStaminaDef;
        }
    }
}
