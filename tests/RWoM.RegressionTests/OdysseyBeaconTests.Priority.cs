using System.Xml.Linq;
using RimWorld;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> PriorityCases()
    {
        yield return new("Odyssey.NetworkPriority.ScarcityAndIndependentChannels", f => Run(f, PriorityScarcity));
        yield return new("Odyssey.NetworkPriority.EqualRankRotationAndModeChanges", f => Run(f, PriorityRotation));
        yield return new("Odyssey.NetworkPriority.DefaultsAndScribe", f => Run(f, () => PrioritySave(f)));
        yield return new("Odyssey.NetworkPriority.CallbackConservation", f => Run(f, PriorityCallbacks));
        yield return new("Odyssey.NetworkPriority.ActualReceiversAndComponentDefaults", f => Run(f, PriorityEligibility));
    }
    static void Priority(ThingComp c, bool mana, int value) => c.GetType().GetProperty(mana ? "ManaPriority" : "FuelPriority")!.SetValue(c, value);
    static int Rank(ThingComp c, bool mana) => (int)Get(c, mana ? "ManaPriority" : "FuelPriority");
    static void PriorityScarcity()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9); var low = Tank(11, 9, true); var high = Tank(12, 9, true);
        // Real device defaults must beat registration order under the shared budget.
        high.parent.def.defName = "TM_MagicalOxygen";
        Tick(60);
        Check.Equal(10f, Mana(high), "life support default served before ordinary tank");
        Check.Equal(0f, Mana(low), "scarce budget leaves lower rank waiting");
        Priority(low, false, 3); Priority(high, false, 1);
        ((CompManaNetworkEndpoint)low).FuelMode = ((CompManaNetworkEndpoint)high).FuelMode = ManaNetworkMode.Receive;
        var sf = AddFuel(s, 20); var lf = AddFuel(low, 0); var hf = AddFuel(high, 0);
        Tick(120);
        Check.Equal(20f, Mana(high), "mana remains independently high");
        Check.Equal(5f, lf.Fuel, "fuel follows its own priority");
        Check.Equal(0f, hf.Fuel, "mana rank does not override fuel rank");
        Check.Equal(15f, sf.Fuel, "one native fuel budget");
        Check.Equal(100f, Mana(s) + Mana(low) + Mana(high), "mana conserved");
    }
    static void PriorityRotation()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9); var low = Tank(11, 9, true); var a = Tank(12, 9, true); var b = Tank(13, 9, true);
        Priority(a, true, 3); Priority(b, true, 3);
        Tick(60); Tick(120);
        Check.Equal(10f, Mana(a), "first high receiver receives once");
        Check.Equal(10f, Mana(b), "same-rank receiver rotates independently of lower ranks");
        Check.Equal(0f, Mana(low), "lower rank cannot steal second pulse by rotation");
        ((CompManaNetworkEndpoint)a).ManaMode = ManaNetworkMode.Off;
        ((CompManaNetworkEndpoint)b).ManaMode = ManaNetworkMode.Send;
        Tick(180);
        Check.Equal(10f, Mana(low), "off and sending high ranks do not occupy receiver queue");
        Check.Equal(100f, Mana(s) + Mana(low) + Mana(a) + Mana(b), "mode changes preserve conservation");
    }
    static void PrioritySave(RuntimeFixture f)
    {
        var c = Tank(10, 9, true);
        foreach (var name in new[] { "TM_MagicalOxygen", "TM_ThermalShieldGenerator", "TM_GrandAtmosphereDome" })
        {
            c.parent.def.defName = name;
            Check.Equal(3, Rank(c, true), "life-support default " + name);
            Check.Equal(3, Rank(c, false), "fuel life-support default " + name);
        }
        c.parent.def.defName = "TM_CombatShieldGenerator";
        Check.Equal(2, Rank(c, true), "combat default");
        c.parent.def.defName = "TM_MagofuelReservoir";
        Check.Equal(1, Rank(c, true), "ordinary default");
        Priority(c, true, 3); Priority(c, false, 2);
        var path = Path.Combine(f.WorkingDirectory, "priority.xml");
        try { Scribe.saver.InitSaving(path, "regression"); c.PostExposeData(); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        Priority(c, true, 1); Priority(c, false, 1);
        try { Scribe.loader.InitLoading(path); c.PostExposeData(); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Check.Equal(3, Rank(c, true), "Scribe restores mana rank");
        Check.Equal(2, Rank(c, false), "Scribe restores separate fuel rank");
        var xml = System.Xml.Linq.XDocument.Load(path);
        xml.Descendants("TM_networkManaPriority").Remove(); xml.Descendants("TM_networkFuelPriority").Remove(); xml.Save(path);
        c.parent.def.defName = "TM_MagicalOxygen";
        try { Scribe.loader.InitLoading(path); c.PostExposeData(); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Check.Equal(3, Rank(c, true), "old save gets device default");
        Check.Equal(3, Rank(c, false), "old save gets fuel default");
        Check.Equal(ManaNetworkMode.Receive, ((CompManaNetworkEndpoint)c).ManaMode, "old mode preserved");
    }
    static void PriorityCallbacks()
    {
        Make("ManaBeacon", 10, 10);
        var s = Tank(10, 9); var a = Tank(11, 9, true); var b = Tank(12, 9, true);
        ((CompManaNetworkEndpoint)a).FuelMode = ((CompManaNetworkEndpoint)b).FuelMode = ManaNetworkMode.Receive;
        Priority(a, false, 3); Priority(b, false, 1);
        var sf = AddFuel(s, 5); var af = AddFuel(a, 0); var bf = AddFuel(b, 0);
        var callback = new CallbackComp { parent = s.parent };
        s.parent.AllComps.Add(callback);
        callback.Callback = signal => { if (signal == "RanOutOfFuel") { Priority(a, false, 1); Priority(b, false, 3); network.MapComponentTick(); } };
        Tick(60);
        Check.Equal(5f, sf.Fuel + af.Fuel + bf.Fuel, "callback priority mutation conserves native fuel");
        Check.Equal(5f, af.Fuel, "in-flight channel uses its priority snapshot");
        callback.Callback = null; sf.Refuel(5);
        Tick(120);
        Check.Equal(5f, bf.Fuel, "changed rank applies on next pulse");
        Check.Equal(10f, sf.Fuel + af.Fuel + bf.Fuel, "no replay or budget multiplication");
    }
    static void PriorityEligibility()
    {
        var c = (CompManaNetworkEndpoint)Tank(10, 9, true);
        Check.Equal(true, c.AutomaticManaReceiver, "buffer receiving mana exposes priority");
        Check.Equal(false, c.AutomaticFuelReceiver, "no native fuel buffer has no priority");
        AddFuel(c, 0); c.FuelMode = ManaNetworkMode.Receive;
        Check.Equal(true, c.AutomaticFuelReceiver, "compatible native fuel receiver exposes priority");
        c.Props.directConsumer = true;
        Check.Equal(false, c.AutomaticManaReceiver, "direct consumer never exposes automatic mana priority");
        Check.Equal(false, c.AutomaticFuelReceiver, "direct consumer never exposes automatic fuel priority");
        Make("ManaBeacon", 10, 10); var s = Tank(11, 9); var sf = AddFuel(s, 20);
        Tick(60);
        Check.Equal(0f, Mana(c), "direct consumer cannot absorb automatic mana budget");
        Check.Equal(20f, sf.Fuel, "direct consumer cannot absorb automatic fuel budget");
        c.Props.directConsumer = false;
        c.ManaMode = ManaNetworkMode.Off; c.FuelMode = ManaNetworkMode.Send;
        Check.Equal(false, c.AutomaticManaReceiver, "off mode hides priority");
        Check.Equal(false, c.AutomaticFuelReceiver, "sender hides priority");
        c.parent.AllComps.Add(new CompMagicalThermal { parent = c.parent, props = new CompProperties_MagicalThermal() });
        Check.Equal(3, c.ManaPriority, "component default supports derived device defs");
        Priority(c, true, 1);
        Check.Equal(1, c.ManaPriority, "explicit user rank overrides device default");
    }
}

