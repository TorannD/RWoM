using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;
internal static class OdysseyOxygenTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Oxygen.PlanetIdlePreservesPaymentsAndResumes", f => Run(f, PlanetIdle));
        yield return new("Odyssey.Oxygen.PlanetIdleRejectsNetworkMana", f => Run(f, () => NetworkTransfer(false, true)));
        yield return new("Odyssey.Oxygen.PlanetIdleRejectsAutomaticFuelConversion", f => Run(f, () => NetworkTransfer(true, true)));
        yield return new("Odyssey.Oxygen.CoreManaAndDuplicateTick", f => Run(f, Mana));
        yield return new("Odyssey.Oxygen.MagicyteModeNoFallbackOrDoublePayment", f => Run(f, Magicyte));
        yield return new("Odyssey.Oxygen.CoreOperationalStops", f => Run(f, Stops));
        yield return new("Odyssey.Oxygen.TwoPumpsPartialPaymentAndNetworkBuffer", f => Run(f, TwoPumps));
        yield return new("Odyssey.Oxygen.RealScribeKeepsModeResidueAndDeadline", f => Run(f, () => Save(f)));
        yield return new("Odyssey.Oxygen.NativeFuelSignalsRetainPayments", f => Run(f, Callbacks));
        yield return new("Odyssey.Oxygen.InvalidRequestsFiltersAndWholeRefill", f => Run(f, Invalid));
        yield return new("Odyssey.Oxygen.PaidResidueNeverRoundsUp", f => Run(f, Rounding));
        yield return new("Odyssey.Oxygen.NativeNetworkTickFundsSubclassBuffer", f => Run(f, NetworkTransfer));
        yield return new("Odyssey.FuelReserve.OxygenAtmosphereAccess", f => Run(f, () => NetworkTransfer(true, false, true)));
        yield return new("Odyssey.FuelReserve.OxygenPipeAccess", f => Run(f, () => NetworkTransfer(true, true, true)));
        yield return new("Odyssey.Oxygen.AutomaticFuelConversionOnDemand", f => Run(f, () => NetworkTransfer(true)));
    }

    static Type T(string n) => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey." + n) ?? throw new Exception("Missing oxygen type " + n);
    static void Set(object o, string name, object value)
    {
        for (Type? t = o.GetType(); t != null; t = t.BaseType)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (f != null)
            {
                f.SetValue(o, value);
                return;
            }
        }

        throw new Exception(name);
    }

    static object Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o)!;
    static void Near(double expected, double actual, string why)
    {
        if (Math.Abs(expected - actual) > .00001)
            throw new Exception($"{why}: expected {expected}, actual {actual}");
    }

    static Map map = null !;
    static Faction faction = null !;
    internal static void Run(RuntimeFixture f, Action test)
    {
        var old = Current.Game;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            _ = T("CompMagicalOxygen");
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.info = new MapInfo
            {
                Size = new IntVec3(40, 1, 40),
                isPocketMap = true
            };
            map.pocketTileInfo = new SurfaceTile();
            Set(map.pocketTileInfo, "biome", new BiomeDef { inVacuum = true });
            Set(Current.Game, "maps", new List<Map> { map });
            faction = new Faction
            {
                def = new FactionDef
                {
                    isPlayer = true
                }
            };
            test();
        }
        finally
        {
            Current.Game = old;
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    public sealed class Pump
    {
        public ThingComp Oxygen, Buffer;
        public CompRefuelable Fuel;
        public CompFlickable Flick;
        public CompBreakdownable Breakdown;
        public Pump(int x = 10)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = "TestMagicalOxygen";
            def.size = new IntVec2(2, 2);
            def.hasInteractionCell = true;
            def.interactionCellOffset = new IntVec3(0, 0, -1);
            var parent = new Building
            {
                def = def
            };
            Set(parent, "positionInt", new IntVec3(x, 0, 10));
            Set(parent, "mapIndexOrState", (sbyte)0);
            Set(parent, "factionInt", faction);
            Oxygen = (ThingComp)Activator.CreateInstance(T("CompMagicalOxygen"))!;
            Oxygen.parent = parent;
            Oxygen.props = (CompProperties)Activator.CreateInstance(T("CompProperties_MagicalOxygen"))!;
            Buffer = (ThingComp)Activator.CreateInstance(T("CompOxygenManaBuffer"))!;
            Buffer.parent = parent;
            Buffer.props = (CompProperties)Activator.CreateInstance(T("CompProperties_OxygenManaBuffer"))!;
            var raw = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            raw.defName = "RawMagicyte";
            raw.virtualDefs = new List<ThingDef>();
            var filter = new ThingFilter();
            filter.SetAllow(raw, true);
            Fuel = new CompRefuelable
            {
                parent = parent,
                props = new CompProperties_Refuelable
                {
                    fuelCapacity = 50,
                    fuelConsumptionRate = 0,
                    consumeFuelOnlyWhenUsed = true,
                    fuelFilter = filter
                }
            };
            Flick = new CompFlickable
            {
                parent = parent,
                props = new CompProperties_Flickable()
            };
            Breakdown = new CompBreakdownable
            {
                parent = parent,
                props = new CompProperties_Breakdownable()
            };
            Set(parent, "comps", new List<ThingComp> { Oxygen, Buffer, Fuel, Flick, Breakdown });
        }

        public void Mana(float amount) => Set(Buffer, "storedMana", amount);
        public float Charge => (float)Get(Buffer, "StoredMana");
        public double Credit => (double)Get(Oxygen, "PaidMagicyteMana");

        public void Mode(string name) => Oxygen.GetType().GetProperty("SupplyMode")!.SetValue(Oxygen, Enum.Parse(T("OxygenSupplyMode"), name));
        public float LastSupplied;
        public void Tick(int time)
        {
            Set(Find.TickManager, "ticksGameInt", time);
            LastSupplied = (float)Oxygen.GetType().GetMethod("SupplyPulse")!.Invoke(Oxygen, new object[] { 10f })!;
        }
    }

    static void PlanetIdle()
    {
        var p = new Pump(); p.Mana(5); p.Fuel.Refuel(2); p.Mode("Magicyte");
        p.Tick(60); Near(9.9, p.Credit, "vacuum prepays one crystal");
        map.Biome.inVacuum = false;
        p.Tick(120); Near(0, p.LastSupplied, "planet atmosphere cannot buy output");
        Near(9.9, p.Credit, "planet keeps paid crystal residue");
        Near(1, p.Fuel.Fuel, "planet does not burn another crystal");
        p.Mode("Mana"); p.Tick(180);
        Near(5, p.Charge, "planet keeps stored mana after mode switch");
        p.Oxygen.GetType().GetProperty("OutputMode")!.SetValue(p.Oxygen, Enum.Parse(T("OxygenOutputMode"), "PipeNetwork"));
        p.Tick(240); Near(.1, p.LastSupplied, "pipe demand stays paid on planet");
        Near(4.9, p.Charge, "planet pipe supply debits mana");
        p.Oxygen.GetType().GetProperty("OutputMode")!.SetValue(p.Oxygen, Enum.Parse(T("OxygenOutputMode"), "Atmosphere"));
        p.Tick(300); Near(4.9, p.Charge, "return to atmosphere idles");
        p.Mode("Magicyte"); map.Biome.inVacuum = true; p.Tick(360);
        Near(9.8, p.Credit, "vacuum resumes retained paid residue");
        Near(1, p.Fuel.Fuel, "resume never pays crystal twice");
    }

    static void Mana()
    {
        var p = new Pump();
        p.Mana(1);
        p.Tick(60);
        Near(.1, p.LastSupplied, "pulse supply capped at configured budget");
        Near(.9, p.Charge, "mana paid first");
        p.Tick(60);
        Near(0, p.LastSupplied, "duplicate pulse rejected");
        p.Tick(120);
        Near(.1, p.LastSupplied, "next pulse");
        Near(.8, p.Charge, "second payment");
    }

    static void Magicyte()
    {
        var p = new Pump();
        p.Fuel.Refuel(2);
        p.Tick(60);
        Near(0, p.LastSupplied, "empty mana mode cannot fall back to magicyte");
        Near(2, p.Fuel.Fuel, "inactive fuel untouched");
        p.Mode("Magicyte");
        p.Mana(2);
        p.Tick(120);
        Near(.1, p.LastSupplied, "magicyte funds one pulse");
        Near(1, p.Fuel.Fuel, "one whole magicyte prepays residue");
        Near(9.9, p.Credit, "paid residue retained");
        Near(2, p.Charge, "magicyte mode never pays mana");
        p.Mode("Mana");
        p.Tick(180);
        Near(1.9, p.Charge, "explicit mana mode pays buffer");
        Near(9.9, p.Credit, "switch does not consume or erase magicyte residue");
        p.Mode("Magicyte");
        p.Tick(240);
        Near(9.8, p.Credit, "return uses existing residue");
        Near(1, p.Fuel.Fuel, "no duplicate crystal payment");
    }

    static void TwoPumps()
    {
        var a = new Pump();
        var b = new Pump(15);
        a.Mana(.05f);
        b.Mana(1);
        a.Tick(60);
        Near(.05, a.LastSupplied, "partial last mana returns only paid amount");
        Near(0, a.Charge, "partial charge consumed once");
        b.Tick(60);
        Near(.1, b.LastSupplied, "second pump independently limited");
        Near(.9, b.Charge, "second pump pays itself");
        var source = (ThingComp)Activator.CreateInstance(T("CompMagofuelReservoir"))!;
        source.parent = new ThingWithComps
        {
            def = a.Oxygen.parent.def
        };
        source.props = (CompProperties)Activator.CreateInstance(T("CompProperties_MagofuelReservoir"))!;
        Set(source, "storedMana", 20f);
        var transferred = (float)source.GetType().GetMethod("TransferManaTo")!.Invoke(source, new object[] { a.Buffer, 10f })!;
        Near(10, transferred, "existing network transfer contract accepts oxygen reservoir subclass");
        Near(10, a.Charge, "buffer receives only paid network mana");
        Near(10, (float)Get(source, "StoredMana"), "network source was debited");
        Check.Equal(0, a.Buffer.CompGetGizmosExtra().Count(), "buffer exposes no reservoir conversion or reserve controls");
        Check.Equal(0, a.Buffer.CompFloatMenuOptions(null !).Count(), "buffer exposes no mage charging menu");
    }

    public sealed class PumpSave : IExposable
    {
        public Pump Value = new Pump();
        public void ExposeData()
        {
            Value.Oxygen.PostExposeData();
            Value.Buffer.PostExposeData();
            Value.Fuel.PostExposeData();
        }
    }

    static void Save(RuntimeFixture f)
    {
        var saved = new PumpSave();
        saved.Value.Mode("Magicyte");
        saved.Value.Fuel.Refuel(3);
        saved.Value.Mana(7);
        saved.Value.Tick(60);
        string path = Path.Combine(f.WorkingDirectory, "oxygen.xml");
        try
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref saved, "pump");
            Scribe.saver.FinalizeSaving();
        }
        finally
        {
            Scribe.ForceStop();
        }

        PumpSave loaded = null !;
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref loaded, "pump");
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            Scribe.ForceStop();
        }

        Check.Equal("Magicyte", Get(loaded.Value.Oxygen, "SupplyMode").ToString()!, "mode survives real Scribe");
        Near(9.9, loaded.Value.Credit, "paid local residue survives");
        Near(2, loaded.Value.Fuel.Fuel, "physical magicyte remains integral");
        Near(7, loaded.Value.Charge, "independent mana buffer survives");
        loaded.Value.Tick(60);
        Near(0, loaded.Value.LastSupplied, "load cannot repeat same pulse");
        loaded.Value.Tick(6000);
        Near(.1, loaded.Value.LastSupplied, "missed ticks do not catch up supply");
        Near(9.8, loaded.Value.Credit, "only one current pulse paid after load gap");
        Set(loaded.Value.Oxygen.parent, "mapIndexOrState", (sbyte)-1);
        var holder = new ThingOwner<Thing>();
        holder.TryAdd(loaded.Value.Oxygen.parent);
        loaded.Value.Tick(6060);
        Near(9.8, loaded.Value.Credit, "held machine retains paid residue without operating");
        holder.Remove(loaded.Value.Oxygen.parent);
        Set(loaded.Value.Oxygen.parent, "mapIndexOrState", (sbyte)0);
        loaded.Value.Tick(6120);
        Near(9.7, loaded.Value.Credit, "reinstalled machine resumes original paid residue");
    }

    public sealed class SignalComp : ThingComp
    {
        public Action<string>? Action;
        public override void ReceiveCompSignal(string signal) => Action?.Invoke(signal);
    }

    static void Callbacks()
    {
        var p = new Pump();
        p.Mode("Magicyte");
        p.Mana(5);
        p.Fuel.Refuel(1);
        var signal = new SignalComp
        {
            parent = p.Oxygen.parent
        };
        p.Oxygen.parent.AllComps.Add(signal);
        signal.Action = s =>
        {
            if (s == "RanOutOfFuel")
            {
                p.Mode("Mana");
                p.Tick(60);
                Near(0, p.LastSupplied, "native callback cannot reenter payment");
            }
        };
        p.Tick(60);
        Near(0, p.LastSupplied, "mode change during native debit rejects output");
        Near(10, p.Credit, "interrupted native debit retains full paid residue");
        Near(0, p.Fuel.Fuel, "exact whole physical magicyte paid");
        Near(5, p.Charge, "callback did not fall back to bank");
        p.Mode("Magicyte");
        signal.Action = null;
        p.Tick(120);
        Near(.1, p.LastSupplied, "next pulse uses retained payment");
        Near(9.9, p.Credit, "retained payment consumed once");
        var q = new Pump(15);
        q.Mode("Magicyte");
        q.Fuel.Refuel(1);
        var throwing = new SignalComp
        {
            parent = q.Oxygen.parent,
            Action = s =>
            {
                if (s == "RanOutOfFuel")
                    throw new InvalidOperationException("oxygen debit signal");
            }
        };
        q.Oxygen.parent.AllComps.Add(throwing);
        try
        {
            q.Tick(180);
        }
        catch (TargetInvocationException e)when (e.InnerException?.Message == "oxygen debit signal")
        {
        }

        Near(10, q.Credit, "throw after native debit cannot lose paid unit");
        Near(0, q.Fuel.Fuel, "throw never duplicates physical unit");
        throwing.Action = null;
        q.Tick(180);
        Near(0, q.LastSupplied, "throwing pulse cannot replay");
        q.Tick(240);
        Near(.1, q.LastSupplied, "next pulse uses paid unit after callback exception");
    }

    static void Invalid()
    {
        var p = new Pump();
        p.Mana(2);
        Set(Find.TickManager, "ticksGameInt", 60);
        foreach (float request in new[]
        {
            0f,
            -1f,
            float.NaN,
            float.PositiveInfinity
        }

        )
            Near(0, (float)p.Oxygen.GetType().GetMethod("SupplyPulse")!.Invoke(p.Oxygen, new object[] { request })!, "invalid request cannot consume");
        Near(2, p.Charge, "invalid requests preserve bank");
        p.Mode("Magicyte");
        p.Fuel.Refuel(1);
        Set(p.Fuel.Props, "fuelMultiplier", 2f);
        p.Tick(60);
        Near(0, p.LastSupplied, "custom multiplier rejected");
        Near(1, p.Fuel.Fuel, "custom multiplier cannot create paid balance");
        Set(p.Fuel.Props, "fuelMultiplier", 1f);
        p.Tick(120);
        Near(.1, p.LastSupplied, "valid native unit prepays");
        var raw = p.Fuel.Props.fuelFilter.AnyAllowedDef;
        raw.thingClass = typeof(Thing);
        raw.destroyable = true;
        raw.stackLimit = 75;
        raw.category = ThingCategory.Item;
        var stack = new Thing
        {
            def = raw,
            stackCount = 1
        };
        var originalMaps = Find.Maps.ToArray();
        Set(p.Oxygen.parent, "mapIndexOrState", (sbyte)-1);
        Find.Maps.Clear();
        try
        {
            p.Fuel.Refuel(new List<Thing> { stack });
        }
        finally
        {
            Find.Maps.AddRange(originalMaps);
            Set(p.Oxygen.parent, "mapIndexOrState", (sbyte)0);
        }

        Near(1, p.Fuel.Fuel, "native refill accepts exactly whole item after prepay");
        Check.Equal(true, stack.Destroyed, "whole raw magicyte item consumed by native refill");
        Near(9.9, p.Credit, "native refill does not erase paid fractional residue");
    }

    static void Rounding()
    {
        var pump = new Pump();
        pump.Mode("Magicyte");
        Set(pump.Oxygen, "paidMagicyteMana", .05d);
        pump.Tick(60);
        Check.Equal(true, (double)pump.LastSupplied <= .05d, "float output cannot exceed paid double residue");
        Check.Equal(.05d, (double)pump.LastSupplied + pump.Credit, "rounding remainder stays owned by generator");
    }

    static void NetworkTransfer() => NetworkTransfer(false);
    static void NetworkTransfer(bool autoFuel, bool planet = false, bool reserve = false)
    {
        var graph = (MapComponent)Activator.CreateInstance(T("MapComponent_ManaNetwork"), map)!;
        var atmosphere = (MapComponent)Activator.CreateInstance(T("MapComponent_MagicalAtmosphere"), map)!;
        map.components = new List<MapComponent>
        {
            graph,
            atmosphere
        };
        var pump = new Pump();
        pump.Fuel.Refuel(1);
        var receiveProps = (CompProperties)Activator.CreateInstance(T("CompProperties_ManaNetworkEndpoint"))!;
        Set(receiveProps, "supportsFuel", false);
        Set(receiveProps, "defaultManaMode", Enum.Parse(T("ManaNetworkMode"), "Receive"));
        Set(receiveProps, "defaultFuelMode", Enum.Parse(T("ManaNetworkMode"), "Off"));
        var receiver = (ThingComp)Activator.CreateInstance(T("CompManaNetworkEndpoint"))!;
        receiver.parent = pump.Oxygen.parent;
        receiver.Initialize(receiveProps);
        pump.Oxygen.parent.AllComps.Add(receiver);
        receiver.PostSpawnSetup(false);
        pump.Oxygen.PostSpawnSetup(false);
        Check.Equal("Receive", Get(receiver, "ManaMode").ToString()!, "oxygen starts with configured receive mode");
        Check.Equal("Off", Get(receiver, "FuelMode").ToString()!, "oxygen never joins physical fuel channel");
        receiver.GetType().GetProperty("ManaTarget")!.SetValue(receiver, .1f);
        var source = (ThingComp)Activator.CreateInstance(T("CompMagofuelReservoir"))!;
        source.props = (CompProperties)Activator.CreateInstance(T("CompProperties_MagofuelReservoir"))!;
        var parent = new Building
        {
            def = pump.Oxygen.parent.def,
            thingIDNumber = 200
        };
        Set(parent, "positionInt", new IntVec3(9, 0, 10));
        Set(parent, "mapIndexOrState", (sbyte)0);
        Set(parent, "factionInt", faction);
        Set(parent, "comps", new List<ThingComp>());
        source.parent = parent;
        parent.AllComps.Add(source);
        Set(source, "storedMana", autoFuel ? 0f : 20f);
        var sender = (ThingComp)Activator.CreateInstance(T("CompManaNetworkEndpoint"))!;
        sender.parent = parent;
        sender.Initialize((CompProperties)Activator.CreateInstance(T("CompProperties_ManaNetworkEndpoint"))!);
        parent.AllComps.Add(sender);
        sender.PostSpawnSetup(false);
        var beacon = (ThingComp)Activator.CreateInstance(T("CompManaBeacon"))!;
        var beaconParent = new Building
        {
            def = parent.def,
            thingIDNumber = 201
        };
        Set(beaconParent, "positionInt", new IntVec3(12, 0, 10));
        Set(beaconParent, "mapIndexOrState", (sbyte)0);
        Set(beaconParent, "factionInt", faction);
        Set(beaconParent, "comps", new List<ThingComp> { beacon });
        beacon.parent = beaconParent;
        beacon.Initialize((CompProperties)Activator.CreateInstance(T("CompProperties_ManaBeacon"))!);
        beacon.PostSpawnSetup(false);
        if (planet)
        {
            var fuelDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); fuelDef.defName = "TM_Magofuel";
            fuelDef.virtualDefs = new List<ThingDef>();
            var filter = new ThingFilter(); filter.SetAllow(fuelDef, true);
            var fuel = new CompRefuelable { parent = parent, props = new CompProperties_Refuelable { fuelCapacity = 500, fuelFilter = filter, consumeFuelOnlyWhenUsed = true } };
            parent.AllComps.Add(fuel); fuel.Refuel(3);
            if (reserve) { parent.def.defName = "TM_MagofuelReservoir"; source.GetType().GetProperty("FuelReserve")!.SetValue(source, 3f); receiver.GetType().GetProperty("ManaPriority")!.SetValue(receiver, 1); }
            void Pulse(int tick) { Set(Find.TickManager, "ticksGameInt", tick); graph.MapComponentTick(); }
            map.Biome.inVacuum = false; pump.Mana(1);
            Pulse(60);
            Near(1, pump.Charge, "planet atmosphere receives no hidden network refill");
            Near(3, fuel.Fuel, "planet atmosphere triggers no remote fuel conversion");
            Near(autoFuel ? 0 : 20, (float)Get(source, "StoredMana"), "idle leaves source mana unchanged");
            pump.Oxygen.GetType().GetProperty("OutputMode")!.SetValue(pump.Oxygen, Enum.Parse(T("OxygenOutputMode"), "PipeNetwork"));
            Pulse(120);
            Near(10, pump.Charge, "planet pipe mode resumes network demand");
            Near(autoFuel ? 2 : 3, fuel.Fuel, "pipe refill pays remote fuel when needed");
            Near(autoFuel ? 1 : 11, (float)Get(source, "StoredMana"), "pipe refill transfers exactly paid deficit");
            pump.Oxygen.GetType().GetProperty("OutputMode")!.SetValue(pump.Oxygen, Enum.Parse(T("OxygenOutputMode"), "Atmosphere"));
            pump.Mana(9); Pulse(180); Near(9, pump.Charge, "atmosphere mode switch restores idle");
            map.Biome.inVacuum = true; Pulse(240); Near(10, pump.Charge, "vacuum resumes refill without resetting modes");
            Near(1, pump.Fuel.Fuel, "local magicyte never charged by network");
            return;
        }
        if (autoFuel)
        {
            var fuelDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            fuelDef.defName = "TM_Magofuel";
            fuelDef.virtualDefs = new List<ThingDef>();
            var filter = new ThingFilter(); filter.SetAllow(fuelDef, true);
            var fuel = new CompRefuelable { parent = parent, props = new CompProperties_Refuelable { fuelCapacity = 500, fuelFilter = filter, consumeFuelOnlyWhenUsed = true } };
            parent.AllComps.Add(fuel); fuel.Refuel(3);
            if (reserve) { parent.def.defName = "TM_MagofuelReservoir"; source.GetType().GetProperty("FuelReserve")!.SetValue(source, 3f); receiver.GetType().GetProperty("ManaPriority")!.SetValue(receiver, 1); }
            void Pulse(int tick) { Set(Find.TickManager, "ticksGameInt", tick); graph.MapComponentTick(); }
            Pulse(60);
            Near(10, pump.Charge, "empty reservoir automatically supplies oxygen from magofuel");
            Near(2, fuel.Fuel, "one whole magofuel paid");
            Pulse(60); Near(2, fuel.Fuel, "same tick cannot convert again");
            pump.Mana(9.9f); Pulse(120);
            Near(10, pump.Charge, "fractional oxygen deficit refilled");
            Near(9.9, (float)Get(source, "StoredMana"), "unused whole-fuel yield stays in reservoir");
            Near(1, fuel.Fuel, "fractional deficit consumes one whole unit only");
            Pulse(180); Near(1, fuel.Fuel, "full receiver does not burn fuel");
            Set(source, "storedMana", 0f); pump.Mana(0);
            Set(pump.Flick, "switchOnInt", false); Pulse(240); Near(1, fuel.Fuel, "off oxygen does not request conversion");
            Set(pump.Flick, "switchOnInt", true); pump.Mode("Magicyte"); Pulse(300); Near(1, fuel.Fuel, "local magicyte mode does not burn remote fuel");
            pump.Mode("Mana");
            receiver.GetType().GetProperty("ManaMode")!.SetValue(receiver, Enum.Parse(T("ManaNetworkMode"), "Off"));
            Pulse(360); Near(1, fuel.Fuel, "disabled mana channel cannot convert");
            receiver.GetType().GetProperty("ManaMode")!.SetValue(receiver, Enum.Parse(T("ManaNetworkMode"), "Receive"));
            Set(beaconParent, "positionInt", new IntVec3(30, 0, 30)); Pulse(420); Near(1, fuel.Fuel, "disconnected receiver cannot convert");
            Set(beaconParent, "positionInt", new IntVec3(12, 0, 10));
            var signal = new SignalComp { parent = parent, Action = s => { if (s == "RanOutOfFuel") throw new InvalidOperationException("conversion test"); } };
            parent.AllComps.Add(signal);
            try { Pulse(480); } catch (InvalidOperationException e) when (e.Message == "conversion test") { }
            Near(0, fuel.Fuel, "native callback already debited physical fuel");
            Near(10, (float)Get(source, "StoredMana"), "throw after debit retains paid converted charge");
            Near(0, pump.Charge, "throw does not credit receiver");
            signal.Action = null; Pulse(480); Near(0, pump.Charge, "failed pulse cannot replay");
            Pulse(540); Near(10, pump.Charge, "next pulse transfers retained charge without second payment");
            pump.Tick(540); Near(.1, pump.LastSupplied, "automatically converted fuel funds oxygen supply");
            Set(source, "storedMana", 0f); pump.Mana(0); fuel.Refuel(1);
            signal.Action = s => { if (s == "RanOutOfFuel") Set(beaconParent, "positionInt", new IntVec3(30, 0, 30)); };
            Pulse(600);
            Near(0, pump.Charge, "callback disconnect prevents delivery through stale network");
            Near(10, (float)Get(source, "StoredMana"), "callback disconnect preserves paid charge in source");
            Near(0, fuel.Fuel, "callback disconnect does not duplicate fuel");
            Near(1, pump.Fuel.Fuel, "oxygen local magicyte remains untouched");
            return;
        }
        Set(Find.TickManager, "ticksGameInt", 60);
        graph.MapComponentTick();
        Near(10, pump.Charge, "real network tick finds derived oxygen buffer via GetComp base type");
        Near(10, (float)Get(source, "StoredMana"), "native network transfer debits the A2 source");
        Near(1, pump.Fuel.Fuel, "mana channel never consumes local raw magicyte");
        pump.Tick(60);
        Near(.1, pump.LastSupplied, "received network charge funds oxygen supply");
        Set(Find.TickManager, "ticksGameInt", 120);
        graph.MapComponentTick();
        Near(10, pump.Charge, "next network tick honors configured target and only restores spent amount");
        Near(9.9, (float)Get(source, "StoredMana"), "top-up debits only accepted fraction");
        pump.Oxygen.PostDeSpawn(map);
        receiver.PostDeSpawn(map);
    }

    static void Stops()
    {
        var p = new Pump();
        p.Mana(5);
        Set(p.Flick, "switchOnInt", false);
        p.Tick(60);
        Near(5, p.Charge, "switch off stops");
        Set(p.Flick, "switchOnInt", true);
        Set(p.Breakdown, "brokenDownInt", true);
        p.Tick(120);
        Near(5, p.Charge, "breakdown stops");
        Set(p.Breakdown, "brokenDownInt", false);
        Set(p.Oxygen.parent, "mapIndexOrState", (sbyte)-1);
        p.Tick(180);
        Near(5, p.Charge, "packed pump stops");
        Set(p.Oxygen.parent, "mapIndexOrState", (sbyte)0);
        Set(p.Oxygen.parent, "factionInt", new Faction { def = new FactionDef() });
        p.Tick(240);
        Near(5, p.Charge, "ownership loss stops");
    }
}

