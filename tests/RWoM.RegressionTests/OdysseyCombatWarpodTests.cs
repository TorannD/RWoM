using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class OdysseyCombatWarpodTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.CombatWarpod.OptionalAvailability", f => Optional());
        if (!File.Exists("C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll")) yield break;
        yield return new("Odyssey.CombatWarpod.NoSafeFloorNoPayment", f => Run(() => NoFloor()));
        yield return new("Odyssey.CombatWarpod.PayloadOwnershipRetryAndDiscovery", f => Run(() => Ownership()));
        yield return new("Odyssey.CombatWarpod.FrozenBoundaryMovesAndRotates", f => Run(() => Moves()));
        yield return new("Odyssey.CombatWarpod.FullHolderScribe", f => Run(() => Save(f)));
        yield return new("Odyssey.CombatWarpod.FullHolderScribeAfterMissingTypeLookup", f => Run(() => SaveAfterMissingTypeLookup(f)));
        yield return new("Odyssey.CombatWarpod.NativeItemPlacement", f => Run(() => NativeDrop()));
    }
    static Assembly External => Assembly.LoadFrom(Environment.GetEnvironmentVariable("RWOM_VGE_ADAPTER") ?? throw new Exception("Missing VGE adapter build"));
    static Type T(string name) => (name.StartsWith("MagicalCombatWarpod") ? External : typeof(MagicData).Assembly).GetType("TorannMagic.Odyssey." + name)!;
    static object Core(string name, params object[] args) => typeof(OdysseyCombatTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args)!;
    static void Set(object value, string field, object data) => Core("Set", value, field, data);
    static void Run(Action action)
    {
        var adapter = (TorannMagic.Odyssey.IMagicalShieldProjectileAdapter)Activator.CreateInstance(External.GetType("TorannMagic.VGE.WarpodShieldAdapter", true)!)!;
        TorannMagic.Odyssey.MagicalShieldProjectileAdapters.Register(adapter);
        try { Core("Run", action); }
        finally { TorannMagic.Odyssey.MagicalShieldProjectileAdapters.Unregister(adapter); }
    }
    static float HP(ThingComp main) => (float)Core("HP", main);
    static bool Skip() => false;
    static void Optional()
    {
        foreach (var name in new[] { "MagicalCombatWarpodImpactPatch", "MagicalCombatWarpodTickPatch", "MagicalCombatWarpodSavePatch" })
        {
            Check.Equal(true, typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey."+name) == null,"warpod patch has left Content");
            var type = T(name);
            Check.Equal(type.GetMethod("TargetMethod")!.Invoke(null, null) != null, (bool)type.GetMethod("Prepare")!.Invoke(null, null)!, "optional warp adapter discovery agrees with loaded types");
        }
        Check.Equal(false, typeof(MagicData).Assembly.GetReferencedAssemblies().Any(a => a.Name!.StartsWith("VanillaGravship")), "no mandatory VGE reference");
    }
    static Thing? blockedCargo;
    static int dropCalls, emptyDestroys;
    static Predicate<IntVec3>? lastValidator;
    static bool nativeDrop;
    static bool blockAllCargo;
    static bool Drop(Thing thing, IntVec3 dropCell, Predicate<IntVec3>? nearPlaceValidator, ref Thing resultingThing, ref bool __result)
    {
        if (nativeDrop) return true;
        dropCalls++; lastValidator = nearPlaceValidator;
        Check.Equal(true, nearPlaceValidator != null, "native ThingOwner drop receives outside validator");
        __result = !blockAllCargo && thing != blockedCargo && nearPlaceValidator!(dropCell);
        resultingThing = __result ? thing : null!;
        if (__result) Set(thing, "positionInt", dropCell);
        return false;
    }
    static bool EmptyDestroy(Thing __instance) { emptyDestroys++; return false; }
    static ThingOwner Owner(Projectile pod) => ((IThingHolder)pod).GetDirectlyHeldThings();
    static object Adapter(string method, params object[] args) => T("MagicalCombatWarpod").GetMethod(method)!.Invoke(null, args)!;

    sealed class World : IDisposable
    {
        public readonly Assembly Vge2;
        public readonly ThingComp Main;
        public readonly Map Map;
        public readonly CompProjectileInterceptor Interceptor;
        public readonly Thing Enemy;
        public readonly Patch Patches = new();
        readonly FieldInfo mainThread = typeof(UnityData).GetField("mainThreadId", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object? oldThread;
        public World(bool floor)
        {
            oldThread = mainThread.GetValue(null); mainThread.SetValue(null, Environment.CurrentManagedThreadId);
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(ThingCategoryDefOf));
            AssemblyLoadContext.Default.Resolving += Resolve;
            Vge2 = Assembly.LoadFrom("C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/3799737423/1.7/Assemblies/VanillaGravshipExpanded2.dll");
            Patches.Apply(typeof(ContentFinder<Texture2D>).GetMethod("Get", new[] { typeof(string), typeof(bool) })!, typeof(OdysseyCombatWarpodTests).GetMethod(nameof(Skip), BindingFlags.Static | BindingFlags.NonPublic)!);
            foreach (var m in typeof(MaterialPool).GetMethods().Where(m => m.Name == "MatFrom")) Patches.Apply(m, typeof(OdysseyCombatWarpodTests).GetMethod(nameof(Skip), BindingFlags.Static | BindingFlags.NonPublic)!);
            var mods = LoadedModManager.RunningModsListForReading.ToArray();
            try { LoadedModManager.RunningModsListForReading.Clear(); RuntimeHelpers.RunClassConstructor(typeof(Projectile).TypeHandle); }
            finally { LoadedModManager.RunningModsListForReading.AddRange(mods); }
            Main = (ThingComp)Core("Source", 15, 15, false); Set(Main, "currentHitPoints", 500f); Core("Tick", 60);
            var nativeWorld = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            nativeWorld.factionManager = new FactionManager(); nativeWorld.factionManager.AllFactionsListForReading.Add(Main.parent.Faction); Set(nativeWorld.factionManager, "ofPlayer", Main.parent.Faction); Set(Current.Game, "worldInt", nativeWorld);
            Map = Main.parent.Map;
            bool temporaryDesignation = DefDatabase<DesignationDef>.DefCount == 0; if (temporaryDesignation) DefDatabase<DesignationDef>.Add(new DesignationDef { defName = "WarpodFixtureDesignation" });
            Map.designationManager = new DesignationManager(Map); if (temporaryDesignation) DefDatabase<DesignationDef>.Clear();
            Map.coverGrid = new CoverGrid(Map); Map.gasGrid = new GasGrid(Map); Map.regionGrid = new RegionGrid(Map);
            Map.regionDirtyer = new RegionDirtyer(Map); Map.regionAndRoomUpdater = new RegionAndRoomUpdater(Map); Set(Map.regionAndRoomUpdater, "initialized", true);
            Patches.Apply(typeof(Thing).GetMethod("DirtyMapMesh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!, typeof(OdysseyCombatWarpodTests).GetMethod(nameof(Skip), BindingFlags.Static | BindingFlags.NonPublic)!);
            Map.terrainGrid = new TerrainGrid(Map);
            var terrain = (TerrainDef)RuntimeHelpers.GetUninitializedObject(typeof(TerrainDef)); terrain.exposesToVacuum = !floor;
            Array.Fill(Map.terrainGrid.topGrid, terrain);
            var grid = new PathGrid(Map, new PathGridDef { defName = "WarpodTestPath" });
            Map.pathing = (Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));
            var context = new PathingContext(Map, grid); Set(Map.pathing, "normal", context); Set(Map.pathing, "fenceBlocked", context);
            Interceptor = (CompProjectileInterceptor)Core("Comp", Main.parent, "MagicalCombatInterceptor");
            Enemy = (Thing)Core("Enemy");
            Patches.Apply(typeof(CompProjectileInterceptor).GetMethod("CheckIntercept")!, T("MagicalCombatProjectilePatch").GetMethod("Prefix")!);
            Patches.Apply(Vge2.GetType("VanillaGravshipExpanded2.Projectile_Warpod")!.GetMethod("Impact")!, T("MagicalCombatWarpodImpactPatch").GetMethod("Prefix")!);
            Patches.Apply(Vge2.GetType("VanillaGravshipExpanded2.Projectile_Warpod")!.GetMethod("Tick")!, T("MagicalCombatWarpodTickPatch").GetMethod("Prefix")!);
            Patches.Apply(Vge2.GetType("VanillaGravshipExpanded2.Projectile_Warpod")!.GetMethod("ExposeData")!, T("MagicalCombatWarpodSavePatch").GetMethod("Postfix")!, true);
            Patches.Apply(typeof(GenDrop).GetMethod("TryDropSpawn")!, typeof(OdysseyCombatWarpodTests).GetMethod(nameof(Drop), BindingFlags.Static | BindingFlags.NonPublic)!);
            Patches.Apply(typeof(Thing).GetMethod("Destroy")!, typeof(OdysseyCombatWarpodTests).GetMethod(nameof(EmptyDestroy), BindingFlags.Static | BindingFlags.NonPublic)!);
            Patches.Apply(typeof(ThingWithComps).GetMethod("Destroy")!, T("MagicalCombatWarpodDestroyPatch").GetMethod("Prefix")!);
            Map.listerThings = new ListerThings(ListerThingsUse.Global); Map.listerMergeables = new ListerMergeables(Map);
            Map.passingShipManager = new PassingShipManager(Map);
            Map.mapPawns = new MapPawns(Map);
        }
        static Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var id in new[] { "2023507013", "3609835606", "3799737423" })
            {
                var p = $"C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/{id}/1.7/Assemblies/{name.Name}.dll";
                if (File.Exists(p)) return context.LoadFromAssemblyPath(p);
            }
            return null;
        }
        public Projectile Pod(Thing? launcher = null)
        {
            var type = Vge2.GetType("VanillaGravshipExpanded2.Projectile_Warpod")!;
            var pod = (Projectile)Activator.CreateInstance(type)!;
            pod.def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            pod.def.defName = "WarpodTestIncoming"; pod.def.thingClass = type; pod.def.size = new IntVec2(1, 1);
            pod.def.category = ThingCategory.Projectile; pod.def.comps = new List<CompProperties>(); pod.def.stackLimit = 1;
            pod.def.projectile = new ProjectileProperties { damageDef = new DamageDef { defaultDamage = 8 }, speed = 85 };
            var warpodDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            warpodDef.defName = "WarpodTestPayload"; warpodDef.thingClass = typeof(Thing); warpodDef.stackLimit = 1; warpodDef.category = ThingCategory.Building;
            warpodDef.comps = new List<CompProperties> { new CompProperties_Explosive { damageAmountBase = 50, explosiveDamageType = DamageDefOf.Bomb }, new CompProperties_Transporter { massCapacity = 150 } };
            Set(pod, "warpodDef", warpodDef); Set(pod, "launchingFaction", Enemy.Faction);
            Set(pod, "mapIndexOrState", (sbyte)0); Set(pod, "launcher", launcher ?? Enemy);
            Set(pod, "origin", new Vector3(35.5f, 0, 15.5f)); Set(pod, "destination", new Vector3(15.5f, 0, 15.5f));
            Set(pod, "positionInt", new IntVec3(35, 0, 15));
            pod.thingIDNumber = 10001;
            return pod;
        }
        public bool Intercept(Projectile pod) => Interceptor.CheckIntercept(pod, new Vector3(35.5f, 0, 15.5f), new Vector3(15.5f, 0, 15.5f));
        public void Dispose() { Patches.Dispose(); mainThread.SetValue(null, oldThread); AssemblyLoadContext.Default.Resolving -= Resolve; blockedCargo = null; dropCalls = emptyDestroys = 0; lastValidator = null; nativeDrop = blockAllCargo = false; }
    }
    static void NoFloor()
    {
        using var world = new World(false);
        var pod = world.Pod(); float before = HP(world.Main);
        Check.Equal(false, world.Intercept(pod), "no solid outside landing cell means native flight and no interception");
        Check.Equal(before, HP(world.Main), "no landing cell must not debit shield");
    }
    static Thing Item()
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "WarpodTestCargo"; def.thingClass = typeof(Thing); def.category = ThingCategory.Item; def.stackLimit = 75; def.size = new IntVec2(1, 1); def.comps = new List<CompProperties>();
        return new Thing { def = def, thingIDNumber = 10002, stackCount = 7 };
    }
    static Pawn Passenger()
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "WarpodTestPassenger"; def.thingClass = typeof(Pawn); def.category = ThingCategory.Pawn; def.race = new RaceProperties { body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef() } } }; def.comps = new List<CompProperties>();
        var pawn = new Pawn { def = def, thingIDNumber = 10003 };
        pawn.health = new Pawn_HealthTracker(pawn);
        return pawn;
    }
    static void Ownership()
    {
        using var world = new World(true);
        var friendly = world.Pod(world.Main.parent); float before = HP(world.Main);
        Check.Equal(false, world.Intercept(friendly), "friendly pod retains native flight");
        var inside = world.Pod(); Set(inside, "origin", new Vector3(15.5f, 0, 15.5f));
        Check.Equal(false, world.Intercept(inside), "internal hostile pod retains native flight");
        var pod = world.Pod(); var owner = Owner(pod); var item = Item(); var pawn = Passenger(); owner.TryAdd(item); owner.TryAdd(pawn);
        world.Map.listerThings.Add(pod); world.Map.thingGrid.Register(pod);
        Check.Equal(true, world.Map.mapPawns.AllPawnsUnspawned.Contains(pawn), "native map pawn discovery includes held passenger before landing");
        blockedCargo = pawn;
        Check.Equal(true, world.Intercept(pod), "hostile pod lands on safe floor outside shield");
        Check.Equal(before - 50f, HP(world.Main), "payload explosive cost replaces incoming nominal8");
        Check.Equal(true, world.Intercept(pod), "repeat interception recognizes persistent landing");
        Check.Equal(before - 50f, HP(world.Main), "no duplicate payment");
        pod.GetType().GetMethod("Impact")!.Invoke(pod, new object?[] { null, true });
        Check.Equal(1, owner.Count, "partial placement retains only failed cargo");
        Check.Same(pawn, owner[0], "same passenger retained, not copied");
        Check.Same(owner, pawn.holdingOwner, "passenger keeps native owner");
        Check.Equal(true, world.Map.mapPawns.AllPawnsUnspawned.Contains(pawn), "pending passenger remains visible to native map removal discovery");
        Check.Equal(false, (bool)Adapter("MayDestroy", pod), "external destruction cannot discard pending cargo on live map");
        pod.Destroy(); Check.Equal(0, emptyDestroys, "real Destroy prefix prevents pending shell destruction");
        int calls = dropCalls; pod.GetType().GetMethod("Tick")!.Invoke(pod, null);
        Check.Equal(calls, dropCalls, "same tick cannot retry ownership side effects");
        blockedCargo = null; Find.TickManager.DebugSetTicksGame(120);
        pod.GetType().GetMethod("Tick")!.Invoke(pod, null);
        Check.Equal(0, owner.Count, "retry transfers remaining passenger");
        Check.Equal(1, emptyDestroys, "shell destroyed only after successful emptying");
        Check.Equal(before - 50f, HP(world.Main), "unloading does not pay again");
    }
    static void Moves()
    {
        using var world = new World(true); var pod = world.Pod(); var item = Item(); Owner(pod).TryAdd(item); blockedCargo = item;
        Set(world.Main, "currentHitPoints", 20f);
        Check.Equal(true, world.Intercept(pod), "lastHP intercept collapses shield");
        Adapter("Impact", pod);
        Check.Equal(false, lastValidator!(new IntVec3(15, 0, 15)), "frozen original circle still excluded after collapse");
        var first = pod.Position; var offset = new IntVec3(15, 0, 15).ToVector3Shifted() - first.ToVector3Shifted();
        pod.Position = new IntVec3(40, 0, 40); pod.Rotation = Rot4.East;
        Adapter("Impact", pod);
        var movedCenter = (offset.RotatedBy(90f) + pod.Position.ToVector3Shifted()).ToIntVec3();
        Check.Equal(false, lastValidator!(movedCenter), "frozen original circle follows retained shell translation and rotation");
        Check.Equal(true, lastValidator!(pod.Position), "translated landing anchor stays outside frozen union");
        Check.Equal(pod.Position.ToVector3Shifted().x, pod.ExactPosition.x, "retained shell drawing follows translated position");
        Check.Equal(pod.Position.ToVector3Shifted().z, pod.ExactPosition.z, "retained shell drawing follows translated position z");
        var direction = (Vector3)typeof(Projectile).GetField("destination", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pod)! - (Vector3)typeof(Projectile).GetField("origin", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pod)!;
        Check.Equal(true, direction.sqrMagnitude > 0f, "landing retains nonzero rendering direction");
    }
    sealed class TypeLookupCache : IDisposable
    {
        readonly FieldInfo field = typeof(GenTypes).GetField("typeCache", BindingFlags.Static | BindingFlags.NonPublic)!;
        readonly object previous;
        public TypeLookupCache()
        {
            previous = field.GetValue(null)!;
            field.SetValue(null, Activator.CreateInstance(field.FieldType));
        }
        public void Dispose() => field.SetValue(null, previous);
    }
    static void SaveAfterMissingTypeLookup(RuntimeFixture fixture)
    {
        using var cache = new TypeLookupCache();
        Check.Equal<Type?>(null, GenTypes.GetTypeInAnyAssembly("VanillaGravshipExpanded2.Projectile_Warpod"),
            "optional assembly is not registered before the save fixture");
        Save(fixture);
        Check.Equal<Type?>(null, GenTypes.GetTypeInAnyAssembly("VanillaGravshipExpanded2.Projectile_Warpod"),
            "save fixture restores the caller's missing-type cache");
    }
    static void Save(RuntimeFixture fixture)
    {
        using var world = new World(true); var pod = world.Pod(); var item = Item(); var pawn = Passenger(); Owner(pod).TryAdd(item); Owner(pod).TryAdd(pawn);
        Check.Equal(true, world.Intercept(pod), "save pending landed pod");
        blockAllCargo = true; Adapter("Impact", pod);
        var defs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        // Register the optional assembly through the native registry. Patching its
        // tiny iterator getter after type lookup has warmed can miss inlined callers.
        var optionalPack = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        optionalPack.assemblies = new ModAssemblyHandler(optionalPack);
        optionalPack.assemblies.loadedAssemblies.Add(world.Vge2);
        LoadedModManager.RunningModsListForReading.Add(optionalPack);
        try
        {
            if (capacities.Length == 0) DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "WarpodFixtureCapacity" });
            var payload = (ThingDef)pod.GetType().GetField("warpodDef")!.GetValue(pod)!;
            foreach (var def in new[] { pod.def, payload, item.def, pawn.def }) DefDatabase<ThingDef>.Add(def);
            Set(pod, "launcher", null!); Set(pod, "launchingFaction", null!);
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            // GenTypes caches failed lookups even across ClearCache(). Registering the optional
            // fixture assembly therefore needs its own name cache, restored when this scope ends.
            using var typeLookupCache = new TypeLookupCache();
            Check.Same(pod.GetType(), GenTypes.GetTypeInAnyAssembly(pod.GetType().FullName!),
                "native Scribe type lookup sees the registered optional assembly");
            var path = Path.Combine(fixture.WorkingDirectory, "full-warpod.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref pod, "pod"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            Projectile loaded = null!;
            Find.TickManager.DebugSetTicksGame(0);
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "pod"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
            Check.Equal(2, Owner(loaded).Count, "full native holder graph restores pawn and items");
            Check.Equal(7, Owner(loaded).Single(t => !(t is Pawn)).stackCount, "full native graph preserves cargo quantity");
            Check.Equal(true, (bool)Adapter("IsLanded", loaded), "own landing marker reconstructed for new instance");
            Check.Equal(true, (bool)typeof(Projectile).GetField("landed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loaded)!, "native landed movement guard restored");
            Check.Equal(true, Owner(loaded).All(t => t.holdingOwner == Owner(loaded)), "loaded cargo has correct original-style native owner");
            Set(loaded, "mapIndexOrState", (sbyte)0); world.Map.thingGrid.Register(loaded); world.Map.listerThings.Add(loaded);
            Check.Equal(true, world.Map.mapPawns.AllPawnsUnspawned.Contains(Owner(loaded).OfType<Pawn>().Single()), "restored native holder remains discoverable on map");
            int calls = dropCalls; Adapter("Tick", loaded);
            Check.Equal(calls + 2, dropCalls, "rewound load clock retries promptly instead of retaining stale deadline");
            Check.Equal(false, lastValidator!(new IntVec3(15, 0, 15)), "loaded frozen boundary still excludes original protected center");
            float before = HP(world.Main); Check.Equal(true, world.Intercept(loaded), "loaded marker dominates repeat native interception");
            Check.Equal(before, HP(world.Main), "loaded landing cannot pay again");
        }
        finally { LoadedModManager.RunningModsListForReading.Remove(optionalPack); DefDatabase<ThingDef>.Clear(); foreach (var d in defs) DefDatabase<ThingDef>.Add(d); DefDatabase<PawnCapacityDef>.Clear(); foreach (var d in capacities) DefDatabase<PawnCapacityDef>.Add(d); }
    }
    static void NativeDrop()
    {
        using var world = new World(true); var pod = world.Pod(); var item = Item(); Owner(pod).TryAdd(item);
        Check.Equal(true, world.Intercept(pod), "native placement setup intercepts");
        var existing = new Thing { def = item.def, thingIDNumber = 10004, stackCount = 10 };
        Set(existing, "positionInt", pod.Position); Set(existing, "mapIndexOrState", (sbyte)0);
        world.Map.thingGrid.Register(existing);
        nativeDrop = true;
        Adapter("Impact", pod);
        Check.Equal(17, existing.stackCount, "real GenDrop and GenPlace select safe outside cell and merge cargo");
        Check.Equal(0, Owner(pod).Count, "real native placement releases original holder ownership");
        Check.Equal(true, world.Map.thingGrid.ThingsListAt(pod.Position).Contains(existing), "delivered stack remains registered at landing cell");
    }
    sealed class Patch : IDisposable
    {
        readonly Assembly a = Assembly.Load("0Harmony"); readonly object h; readonly string id = "rwom.warpod." + Guid.NewGuid();
        public Patch() { h = Activator.CreateInstance(a.GetType("HarmonyLib.Harmony")!, id)!; }
        public void Apply(MethodBase target, MethodInfo patch, bool postfix = false)
        {
            var hm = Activator.CreateInstance(a.GetType("HarmonyLib.HarmonyMethod")!, patch)!;
            h.GetType().GetMethod("Patch")!.Invoke(h, new object?[] { target, postfix ? null : hm, postfix ? hm : null, null, null });
        }
        public void Dispose() => h.GetType().GetMethod("UnpatchAll")!.Invoke(h, new object[] { id });
    }
}
