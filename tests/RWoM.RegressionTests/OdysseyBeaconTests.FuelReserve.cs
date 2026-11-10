using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using TorannMagic.Odyssey;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    internal static IEnumerable<TestCase> FuelReserveCases()
    {
        yield return new("Odyssey.FuelReserve.AutomaticSurplusAndLifeSupport", f => Run(f, ReserveTransfers));
        yield return new("Odyssey.FuelReserve.ManualConversionAndFlightAccess", f => Run(f, ReserveConversionFlight));
        yield return new("Odyssey.FuelReserve.FixedQuantitySaveAndTransfer", f => Run(f, () => ReserveSave(f)));
        yield return new("Odyssey.FuelReserve.DirectPaymentsAndMixedSources", f => Run(f, ReserveDirectPayments));
        yield return new("Odyssey.FuelReserve.StellarCallbacksRefundAllSources", f => Run(f, ReserveStellar));
        yield return new("Odyssey.FuelReserve.NativeCallbackReserveChanges", f => Run(f, ReserveCallbacks));
        yield return new("Odyssey.FuelReserve.DirectCallbackPartialRollback", f => Run(f, ReserveDirectCallbacks));
        yield return new("Odyssey.FuelReserve.PhysicalStorageScopeAndCore", f => Run(f, ReserveStorageScope));
        yield return new("Odyssey.FuelReserve.ConversionRechecksLifeSupportPermission", f => Run(f, ReserveConversionPermission));
        yield return new("Odyssey.FuelReserve.ThrowingCallbackCannotBypassReserve", f => Run(f, ReserveConversionThrow));
    }
    static CompMagofuelReservoir Bank(ThingComp c) => c.parent.GetComp<CompMagofuelReservoir>();
    static void Reserve(ThingComp c, float value) => Check.NotNull(typeof(CompMagofuelReservoir).GetProperty("FuelReserve"), "saved physical reserve API").SetValue(Bank(c), value);
    static float Reserved(ThingComp c) => (float)Get(Bank(c), "FuelReserve");
    static ThingComp ReserveTank(int x, float fuel, bool receiver = false)
    {
        var c = Tank(x, 9, receiver); c.parent.def.defName = "TM_MagofuelReservoir"; AddFuel(c, fuel); return c;
    }
    static void ReserveTransfers()
    {
        Make("ManaBeacon", 10, 10);
        var s = ReserveTank(10, 10); var r = ReserveTank(11, 0, true);
        ((CompManaNetworkEndpoint)r).FuelMode = ManaNetworkMode.Receive;
        Reserve(s, 8); Priority(r, false, 3);
        Tick(60);
        Check.Equal(8f, s.parent.GetComp<CompRefuelable>().Fuel, "ordinary transfer protects fixed quantity even at highest priority");
        Check.Equal(2f, r.parent.GetComp<CompRefuelable>().Fuel, "only surplus moves to ordinary tank");
        r.parent.AllComps.Add(new CompGrandAtmosphereDome { parent = r.parent, props = new CompProperties_GrandAtmosphereDome() });
        Priority(r, false, 1); Tick(120);
        Check.Equal(3f, s.parent.GetComp<CompRefuelable>().Fuel, "actual life support can draw protected fuel even at low priority");
        Check.Equal(7f, r.parent.GetComp<CompRefuelable>().Fuel, "life-support transfer keeps ordinary five-unit budget");
        Tick(180);
        Check.Equal(0f, s.parent.GetComp<CompRefuelable>().Fuel, "life support can empty reserve");
        Check.Equal(10f, r.parent.GetComp<CompRefuelable>().Fuel, "conserved physical total");
    }
    static void ReserveConversionFlight()
    {
        var s = ReserveTank(10, 10); Set(Bank(s), "storedMana", 0f); Reserve(s, 8);
        Check.Equal(20f, Bank(s).ConvertAvailableFuel(), "manual conversion consumes only two unreserved units");
        Check.Equal(8f, s.parent.GetComp<CompRefuelable>().Fuel, "manual conversion stops at reserve");
        Check.Equal(0f, Bank(s).ConvertFuel(10), "single conversion cannot bypass reserve");
        var group = Activator.CreateInstance(T("MagicalThrusterFuelGroup"), new object[] { new List<CompRefuelable> { s.parent.GetComp<CompRefuelable>() }, 10f })!;
        Check.Equal(8f, (float)Get(group, "Fuel"), "flight range sees all physical fuel");
        Check.Equal(8f, (float)Call(group, "ConsumeAmount", 8f, true)!, "flight may consume protected fuel");
        Check.Equal(0f, s.parent.GetComp<CompRefuelable>().Fuel, "flight exact debit");
    }
    static void ReserveSave(RuntimeFixture f)
    {
        var s = ReserveTank(10, 80); Check.Equal(0f, Reserved(s), "default zero"); Reserve(s, 37);
        var path = Path.Combine(f.WorkingDirectory, "reserve.xml");
        try { Scribe.saver.InitSaving(path, "regression"); Bank(s).PostExposeData(); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        Reserve(s, 0);
        try { Scribe.loader.InitLoading(path); Bank(s).PostExposeData(); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Check.Equal(37f, Reserved(s), "exact quantity saved independently of fuel amount");
        s.PostDeSpawn(map); s.PostSpawnSetup(true); Check.Equal(37f, Reserved(s), "registry handoff preserves physical setting");
        s.parent.GetComp<CompRefuelable>().Props.fuelCapacity = 20;
        Check.Equal(80f, s.parent.GetComp<CompRefuelable>().Fuel, "capacity changes never destroy saved fuel");
        Check.Equal(20f, Reserved(s), "effective reserve bounded by current capacity");
        s.parent.GetComp<CompRefuelable>().Props.fuelCapacity = 100;
        Check.Equal(37f, Reserved(s), "fixed setting retained across capacity change");
        var xml = XDocument.Load(path); xml.Descendants("TM_reservoirFuelReserve").Remove(); xml.Save(path);
        try { Scribe.loader.InitLoading(path); Bank(s).PostExposeData(); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Check.Equal(0f, Reserved(s), "old saves without field retain old behavior");
    }
    static void ReservePlayerWorld()
    {
        var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
        world.factionManager = new FactionManager(); world.factionManager.AllFactionsListForReading.Add(faction); Set(world.factionManager, "ofPlayer", faction); Set(Current.Game, "worldInt", world);
    }
    static void ReserveDirectPayments()
    {
        ReservePlayerWorld(); Make("ManaBeacon",10,10);
        var a = ReserveTank(9, 60); var b = ReserveTank(11, 60); Reserve(a, 20); Reserve(b, 20);
        var gate = Make("ManaNetworkEndpoint",10,11); ((CompManaNetworkEndpoint)gate).FuelMode = ManaNetworkMode.Receive;
        Check.Equal(80f, CoreReturnUtility.AvailableFuel(gate.parent), "teleport and gate previews exclude reserve");
        Check.Equal(false, EtherGateFuelPayment.TryDebit(gate.parent,100,out _), "summoning rejects insufficient surplus");
        Check.Equal(false, CoreReturnUtility.TryConsumeFuel(gate.parent,100), "teleport rejects insufficient surplus");
        Check.Equal(120f, a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel, "failed previews never pay");
        Check.Equal(true, EtherGateFuelPayment.TryDebit(gate.parent,80,out var receipt), "mixed sources pay exactly available surplus");
        Check.Equal(40f, a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel, "only protected quantities remain");
        receipt.Refund(); receipt.Refund();
        Check.Equal(120f, a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel, "receipt refunds once");
        Check.Equal(true, CoreReturnUtility.TryConsumeFuel(gate.parent,80), "maintenance and teleport share surplus policy");
        Check.Equal(40f, a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel, "core payment preserves reserve");
    }
    static void ReserveStellar()
    {
        Make("ManaBeacon",10,10);
        var a = ReserveTank(9,30); var b = ReserveTank(11,30); Reserve(a,10); Reserve(b,10);
        var r = Make("ManaNetworkEndpoint",10,11); ((CompManaNetworkEndpoint)r).ManaMode = ((CompManaNetworkEndpoint)r).FuelMode = ManaNetworkMode.Receive;
        Check.Equal(false, StellarDivinationPayment.CanPay(r.parent), "ritual excludes reserved fuel");
        Reserve(a,0); Reserve(b,0);
        var callback = new CallbackComp { parent=a.parent, Callback=signal => { if(signal=="RanOutOfFuel") Reserve(b,30); } }; a.parent.AllComps.Add(callback);
        Check.Equal(false, StellarDivinationPayment.TryPay(r.parent,()=>true), "later-source reserve mutation aborts partial payment");
        Check.Equal(60f, a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel, "all native fuel refunded");
        Check.Equal(200f, Mana(a)+Mana(b), "mana refunded along with fuel");
        callback.Callback=null; Reserve(b,10);
        Check.Equal(true, StellarDivinationPayment.TryPay(r.parent,()=>true), "exact surplus ritual succeeds");
        Check.Equal(10f, a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel, "protected remainder retained");
    }
    static void ReserveCallbacks()
    {
        // This case can be the first reflection write to native CompRefuelable.
        // Native fuel/payment remains real; only presentation asset factories
        // are suppressed during static initialization in this headless process.
        var assembly = Assembly.Load("0Harmony");
        var patchId = "rwom.reserve.graphics." + Guid.NewGuid();
        var harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, patchId)!;
        var prefix = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!,
            typeof(OdysseyBeaconTests).GetMethod(nameof(SkipReserveGraphics), BindingFlags.Static | BindingFlags.NonPublic)!)!;
        try
        {
            var methods = typeof(SolidColorMaterials).GetMethods().Where(m => m.Name == "SimpleSolidColorMaterial")
                .Append(typeof(ContentFinder<UnityEngine.Texture2D>).GetMethod("Get", new[] { typeof(string), typeof(bool) })!);
            foreach (var method in methods)
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { method, prefix, null, null, null });
            RuntimeHelpers.RunClassConstructor(typeof(CompRefuelable).TypeHandle);
        }
        finally
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { patchId });
        }
        Make("ManaBeacon",10,10);
        var s = ReserveTank(9,5); var r = ReserveTank(11,0,true);
        ((CompManaNetworkEndpoint)r).FuelMode = ManaNetworkMode.Receive;
        var callback = new CallbackComp { parent=s.parent, Callback=signal => { if(signal=="RanOutOfFuel") { Reserve(s,5); network.MapComponentTick(); } } };
        s.parent.AllComps.Add(callback);
        Tick(60);
        Check.Equal(5f,s.parent.GetComp<CompRefuelable>().Fuel,"reserve changed during native debit restores source");
        Check.Equal(0f,r.parent.GetComp<CompRefuelable>().Fuel,"invalidated fuel transfer never credits receiver");
        ((CompManaNetworkEndpoint)s).FuelMode=ManaNetworkMode.Off;
        Reserve(s,0); Set(s.parent.GetComp<CompRefuelable>(),"fuel",1f); Set(Bank(s),"storedMana",0f);
        Check.Equal(0f,Bank(s).ConvertFuel(10),"manual conversion returns no charge if callback reserves paid fuel");
        Check.Equal(1f,s.parent.GetComp<CompRefuelable>().Fuel,"manual conversion refunded native debit");
        Check.Equal(0f,Mana(s),"refund did not also mint converted mana");
        callback.Callback=null; Reserve(s,0);
        Check.Equal(10f,Bank(s).ConvertFuel(10),"conversion works after cancelled transaction");
    }
    static bool SkipReserveGraphics() => false;
    static void ReserveDirectCallbacks()
    {
        ReservePlayerWorld(); Make("ManaBeacon",10,10);
        var a=ReserveTank(9,30); var b=ReserveTank(11,30); var gate=Make("ManaNetworkEndpoint",10,11);
        ((CompManaNetworkEndpoint)gate).FuelMode=ManaNetworkMode.Receive;
        var callback=new CallbackComp { parent=a.parent, Callback=signal=>{if(signal=="RanOutOfFuel") Reserve(b,30);} }; a.parent.AllComps.Add(callback);
        Check.Equal(false,EtherGateFuelPayment.TryDebit(gate.parent,50,out var receipt),"gate rechecks later source protection");
        receipt.Refund();
        Check.Equal(60f,a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel,"gate partial debit restored exactly once");
        RuntimeLog.Clear(); Reserve(b,0);
        Check.Equal(false,CoreReturnUtility.TryConsumeFuel(gate.parent,50),"core return rechecks later source protection");
        Check.Equal(60f,a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel,"core partial debit restored");
        RuntimeLog.Clear(); callback.Callback=null; Reserve(b,0);
        var later=new CallbackComp { parent=b.parent, Callback=signal=>{if(signal=="RanOutOfFuel") Reserve(a,30);} }; b.parent.AllComps.Add(later);
        Check.Equal(false,EtherGateFuelPayment.TryDebit(gate.parent,60,out _),"later callback cannot retroactively consume newly protected first source");
        Check.Equal(60f,a.parent.GetComp<CompRefuelable>().Fuel+b.parent.GetComp<CompRefuelable>().Fuel,"final validation refunds both sources");
        RuntimeLog.Clear();
    }
    static void ReserveStorageScope()
    {
        var c=ReserveTank(10,50); Reserve(c,25);
        c.parent.def.defName="OtherConsumer";
        Check.Equal(0f,Reserved(c),"arbitrary consumer buffer has no protected store");
        Check.Equal(50f,MagofuelReserve.Available(c.parent.GetComp<CompRefuelable>()),"unrelated physical fuel retains old semantics");
        var core=(Building_MagicalGravEngine)RuntimeHelpers.GetUninitializedObject(typeof(Building_MagicalGravEngine)); core.def=c.parent.def;
        var bank=Bank(c); var fuel=c.parent.GetComp<CompRefuelable>();
        bank.parent=core; fuel.parent=core; Set(core,"comps",new List<ThingComp>{bank,fuel});
        Check.Equal(25f,bank.FuelReserve,"actual magical core recognized by type independent of def name");
        Check.Equal(25f,MagofuelReserve.Available(fuel),"core ordinary surplus");
        Check.Equal(50f,MagofuelReserve.Available(fuel,true),"explicit life-support permission sees full physical fuel");
        bank.FuelReserve=float.NaN; Check.Equal(0f,bank.FuelReserve,"invalid input resets safely");
        bank.FuelReserve=1000; Check.Equal(100f,bank.FuelReserve,"UI/API setting bounded to capacity");
    }
    static void ReserveConversionPermission()
    {
        var s=ReserveTank(9,1); var receiver=Tank(11,9,true); Reserve(s,1); Set(Bank(s),"storedMana",0f);
        var oxygen=new CompMagicalOxygen {parent=receiver.parent,props=new CompProperties_MagicalOxygen()}; receiver.parent.AllComps.Add(oxygen);
        s.parent.AllComps.Add(new CallbackComp {parent=s.parent, Callback=signal=>{if(signal=="RanOutOfFuel") receiver.parent.AllComps.Remove(oxygen);}});
        var convert=typeof(CompMagofuelReservoir).GetMethod("ConvertFuelForReceiver",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Check.Equal(0f,(float)convert.Invoke(Bank(s),[10f,receiver.parent])!,"lost life support permission cancels protected conversion");
        Check.Equal(1f,s.parent.GetComp<CompRefuelable>().Fuel,"protected physical debit restored");
        Check.Equal(0f,Mana(s),"refund creates no shared mana");
    }
    static void ReserveConversionThrow()
    {
        var s=ReserveTank(9,1); Set(Bank(s),"storedMana",0f);
        s.parent.AllComps.Add(new CallbackComp {parent=s.parent, Callback=signal=>{if(signal=="RanOutOfFuel") { Reserve(s,1); throw new InvalidOperationException("reserve callback"); }}});
        try { Bank(s).ConvertFuel(10); throw new Exception("Expected injected callback failure"); }
        catch(InvalidOperationException ex) when(ex.Message=="reserve callback") { }
        Check.Equal(1f,s.parent.GetComp<CompRefuelable>().Fuel,"throw cannot bypass changed reserve and destroy physical payment");
        Check.Equal(0f,Mana(s),"cancelled payment creates no charge on throwing callback");
    }
}
