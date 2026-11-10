using System.Reflection;
using System.Runtime.CompilerServices;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static class EtherCaravanTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EtherGate.NativeCargoTransferToDurableOwner", _ => FleshStatTests.InGame(() =>
        {
            FleshStatTests.Set(Current.Game,"maps",new List<Map>());
            var type=typeof(MagicData).Assembly.GetType("TorannMagic.Odyssey.Pawn_EtherServant")!;
            var pawn=(Pawn)Activator.CreateInstance(type)!;
            pawn.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); pawn.def.race=new RaceProperties();
            pawn.inventory=new Pawn_InventoryTracker(pawn); pawn.carryTracker=new Pawn_CarryTracker(pawn);
            var receiver=new Pawn {def=pawn.def}; receiver.inventory=new Pawn_InventoryTracker(receiver);
            var def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef)); def.stackLimit=75; def.thingClass=typeof(Thing);
            var carry=new Thing {def=def,stackCount=12}; var pack=new Thing {def=def,stackCount=7};
            pawn.carryTracker.innerContainer.TryAdd(carry,false); pawn.inventory.innerContainer.TryAdd(pack,false);
            var method=Check.NotNull(type.GetMethod("TryTransferCargoTo"),"offmap durable transfer API");
            Check.Equal(true,(bool)method.Invoke(pawn,[receiver.inventory.innerContainer])!,"transfer succeeds");
            Check.Same(receiver.inventory,carry.ParentHolder,"carry retains durable owner");
            Check.Same(receiver.inventory,pack.ParentHolder,"inventory retains durable owner");
            Check.Equal(19,receiver.inventory.innerContainer.Sum(t=>t.stackCount),"total cargo retained");
            Check.Equal(0,pawn.inventory.innerContainer.Count,"inventory released");
            Check.Equal(0,pawn.carryTracker.innerContainer.Count,"carry released");
            Check.Equal(true,(bool)method.Invoke(pawn,[receiver.inventory.innerContainer])!,"repeat empty transfer harmless");
        }));
    }
}



