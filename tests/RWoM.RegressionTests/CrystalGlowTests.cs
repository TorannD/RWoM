using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.Odyssey;
using UnityEngine;
using Verse;

namespace RWoM.RegressionTests;

internal static class CrystalGlowTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Glow.DefinitionsRotationAndMainThread", Definitions);
        yield return new("Odyssey.Glow.FadeOwnershipAndTransientLifecycle", f => OdysseyOxygenTests.Run(f, Lifecycle));
        yield return new("Odyssey.Glow.OxygenPaidServiceStarvationAndPlanet", f => OdysseyOxygenTests.Run(f, Oxygen));
        yield return new("Odyssey.Glow.InvalidEventsAndBoundedEnvelope", f => OdysseyOxygenTests.Run(f, Events));
    }
    internal static CompCrystalGlow Attach(ThingWithComps parent)
    {
        var glow = new CompCrystalGlow { parent = parent, props = new CompProperties_CrystalGlow() };
        parent.AllComps.Add(glow);
        glow.PostSpawnSetup(false);
        return glow;
    }
    static void Set(object obj, string field, object value)
    {
        for (Type? type = obj.GetType(); type != null; type = type.BaseType)
        {
            var f = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (f != null) { f.SetValue(obj, value); return; }
        }
        throw new Exception(field);
    }
    static void Definitions(RuntimeFixture fixture)
    {
        int count = 0;
        foreach (string file in Directory.GetFiles(Path.Combine(fixture.Repository, "v1.7/Odyssey/Defs"), "Buildings*.xml"))
            foreach (var def in XDocument.Load(file).Root!.Elements("ThingDef"))
            {
                var comp = def.Element("comps")?.Elements("li").SingleOrDefault(c => (string?)c.Attribute("Class") == "TorannMagic.Odyssey.CompProperties_CrystalGlow");
                if (comp == null) continue;
                count++;
                Check.Equal(true, new[] { "MapMeshAndRealTime", "RealtimeOnly" }.Contains(def.Element("drawerType")!.Value), "native dynamic draw path");
                foreach (string rotation in new[] { "north", "east", "south", "west" })
                    Check.Equal(true, comp.Element(rotation) != null, "explicit texture anchor " + rotation);
            }
        Check.Equal(10, count, "all approved crystal/core infrastructure; thrusters and extender deferred");
        var props = new CompProperties_CrystalGlow { north = new(.5f,.2f), east = new(.3f,.3f), south = new(.5f,.25f), west = new(.7f,.3f) };
        var size = new Vector2(2,3);
        Check.Equal(true, Math.Abs(props.Offset(Rot4.North,size).z - .9f) < .0001f, "normalized north vertical anchor");
        Check.Equal(true, Math.Abs(props.Offset(Rot4.East,size).x + .4f) < .0001f, "east sprite crystal at left, not world rotated");
        Check.Equal(true, Math.Abs(props.Offset(Rot4.West,size).x - .4f) < .0001f, "west sprite crystal at right");
        Check.Equal(true, Math.Abs(props.Offset(Rot4.South,size).z - .75f) < .0001f, "south anchor and draw size");
        var single = new Graphic_Single(); // Pure native rotation helper: no Init, materials, or textures.
        var symmetrical = new CompProperties_CrystalGlow();
        var bias = new Vector3(.1f,0,.2f);
        foreach(var rotation in new[] { Rot4.North,Rot4.East,Rot4.South,Rot4.West })
        {
            var offset=symmetrical.WorldOffset(rotation,new Vector2(2,2),single.QuatFromRot(rotation),bias)-bias;
            var expected=rotation==Rot4.North ? new Vector3(0,.04f,.4f) : rotation==Rot4.East ? new Vector3(.4f,.04f,0) : rotation==Rot4.South ? new Vector3(0,.04f,-.4f) : new Vector3(-.4f,.04f,0);
            Check.Equal(true,(offset-expected).sqrMagnitude<.00001f,"Single anchor follows native mesh on ship rotation "+rotation);
        }
        Check.Equal(true, typeof(CrystalGlowRenderer).IsDefined(typeof(StaticConstructorOnStartup), false), "Unity resources initialized on main thread");
        Check.Equal(32, CrystalGlowRenderer.Levels, "bounded alpha material pool");
        Check.Equal(false, typeof(CompCrystalGlow).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Any(f => typeof(Thing).IsAssignableFrom(f.FieldType) || typeof(Map).IsAssignableFrom(f.FieldType)), "no static world retention");
    }
    static void Lifecycle()
    {
        var pump = new OdysseyOxygenTests.Pump();
        var glow = Attach(pump.Oxygen.parent);
        glow.NotifyService(60, 600);
        glow.Sample(60);
        Check.Equal(true, glow.Sample(138) > .99f, "slow startup fade");
        Set(pump.Flick, "switchOnInt", false);
        float half = glow.Sample(177);
        Check.Equal(true, half > .49f && half < .51f, "off fades gradually");
        Check.Equal(0f, glow.Sample(216), "off fade completes");
        Set(pump.Flick, "switchOnInt", true);
        glow.NotifyTransfer(1f, 220); glow.NotifyHit(1f, 220);
        Set(pump.Oxygen.parent, "factionInt", new Faction { def = new FactionDef() });
        Check.Equal(false, glow.Working(221), "captured building cannot work");
        Check.Equal(-1, glow.TransferTick, "capture clears pending transfer");
        Check.Equal(-1, glow.HitTick, "capture clears pending impact");
        glow.PostDeSpawn(pump.Oxygen.parent.Map);
        Check.Equal(0f, glow.Level, "despawn clears fade");
        glow.PostSpawnSetup(true);
        Check.Equal(-1, glow.HitTick, "load/respawn cannot replay flashes");
    }
    static void Oxygen()
    {
        var pump = new OdysseyOxygenTests.Pump();
        var glow = Attach(pump.Oxygen.parent);
        pump.Mana(1f); pump.Tick(60);
        Check.Equal(true, glow.Working(60), "actual paid oxygen starts service");
        Check.Equal(-1, glow.TransferTick, "local consumption is not transport");
        pump.Tick(60);
        Check.Equal(false, glow.Working(120), "repeated pulse cannot extend lease");
        pump.Mana(0f); pump.Tick(120);
        Check.Equal(false, glow.Working(120), "starved pump does not glow from stale LastSuppliedMana");
        pump.Mana(1f); pump.Tick(180);
        pump.Oxygen.parent.Map.Biome.inVacuum = false;
        Check.Equal(false, glow.Working(181), "planet atmosphere service off immediately");
        pump.Tick(240);
        Check.Equal(false, glow.Working(240), "planet free atmosphere cannot create lease");
    }
    static void Events()
    {
        var pump = new OdysseyOxygenTests.Pump(); var glow = Attach(pump.Oxygen.parent);
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        { glow.NotifyTransfer(invalid, 60); glow.NotifyHit(invalid, 60); }
        Check.Equal(-1, glow.TransferTick, "no fake zero/invalid transport");
        Check.Equal(-1, glow.HitTick, "no fake zero/invalid absorption");
        glow.NotifyTransfer(1f, 60); glow.NotifyTransfer(1f, 60);
        Check.Equal(60, glow.TransferTick, "same-tick events coalesce");
        Check.Equal(0f, CompCrystalGlow.Envelope(111, glow.TransferTick, 51), "transfer ends after 850ms game time");
        Check.Equal(0f, CompCrystalGlow.Envelope(59, 60, 51), "rewind cannot replay future event");
        Set(pump.Flick, "switchOnInt", false); glow.NotifyHit(1f, 120);
        Check.Equal(-1, glow.HitTick, "off rejects visual events");
    }
}

internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> GlowCases()
    {
        yield return new("Odyssey.Glow.RealNetworkManaNotFuelOrZero", f => Run(f, GlowNetwork));
        yield return new("Odyssey.Glow.SiphonDonationAndDrainReachRelays", f => SiphonRun(f, GlowSiphon));
        yield return new("Odyssey.Glow.ReservoirChargeFuelAndDisabledModes", f => Run(f, GlowReservoir));
    }
    static void GlowReservoir()
    {
        var tank = Tank(10,10); var glow = CrystalGlowTests.Attach(tank.parent);
        glow.Props.state = CrystalGlowState.Reservoir;
        Check.Equal(true,glow.Working(60),"charged reservoir softly glows");
        Set(tank.parent.GetComp<CompMagofuelReservoir>(),"storedMana",0f);
        Check.Equal(false,glow.Working(60),"empty reservoir off");
        AddFuel(tank,10f);
        Check.Equal(true,glow.Working(60),"available fuel energizes storage crystal");
        var endpoint = (CompManaNetworkEndpoint)tank;
        endpoint.ManaMode=ManaNetworkMode.Off; endpoint.FuelMode=ManaNetworkMode.Off;
        Check.Equal(false,glow.Working(60),"both channels off fades storage");
        Check.Equal(10f,tank.parent.GetComp<CompRefuelable>().Fuel,"presentation never consumes fuel");
    }
    static void GlowSiphon()
    {
        var (s,t,p,n) = SiphonSetup();
        var source = CrystalGlowTests.Attach(s.parent); var target = CrystalGlowTests.Attach(t.parent);
        var relay = CrystalGlowTests.Attach(((MapComponent_ManaNetwork)network).GetLiveBeacons()[0].parent);
        Set(Find.TickManager,"ticksGameInt",60);
        Near(.01f,Donate(s,p),"real personal mana donation");
        Check.Equal(60,source.TransferTick,"siphon flashes paid donation");
        Check.Equal(60,target.TransferTick,"tank flashes actual credit");
        Check.Equal(60,relay.TransferTick,"relay flashes real siphon network transfer");
        Set(s,"bufferedMana",.5f); Set(Find.TickManager,"ticksGameInt",120);
        Call(s,"DrainBuffer");
        Check.Equal(120,relay.TransferTick,"paid hidden buffer movement reaches relay");
        Set(Find.TickManager,"ticksGameInt",180); Call(s,"DrainBuffer");
        Check.Equal(120,relay.TransferTick,"empty drain cannot flash");
    }
    static void GlowNetwork()
    {
        var beacon = Make("ManaBeacon", 10,10);
        var relay = CrystalGlowTests.Attach(beacon.parent);
        var source = Tank(10,9); var receiver = Tank(11,9,true);
        var sender = CrystalGlowTests.Attach(source.parent); var target = CrystalGlowTests.Attach(receiver.parent);
        Tick(60);
        Check.Equal(90f, Mana(source), "visual hook preserves sender payment");
        Check.Equal(10f, Mana(receiver), "visual hook preserves receipt");
        Check.Equal(60, relay.TransferTick, "real network movement flashes beacon");
        Check.Equal(60, sender.TransferTick, "real movement flashes source");
        Check.Equal(60, target.TransferTick, "real movement flashes receiver");
        Check.Equal(true, relay.Working(60), "connected beacon idle participation lease");
        Set(source.parent.AllComps.Single(c => c.GetType().Name == "CompMagofuelReservoir"), "storedMana", 0f);
        Tick(120);
        Check.Equal(60, relay.TransferTick, "empty transfer never refreshes flash");
        source.GetType().GetProperty("ManaMode")!.SetValue(source, Enum.Parse(T("ManaNetworkMode"), "Off"));
        var sourceFuel = AddFuel(source, 10f); var targetFuel = AddFuel(receiver, 0f);
        receiver.GetType().GetProperty("FuelMode")!.SetValue(receiver, Enum.Parse(T("ManaNetworkMode"), "Receive"));
        Tick(180);
        Check.Equal(true, targetFuel.Fuel > 0f, "real fuel transport occurred");
        Check.Equal(60, relay.TransferTick, "fuel movement is not mislabeled as mana");
    }
}

