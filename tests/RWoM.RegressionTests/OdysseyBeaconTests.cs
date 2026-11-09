using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> Cases()
    {
        foreach (var test in ThrusterNetworkCases()) yield return test;
        yield return new("Odyssey.MagicalThruster.GroupDebitDryRunRefund", f => Run(f, ThrusterFuel));
        yield return new("Odyssey.Beacon.SharedBudgetsAndNoReplay", f => Run(f, Budgets));
        yield return new("Odyssey.Beacon.RangeTopologyAndNearest", f => Run(f, Topology));
        yield return new("Performance.Beacon.GraphReuseAndImmediateInvalidation", f => Run(f, GraphReuse));
        yield return new("Odyssey.Beacon.NativeFuelAndTargets", f => Run(f, Fuel));
        yield return new("Odyssey.Beacon.SaveDeadlineModesAndRebuiltRegistry", f => Run(f, () => Save(f)));
        yield return new("Odyssey.Beacon.NearestTieFootprintAndBranchSplit", f => Run(f, Footprint));
        yield return new("Odyssey.Beacon.NativeCallbackRevalidation", f => Run(f, Callbacks));
        yield return new("Odyssey.Beacon.ThrowingNativeCallbackPreservesPayment", f => Run(f, ThrowingCallback));
        yield return new("Odyssey.Beacon.TargetsModesOwnershipAndSeparateMaps", f => Run(f, Denials));
    }

    static void GraphReuse()
    {
        var a = Make("ManaBeacon", 10, 10);
        var b = Make("ManaBeacon", 15, 10);
        var endpoint = Tank(10, 9);
        var field = network.GetType().GetField("componentGroups", BindingFlags.Instance | BindingFlags.NonPublic)!;
        System.Collections.IList Connected() => (System.Collections.IList)Call(network, "GetConnectedBeacons", endpoint)!;
        Check.Equal(2, Connected().Count, "initial connected pair");
        var original = field.GetValue(network)!;
        var liveField = network.GetType().GetField("liveNodes", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sorted = liveField.GetValue(network)!;
        for (int i = 0; i < 1000; i++) Check.Equal(2, Connected().Count, "repeated same topology");
        Check.Same(original, field.GetValue(network)!, "graph reused across repeated queries");
        Check.Same(sorted, liveField.GetValue(network)!, "unchanged live registry does not re-sort or rebuild a list");
        ((System.Collections.IList)Call(network, "GetLiveBeacons")!).Clear();
        Check.Equal(2, Connected().Count, "public snapshot cannot corrupt live registry cache");
        Connected().Clear();
        Check.Equal(2, Connected().Count, "caller cannot corrupt cached graph");
        Set(b.parent, "positionInt", new IntVec3(16, 0, 10));
        Check.Equal(1, Connected().Count, "same-tick movement splits graph immediately");
        Check.Equal(false, ReferenceEquals(original, field.GetValue(network)), "movement rebuilds graph");
        Set(b.parent, "positionInt", new IntVec3(15, 0, 10));
        Check.Equal(2, Connected().Count, "restoration reconnects immediately");
        Set(b.parent, "factionInt", new Faction {def = new FactionDef()});
        Check.Equal(1, Connected().Count, "ownership change removes node immediately");
        Set(b.parent, "factionInt", faction);
        Check.Equal(2, Connected().Count, "ownership restoration detected");
        b.PostDeSpawn(map);
        Check.Equal(1, Connected().Count, "unregister drops cached references");
        b.PostSpawnSetup(false);
        Check.Equal(2, Connected().Count, "registered again reconnects");
    }

    static Type T(string n) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + n) ?? throw new Exception("Missing network type " + n);
    static void Set(object o, string n, object v)
    {
        for (Type? t = o.GetType(); t != null; t = t.BaseType)
        {
            var p = t.GetField(n, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (p != null)
            {
                p.SetValue(o, v);
                return;
            }
        }

        throw new Exception(n);
    }

    static object Get(object o, string p) => o.GetType().GetProperty(p)!.GetValue(o)!;
    static object? Call(object o, string n, params object[] a) => o.GetType().GetMethod(n)!.Invoke(o, a);
    static Map map = null !;
    static MapComponent network = null !;
    static Faction faction = null !;
    static int id;
    static void Run(RuntimeFixture f, Action test)
    {
        var old = Current.Game;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            Set(Current.Game, "maps", new List<Map> { map });
            Set(Current.Game, "tickManager", new TickManager());
            faction = new Faction
            {
                def = new FactionDef
                {
                    isPlayer = true
                }
            };
            network = (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), map)!;
            map.components = new List<MapComponent>
            {
                network
            };
            id = 2000;
            test();
        }
        finally
        {
            Current.Game = old;
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    static ThingComp Make(string name, int x, int z)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = "NetworkTest";
        d.size = new IntVec2(1, 1);
        var b = new Building
        {
            def = d,
            thingIDNumber = ++id
        };
        Set(b, "positionInt", new IntVec3(x, 0, z));
        Set(b, "mapIndexOrState", (sbyte)0);
        Set(b, "factionInt", faction);
        var c = (ThingComp)Activator.CreateInstance(T("Comp" + name))!;
        c.parent = b;
        c.props = (CompProperties)Activator.CreateInstance(T("CompProperties_" + name))!;
        Set(b, "comps", new List<ThingComp> { c });
        c.Initialize(c.props);
        c.PostSpawnSetup(false);
        return c;
    }

    static ThingComp Tank(int x, int z, bool receive = false)
    {
        var c = Make("ManaNetworkEndpoint", x, z);
        var b = c.parent;
        var bank = (ThingComp)Activator.CreateInstance(T("CompMagofuelReservoir"))!;
        bank.parent = b;
        bank.props = (CompProperties)Activator.CreateInstance(T("CompProperties_MagofuelReservoir"))!;
        b.AllComps.Add(bank);
        Set(bank, "storedMana", receive ? 0f : 100f);
        if (receive)
            c.GetType().GetProperty("ManaMode")!.SetValue(c, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        return c;
    }

    static float Mana(ThingComp c) => (float)Get(c.parent.AllComps.Single(x => x.GetType().Name == "CompMagofuelReservoir"), "StoredMana");
    static void Tick(int t)
    {
        Set(Find.TickManager, "ticksGameInt", t);
        network.MapComponentTick();
    }

    static void ThrusterFuel()
    {
        var a = AddFuel(Tank(10, 9), 40);
        var b = AddFuel(Tank(11, 9), 60);
        var groupType = T("MagicalThrusterFuelGroup");
        var group = Activator.CreateInstance(groupType, new object[] { new List<CompRefuelable> { a, a, b }, 10f })!;
        Check.Equal(100f, (float)Get(group, "Fuel"), "physical sources deduplicated");
        Check.Equal(10f, (float)Get(group, "Range"), "fuel range capped by connected thrusters");
        Check.Equal(25f, (float)Call(group, "ConsumeRatio", .5f, false)!, "dry run predicts bounded fuel use");
        Check.Equal(100f, a.Fuel+b.Fuel, "dry run never pays");
        Check.Equal(25f, (float)Call(group, "ConsumeRatio", .5f, true)!, "native payment matches dry run");
        Check.Equal(75f, a.Fuel+b.Fuel, "exact physical payment");
        Check.Equal(25f, (float)Call(group, "AddFuel", 25f)!, "refund enters physical reservoirs");
        Check.Equal(100f, a.Fuel+b.Fuel, "refund restores sum");
        Check.Equal(0f, (float)Call(group, "ConsumeRatio", float.NaN, true)!, "invalid ratio rejected");
    }

    static void Budgets()
    {
        Make("ManaBeacon", 10, 10);
        Make("ManaBeacon", 15, 10);
        Make("ManaBeacon", 13, 13);
        var s = Tank(10, 9);
        var a = Tank(15, 9, true);
        var b = Tank(14, 9, true);
        Tick(60);
        Check.Equal(90f, Mana(s), "one component budget across cycle");
        Check.Equal(10f, Mana(a) + Mana(b), "paid amount conserved");
        Tick(60);
        Check.Equal(90f, Mana(s), "duplicate pulse rejected");
        Tick(120);
        Check.Equal(80f, Mana(s), "second finite budget");
        Check.Equal(true, Mana(a) > 0 && Mana(b) > 0, "receivers rotate fairly");
    }

    static CompRefuelable AddFuel(ThingComp endpoint, float amount)
    {
        var d = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        d.defName = "TM_Magofuel";
        d.virtualDefs = new List<ThingDef>();
        var filter = new ThingFilter();
        filter.SetAllow(d, true);
        var c = new CompRefuelable
        {
            parent = endpoint.parent,
            props = new CompProperties_Refuelable
            {
                fuelCapacity = 100,
                fuelFilter = filter,
                consumeFuelOnlyWhenUsed = true
            }
        };
        endpoint.parent.AllComps.Add(c);
        c.Refuel(amount);
        return c;
    }

    static void Fuel()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9);
        var r = Tank(11, 9, true);
        r.GetType().GetProperty("FuelMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        var sf = AddFuel(s, 20);
        var rf = AddFuel(r, 0);
        rf.TargetFuelLevel = 3;
        Tick(60);
        Check.Equal(17f, sf.Fuel, "source physically paid");
        Check.Equal(3f, rf.Fuel, "native target limits receipt");
        Tick(120);
        Check.Equal(17f, sf.Fuel, "full target never loses fuel");
        rf.TargetFuelLevel = 100;
        Set(rf.Props, "fuelMultiplier", 2f);
        Tick(180);
        Check.Equal(3f, rf.Fuel, "unsafe fuel multiplier rejected");
        Check.Equal(17f, sf.Fuel, "unsafe receiver retains source fuel");
        Set(rf.Props, "fuelMultiplier", 1f);
        Tick(240);
        Check.Equal(8f, rf.Fuel, "restored native receiver gets shared five unit budget");
        var other = Tank(12, 9, true);
        other.GetType().GetProperty("FuelMode")!.SetValue(other, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        var otherFuel = AddFuel(other, 0);
        rf.TargetFuelLevel = 10.5f;
        Tick(300);
        Check.Equal(10f, rf.Fuel, "only whole units enter fractional target gap");
        Check.Equal(3f, otherFuel.Fuel, "second receiver shares remaining five-unit component budget");
        Check.Equal(7f, sf.Fuel, "shared fuel budget cannot multiply across receivers");
        sf.ConsumeFuel(6.5f);
        Tick(360);
        Check.Equal(.5f, sf.Fuel, "fractional native source remainder never transferred as whole item");
        Check.Equal(13f, rf.Fuel + otherFuel.Fuel, "fractional source cannot mint fuel");
    }

    public sealed class NetworkSave : IExposable
    {
        public void ExposeData() => network.ExposeData();
    }

    static void Save(RuntimeFixture f)
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9);
        var r = Tank(11, 9, true);
        var second = Tank(12, 9, true);
        r.GetType().GetProperty("FuelMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Off"));
        r.GetType().GetProperty("ManaTarget")!.SetValue(r, .3f);
        Tick(60);
        var save = new NetworkSave();
        string path = Path.Combine(f.WorkingDirectory, "network.xml");
        try
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref save, "network");
            r.PostExposeData();
            Scribe.saver.FinalizeSaving();
        }
        finally
        {
            Scribe.ForceStop();
        }

        network = (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), map)!;
        map.components = new List<MapComponent>
        {
            network
        };
        r.GetType().GetProperty("ManaMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Off"));
        r.GetType().GetProperty("FuelMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        r.GetType().GetProperty("ManaTarget")!.SetValue(r, 1f);
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref save, "network");
            r.PostExposeData();
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            Scribe.ForceStop();
        }

        Check.Equal(.3f, (float)Get(r, "ManaTarget"), "real endpoint Scribe retains target");
        Check.Equal("Receive", Get(r, "ManaMode").ToString()!, "real endpoint Scribe retains channel mode");
        Check.Equal("Off", Get(r, "FuelMode").ToString()!, "real Scribe restores independently changed fuel mode");
        second.PostSpawnSetup(true);
        s.PostSpawnSetup(true);
        r.PostSpawnSetup(true);
        Make("ManaBeacon", 10, 10);
        Tick(60);
        Check.Equal(10f, Mana(r), "loaded deadline blocks replay");
        Tick(120);
        Check.Equal(10f, Mana(second), "saved receiver cursor rotates to next receiver after rebuilt graph");
        Check.Equal(10f, Mana(r), "loaded cursor does not favor first receiver again");
        Tick(120);
        Check.Equal(20f, Mana(r) + Mana(second), "loaded repeated tick is idempotent");
    }

    static void Footprint()
    {
        var a = Make("ManaBeacon", 10, 10);
        var b = Make("ManaBeacon", 18, 10);
        var s = Tank(9, 10);
        var r = Tank(14, 10, true);
        Check.Equal(a, Call(network, "AttachedBeacon", r)!, "equal distance picks lower ID beacon");
        Tick(60);
        Check.Equal(10f, Mana(r), "endpoint chooses only one disconnected component");
        Set(r.parent, "positionInt", new IntVec3(15, 0, 10));
        Tick(120);
        Check.Equal(10f, Mana(r), "moving nearest attachment splits resource route");
        var bridge = Make("ManaBeacon", 14, 10);
        Tick(180);
        Check.Equal(20f, Mana(r), "branch merge restores single budget");
        bridge.PostDeSpawn(map);
        Set(bridge.parent, "mapIndexOrState", (sbyte)-1);
        Tick(240);
        Check.Equal(20f, Mana(r), "branch despawn splits topology");
        r.parent.def.size = new IntVec2(5, 1);
        Set(r.parent, "positionInt", new IntVec3(3, 0, 10));
        Check.Equal(a, Call(network, "AttachedBeacon", r)!, "occupied rectangle edge within five attaches");
        Set(r.parent, "rotationInt", Rot4.East);
        Check.Equal(true, Call(network, "AttachedBeacon", r) == null, "rotation updates occupied rectangle distance");
    }

    public sealed class CallbackComp : ThingComp
    {
        public Action<string>? Callback;
        public override void ReceiveCompSignal(string signal) => Callback?.Invoke(signal);
    }

    static void Callbacks()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9);
        var r = Tank(11, 9, true);
        r.GetType().GetProperty("FuelMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        var sf = AddFuel(s, 5);
        var rf = AddFuel(r, 0);
        var callback = new CallbackComp
        {
            parent = s.parent
        };
        s.parent.AllComps.Add(callback);
        callback.Callback = signal =>
        {
            if (signal == "RanOutOfFuel")
                rf.TargetFuelLevel = 0;
        };
        Tick(60);
        Check.Equal(5f, sf.Fuel, "native debit callback filling target rolls back source");
        Check.Equal(0f, rf.Fuel, "invalidated receiver not credited");
        callback.Callback = null;
        rf.TargetFuelLevel = 100;
        var receive = new CallbackComp
        {
            parent = r.parent
        };
        r.parent.AllComps.Add(receive);
        receive.Callback = signal =>
        {
            if (signal == "Refueled")
                network.MapComponentTick();
        };
        Tick(120);
        Check.Equal(0f, sf.Fuel, "source debited once before callback credit");
        Check.Equal(5f, rf.Fuel, "native refuel signal cannot reenter pulse");
    }

    static void ThrowingCallback()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9);
        var r = Tank(11, 9, true);
        r.GetType().GetProperty("FuelMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        var sf = AddFuel(s, 5);
        var rf = AddFuel(r, 0);
        var cb = new CallbackComp
        {
            parent = s.parent,
            Callback = signal =>
            {
                if (signal == "RanOutOfFuel")
                    throw new InvalidOperationException("test callback");
            }
        };
        s.parent.AllComps.Add(cb);
        try
        {
            Tick(60);
        }
        catch (InvalidOperationException e)when (e.Message == "test callback")
        {
        }

        Check.Equal(5f, sf.Fuel + rf.Fuel, "throwing native debit callback cannot lose fuel");
        cb.Callback = null;
        var dest = new CallbackComp
        {
            parent = r.parent,
            Callback = signal =>
            {
                if (signal == "Refueled")
                    throw new InvalidOperationException("test credit");
            }
        };
        r.parent.AllComps.Add(dest);
        try
        {
            Tick(120);
        }
        catch (InvalidOperationException e)when (e.Message == "test credit")
        {
        }

        Check.Equal(5f, sf.Fuel + rf.Fuel, "throwing post-credit callback cannot duplicate refund");
        Check.Equal(5f, rf.Fuel, "credit committed before native callback exception");
        Tick(120);
        Check.Equal(5f, rf.Fuel, "exception cannot replay same pulse");
    }

    static void Denials()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9);
        var r = Tank(11, 9, true);
        r.GetType().GetProperty("ManaTarget")!.SetValue(r, .1f);
        var bank = r.parent.AllComps.Single(c => c.GetType().Name == "CompMagofuelReservoir");
        Set(bank, "storedMana", 98f);
        Tick(60);
        Check.Equal(100f, Mana(r), "mana target bounds partial delivery");
        Check.Equal(98f, Mana(s), "only accepted amount paid");
        Tick(120);
        Check.Equal(98f, Mana(s), "full target keeps source");
        Set(bank, "storedMana", 0f);
        r.GetType().GetProperty("ManaMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Off"));
        Tick(180);
        Check.Equal(0f, Mana(r), "disabled channel rejects");
        r.GetType().GetProperty("ManaMode")!.SetValue(r, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        Set(r.parent, "factionInt", new Faction { def = new FactionDef { isPlayer = true } });
        Tick(240);
        Check.Equal(0f, Mana(r), "distinct player factions cannot share resources");
        Set(r.parent, "factionInt", faction);
        Find.Maps.Add((Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)));
        Set(r.parent, "mapIndexOrState", (sbyte)1);
        Tick(300);
        Check.Equal(0f, Mana(r), "other map rejects stale registry entry");
        Set(r.parent, "mapIndexOrState", (sbyte)0);
        Tick(360);
        Check.Equal(10f, Mana(r), "return to original map reconnects");
    }

    static void Topology()
    {
        var first = Make("ManaBeacon", 10, 10);
        var last = Make("ManaBeacon", 16, 10);
        var s = Tank(9, 10);
        var r = Tank(17, 10, true);
        Tick(60);
        Check.Equal(0f, Mana(r), "six cell gap disconnected despite overlap");
        Set(last.parent, "positionInt", new IntVec3(15, 0, 10));
        Tick(120);
        Check.Equal(10f, Mana(r), "five cell gap joins");
        Set(last.parent, "factionInt", new Faction { def = new FactionDef() });
        Tick(180);
        Check.Equal(10f, Mana(r), "ownership change breaks route");
        Set(last.parent, "factionInt", faction);
        Set(r.parent, "mapIndexOrState", (sbyte)-1);
        Tick(240);
        Check.Equal(10f, Mana(r), "unspawned rejects cached endpoint");
    }
}

