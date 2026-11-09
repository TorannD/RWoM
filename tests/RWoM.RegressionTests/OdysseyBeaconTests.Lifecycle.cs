using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> LifecycleCases()
    {
        yield return new("Odyssey.Lifecycle.NativeRotatedNetworkHandoff", f => Run(f, () => ThrusterEnvironment(RotatedNetworkHandoff)));
    }

    static void RotatedNetworkHandoff()
    {
        var active = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previous = active.GetValue(null);
        try { active.SetValue(null, true); RotatedNetworkHandoffActive(); }
        finally { active.SetValue(null, previous); }
    }

    static void RotatedNetworkHandoffActive()
    {
        var floors = new HashSet<IntVec3>();
        for (int x = 5; x <= 20; x++) for (int z = 5; z <= 20; z++) floors.Add(new IntVec3(x, 0, z));
        var engine = NativeEngine(floors);
        // Native GetComp expects a nonempty comp list (or null), as a real engine has.
        engine.AllComps.Add(new CallbackComp { parent = engine, props = new CompProperties() });
        var beacon = (CompManaBeacon)Make("ManaBeacon", 10, 10);
        var source = (CompManaNetworkEndpoint)Tank(10, 9);
        source.parent.def.defName = "TM_MagofuelReservoir";
        var fuel = AddFuel(source, 80);
        var thruster = NativeThruster(engine, 12, 10, 10);
        var endpoint = thruster.parent.GetComp<CompManaNetworkEndpoint>();
        var members = new ThingWithComps[] { engine, beacon.parent, source.parent, thruster.parent };
        foreach (var member in members)
        {
            member.def.bringAlongOnGravship = true;
            member.def.category = ThingCategory.Building;
            member.def.rotatable = true;
        }
        Check.Equal(1, MagicalThrusterFuel.Sources(thruster).Count, "original map has one source");
        var pending = MagicalThrusterFuel.Group(engine, thruster);
        var ship = (Gravship)Activator.CreateInstance(typeof(Gravship), true)!;
        Set(ship, "engine", engine);
        var capture = typeof(Gravship).GetMethod("AddThing", BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var member in members) capture.Invoke(ship, [member, member.Position - engine.Position]);
        Check.Equal(4, ship.Things.Count(), "native capture includes engine, beacon, tank and thruster");
        Check.Equal(true, ship.Thrusters.Contains(thruster.parent), "native capture recognizes active magical thruster");

        var origin = map;
        var oldNetwork = (MapComponent_ManaNetwork)network;
        foreach (var c in new ThingComp[] { beacon, source, endpoint }) c.PostDeSpawn(origin, DestroyMode.WillReplace);
        foreach (var member in members) member.ForceSetStateToUnspawned();
        Check.Equal(0, oldNetwork.GetLiveBeacons().Count, "launch removes original map registry");
        Check.Equal(0f, pending.ConsumeAmount(5, true), "captured prelaunch payment handle cannot debit in transit");
        Check.Equal(80f, fuel.Fuel, "transit preserves physical fuel");
        Check.Equal(100f, Mana(source), "transit preserves mana");

        var destination = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        var newNetwork = new MapComponent_ManaNetwork(destination);
        destination.components = [newNetwork];
        Set(Current.Game, "maps", new List<Map> { origin, destination });
        var anchor = new IntVec3(40, 0, 40);
        for (int rotation = 0; rotation < 4; rotation++)
        {
            Set(ship, "rotation", new Rot4(rotation));
            var placements = ship.ThingPlacements;
            foreach (var member in members)
            {
                Set(member, "positionInt", anchor + placements[member].local);
                Set(member, "rotationInt", placements[member].rotation);
                Set(member, "mapIndexOrState", (sbyte)1);
            }
            floors.Clear();
            foreach (var member in members) floors.UnionWith(member.OccupiedRect());
            foreach (var c in new ThingComp[] { beacon, source, endpoint }) c.PostSpawnSetup(false);
            Check.Equal(1, newNetwork.GetLiveBeacons().Count, "landing registration is idempotent");
            Check.Equal(0, oldNetwork.GetLiveBeacons().Count, "old map cannot regain moved nodes");
            var sources = MagicalThrusterFuel.Sources(thruster);
            Check.Equal(1, sources.Count, "native rotated placement preserves network adjacency");
            Check.Equal(true, ReferenceEquals(fuel, sources.Single()), "landing reuses the original physical reservoir");
            Check.Equal(10f, MagicalThrusterFuel.Group(engine, thruster).Range, "rotated ship retains funded range");
            Check.Equal(80f, fuel.Fuel, "rotation and previews never pay");
            Check.Equal(100f, Mana(source), "rotation preserves stored mana");
            foreach (var c in new ThingComp[] { beacon, source, endpoint }) c.PostDeSpawn(destination, DestroyMode.WillReplace);
            foreach (var member in members) member.ForceSetStateToUnspawned();
        }
        Check.Equal(0, newNetwork.GetLiveBeacons().Count, "final removal clears destination registry");
    }
}