internal static partial class OdysseyPowerTests
{
    public static IEnumerable<TestCase> GlowCases()
    {
        yield return new("Odyssey.Glow.PowerRequiresActualPaidOutput", f => WithMap(f, _ =>
        {
            var generator = new GeneratorSave();
            Set(generator.Power.parent,"factionInt",new Faction { def = new FactionDef { isPlayer = true } });
            var glow = CrystalGlowTests.Attach(generator.Power.parent); glow.Props.state=CrystalGlowState.Power;
            Check.Equal(false,glow.Working(0),"no output no glow");
            Fill(generator,1); Advance(generator.Power,1);
            Check.Equal(true,glow.Working(1),"actual paid output glows");
            double paid=Credit(generator.Power);
            for(int i=0;i<20;i++)glow.Working(i);
            Near(paid,Credit(generator.Power),"visual checks never buy more power");
            Set(generator.Flick,"switchOnInt",false);
            Check.Equal(false,glow.Working(2),"switch suppresses previous output immediately");
            Set(generator.Flick,"switchOnInt",true); Set(generator.Power,"paidFuel",0d);
            Advance(generator.Power,1);
            Check.Equal(false,glow.Working(3),"no paid fuel no output glow");
        }));
    }
}

internal static partial class OdysseyGrandDomeTests
{
    public static IEnumerable<TestCase> GlowCases()
    {
        yield return new("Odyssey.Glow.DomeNativeOrderingAndReadOnlySnapshot", f => Run(() =>
        {
            var dome=Dome(); Arm(dome); var glow=CrystalGlowTests.Attach(dome.parent); glow.Props.state=CrystalGlowState.Dome;
            // Match production XML comp ordering; the native TickList prefix pays before any comp samples.
            var xml=XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_GrandDome.xml"));
            var classes=xml.Root!.Element("ThingDef")!.Element("comps")!.Elements("li").Select(x=>(string)x.Attribute("Class")!).ToList();
            if(classes.IndexOf("TorannMagic.Odyssey.CompProperties_CrystalGlow") < classes.IndexOf("TorannMagic.Odyssey.CompProperties_GrandAtmosphereDome"))
            { dome.parent.AllComps.Remove(glow); dome.parent.AllComps.Insert(0,glow); }
            var (list,consumer)=NativeList(()=> { foreach(var comp in dome.parent.AllComps) comp.CompTick(); });
            for(int i=0;i<=80;i++) { Set(Find.TickManager,"ticksGameInt",i); list.Tick(); glow.Sample(i); }
            Check.Equal(true,glow.Level>.99f,"native payment before visual comp produces steady glow");
            Near(200-81d/600,Total(dome),"visual sampling never duplicates native fuel payment");
            map.Biome.inVacuum=false;
            Check.Equal(false,glow.Working(80),"invalid environment suppresses visual snapshot");
            Check.Equal(true,Get<bool>(dome,"Armed"),"visual snapshot does not call mutating ValidateArmed");
        }));
    }
}

