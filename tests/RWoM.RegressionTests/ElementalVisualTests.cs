using System.Runtime.CompilerServices;
using System.Reflection;
using TorannMagic;
using UnityEngine;
using Verse;

namespace RWoM.RegressionTests;

internal static class ElementalVisualTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Performance.ReverseShield.ProjectileSnapshot", ReverseShieldSnapshot);
        yield return new("Elemental.Rift.OrphanedWaveAndNearestLord", RiftWaveLord);
        yield return new("Elemental.Debug.ForcedRiftFamiliesAndRandomDefault", DebugRifts);
        yield return new("Elemental.Runtime.ExactRaceScope", Scope);
        yield return new("Elemental.Runtime.BreathingIsLocalAndTickDeterministic", Breathing);
        yield return new("Elemental.Runtime.NoAnimationForOffMapOrUnrelatedPawn", Gates);
        yield return new("Elemental.Runtime.NativeAnimalRenderingContract", NativeContract);
        yield return new("Elemental.Runtime.LiveMapVisibilityGates", LiveMapGates);
    }
    private static void RiftWaveLord(RuntimeFixture f)
    {
        var oldGame = Current.Game;
        try
        {
        Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        FleshStatTests.Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
        var find = typeof(MagicData).Assembly.GetType("TorannMagic.ElementalRiftLordUtility")!.GetMethod("FindExisting", BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (string family in new[] { "Earth", "Fire", "Water", "Wind" })
        foreach (string rank in new[] { "Lesser", "", "Greater" })
        {
            Pawn PawnAt(int x)
            {
                var p = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
                p.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
                p.def.defName = "TM_" + rank + family + "_ElementalR";
                p.thingIDNumber = 90000 + x;
                FleshStatTests.Set(p, "positionInt", new IntVec3(x, 0, 10));
                FleshStatTests.Set(p, "mapIndexOrState", (sbyte)0);
                return p;
            }
            var incoming = PawnAt(10); var orphan = PawnAt(11); var near = PawnAt(12); var far = PawnAt(30);
            object? Find(params Pawn[] pawns) => find.Invoke(null, [incoming, pawns]);
            Check.Equal<object?>(null, Find(incoming), "first wave has no existing group");
            Check.Equal<object?>(null, Find(incoming, orphan), "second wave with orphaned survivors must allow new group creation");
            var nearLord = (Verse.AI.Group.Lord)RuntimeHelpers.GetUninitializedObject(typeof(Verse.AI.Group.Lord));
            var farLord = (Verse.AI.Group.Lord)RuntimeHelpers.GetUninitializedObject(typeof(Verse.AI.Group.Lord));
            FleshStatTests.Set(near, "lord", nearLord); FleshStatTests.Set(far, "lord", farLord);
            Check.Same(nearLord, Find(incoming, orphan, far, near)!, "nearest existing group is reused for every element and rank");
            FleshStatTests.Set(near, "lord", null!); FleshStatTests.Set(far, "lord", null!);
            Check.Equal<object?>(null, Find(incoming, orphan, far, near), "ended previous-wave groups do not throw");
        }
        }
        finally { Current.Game = oldGame; }
    }
    private static void ReverseShieldSnapshot(RuntimeFixture f)
    {
        var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.listerThings = new ListerThings(ListerThingsUse.Global);
        var first = (Projectile)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Bullet));
        var second = (Projectile)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Bullet));
        var lists = (List<Thing>[])typeof(ListerThings).GetField("listsByGroup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(map.listerThings)!;
        lists[(int)ThingRequestGroup.Projectile] = [first, second];
        // Large unrelated map contents must never enter the candidate snapshot.
        for (int i = 0; i < 20000; i++) map.listerThings.AllThings.Add(new Thing());
        List<Thing> Snapshot() => (List<Thing>)typeof(MagicData).Assembly.GetType("TorannMagic.ReverseShieldProjectiles")!.GetMethod("Snapshot", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [map])!;
        var snapshot = Snapshot();
        Check.Equal(2, snapshot.Count, "only indexed projectiles are candidates");
        Check.Same(first, snapshot[0], "native order retained");
        lists[(int)ThingRequestGroup.Projectile].RemoveAt(0);
        Check.Equal(2, snapshot.Count, "interception cannot mutate in-flight snapshot");
        Check.Same(second, Snapshot()[0], "next pulse reads fresh registry");
    }
    private static void DebugRifts(RuntimeFixture f)
    {
        var assembly = Assembly.Load("0Harmony");
        var harmonyType = assembly.GetType("HarmonyLib.Harmony")!;
        const string id = "rwom.tests.elemental-rift-materials";
        var patches = Activator.CreateInstance(harmonyType, id)!;
        try
        {
            foreach (var method in typeof(MaterialPool).GetMethods().Where(m => m.Name == "MatFrom"))
            {
                var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ElementalVisualTests).GetMethod(nameof(SkipRiftMaterial), BindingFlags.NonPublic | BindingFlags.Static)!)!;
                harmonyType.GetMethod("Patch")!.Invoke(patches, [method, hook, null, null, null]);
            }
            RuntimeHelpers.RunClassConstructor(typeof(Building_TMElementalRift).TypeHandle);
        }
        finally { harmonyType.GetMethod("UnpatchAll")!.Invoke(patches, [id]); }
        var rift = new Building_TMElementalRift();
        var rnd = typeof(Building_TMElementalRift).GetField("rnd", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var selected = typeof(Building_TMElementalRift).GetField("debugElement", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check.Equal(-1, (int)selected.GetValue(rift)!, "ordinary portals retain random selection");
        for (int family = 0; family < 4; family++)
        {
            rift.SetDebugElement(family);
            for (int repeat = 0; repeat < 8; repeat++)
            {
                rift.DetermineElementalType();
                Check.Equal(family * 2, (int)rnd.GetValue(rift)!, "selection survives native initialization and repeat calls");
            }
        }
        foreach (int invalid in new[] { -1, 4 })
        {
            bool rejected = false;
            try { rift.SetDebugElement(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check.Equal(true, rejected, "invalid families cannot enter spawn logic");
        }
        foreach (string family in new[] { "Earth", "Fire", "Water", "Air" })
        {
            var method = typeof(TorannMagic.ModOptions.TM_DebugTools).GetMethod("ElementalRift" + family)!;
            Check.Equal(true, method.GetCustomAttributes().Any(a => a.GetType().Name == "DebugActionAttribute"), "all four map tools registered");
        }
    }
    private static bool SkipRiftMaterial() => false;
    private static void Scope(RuntimeFixture f)
    {
        string[] families = { "Earth", "Fire", "Water", "Wind" };
        for (int i = 0; i < families.Length; i++)
            foreach (string rank in new[] { "Lesser", "", "Greater" })
                Check.Equal(i, ElementalVisuals.Family("TM_" + rank + families[i] + "_ElementalR"), "all three ranks retain their family");
        foreach (string name in new[] { "TM_MinionR", "TM_Earth_Elemental", "Other_TM_Earth_ElementalR", "TM_Earth_ElementalRExtra", "Human" })
            Check.Equal(-1, ElementalVisuals.Family(name), "exact race whitelist excludes unrelated pawns");
    }
    private static void Breathing(RuntimeFixture f)
    {
        float[] periods = { 5.8f, 4.3f, 5.1f, 4.7f };
        for (int family = 0; family < 4; family++)
        {
            float inhale = ElementalVisuals.Inhale(family, 37.5f, 91);
            Check.Equal(inhale, ElementalVisuals.Inhale(family, 37.5f, 91), "same game tick freezes animation");
            Check.Equal(true, Math.Abs(ElementalVisuals.Inhale(family, 0, 0) - ElementalVisuals.Inhale(family, periods[family], 0)) < .00001f, "full breathing period returns to initial phase");
            Vector3 chest = new(.5f, 0, .129f), tail = new(.5f, 0, -.49f);
            Vector3 expanded = ElementalVisuals.Breathe(chest, family, 1), tip = ElementalVisuals.Breathe(tail, family, 1);
            Check.Equal(true, expanded.x > chest.x && expanded.x <= .518f, "chest expansion remains subtle");
            Check.Equal(true, expanded.z > chest.z && expanded.z - chest.z < .011f, "subtle shoulder rise");
            Check.Equal(true, Math.Abs(tip.x-tail.x) < .00001f, "tail is effectively anchored");
            Check.Equal(chest, ElementalVisuals.Breathe(chest, family, 0), "exhale restores source quad geometry");
        }
    }
    private static void Gates(RuntimeFixture f)
    {
        Check.Equal(false, ElementalVisuals.CanAnimate(default), "missing pawn never animates");
        var pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
        pawn.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawn.def.defName = "Human";
        Check.Equal(false, ElementalVisuals.CanAnimate(new PawnDrawParms { pawn = pawn }), "unrelated pawn bypasses effects");
        pawn.def.defName = "TM_Water_ElementalR";
        typeof(Thing).GetField("mapIndexOrState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(pawn, (sbyte)-1);
        Check.Equal(false, ElementalVisuals.CanAnimate(new PawnDrawParms { pawn = pawn }), "off-map elemental bypasses effects without resolving camera");
    }
    private static void NativeContract(RuntimeFixture f)
    {
        Check.Equal(typeof(PawnRenderNode_AnimalPart_Body), typeof(PawnRenderNode_ElementalBody).BaseType!, "keep native animal body graphic and state ownership");
        Check.Equal(typeof(PawnRenderNodeWorker_AnimalBody), typeof(PawnRenderNodeWorker_ElementalBody).BaseType!, "keep native transforms, materials and visibility");
        var worker = new PawnRenderNodeWorker_ElementalBody();
        Check.Equal(true, worker.GetFinalizedMaterial(null!, default) == null, "native request refresh cannot reintroduce a duplicate undeformed body");
        var requests = new List<PawnGraphicDrawRequest>();
        worker.AppendDrawRequests(null!, default, requests);
        Check.Equal(1, requests.Count, "one bounded body request");
        Check.Equal(true, requests[0].material == null, "body is drawn exactly once at PostDraw with current tick frame");
    }

    private static Map? gateMap;
    private static CameraDriver? gateCamera;
    private static CellRect gateView;
    private static bool gateFog;
    private static bool MapPrefix(ref Map __result) { __result = gateMap!; return false; }
    private static bool CameraPrefix(ref CameraDriver __result) { __result = gateCamera!; return false; }
    private static bool ViewPrefix(ref CellRect __result) { __result = gateView; return false; }
    private static bool FogPrefix(ref bool __result) { __result = gateFog; return false; }

    private static void LiveMapGates(RuntimeFixture f)
    {
        var savedGame = Current.Game;
        var assembly = Assembly.Load("0Harmony");
        var harmonyType = assembly.GetType("HarmonyLib.Harmony")!;
        string id = "rwom.elemental-visibility." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(harmonyType, id)!;
        void Patch(MethodInfo original, string prefix)
        {
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(ElementalVisualTests).GetMethod(prefix, BindingFlags.NonPublic | BindingFlags.Static)!)!;
            harmonyType.GetMethod("Patch")!.Invoke(harmony, [original, hook, null, null, null]);
        }
        try
        {
            // Only UI/environment queries are replaced; execute the real production gate,
            // native Spawned/Dead/Destroyed and the real map lookup on an isolated game.
            Patch(typeof(Find).GetProperty("CurrentMap")!.GetMethod!, nameof(MapPrefix));
            Patch(typeof(Find).GetProperty("CameraDriver")!.GetMethod!, nameof(CameraPrefix));
            Patch(typeof(CameraDriver).GetProperty("CurrentViewRect")!.GetMethod!, nameof(ViewPrefix));
            Patch(typeof(FogGrid).GetMethod("IsFogged", [typeof(IntVec3)])!, nameof(FogPrefix));
            gateMap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            gateMap.fogGrid = (FogGrid)RuntimeHelpers.GetUninitializedObject(typeof(FogGrid));
            gateCamera = (CameraDriver)RuntimeHelpers.GetUninitializedObject(typeof(CameraDriver));
            gateView = new CellRect(0, 0, 20, 20); gateFog = false;
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            FleshStatTests.Set(Current.Game, "maps", new List<Map> { gateMap });
            var pawn = (Pawn)RuntimeHelpers.GetUninitializedObject(typeof(Pawn));
            pawn.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            pawn.def.defName = "TM_GreaterWind_ElementalR";
            pawn.health = (Pawn_HealthTracker)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_HealthTracker));
            FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)0);
            FleshStatTests.Set(pawn, "positionInt", new IntVec3(10, 0, 10));
            FleshStatTests.Set(pawn.health, "healthState", PawnHealthState.Mobile);
            var parms = new PawnDrawParms { pawn = pawn, rotDrawMode = RotDrawMode.Fresh };
            Check.Equal(true, ElementalVisuals.CanAnimate(parms), "live visible elemental uses animated renderer");
            foreach (var flag in new[] { PawnRenderFlags.Portrait, PawnRenderFlags.Statue, PawnRenderFlags.Invisible, PawnRenderFlags.DrawNow })
            {
                parms.flags = flag;
                Check.Equal(false, ElementalVisuals.CanAnimate(parms), flag + " must use native static body");
            }
            parms.flags = default;
            parms.rotDrawMode = RotDrawMode.Rotting;
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "corpse graphics never animate");
            parms.rotDrawMode = RotDrawMode.Fresh;
            FleshStatTests.Set(pawn.health, "healthState", PawnHealthState.Dead);
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "fresh dead pawn never animates");
            FleshStatTests.Set(pawn.health, "healthState", PawnHealthState.Mobile);
            gateFog = true;
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "fog hides effects");
            gateFog = false; gateView = new CellRect(100, 100, 20, 20);
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "outside camera hides effects");
            gateView = new CellRect(0, 0, 20, 20); gateCamera = null;
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "no active camera hides effects");
            gateCamera = (CameraDriver)RuntimeHelpers.GetUninitializedObject(typeof(CameraDriver));
            gateMap = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "other map hides effects");
            gateMap = Current.Game.Maps[0];
            Check.Equal(true, ElementalVisuals.CanAnimate(parms), "returning to live visible map restores animation");
            FleshStatTests.Set(pawn, "mapIndexOrState", (sbyte)-2);
            Check.Equal(false, ElementalVisuals.CanAnimate(parms), "destroyed pawn never animates");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
            Current.Game = savedGame; gateMap = null; gateCamera = null; gateFog = false;
        }
    }
}



