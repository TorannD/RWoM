using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using TorannMagic;
using TorannMagic.Odyssey;
using Verse;
using Verse.AI;
namespace RWoM.RegressionTests;
internal static partial class OdysseyGrandDomeTests
{
    public static IEnumerable<TestCase> StellarRitualCases()
    {
        yield return new("Odyssey.Stellar.RitualActualWorkReservationNetworkOnly", f => Run(() => {
            using var seam = new StellarEligibilitySeam();
            var world=(World)RuntimeHelpers.GetUninitializedObject(typeof(World));world.components=new List<WorldComponent>{new WorldComponent_StellarDivination(world)};Set(Current.Game,"worldInt",world);
            var network=(MapComponent_ManaNetwork)AddField("ManaNetwork");
            var c=(CompStellarObservatory)AddComp(Dome().parent,"StellarObservatory");
            var endpoint=(CompManaNetworkEndpoint)AddComp(c.parent,"ManaNetworkEndpoint");endpoint.Initialize(endpoint.props);endpoint.ManaMode=ManaNetworkMode.Receive;endpoint.FuelMode=ManaNetworkMode.Receive;endpoint.PostSpawnSetup(false);
            var beacon=AddComp(Dome().parent,"ManaBeacon");beacon.PostSpawnSetup(false);
            var source=Dome().parent;source.def.defName="TM_MagofuelReservoir";var bank=(CompMagofuelReservoir)AddComp(source,"MagofuelReservoir");Set(bank,"storedMana",100f);
            var sender=(CompManaNetworkEndpoint)AddComp(source,"ManaNetworkEndpoint");sender.Initialize(sender.props);sender.PostSpawnSetup(false);
            // Give the physical store the normal magofuel filter.
            var fuel=source.GetComp<CompRefuelable>();var item=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));item.defName="TM_Magofuel";item.virtualDefs=new List<ThingDef>();var filter=new ThingFilter();filter.SetAllow(item,true);fuel.Props.fuelFilter=filter;Set(fuel,"fuel",50f);
            var pawn=Mage(c);var mana=pawn.needs.TryGetNeed<Need_Mana>();mana.CurLevel=.01f;
            map.reservationManager=new ReservationManager(map);map.physicalInteractionReservationManager=new PhysicalInteractionReservationManager();map.events=new MapEvents(map);
            var driver=new JobDriver_ActivateStellarObservatory{pawn=pawn,job=new Job(new JobDef{defName="StellarRitual"},c.parent)};
            Check.Equal(true,driver.TryMakePreToilReservations(false),"low personal mana mage can reserve ritual");
            var toil=RitualToil(driver);
            map.reservationManager.Release(c.parent,pawn,driver.job);toil.tickAction();
            Check.Equal(0,(int)driver.GetType().GetField("workedTicks",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(driver)!,"lost reservation cannot advance ritual");
            Check.Equal(100f,bank.StoredMana,"lost reservation cannot charge");Check.Equal(true,driver.TryMakePreToilReservations(false),"fresh reservation restores valid work");
            for(int i=0;i<3599;i++){Set(Find.TickManager,"ticksGameInt",i);toil.tickAction();}
            Check.Equal(100f,bank.StoredMana,"interrupted ritual has no debit");Check.Equal(50f,fuel.Fuel,"interrupted ritual keeps fuel");
            driver=(JobDriver_ActivateStellarObservatory)SaveDriver(f,driver,"stellar-ritual-work");toil=RitualToil(driver);toil.tickAction();Check.Equal(false,c.IsSearching,"saved same tick cannot replay work");
            Set(Find.TickManager,"ticksGameInt",3599);toil.tickAction();Check.Equal(true,c.IsSearching,"3600 actual ticks complete");Check.Equal(0f,bank.StoredMana,"100 network mana paid");Check.Equal(0f,fuel.Fuel,"50 network fuel paid");Check.Equal(.01f,mana.CurLevel,"personal mana untouched");Check.Equal(true,c.RemainingTicks>=480000&&c.RemainingTicks<=600000,"native duration range");
            Check.Equal(false,c.CompleteActivation(pawn),"repeat ritual completion rejected");toil.tickAction();Check.Equal(true,c.IsSearching,"repeat job callback safe");
        }));
    }
    static bool StellarEligible(ref bool __result){__result=true;return false;}
    sealed class StellarEligibilitySeam:IDisposable
    {
        readonly Assembly asm=Assembly.Load("0Harmony");readonly object harmony;readonly string id="rwom.stellar.eligibility."+Guid.NewGuid();
        public StellarEligibilitySeam()
        {
            harmony=Activator.CreateInstance(asm.GetType("HarmonyLib.Harmony")!,id)!;
            var hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,typeof(OdysseyGrandDomeTests).GetMethod(nameof(StellarEligible),BindingFlags.Static|BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{typeof(StellarDivinationQuestUtility).GetMethod("HasEligibleQuest"),hm,null,null,null});
        }
        public void Dispose()=>harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,new object[]{id});
    }
}
