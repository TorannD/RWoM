using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperRescueTrialTests
{
    private static bool threat;
    public static bool Threat(ref bool __result) { __result = threat; return false; }
    public static bool RangeMultiplier(ref float __result) { __result = 1f; return false; }
    public static bool SkipFactionInit() => false;
    private static ThingDef Def() => (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));

    internal static void Verify(Pawn sniper)
    {
        const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Static;
        var path = ClassIdentity.Component(sniper).State.Find("rwom.sniper");
        var field = path.GetType().GetField("CoveredRescues",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var saved = field.GetValue(path);
        int Count() => (field.GetValue(path) as List<int>)?.Count ?? 0;
        var map = sniper.Map;
        var oldPawns = map.mapPawns; var oldWeather = map.weatherManager;
        var oldGear = sniper.equipment; var oldDraft = sniper.drafter;
        var mainThread = typeof(UnityData).GetField("mainThreadId",hidden)!; var previousThread = mainThread.GetValue(null);
        var oldJob = JobDefOf.Rescue; var pos = sniper.Position;
        var lib = Assembly.Load("0Harmony"); var ht = lib.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.sniper.rescue."+Guid.NewGuid(); var harmony = Activator.CreateInstance(ht,id)!;
        void Prefix(MethodBase method,string name)
        {
            var h = Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(SniperRescueTrialTests).GetMethod(name)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[method,h,null,null,null]);
        }
        try
        {
            mainThread.SetValue(null,Environment.CurrentManagedThreadId);
            Prefix(typeof(StatExtension).GetMethod("GetStatValue",[typeof(Thing),typeof(StatDef),typeof(bool),typeof(int)])!,nameof(RangeMultiplier));
            Prefix(typeof(MapPawns).GetMethod("EnsureFactionsListsInit",BindingFlags.NonPublic|BindingFlags.Instance)!,nameof(SkipFactionInit));
            Prefix(typeof(GenHostility).GetMethod("AnyHostileActiveThreatToPlayer")!,nameof(Threat));
            JobDefOf.Rescue = new JobDef {defName="Rescue"};
            Set(sniper,"positionInt",new IntVec3(10,0,10));
            map.mapPawns = new MapPawns(map);
            map.mapPawns.SpawnedPawnsInFaction(sniper.Faction).Add(sniper);
            map.weatherManager=(WeatherManager)RuntimeHelpers.GetUninitializedObject(typeof(WeatherManager));
            map.weatherManager.curWeather=new WeatherDef {maxRangeCap=-1};
            var cell = new IntVec3(20,0,10);
            Pawn Friend()
            {
                var ally=PawnWithComps(false,false).pawn;
                ally.kindDef = new PawnKindDef {race=ally.def}; ally.mindState = new Pawn_MindState(ally); ally.guest = new Pawn_GuestTracker(ally);
                ally.def.category=ThingCategory.Pawn;ally.def.size=new IntVec2(1,1);
                Set(ally,"mapIndexOrState",(sbyte)0);Set(ally,"positionInt",cell);Set(ally,"factionInt",sniper.Faction);
                return ally;
            }
            var rescuer=Friend();var casualty=Friend();casualty.thingIDNumber=8811;
            map.thingGrid.Register(casualty);
            var bed=new Building_Bed {def=Def()};bed.def.size=new IntVec2(1,1);
            Set(bed,"positionInt",cell);Set(bed,"mapIndexOrState",(sbyte)0);map.thingGrid.Register(bed);
            casualty.jobs=new Pawn_JobTracker(casualty);Set(casualty.jobs,"curJob",new Job {def=new JobDef {defName="LayDown"}});
            casualty.jobs.posture=PawnPosture.LayingInBed;
            Set(casualty.health,"healthState",PawnHealthState.Down);
            casualty.health.hediffSet.hediffs.Add(new Hediff_Injury {pawn=casualty,def=new HediffDef {defName="SniperTrialWound"}});
            var driver = new JobDriver_TakeToBed {pawn=rescuer,job=new Job {def=JobDefOf.Rescue,targetA=casualty,targetB=bed}};
            var hook=typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.GladiatorRescueTrial",true)!.GetMethod("Postfix",hidden)!;
            void Delivered() => ((IEnumerable<Toil>)hook.Invoke(null,[Array.Empty<Toil>(),driver])!).Single().initAction();
            sniper.drafter=new Pawn_DraftController(sniper);
            sniper.equipment=new Pawn_EquipmentTracker(sniper);
            Set(sniper.drafter,"draftedInt",true);
            threat=true;
            Delivered();Check.Equal(0, Count(),"unarmed sniper does not cover");
            var weaponDef=Def(); weaponDef.category=ThingCategory.Item;weaponDef.equipmentType=EquipmentType.Primary;weaponDef.comps=[];
            var gun=new ThingWithComps {def=weaponDef};
            var eq=new CompEquippable {parent=gun};eq.Initialize(new CompProperties {compClass=typeof(CompEquippable)});
            var shot=new Verb_Shoot {caster=sniper,verbProps=new VerbProperties {verbClass=typeof(Verb_Shoot),range=32,defaultProjectile=Def()},verbTracker=eq.verbTracker};
            Set(eq.verbTracker,"verbs",new List<Verb>{shot});Set(gun,"comps",new List<ThingComp>{eq});
            sniper.equipment.AddEquipment(gun);
            threat=false;Delivered();Check.Equal(0,Count(),"peacetime rescue does not count");
            threat=true;Set(sniper.drafter,"draftedInt",false);Delivered();Check.Equal(0,Count(),"idle sniper does not cover");
            Set(sniper.drafter,"draftedInt",true);
            driver.pawn=sniper;Delivered();Check.Equal(0,Count(),"rescuer cannot cover own rescue");driver.pawn=rescuer;
            Set(sniper,"positionInt",new IntVec3(60,0,10));Delivered();Check.Equal(0,Count(),"out of range");
            Set(sniper,"positionInt",new IntVec3(10,0,10));
            var wall = new Building {def=Def()};wall.def.fillPercent=1f;wall.def.size=new IntVec2(1,1);wall.def.building=new BuildingProperties();
            Set(wall,"positionInt",new IntVec3(15,0,10));Set(wall,"mapIndexOrState",(sbyte)0);map.edificeGrid.Register(wall);
            Delivered();Check.Equal(0,Count(),"obscured evacuation");
            map.edificeGrid.DeRegister(wall);
            Delivered();Check.Equal(1,Count(),"successful covered delivery");
            Delivered();Check.Equal(1,Count(),"repeated delivery of same casualty");
            map.thingGrid.Deregister(casualty);map.thingGrid.Deregister(bed);
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
            mainThread.SetValue(null,previousThread);
            field.SetValue(path,saved); Set(sniper,"positionInt",pos);map.mapPawns=oldPawns;map.weatherManager=oldWeather;
            sniper.equipment=oldGear;sniper.drafter=oldDraft;JobDefOf.Rescue=oldJob;threat=false;
        }
    }
}
