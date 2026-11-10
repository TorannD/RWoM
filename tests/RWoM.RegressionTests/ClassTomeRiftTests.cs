using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse.AI.Group;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class ClassTomeRaidTests
{
    static int spawnCount, lordSize, capacity;
    private sealed class MarkerThing : Thing
    {
        public bool closed;
        public override void Destroy(DestroyMode mode=DestroyMode.Vanish) {closed=true; Set(this,"mapIndexOrState",(sbyte)-1);}
    }
    public static bool AvailableCells(IntVec3 cell, ref bool __result)
    { __result = cell.z==10 && cell.x>10 && cell.x<=10+capacity; return false; }
    public static bool SpawnRiftPawn(Thing newThing, IntVec3 loc, Map map, ref Thing __result)
    {
        spawnCount++; Set(newThing,"positionInt",loc); Set(newThing,"mapIndexOrState",(sbyte)0);
        map.thingGrid.Register(newThing); __result=newThing; return false;
    }
    public static bool AssaultGroup(IEnumerable<Pawn> startingPawns) { lordSize=startingPawns.Count(); return false; }
    static void Arrival(RuntimeFixture fixture,bool obstructed)
    {
        using var game=new GameScope();
        var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        var parent=(MapParent)RuntimeHelpers.GetUninitializedObject(typeof(MapParent));
        map.info=new MapInfo{Size=new IntVec3(32,1,32),parent=parent};
        Current.Game.Maps.Add(map);
        map.cellIndices=new CellIndices(map);map.thingGrid=new ThingGrid(map);
        Set(map,"wasSpawnedViaGravShipLanding",true);
        var player=new Faction{def=new FactionDef{isPlayer=true}};
        var enemy=new Faction{def=new FactionDef()};
        Set(Find.FactionManager,"ofPlayer",player);
        Set(enemy,"relations",new List<FactionRelation>{new(){other=player,kind=FactionRelationKind.Hostile}});
        Set(player,"relations",new List<FactionRelation>{new(){other=enemy,kind=FactionRelationKind.Hostile}});
        var group=new List<Pawn>();
        for(int i=0;i<3;i++)
        {
            var pawn=GrimoireProgressionTests.PawnWithComps(false,false).pawn; pawn.def.size=new IntVec2(1,1);
            Set(pawn,"factionInt",enemy);pawn.kindDef=new PawnKindDef{race=pawn.def};
            pawn.mindState=new Verse.AI.Pawn_MindState(pawn); pawn.guest=new Pawn_GuestTracker(pawn); group.Add(pawn);
        }
        var part=new QuestPart_ClassTomeRaid();
        part.Configure(group[0],"rwom.ice",Book().def,parent,group,"accept");
        var marker=new MarkerThing{def=Book().def};
        Set(marker,"mapIndexOrState",(sbyte)0); Set(marker,"positionInt",new IntVec3(10,0,10));
        Set(part,"ElementalRift",true);Set(part,"phase",QuestPart_ClassTomeRaid.RaidPhase.Preparing);
        Set(part,"arrivalTick",0);Set(part,"riftMarker",marker);
        var asm=Assembly.Load("0Harmony"); var ht=asm.GetType("HarmonyLib.Harmony")!;
        string id="rwom.tome.rift."+Guid.NewGuid(); var harmony=Activator.CreateInstance(ht,id)!;
        void Patch(MethodInfo m,string name)
        {
            var prefix=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,typeof(ClassTomeRaidTests).GetMethod(name)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[m,prefix,null,null,null]);
        }
        try
        {
            Patch(typeof(ClassTomeQuestOffer).Assembly.GetType("TorannMagic.ClassProgression.TomeElementalRift")!.GetMethod("Empty",BindingFlags.Static|BindingFlags.NonPublic)!,nameof(AvailableCells));
            Patch(typeof(GenSpawn).GetMethod("Spawn",[typeof(Thing),typeof(IntVec3),typeof(Map),typeof(WipeMode)])!,nameof(SpawnRiftPawn));
            Patch(typeof(LordMaker).GetMethod("MakeNewLord")!,nameof(AssaultGroup));
            spawnCount=lordSize=0;capacity=obstructed?2:3;
            Set(Find.TickManager,"ticksGameInt",1);
            part.QuestPartTick();
            Check.Equal(true,marker.closed,"rift closes exactly once on resolution");
            Check.Equal(obstructed?0:3,spawnCount,"finite original roster only");
            Check.Equal(obstructed?0:3,lordSize,"all and only arrived pawns join assault");
            Check.Equal(obstructed?QuestPart_ClassTomeRaid.RaidPhase.Failed:QuestPart_ClassTomeRaid.RaidPhase.Active,part.Phase,"obstruction cannot award a win");
            if(!obstructed) Check.Equal(3,group.Select(p=>p.Position).Distinct().Count(),"distinct arrival positions");
            part.QuestPartTick();Check.Equal(obstructed?0:3,spawnCount,"no recurring portal spawn");
            Check.Equal(0,part.GetDirectlyHeldThings().Count,"appearance alone cannot pay");
        }
        finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
    }

    static IEnumerable<TestCase> RiftCases()
    {
        yield return new("ClassTome.RiftSingleFiniteArrival",f=>Arrival(f,false));
        yield return new("ClassTome.RiftObstructionNeverPays",f=>Arrival(f,true));
        yield return new("ClassTome.RiftBudgetAndCounterplay", f => {
            var original = DefDatabase<PawnKindDef>.AllDefsListForReading.ToArray();
            var api = typeof(ClassTomeQuestOffer).Assembly.GetType("TorannMagic.ClassProgression.TomeElementalRift",true)!;
            const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic;
            float Budget(float value) => (float)api.GetMethod("Budget",hidden)!.Invoke(null,[value])!;
            List<PawnKindDef> Options(string id,float budget) => (List<PawnKindDef>)api.GetMethod("Kinds",hidden)!.Invoke(null,[id,budget])!;
            try
            {
                var xml=XDocument.Load(Path.Combine(f.Repository,"v1.7","Defs","PawnDefs","Races_Elemental.xml"));
                foreach(var element in new[]{"Fire","Water","Earth","Wind"})
                {
                    var name="TM_Lesser"+element+"_Elemental";
                    var def=xml.Root!.Elements().Single(e=>(string?)e.Element("defName")==name);
                    var race=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                    race.thingClass=typeof(TMPawnSummoned);
                    DefDatabase<PawnKindDef>.Add(new PawnKindDef{defName=name,race=race,combatPower=(float)def.Element("combatPower")!});
                }
                Check.Equal(0f,Budget(float.NaN),"invalid threat");
                Check.Equal(2400f,Budget(99999),"finite budget cap");
                Check.Equal(0,Options("rwom.priest",Budget(100)).Count,"no forced minimum fight");
                foreach(var profile in new[]{("rwom.fire","Fire"),("rwom.ice","Water"),("rwom.lightning","Wind"),("rwom.geomancer","Earth")})
                {
                    var kinds=Options(profile.Item1,800);
                    Check.Equal(3,kinds.Count,"alternative elemental opposition");
                    Check.Equal(false,kinds.Any(k=>k.defName.Contains(profile.Item2)), "same elemental family excluded");
                }
                Check.Equal(4,Options("rwom.priest",800).Count,"neutral discipline can meet any element");
                var marker=XDocument.Load(Path.Combine(f.Repository,"v1.7","Defs","ThingDefs","TM_MasterTomeRift.xml")).Root!.Elements().Single();
                Check.Equal("Thing",(string?)marker.Element("thingClass"),"quest rift cannot generate waves independently");
            }
            finally { DefDatabase<PawnKindDef>.Clear();foreach(var def in original) DefDatabase<PawnKindDef>.Add(def); }
        });
        yield return new("ClassTome.RiftSaveAndMissingMapFail", f=>{
            using var game=new GameScope();
            var part=new QuestPart_ClassTomeRaid();
            Set(part,"ElementalRift",true);
            Set(part,"phase",QuestPart_ClassTomeRaid.RaidPhase.Preparing);
            Set(part,"arrivalTick",0);
            var quest=new Quest{id=8111};quest.AddPart(part);
            string path=Path.Combine(f.WorkingDirectory,"rift.xml");
            try {Scribe.saver.InitSaving(path,"test");Scribe_Deep.Look(ref quest,"quest");Scribe.saver.FinalizeSaving();} finally {Scribe.ForceStop();}
            quest=null!;
            try {Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref quest,"quest");Scribe.loader.FinalizeLoading();} finally {Scribe.ForceStop();}
            var loaded=(QuestPart_ClassTomeRaid)quest.PartsListForReading.Single();
            Check.Equal(true,(bool)Get(loaded,"ElementalRift"),"old raid mode cannot replace saved rift");
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Preparing,loaded.Phase,"still awaiting arrival");
            // Detach lifecycle notifications from the isolated quest manager, preserving native part state.
            loaded.quest=null;
            loaded.QuestPartTick();
            Check.Equal(QuestPart_ClassTomeRaid.RaidPhase.Failed,loaded.Phase,"missing map before rift arrival is failure");
            Check.Equal(0,loaded.GetDirectlyHeldThings().Count,"no payment for missing map");
        });
    }
}
