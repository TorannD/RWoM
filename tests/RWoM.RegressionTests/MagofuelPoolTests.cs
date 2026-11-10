using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// The virtual mana/magofuel pool: network tanks behave as one shared store for
// consumers, while off-network buildings stay strictly local. Fuel pulls keep
// each source tank's configured reserve intact.
internal static class MagofuelPoolTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MagofuelPool.TotalsGroupAndIsolation", f => Run(f, Totals));
        yield return new("MagofuelPool.WithdrawFallsBackAcrossNetwork", f => Run(f, Withdraw));
        yield return new("MagofuelPool.SynthesisFeedsFromPool", f => Run(f, Synthesis));
        yield return new("MagofuelPool.FuelDrawRespectsReserve", f => Run(f, FuelReserve));
    }

    static Type T(string n) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + n)
        ?? throw new Exception("Missing pool type " + n);

    static void Set(object o, string n, object v)
    {
        for (Type? t = o.GetType(); t != null; t = t.BaseType)
        {
            var p = t.GetField(n, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (p != null) { p.SetValue(o, v); return; }
        }
        throw new Exception(n);
    }

    static object Get(object o, string p) => o.GetType().GetProperty(p)!.GetValue(o)!;
    static object? Call(object o, string n, params object[] a) => o.GetType().GetMethod(n)!.Invoke(o, a);

    static Map map = null!;
    static Faction faction = null!;
    static int id;

    static void Run(RuntimeFixture f, Action test)
    {
        var old = Current.Game;
        var oldLanguage = LanguageDatabase.activeLanguage; var oldDefault = LanguageDatabase.defaultLanguage;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try
        {
            // InspectLine translates; a headless English worker keeps the log clean.
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English"; Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement>();
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Set(Current.Game, "maps", new List<Map> { map });
            Set(Current.Game, "tickManager", new TickManager());
            faction = new Faction { def = new FactionDef { isPlayer = true } };
            var network = (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), map)!;
            map.components = new List<MapComponent> { network };
            id = 3000;
            test();
        }
        finally { Current.Game = old; LanguageDatabase.activeLanguage = oldLanguage; LanguageDatabase.defaultLanguage = oldDefault; System.Globalization.CultureInfo.CurrentCulture = culture; }
    }

    static ThingComp Beacon(int x, int z)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = "NetworkTestBeacon"; d.size = new IntVec2(1, 1);
        var b = new Building { def = d, thingIDNumber = ++id };
        Set(b, "positionInt", new IntVec3(x, 0, z));
        Set(b, "mapIndexOrState", (sbyte)0);
        Set(b, "factionInt", faction);
        var c = (ThingComp)Activator.CreateInstance(T("CompManaBeacon"))!;
        c.parent = b;
        c.props = (CompProperties)Activator.CreateInstance(T("CompProperties_ManaBeacon"))!;
        Set(b, "comps", new List<ThingComp> { c });
        c.Initialize(c.props);
        c.PostSpawnSetup(false);
        return c;
    }

    // A real magofuel reservoir: exact comp type and storage def name, so the
    // production gating (MagofuelReserve.IsStorage, pool membership) applies.
    static ThingComp Tank(int x, int z, float mana)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = "TM_MagofuelReservoir"; d.size = new IntVec2(1, 1);
        var b = new Building { def = d, thingIDNumber = ++id };
        Set(b, "positionInt", new IntVec3(x, 0, z));
        Set(b, "mapIndexOrState", (sbyte)0);
        Set(b, "factionInt", faction);
        var endpoint = (ThingComp)Activator.CreateInstance(T("CompManaNetworkEndpoint"))!;
        endpoint.parent = b;
        endpoint.props = (CompProperties)Activator.CreateInstance(T("CompProperties_ManaNetworkEndpoint"))!;
        var bank = (ThingComp)Activator.CreateInstance(T("CompMagofuelReservoir"))!;
        bank.parent = b;
        bank.props = (CompProperties)Activator.CreateInstance(T("CompProperties_MagofuelReservoir"))!;
        var fuelProps = new CompProperties_Refuelable { fuelCapacity = 100f, initialConfigurableTargetFuelLevel = 100f };
        var fuelDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        fuelDef.defName = "TM_Magofuel"; fuelDef.thingClass = typeof(Thing); fuelDef.stackLimit = 75;
        fuelDef.destroyable = true; fuelDef.category = ThingCategory.Item; fuelDef.virtualDefs = [];
        fuelProps.fuelFilter = new ThingFilter(); fuelProps.fuelFilter.SetAllow(fuelDef, true);
        var fuel = new CompRefuelable { parent = b, props = fuelProps };
        fuel.Initialize(fuelProps);
        Set(b, "comps", new List<ThingComp> { endpoint, bank, fuel });
        endpoint.Initialize(endpoint.props);
        endpoint.PostSpawnSetup(false);
        Set(bank, "storedMana", mana);
        return bank;
    }

    static float Mana(ThingComp bank) => (float)Get(bank, "StoredMana");
    static float Fuel(ThingComp bank) => bank.parent.GetComp<CompRefuelable>().Fuel;

    static void Totals()
    {
        Beacon(10, 10); Beacon(15, 10); // one connected pair
        var a = Tank(10, 9, 100f); var b = Tank(15, 9, 50f); a.parent.GetComp<CompRefuelable>().Refuel(40f); b.parent.GetComp<CompRefuelable>().Refuel(60f);
        var lonely = Tank(60, 60, 30f);
        var totals = ((float mana, float manaCapacity, float fuel, float fuelCapacity, int tanks))CallPool("Totals", a.parent)!;
        Check.Equal(150f, totals.mana, "pool mana aggregates the connected group");
        Check.Equal(100f, totals.fuel, "pool fuel aggregates the connected group");
        Check.Equal(2, totals.tanks, "pool counts connected tanks");
        var alone = ((float mana, float manaCap2, float fuel, float fuelCap2, int tanks))CallPool("Totals", lonely.parent)!;
        Check.Equal(30f, alone.mana, "an off-network tank sees only itself");
        Check.Equal(1, alone.tanks, "an off-network tank is a pool of one");
        Check.Equal<string?>(null, (string?)CallPool("InspectLine", lonely.parent), "a local-only tank renders no pool line");
        Check.Equal(true, (string?)CallPool("InspectLine", a.parent) != null, "a networked tank renders the pool line");
    }

    static object? CallPool(string method, params object?[] args)
        => typeof(TorannMagic.Odyssey.MagofuelPool).GetMethod(method)!.Invoke(null, args);

    static void Withdraw()
    {
        Beacon(10, 10); Beacon(15, 10);
        var a = Tank(10, 9, 0f); var b = Tank(15, 9, 50f);
        var isolated = Tank(60, 60, 0f);
        Check.Equal(5f, (float)Call(a, "WithdrawMana", 5f)!, "an empty connected tank withdraws from the pool");
        Check.Equal(45f, Mana(b), "the pool debit leaves the source tank");
        Check.Equal(0f, (float)Call(isolated, "WithdrawMana", 5f)!, "an off-network tank cannot pull from the network");
        Check.Equal(0f, Mana(isolated), "the isolated tank stays empty");
        Check.Equal(45f, Mana(b), "the isolated request never touched the network again");
    }

    static void Synthesis()
    {
        Beacon(10, 10); Beacon(15, 10);
        var a = Tank(10, 9, 0f); var b = Tank(15, 9, 20f);
        a.parent.GetComp<CompRefuelable>().Props.initialConfigurableTargetFuelLevel = 100f;
        Set(a, "chemfuelHopper", 1f);
        Call(a, "CompTickInterval", 1200);
        Check.Equal(1f, Fuel(a), "synthesis produces one unit from pooled mana");
        Check.Equal(10f, Mana(b), "synthesis debited the pool source");
        Check.Equal(0f, Mana(a), "synthesis consumed the local top-up");
        var fuel = a.parent.GetComp<CompRefuelable>();
        fuel.Refuel(99f);
        Set(a, "chemfuelHopper", 1f);
        Call(a, "CompTickInterval", 1200);
        Check.Equal(10f, Mana(b), "full target never withdraws remote mana");
        fuel.ConsumeFuel(1f);
        Call(a, "CompTickInterval", 59);
        Check.Equal(99f, Fuel(a), "paused production does not probe every tick");
        Call(a, "CompTickInterval", 1);
        Check.Equal(100f, Fuel(a), "bounded retry resumes once space is available");
    }

    static void FuelReserve()
    {
        Beacon(10, 10); Beacon(15, 10);
        var a = Tank(10, 9, 0f); var b = Tank(15, 9, 0f);
        b.parent.GetComp<CompRefuelable>().Refuel(10f);
        var reserve = typeof(TorannMagic.Odyssey.CompMagofuelReservoir).GetProperty("FuelReserve")!;
        reserve.SetValue(b, 5f);
        Check.Equal(5f, (float)CallPool("DrawFuel", a.parent, 10f)!, "a reserved source surrenders only its surplus");
        Check.Equal(5f, Fuel(a), "the target received exactly the surplus");
        Check.Equal(5f, Fuel(b), "the source keeps its configured reserve");
        reserve.SetValue(b, 0f);
        Check.Equal(5f, (float)CallPool("DrawFuel", a.parent, 10f)!, "clearing the reserve releases the rest");
        Check.Equal(10f, Fuel(a), "the pool drained fully on demand");
        Check.Equal(0f, Fuel(b), "the source tank is empty after the pool draw");
    }
}
