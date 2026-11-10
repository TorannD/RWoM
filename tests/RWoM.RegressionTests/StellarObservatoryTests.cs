using System.Reflection;
using RimWorld.Planet;
namespace RWoM.RegressionTests;
internal static class StellarObservatoryTests
{
    static Type T(string n) => typeof(TorannMagic.MagicData).Assembly.GetType("TorannMagic.Odyssey."+n) ?? throw new Exception("Missing stellar gameplay: "+n);
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Stellar.SharedCooldownAtomicReentry", _ => {
            var type=T("WorldComponent_StellarDivination");var world=Activator.CreateInstance(type,new object?[]{null})!;
            var start=type.GetMethod("TryStart")!;int paid=0;
            bool Start(int now,Func<bool> pay)=>(bool)start.Invoke(world,[now,pay])!;
            Check.Equal(false,Start(0,()=>false),"failed payment does not consume interval");
            Check.Equal(true,Start(0,()=> {paid++; Check.Equal(false,Start(0,()=>{paid++;return true;}),"callback reentry blocked globally");return true;}),"tick zero purchase");
            Check.Equal(false,Start(1079999,()=>{paid++;return true;}),"18 day cooldown exclusive boundary");
            Check.Equal(true,Start(1080000,()=>{paid++;return true;}),"exact next interval");
            Check.Equal(2,paid,"only winners paid");
        });
        yield return new("Odyssey.Stellar.SearchActiveTicksPauseAndDuplicate", _ => {
            var type=T("StellarDivinationSearch");var search=Activator.CreateInstance(type)!;
            type.GetMethod("Begin")!.Invoke(search,[1,480000]);
            void Tick(int now,int delta,bool active)=>type.GetMethod("Advance")!.Invoke(search,[now,delta,active]);
            int Remaining()=>(int)type.GetProperty("RemainingTicks")!.GetValue(search)!;
            Tick(0,60,true);Tick(0,60,true);Check.Equal(479940,Remaining(),"duplicate callbacks consume one interval");
            Tick(6000,60,false);Tick(12000,1,true);Check.Equal(479939,Remaining(),"pause and wallclock gap grant no progress");
            Tick(12001,479939,true);Check.Equal(0,Remaining(),"exact completion");
        });
    }
}
