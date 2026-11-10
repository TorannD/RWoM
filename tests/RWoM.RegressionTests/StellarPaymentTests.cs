using System.Reflection;
using TorannMagic.Odyssey;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> StellarPaymentCases()
    {
        yield return new("Odyssey.Stellar.NetworkPaymentAndRollback", f => Run(f, () => {
            Make("ManaBeacon",10,10);
            var a=Tank(9,9);a.parent.def.defName="TM_MagofuelReservoir";var af=AddFuel(a,30);
            var b=Tank(11,9);b.parent.def.defName="TM_MagofuelReservoir";var bf=AddFuel(b,20);
            Set(a.parent.GetComp<CompMagofuelReservoir>(),"storedMana",40f);Set(b.parent.GetComp<CompMagofuelReservoir>(),"storedMana",60f);
            var receiver=Make("ManaNetworkEndpoint",10,11);((CompManaNetworkEndpoint)receiver).ManaMode=ManaNetworkMode.Receive;((CompManaNetworkEndpoint)receiver).FuelMode=ManaNetworkMode.Receive;
            var type=T("StellarDivinationPayment");var debit=type.GetMethod("TryPay")!;
            bool Pay(Func<bool> valid)=>(bool)debit.Invoke(null,[receiver.parent,valid])!;
            Check.Equal(false,Pay(()=>false),"invalidated transaction rolled back");Check.Equal(100f,Mana(a)+Mana(b),"mana refunded");Check.Equal(50f,af.Fuel+bf.Fuel,"fuel refunded");
            var failure=new CallbackComp{parent=bf.parent,Callback=s=>{if(s=="RanOutOfFuel"){af.Props.fuelCapacity=5;bf.Props.fuelCapacity=5;throw new InvalidOperationException("stellar payment injected failure");}}};bf.parent.AllComps.Add(failure);
            Check.Equal(false,Pay(()=>true),"native callback exception rolls back partial sources");Check.Equal(100f,Mana(a)+Mana(b),"all mana refunded after exception");Check.Equal(50f,af.Fuel+bf.Fuel,"all fuel refunded after exception");failure.Callback=null;
            af.Props.fuelCapacity=100;bf.Props.fuelCapacity=100;
            var diagnostics=(List<string>)typeof(RuntimeLog).GetField("Problems",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;Check.Equal(1,diagnostics.Count,"one expected cancellation diagnostic");Check.Equal(true,diagnostics[0].Contains("stellar payment injected failure"),"preserve callback diagnosis");RuntimeLog.Clear();
            failure.Callback=s=>{if(s=="RanOutOfFuel"){bf.Refuel(7f);throw new InvalidOperationException("stellar callback refilled before failure");}};
            Check.Equal(false,Pay(()=>true),"refilling callback aborts safely");Check.Equal(57f,af.Fuel+bf.Fuel,"refund preserves both exact debit and callback-added fuel");Check.Equal(100f,Mana(a)+Mana(b),"mana refunded alongside refilling callback");failure.Callback=null;Set(bf,"fuel",20f);
            Check.Equal(1,diagnostics.Count,"one expected refilling callback diagnostic");Check.Equal(true,diagnostics[0].Contains("stellar callback refilled before failure"),"expected refilling failure");RuntimeLog.Clear();
            bf.parent.AllComps.Add(new CallbackComp{parent=bf.parent,Callback=s=>{if(s=="RanOutOfFuel")Check.Equal(false,Pay(()=>true),"reentrant payment denied");}});
            Check.Equal(true,Pay(()=>true),"split network pays");Check.Equal(0f,Mana(a)+Mana(b),"exact hundred network mana");Check.Equal(0f,af.Fuel+bf.Fuel,"exact fifty physical fuel");
            Check.Equal(false,Pay(()=>true),"exhausted sources cannot repay");
        }));
    }
}