internal static partial class OdysseyCombatTests
{
    public static IEnumerable<TestCase> GlowCases()
    {
        yield return new("Odyssey.Glow.CombatPaidStandbyActualHitAndCollapse", f => Run(GlowCombat));
        yield return new("Odyssey.Glow.BoosterFlashesOnlyItsAbsorbedHitPoints", f => Run(GlowBooster));
    }
    static void GlowBooster()
    {
        var main=Source(); var booster=Source(15,16,true); Beacon(15,17);
        var core=CrystalGlowTests.Attach(main.parent); var glow=CrystalGlowTests.Attach(booster.parent);
        Set(main,"currentHitPoints",1000f); Set(booster,"currentHitPoints",10f); Tick(60);
        Check.Equal(true,glow.Working(60),"paid nonempty booster participation glows");
        float before=HP(booster); Hit(main,10f);
        Near(before-10f,HP(booster),"actual booster absorbs first");
        Near(1000f,HP(main),"core HP unchanged by booster hit");
        Check.Equal(60,glow.HitTick,"spent booster flashes");
        Check.Equal(-1,core.HitTick,"core does not fake damage absorbed by booster");
        Hit(main,10000f);
        Check.Equal(false,glow.Working(60),"depleted booster fades immediately");
    }
    static void GlowCombat()
    {
        var main = Source(); var spare = Source(15,16); Beacon(15,17);
        var glow = CrystalGlowTests.Attach(main.parent); var standby = CrystalGlowTests.Attach(spare.parent);
        Check.Equal(false, Hit(main, 1f), "unpaid hit refused");
        Check.Equal(-1, glow.HitTick, "unpaid hit has no flash");
        Tick(60);
        Check.Equal(true, glow.Working(60), "paid elected generator glows");
        Check.Equal(false, standby.Working(60), "standby generator stays off");
        float before = HP(main);
        Check.Equal(false, Hit(main, 0f), "zero hit refused");
        Check.Equal(-1, glow.HitTick, "zero hit no flash");
        Check.Equal(true, Hit(main, 1f), "real absorption");
        Near(before-1f,HP(main),"visual hit keeps damage accounting");
        Check.Equal(60,glow.HitTick,"actual absorbed HP flash");
        Hit(main, 10000f);
        Check.Equal(false,glow.Working(60),"collapsed generator stops base glow immediately");
        Check.Equal(60,glow.HitTick,"final absorbed hit can still flash");
    }
}
