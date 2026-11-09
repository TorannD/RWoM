using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class OdysseyReservoirTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Reservoir.ConversionPaysAndStopsAtCapacity", Conversion);
        yield return new("Odyssey.Reservoir.TransferPreservesTotalAndRejectsInvalid", Transfer);
        yield return new("Odyssey.Reservoir.RealScribePreservesBothStores", Save);
        yield return new("Odyssey.Reservoir.TinyPaymentsCannotCreateCharge", Tiny);
        yield return new("Odyssey.Reservoir.MagePaymentUsesRealNeedAndCurrentClass", MagePayment);
        yield return new("Odyssey.Reservoir.PersonalReserveSurvivesScribe", Reserve);
        yield return new("Odyssey.Reservoir.NativeRefillAndMinifiedOwnership", NativeOwnership);
        yield return new("Odyssey.Reservoir.ManualConversionFillsAvailableSpace", ConvertAvailable);
    }
    static Type BankType => typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.CompMagofuelReservoir")
        ?? throw new InvalidOperationException("Reservoir component not implemented");
    static float Call(ThingComp bank, string method, params object[] args) => (float)BankType.GetMethod(method)!.Invoke(bank, args)!;
    static float Charge(ThingComp bank) => (float)BankType.GetProperty("StoredMana")!.GetValue(bank)!;
    static void Set(object obj, string name, object value)
    {
        for (Type? t = obj.GetType(); t != null; t = t.BaseType)
        { var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); if (field != null) { field.SetValue(obj, value); return; } }
        throw new Exception(name);
    }
    static void Reserve(RuntimeFixture f)
    {
        var oldCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        try {
        var tank = new TankSave();
        var property = BankType.GetProperty("PersonalReserve") ?? throw new Exception("Personal reserve not implemented");
        Check.Equal(.2f, (float)property.GetValue(tank.Bank)!, "default reserve");
        property.SetValue(tank.Bank, .6f);
        string path = Path.Combine(f.WorkingDirectory, "reservoir-reserve.xml");
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref tank, "tank"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        TankSave loaded = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "tank"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Check.Equal(.6f, (float)property.GetValue(loaded.Bank)!, "configured reserve survives actual save/load");
        } finally { System.Globalization.CultureInfo.CurrentCulture = oldCulture; }
    }
    static void NativeOwnership(RuntimeFixture f)
    {
        var oldGame = Current.Game;
        var definitions = DefDatabase<ThingDef>.AllDefsListForReading;
        var names = (Dictionary<string, ThingDef>)typeof(DefDatabase<ThingDef>).GetField("defsByName", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var oldDefinitions = definitions.ToArray();
        var oldNames = names.ToArray();
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            Set(Current.Game, "maps", new List<Map>());
            var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager = new FactionManager(); Set(Current.Game, "worldInt", world);
            var tank = new TankSave();
            tank.Bank.parent.def.stackLimit = 1;
            var fuelDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            fuelDef.defName = "TM_Magofuel"; fuelDef.thingClass = typeof(Thing); fuelDef.stackLimit = 75; fuelDef.destroyable = true; fuelDef.category = ThingCategory.Item;
            var largeTank = new TankSave();
            var fuelXml = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/Buildings_Mana.xml")).Descendants("li").Single(x=>(string?)x.Attribute("Class")=="CompProperties_Refuelable");
            ((CompProperties_Refuelable)largeTank.Fuel.props).fuelCapacity = float.Parse(fuelXml.Element("fuelCapacity")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            var stacks = Enumerable.Range(0,7).Select(_=>new Thing { def=fuelDef, stackCount=75 }).ToList();
            var originalStacks = stacks.ToArray();
            largeTank.Fuel.Refuel(stacks);
            Check.Equal(500f,largeTank.Fuel.Fuel,"production capacity accepts 500 native fuel from whole stacks");
            Check.Equal(25,originalStacks.Sum(t=>t.Destroyed?0:t.stackCount),"500-capacity native refill preserves excess");
            var first = new Thing { def = fuelDef, stackCount = 75 }; var second = new Thing { def = fuelDef, stackCount = 40 };
            tank.Fuel.Refuel(new List<Thing> { first, second });
            Check.Equal(100f, tank.Fuel.Fuel, "native list refill fills exact physical capacity");
            Check.Equal(15, (first.Destroyed ? 0 : first.stackCount) + (second.Destroyed ? 0 : second.stackCount), "native split stacks retain excess fuel");
            Call(tank.Bank, "ConvertFuel", 10f);
            Check.Equal(0f, Call(tank.Bank, "ConvertFuel", 3f), "sub-item request cannot create fractional deficit");
            var refill = new Thing { def = fuelDef, stackCount = 2 };
            tank.Fuel.Refuel(new List<Thing> { refill });
            Check.Equal(1, refill.stackCount, "native refill after conversion consumes exactly one physical item");
            Check.Equal(100f, tank.Fuel.Fuel, "native refill restores whole deficit without waste");
            Call(tank.Bank, "ConvertFuel", 10f);
            var minifiedDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            minifiedDef.defName = "RegressionMinifiedTank"; minifiedDef.thingClass = typeof(MinifiedThing); minifiedDef.stackLimit = 1; minifiedDef.comps = new List<CompProperties>();
            tank.Bank.parent.def.minifiedDef = minifiedDef;
            tank.Bank.parent.def.comps = new List<CompProperties> { tank.Fuel.props, tank.Bank.props };
            tank.Bank.parent.def.thingClass = typeof(ThingWithComps);
            tank.Bank.parent.thingIDNumber = 901001;
            DefDatabase<ThingDef>.Add(tank.Bank.parent.def); DefDatabase<ThingDef>.Add(minifiedDef);
            var minified = tank.Bank.parent.MakeMinified();
            var owner = ((IThingHolder)minified).GetDirectlyHeldThings();
            Check.Equal(1, owner.Count, "actual native minification owns one tank");
            Check.Equal(tank.Bank.parent, minified.InnerThing, "minification owns original component state");
            Check.Equal(99f, minified.InnerThing.TryGetComp<CompRefuelable>().Fuel, "physical fuel retained under native owner");
            Check.Equal(20f, Charge(tank.Bank), "charge retained while minified");
            string path = Path.Combine(f.WorkingDirectory, "full-minified-tank.xml");
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref minified, "tank"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            MinifiedThing loaded = null!;
            try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "tank"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
            Check.Equal(99f, loaded.InnerThing.TryGetComp<CompRefuelable>().Fuel, "whole native minified Thing Scribe retains fuel");
            var loadedBank = ((ThingWithComps)loaded.InnerThing).AllComps.Single(c => c.GetType() == BankType);
            Check.Equal(20f, Charge(loadedBank), "whole native minified Thing Scribe retains charge");
            Check.Equal(1, ((IThingHolder)loaded).GetDirectlyHeldThings().Count, "loaded wrapper owns exactly one tank");
            // Reinstallation reuses the inner Thing and calls its spawn lifecycle; it must
            // not rerun Initialize (which would grant/erase starting fuel). Exercise the
            // real component callbacks for placement, removal and load placement.
            var unpacked = (ThingWithComps)loaded.InnerThing;
            ((IThingHolder)loaded).GetDirectlyHeldThings().Remove(unpacked);
            foreach (var component in unpacked.AllComps) component.PostSpawnSetup(false);
            foreach (var component in unpacked.AllComps) component.PostDeSpawn(null!);
            foreach (var component in unpacked.AllComps) component.PostSpawnSetup(true);
            Check.Equal(99f, unpacked.GetComp<CompRefuelable>().Fuel, "native reinstallation component lifecycle retains fuel");
            Check.Equal(20f, Charge(loadedBank), "repeated spawn lifecycle cannot grant or erase charge");
            var carrier = new ThingOwner<Thing>();
            Check.Equal(true, owner.TryTransferToContainer(tank.Bank.parent, carrier), "native transfer retains exact inner object");
            Check.Equal(1, carrier.Count, "single owner after transfer"); Check.Equal(0, owner.Count, "previous owner emptied");
            Check.Equal(20f, Charge(tank.Bank), "owner transfer never duplicates or converts charge");
            carrier.Remove(tank.Bank.parent);
            tank.Bank.parent.def.destroyable = true; tank.Bank.parent.Destroy(DestroyMode.Vanish);
            Check.Equal(0f, Call(tank.Bank, "ConvertFuel", 10f), "destroyed tank cannot convert");
            Check.Equal(0f, Call(tank.Bank, "WithdrawMana", 10f), "destroyed tank cannot emit charge");
        }
        finally
        {
            Current.Game = oldGame;
            definitions.Clear(); definitions.AddRange(oldDefinitions);
            names.Clear(); foreach (var pair in oldNames) names.Add(pair.Key, pair.Value);
        }
    }
    static void ConvertAvailable(RuntimeFixture f)
    {
        Check.NotNull(BankType.GetMethod("ConvertAvailableFuel"), "one command converts available fuel");
        var tank = new TankSave(); tank.Fuel.Refuel(100);
        Check.Equal(1000f, Call(tank.Bank, "ConvertAvailableFuel"), "one user action fills bank");
        Check.Equal(0f, tank.Fuel.Fuel, "all converted charge paid");
        tank.Fuel.Refuel(10);
        Check.Equal(0f, Call(tank.Bank, "ConvertAvailableFuel"), "full bank refuses command");
        Check.Equal(10f, tank.Fuel.Fuel, "full bank retains fuel");
        Call(tank.Bank, "WithdrawMana", 3f);
        Check.Equal(0f, Call(tank.Bank, "ConvertAvailableFuel"), "less than one whole fuel yield refuses conversion");
        Check.Equal(10f, tank.Fuel.Fuel, "physical fuel remains whole for lossless native refill");
    }
    static void MagePayment(RuntimeFixture f)
    {
        Check.NotNull(BankType.GetMethod("ChargeFromMage"), "mage charging must be implemented");
        var oldGame = Current.Game;
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var tank = new TankSave();
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            Set(tank.Bank.parent, "factionInt", faction); Set(tank.Bank.parent, "mapIndexOrState", (sbyte)0);
            Set(tank.Bank.parent, "positionInt", new IntVec3(10, 0, 10));
            tank.Bank.parent.def.size = new IntVec2(1, 1);
            tank.Bank.parent.def.hasInteractionCell = true;
            tank.Bank.parent.def.interactionCellOffset = new IntVec3(0, 0, -1);
            tank.Bank.parent.def.race = new RaceProperties();
            var pawn = new Pawn { def = tank.Bank.parent.def };
            pawn.health = new Pawn_HealthTracker(pawn); pawn.needs = new Pawn_NeedsTracker(pawn);
            pawn.mindState = (Pawn_MindState)RuntimeHelpers.GetUninitializedObject(typeof(Pawn_MindState));
            pawn.mindState.mentalStateHandler = new MentalStateHandler(pawn);
            pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
            pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "ageBiologicalTicksInt", 72000000L);
            Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0); Set(pawn, "positionInt", tank.Bank.parent.InteractionCell);
            var comp = new CompAbilityUserMagic { parent = pawn }; Set(pawn, "comps", new List<ThingComp> { comp });
            var trait = new TraitDef { defName = "Faceless" }; pawn.story.traits.allTraits.Add(new Trait(trait));
            var need = (Need_Mana)RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)); Set(need, "pawn", pawn);
            need.def = new NeedDef { defName = "TM_Mana" }; pawn.needs.AllNeeds.Add(need); need.CurLevel = 1f;
            Check.Equal(1f, Call(tank.Bank, "ChargeFromMage", pawn), "one pulse yields one bank unit");
            Check.Equal(.99f, need.CurLevel, "real need paid before bank credit");
            comp.maxMP = .4f;
            Check.Equal(false, (bool)BankType.GetMethod("CanChargeFromMage")!.Invoke(tank.Bank, [pawn, true])!, "capacity drop cannot admit an overcharged payment");
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "capacity drop rejects excess native clamping");
            Check.Equal(.99f, need.CurLevel, "rejected payment preserves mana before native normalization");
            Check.Equal(1f, Charge(tank.Bank), "capacity drop cannot grant bank credit");
            comp.maxMP = 1f;
            need.CurLevel = .205f;
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "reserve disallows partial pulse");
            Check.Equal(.205f, need.CurLevel, "refused pulse keeps mana");
            need.CurLevel = 1f; pawn.story.traits.allTraits.Clear();
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "class loss rejects cached magic status");
            Check.Equal(1f, Charge(tank.Bank), "no unpaid charge after class loss");
            pawn.story.traits.allTraits.Add(new Trait(trait));
            Set(pawn, "positionInt", new IntVec3(50, 0, 50));
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "remote charging rejected");
            Set(pawn, "positionInt", tank.Bank.parent.InteractionCell); Set(pawn, "factionInt", new Faction());
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "ownership mismatch rejected");
            Set(pawn, "factionInt", faction);
            pawn.drafter = new Pawn_DraftController(pawn); Set(pawn.drafter, "draftedInt", true);
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "drafted mage cannot pay");
            Set(pawn.drafter, "draftedInt", false); Set(pawn.health, "healthState", PawnHealthState.Down);
            Check.Equal(0f, Call(tank.Bank, "ChargeFromMage", pawn), "downed mage cannot pay");
            Set(pawn.health, "healthState", PawnHealthState.Mobile);
            var driverType = typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.JobDriver_ChargeMagofuelReservoir");
            Check.NotNull(driverType, "manual charging job implemented");
            var driver = (JobDriver)Activator.CreateInstance(driverType!)!;
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            driver.pawn = pawn; driver.job = new Job(new JobDef { defName = "RegressionCharge" }, tank.Bank.parent);
            var toils = ((IEnumerable<Toil>)driverType!.GetMethod("MakeNewToils", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(driver, null)!).ToList();
            var charge = toils.Last();
            Check.Equal(ToilCompleteMode.Never, charge.defaultCompleteMode, "explicit charging continues until full or personal reserve");
            charge.initAction();
            Check.Equal(1f, need.CurLevel, "setup does not pay");
            charge.tickAction(); Check.Equal(1f, need.CurLevel, "first tick does not pay");
            driver = SaveDriver(f, driver, "before-pulse");
            charge = ((IEnumerable<Toil>)driverType.GetMethod("MakeNewToils", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(driver, null)!).Last();
            charge.initAction(); charge.tickAction();
            Check.Equal(1f, need.CurLevel, "reload before first pulse does not pay");
            Set(Current.Game.tickManager, "ticksGameInt", 60);
            charge.tickAction(); Check.Equal(.99f, need.CurLevel, "sixty ticks pay once");
            charge.tickAction(); Check.Equal(.99f, need.CurLevel, "duplicate callback same tick cannot pay again");
            driver = SaveDriver(f, driver, "after-pulse");
            charge = ((IEnumerable<Toil>)driverType.GetMethod("MakeNewToils", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(driver, null)!).Last();
            charge.initAction(); charge.tickAction();
            Check.Equal(.99f, need.CurLevel, "reload after payment cannot replay pulse");
            Set(Current.Game.tickManager, "ticksGameInt", 120); charge.tickAction();
            Check.Equal(.98f, need.CurLevel, "next pulse after load pays exactly once");
            driver.Cleanup(JobCondition.InterruptForced);
            Check.Equal(.98f, need.CurLevel, "cancellation does not pay or refund");
        }
        finally { Current.Game = oldGame; }
    }
    static JobDriver SaveDriver(RuntimeFixture f, JobDriver source, string name)
    {
        string path = Path.Combine(f.WorkingDirectory, name + ".xml");
        var saved = source;
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref saved, "driver"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        JobDriver loaded = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "driver"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        loaded.pawn = source.pawn; loaded.job = source.job;
        return loaded;
    }

    public sealed class TankSave : IExposable
    {
        public readonly ThingComp Bank;
        public readonly CompRefuelable Fuel;
        public TankSave()
        {
            var definition = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            definition.defName = "RegressionReservoir";
            var parent = new ThingWithComps { def = definition };
            var properties = (CompProperties)Activator.CreateInstance(typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.CompProperties_MagofuelReservoir")!)!;
            Bank = (ThingComp)Activator.CreateInstance(BankType)!; Bank.props = properties; Bank.parent = parent;
            Fuel = new CompRefuelable { parent = parent, props = new CompProperties_Refuelable { fuelCapacity = 100, fuelConsumptionRate = 0 } };
            typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(parent, new List<ThingComp> { Fuel, Bank });
        }
        public void ExposeData() { Fuel.PostExposeData(); Bank.PostExposeData(); }
    }
    static void Conversion(RuntimeFixture f)
    {
        _ = BankType;
        var tank = new TankSave(); tank.Fuel.Refuel(100);
        Check.Equal(10f, Call(tank.Bank, "ConvertFuel", 20f), "one operation respects ten-unit flow limit");
        Check.Equal(99f, tank.Fuel.Fuel, "ten mana costs one physical fuel");
        for (int i = 0; i < 99; i++) Call(tank.Bank, "ConvertFuel", 10f);
        Check.Equal(1000f, Charge(tank.Bank), "capacity filled exactly");
        Check.Equal(0f, tank.Fuel.Fuel, "no free charge");
        tank.Fuel.Refuel(2);
        Check.Equal(0f, Call(tank.Bank, "ConvertFuel", 1f), "full bank refuses conversion");
        Check.Equal(2f, tank.Fuel.Fuel, "refusal retains fuel");
        Check.Equal(3f, Call(tank.Bank, "WithdrawMana", 3f), "partial withdrawal");
        Check.Equal(0f, Call(tank.Bank, "ConvertFuel", 10f), "less than one whole fuel yield refuses conversion");
        Check.Equal(2f, tank.Fuel.Fuel, "partial room retains physical fuel for lossless refill");
        Check.Equal(0f, Charge(new TankSave().Bank), "separate instances do not share charge");
    }
    static void Transfer(RuntimeFixture f)
    {
        _ = BankType;
        var a = new TankSave(); var b = new TankSave();
        a.Fuel.Refuel(1); Call(a.Bank, "ConvertFuel", 10f);
        foreach (float invalid in new[] { -1f, 0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Check.Equal(0f, Call(a.Bank, "ConvertFuel", invalid), "invalid conversion rejected");
            Check.Equal(0f, Call(a.Bank, "WithdrawMana", invalid), "invalid withdrawal rejected");
            Check.Equal(0f, Call(a.Bank, "TransferManaTo", b.Bank, invalid), "invalid transfer rejected");
        }
        Check.Equal(0f, Call(a.Bank, "TransferManaTo", a.Bank, 5f), "self transfer rejected");
        Check.Equal(7f, Call(a.Bank, "TransferManaTo", b.Bank, 7f), "first consumer");
        Check.Equal(3f, Call(a.Bank, "TransferManaTo", b.Bank, 7f), "second consumer gets only remainder");
        Check.Equal(10f, Charge(a.Bank) + Charge(b.Bank), "transfer conserves total");
        Check.Equal(0f, Call(a.Bank, "WithdrawMana", 1f), "empty withdrawal refused");
        b.Fuel.Refuel(100);
        for (int i = 0; i < 99; i++) Call(b.Bank, "ConvertFuel", 10f);
        a.Fuel.Refuel(1); Call(a.Bank, "ConvertFuel", 10f);
        Check.Equal(0f, Call(a.Bank, "TransferManaTo", b.Bank, 5f), "full receiver refuses without payment");
        Check.Equal(10f, Charge(a.Bank), "refusal preserves donor charge");
    }
    static void Tiny(RuntimeFixture f)
    {
        _ = BankType;
        var a = new TankSave(); a.Fuel.Refuel(100);
        Check.Equal(0f, Call(a.Bank, "ConvertFuel", float.Epsilon), "unrepresentable fuel debit cannot create mana");
        Check.Equal(100f, a.Fuel.Fuel, "tiny refused debit leaves fuel unchanged");
        Call(a.Bank, "ConvertFuel", 10f);
        Check.Equal(0f, Call(a.Bank, "WithdrawMana", float.Epsilon), "unrepresentable withdrawal returns no payment");
        var b = new TankSave();
        Check.Equal(0f, Call(a.Bank, "TransferManaTo", b.Bank, float.Epsilon), "unrepresentable debit cannot credit another tank");
        Check.Equal(0f, Charge(b.Bank), "no free recipient charge");
    }
    static void Save(RuntimeFixture f)
    {
        _ = BankType;
        var tank = new TankSave(); tank.Fuel.Refuel(12); Call(tank.Bank, "ConvertFuel", 10f);
        string path = Path.Combine(f.WorkingDirectory, "odyssey-reservoir.xml");
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref tank, "tank"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        TankSave loaded = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "tank"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Check.Equal(11f, loaded.Fuel.Fuel, "physical fuel survives native Scribe");
        Check.Equal(10f, Charge(loaded.Bank), "mana survives native Scribe");
        Check.Equal(10f, Call(loaded.Bank, "ConvertFuel", 10f), "new conversion after load");
        Check.Equal(10f, loaded.Fuel.Fuel, "load does not replay payment");
        Check.Equal(11f, tank.Fuel.Fuel, "loaded instance independent from source");
    }
}


