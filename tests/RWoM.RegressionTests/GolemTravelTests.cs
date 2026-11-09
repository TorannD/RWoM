using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.Golems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

// Player report: a gravship flight destroys the old ship map, and a golem rest
// place bound to it reset on landing. The dormant state must self-heal to the
// golem's current spot when its dormant map is dead, while an assignment on a
// live map (the home base) is preserved untouched.
internal static class GolemTravelTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GolemTravel.DeadDormantMapHealsToCurrentSpot", DeadMapHeals);
        yield return new("GolemTravel.LiveDormantMapPreserved", LiveMapPreserved);
    }

    private static CompGolem Golem(Map map, IntVec3 position, Map dormantMap, IntVec3 dormantPosition)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "GolemTravelPawn"; def.category = ThingCategory.Pawn;
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = def };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.jobs = new Pawn_JobTracker(pawn);
        pawn.needs = new Pawn_NeedsTracker(pawn);
        var rest = new Need_Rest(pawn) { def = new NeedDef { defName = "GolemTravelRest" } };
        pawn.needs.rest = rest;
        pawn.needs.rest.CurLevel = 1f;
        FleshStatTests.Set(pawn, "comps", new List<ThingComp>());
        FleshStatTests.Set(pawn, "positionInt", position);
        FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)0);
        map.thingGrid.Register(pawn);
        var comp = new CompGolem { parent = pawn };
        comp.age = 1;
        FleshStatTests.Set(comp, "initialized", true);
        FleshStatTests.Set(comp, "nextActionTick", int.MaxValue);
        comp.dormantMap = dormantMap;
        comp.dormantPosition = dormantPosition;
        var energyDef = new NeedDef { defName = "TM_GolemEnergy", needClass = typeof(Need_GolemEnergy) };
        var energy = (Need_GolemEnergy)RuntimeHelpers.GetUninitializedObject(typeof(Need_GolemEnergy));
        energy.def = energyDef; energy.maxEnergy = 1000f; energy.energyEfficiency = .5f;
        pawn.needs.AllNeeds.Add(energy);
        energy.CurLevel = 500f; // above the shutdown threshold: the heal is what's under test
        NeedDef oldNeed = TorannMagicDefOf.TM_GolemEnergy;
        TorannMagicDefOf.TM_GolemEnergy = energyDef;
        comp.CompTick();
        TorannMagicDefOf.TM_GolemEnergy = oldNeed;
        return comp;
    }

    private static void DeadMapHeals(RuntimeFixture _)
    {
        var oldGame = Current.Game;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            FleshStatTests.Set(Current.Game, "tickManager", new TickManager());
            var map = Blank<Map>(); map.info = new MapInfo { Size = new IntVec3(24, 1, 24) };
            FleshStatTests.Set(Current.Game, "maps", new List<Map> { map });
            map.cellIndices = new CellIndices(map);
            map.thingGrid = new ThingGrid(map);
            var pathGrid = new PathGrid(map, new PathGridDef { defName = "GolemTravelPath" });
            map.pathing = Blank<Pathing>();
            var context = new PathingContext(map, pathGrid);
            FleshStatTests.Set(map.pathing, "normal", context);
            FleshStatTests.Set(map.pathing, "fenceBlocked", context);
            var home = Blank<Map>(); home.info = new MapInfo { Size = new IntVec3(24, 1, 24) };
            // home is NOT in Find.Maps: the flight destroyed the old ship map
            var comp = Golem(map, new IntVec3(6, 0, 6), home, new IntVec3(3, 0, 3));
            Check.Same(map, comp.dormantMap, "a dead dormant map re-points to the current map");
            Check.Equal(new IntVec3(6, 0, 6), comp.dormantPosition, "the rest place heals to the golem's current spot");
        }
        finally { Current.Game = oldGame; }
    }

    private static void LiveMapPreserved(RuntimeFixture _)
    {
        var oldGame = Current.Game;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            FleshStatTests.Set(Current.Game, "tickManager", new TickManager());
            var map = Blank<Map>(); map.info = new MapInfo { Size = new IntVec3(24, 1, 24) };
            FleshStatTests.Set(Current.Game, "maps", new List<Map> { map });
            map.cellIndices = new CellIndices(map);
            map.thingGrid = new ThingGrid(map);
            var pathGrid = new PathGrid(map, new PathGridDef { defName = "GolemTravelPathLive" });
            map.pathing = Blank<Pathing>();
            var context = new PathingContext(map, pathGrid);
            FleshStatTests.Set(map.pathing, "normal", context);
            FleshStatTests.Set(map.pathing, "fenceBlocked", context);
            var comp = Golem(map, new IntVec3(6, 0, 6), map, new IntVec3(18, 0, 18));
            Check.Same(map, comp.dormantMap, "a live dormant map is preserved");
            Check.Equal(new IntVec3(18, 0, 18), comp.dormantPosition, "the player's assignment on a live map is untouched");
        }
        finally { Current.Game = oldGame; }
    }

    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
}
