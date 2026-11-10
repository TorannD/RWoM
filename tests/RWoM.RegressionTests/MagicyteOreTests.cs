using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.Resources;
using Verse;

namespace RWoM.RegressionTests;

internal static class MagicyteOreTests
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly List<(string name, int count)> delivered = [];
    static float? chance;
    static bool blocked;
    static bool partial;
    static bool throwAfterTransfer;
    static MapComponent_MagicyteRewards? reenter;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Magicyte.Ore.InitializationRangeBonusBoundaryAndAllGems", Initialization);
        yield return new("Magicyte.Ore.NativeMiningCombatAndRepeatedCallbacks", Mining);
        yield return new("Magicyte.Ore.RealScribeNoRerollOrDuplicate", Save);
        yield return new("Magicyte.Ore.BlockedPartialPlacementPersistsAndRetries", Placement);
        yield return new("Magicyte.Ore.ThrowAfterTransferAndReentryCannotDuplicate", ThrowingPlacement);
    }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    static FieldInfo Field(object value, string name)
    {
        for (Type? t = value.GetType(); t != null; t = t.BaseType)
            if (t.GetField(name, Flags | BindingFlags.DeclaredOnly) is { } field) return field;
        throw new Exception(name);
    }
    static void Set(object value, string name, object? data) => Field(value, name).SetValue(value, data);
    static object? Get(object value, string name) => Field(value, name).GetValue(value);
    static void Initialization(RuntimeFixture f)
    {
        using var world = new World(f);
        chance = .01f;
        var boundary = world.Ore();
        Check.Equal<ThingDef?>(null, (ThingDef?)Get(boundary, "bonusGem"), "exact 1% boundary excludes gem");
        chance = .009999f;
        var gems = new HashSet<string>(); var yields = new HashSet<int>();
        Rand.PushState(8071);
        try {
            for (int i = 0; i < 2000; i++)
            {
                var ore = world.Ore();
                int count = (int)Get(ore, "magicyteYield")!;
                Check.Equal(true, count >= 15 && count <= 100, "inclusive per-tile range");
                yields.Add(count); gems.Add(((ThingDef)Get(ore, "bonusGem")!).defName);
                ore.PostMake();
                Check.Equal(count, (int)Get(ore, "magicyteYield")!, "repeated initialization keeps roll");
            }
        } finally { Rand.PopState(); }
        Check.Equal(true, yields.Contains(15) && yields.Contains(100), "both inclusive endpoints reachable");
        Check.Equal(24, gems.Count, "all existing enchantment types and grades reachable");
    }
    static void Mining(RuntimeFixture f)
    {
        using var world = new World(f);
        var combat = world.Ore(100, true); combat.Destroy(DestroyMode.KillFinalize);
        Check.Equal(0, delivered.Count, "combat destroys ore without loot");
        NativeYield(combat, world.Map);
        Check.Equal(0, delivered.Count, "combat cannot subsequently claim mining reward");
        var ore = world.Ore(100, true);
        ore.DestroyMined(null);
        Check.Equal(100, delivered.Where(x => x.name == "RawMagicyte").Sum(x => x.count), "native mining exact yield");
        Check.Equal(1, delivered.Where(x => x.name.StartsWith("TM_EStone_")).Sum(x => x.count), "exactly one gem");
        Check.Equal(true, delivered.All(x => x.count <= 75), "native placement receives legal split stacks");
        NativeYield(ore, world.Map);
        Check.Equal(101, delivered.Sum(x => x.count), "repeated callback cannot duplicate");
        var ordinary = new Mineable { def = world.OreDef };
        Check.Equal(true, MagicyteMiningYieldPatch.Prefix(ordinary, world.Map, true, null!), "ordinary minerals retain native yield");
        delivered.Clear();
        foreach (float miningYield in new[] { 0f, .5f, 1f, 20f })
        {
            var fixedOre = world.Ore(42);
            Set(fixedOre, "yieldPct", miningYield);
            fixedOre.DestroyMined(null);
        }
        Check.Equal(true, delivered.Count == 4 && delivered.All(x => x.count == 42), "native accumulated MiningYield cannot change saved ore quantity");
    }
    static void Save(RuntimeFixture f)
    {
        using var world = new World(f);
        chance = .009f;
        var ore = world.Ore();
        var saved = new Envelope { Ore = ore };
        int count = (int)Get(ore, "magicyteYield")!;
        string gem = ((ThingDef)Get(ore, "bonusGem")!).defName;
        chance = 1f;
        var loaded = RoundTrip(f, saved, "ore");
        Check.Equal(count, (int)Get(loaded.Ore, "magicyteYield")!, "real Scribe retains yield");
        Check.Equal(gem, ((ThingDef)Get(loaded.Ore, "bonusGem")!).defName, "real Scribe retains gem choice");
        world.Spawn(loaded.Ore); loaded.Ore.DestroyMined(null);
        var consumed = RoundTrip(f, loaded, "consumed");
        NativeYield(consumed.Ore, world.Map);
        Check.Equal(count + 1, delivered.Sum(x => x.count), "saved consumed flag prevents post-load duplicate");
    }
    static void Placement(RuntimeFixture f)
    {
        using var world = new World(f);
        blocked = true; partial = true;
        world.Ore(100, true).DestroyMined(null);
        Check.Equal(10, delivered.Sum(x => x.count), "partial failed placement accounted exactly once");
        var state = RoundTrip(f, new Envelope { Rewards = world.Rewards }, "blocked");
        Set(state.Rewards!, "map", world.Map);
        blocked = false; partial = false;
        state.Rewards!.TryDeliver(); state.Rewards.TryDeliver();
        Check.Equal(100, delivered.Where(x => x.name == "RawMagicyte").Sum(x => x.count), "saved pending resource retries without loss or duplication");
        Check.Equal(1, delivered.Where(x => x.name.StartsWith("TM_EStone_")).Sum(x => x.count), "saved pending gem retries once");
    }
    static void ThrowingPlacement(RuntimeFixture f)
    {
        using var world = new World(f);
        reenter = world.Rewards; throwAfterTransfer = true;
        try { world.Ore(100, true).DestroyMined(null); throw new Exception("placement must throw"); }
        catch (InvalidOperationException e) when (e.Message == "injected placement failure") { }
        throwAfterTransfer = false;
        world.Rewards.TryDeliver(); world.Rewards.TryDeliver();
        Check.Equal(101, delivered.Sum(x => x.count), "spawn then throw and reentry cannot duplicate");
        delivered.Clear(); blocked = partial = throwAfterTransfer = true;
        try { world.Ore(100).DestroyMined(null); throw new Exception("partial merge must throw"); }
        catch (InvalidOperationException e) when (e.Message == "injected placement failure") { }
        blocked = throwAfterTransfer = false;
        world.Rewards.TryDeliver();
        Check.Equal(100, delivered.Sum(x => x.count), "merge then throw retains only unpaid balance");
    }
    static void NativeYield(Mineable ore, Map map) => typeof(Mineable).GetMethod("TrySpawnYield", Flags, null, [typeof(Map), typeof(bool), typeof(Pawn)], null)!.Invoke(ore, [map, true, null]);
    public class Envelope : IExposable
    {
        public MineableMagicyte Ore = null!;
        public MapComponent_MagicyteRewards? Rewards;
        public void ExposeData() { Scribe_Deep.Look(ref Ore, true, "ore"); Scribe_Deep.Look(ref Rewards, "rewards", (Map)null!); }
    }
    static Envelope RoundTrip(RuntimeFixture f, Envelope value, string name)
    {
        string path = Path.Combine(f.WorkingDirectory, "magicyte-" + name + ".xml");
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref value, "state"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Envelope loaded = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "state"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        return loaded;
    }
    // Boundaries isolate rendering/map teardown. Native Mineable.DestroyMined,
    // native callback dispatch and production Scribe remain real.
    static bool Skip() => false;
    static bool Value(ref float __result) { if (!chance.HasValue) return true; __result = chance.Value; return false; }
    static bool Destroy(Thing __instance) { Set(__instance, "mapIndexOrState", (sbyte)-2); return false; }
    static bool BaseSave(Thing __instance) { Scribe_Defs.Look(ref __instance.def, "def"); return false; }
    static bool Place(Thing thing, ref Thing lastResultingThing, ref bool __result)
    {
        lastResultingThing = thing;
        if (blocked)
        {
            if (partial && thing.def.defName == "RawMagicyte") { delivered.Add((thing.def.defName, 10)); thing.stackCount -= 10; partial = false; if (throwAfterTransfer) throw new InvalidOperationException("injected placement failure"); }
            __result = false; return false;
        }
        delivered.Add((thing.def.defName, thing.stackCount));
        Set(thing, "mapIndexOrState", (sbyte)0);
        reenter?.TryDeliver();
        if (throwAfterTransfer) throw new InvalidOperationException("injected placement failure");
        __result = true; return false;
    }
    sealed class World : IDisposable
    {
        readonly Game old = Current.Game;
        readonly ThingDef[] oldDefs = DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        readonly Patches patches = new();
        public readonly Map Map = Blank<Map>();
        public readonly ThingDef OreDef;
        public readonly MapComponent_MagicyteRewards Rewards;
        public World(RuntimeFixture f)
        {
            delivered.Clear(); chance = 1f; blocked = partial = throwAfterTransfer = false; reenter = null;
            Current.Game = Blank<Game>(); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager()); Set(Current.Game, "maps", new List<Map> { Map });
            Rewards = new(Map); Set(Map, "components", new List<MapComponent> { Rewards });
            DefDatabase<ThingDef>.Clear();
            var raw = Def("RawMagicyte");
            string gemFile = Directory.GetFiles(Path.Combine(f.Repository, "v1.7/Defs"), "Item_EnchantmentStones.xml", SearchOption.AllDirectories).Single();
            foreach (var name in XDocument.Load(gemFile).Descendants("defName").Select(x => x.Value).Where(x => x.StartsWith("TM_EStone_"))) Def(name);
            OreDef = Def("TM_MineableMagicyte"); OreDef.thingClass = typeof(MineableMagicyte); OreDef.category = ThingCategory.Building;
            OreDef.building = new BuildingProperties { mineableThing = raw, mineableYieldWasteable = false };
            patches.Prefix(typeof(ThingWithComps).GetMethod("PostMake")!, nameof(Skip));
            patches.Prefix(typeof(Thing).GetMethod("PostMake")!, nameof(Skip));
            patches.Prefix(typeof(Building).GetMethod("Destroy")!, nameof(Destroy));
            patches.Prefix(typeof(Thing).GetMethod("Destroy")!, nameof(Destroy));
            patches.Prefix(typeof(Building).GetMethod("ExposeData")!, nameof(BaseSave));
            patches.Prefix(typeof(Rand).GetProperty("Value")!.GetMethod!, nameof(Value));
            patches.Prefix(typeof(GenPlace).GetMethods().Single(m => m.Name == "TryPlaceThing" && m.GetParameters().Any(p => p.IsOut)), nameof(Place));
            patches.Add(typeof(MagicyteMiningYieldPatch));
        }
        ThingDef Def(string name) { var def = Blank<ThingDef>(); def.defName = name; def.thingClass = typeof(Thing); def.stackLimit = 75; def.category = ThingCategory.Item; def.destroyable = true; def.comps = []; DefDatabase<ThingDef>.Add(def); return def; }
        public void Spawn(Thing ore) { Set(ore, "mapIndexOrState", (sbyte)0); Set(ore, "positionInt", new IntVec3(4, 0, 4)); }
        public MineableMagicyte Ore(int? count = null, bool bonus = false)
        {
            var ore = new MineableMagicyte { def = OreDef }; ore.PostMake(); Spawn(ore);
            if (count.HasValue) Set(ore, "magicyteYield", count.Value);
            if (bonus) Set(ore, "bonusGem", DefDatabase<ThingDef>.GetNamed("TM_EStone_wonder_major"));
            return ore;
        }
        public void Dispose() { patches.Dispose(); Current.Game = old; DefDatabase<ThingDef>.Clear(); foreach (var def in oldDefs) DefDatabase<ThingDef>.Add(def); chance = null; }
    }
    sealed class Patches : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly object harmony;
        readonly string id = "rwom.regression.magicyte." + Guid.NewGuid();
        public Patches() => harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        public void Prefix(MethodBase original, string method)
        {
            var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(MagicyteOreTests).GetMethod(method, Flags)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [original, prefix, null, null, null]);
        }
        public void Add(Type type) { var processor = harmony.GetType().GetMethod("CreateClassProcessor")!.Invoke(harmony, [type])!; processor.GetType().GetMethod("Patch")!.Invoke(processor, null); }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
}
