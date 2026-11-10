using System.Reflection;
using TorannMagic.Odyssey;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> StellarLifecycleCases()
    {
        yield return new("Odyssey.Stellar.BuildingPauseOwnershipAndTransfer", f => Run(f, () => {
            var flag=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.NonPublic|BindingFlags.Static)!;var old=flag.GetValue(null);
            try {
                flag.SetValue(null,true);
                var c=Make("StellarObservatory",10,10);
                var search=Activator.CreateInstance(T("StellarDivinationSearch"))!;Call(search,"Begin",1,480000);Set(c,"search",search);
                void Advance(int now) {Set(Find.TickManager,"ticksGameInt",now);c.CompTick();}
                Advance(0);Check.Equal(479999,(int)Get(c,"RemainingTicks"),"spawned building advances");
                Set(c.parent,"mapIndexOrState",(sbyte)-1);Advance(1);Advance(10000);Check.Equal(479999,(int)Get(c,"RemainingTicks"),"minification pauses paid progress");
                Set(c.parent,"mapIndexOrState",(sbyte)0);Advance(10001);Check.Equal(479998,(int)Get(c,"RemainingTicks"),"relocation resumes no wallclock catchup");
                Set(c.parent,"factionInt",new RimWorld.Faction{def=new RimWorld.FactionDef()});Advance(10002);Check.Equal(479998,(int)Get(c,"RemainingTicks"),"capture pauses player search");
                Set(c.parent,"factionInt",faction);Set(c.parent,"mapIndexOrState",(sbyte)-2);Advance(10003);Check.Equal(479998,(int)Get(c,"RemainingTicks"),"destroyed device cannot advance");
            } finally {flag.SetValue(null,old);}
        }));
    }
}
