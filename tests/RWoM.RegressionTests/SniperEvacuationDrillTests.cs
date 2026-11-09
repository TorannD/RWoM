using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperEvacuationDrillTests
{
    internal static void Verify(Pawn actor)
    {
        var state = ClassIdentity.Component(actor).State.Find("rwom.sniper")!;
        var origin = actor.Position;
        var previousWorld = Current.Game.World;
        var oldLords = actor.Map.lordManager;
        var world = (RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
        world.factionManager = new FactionManager();
        world.factionManager.AllFactionsListForReading.Add(actor.Faction);
        Set(world.factionManager, "ofPlayer", actor.Faction);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        int Read(string field) => (int)state.GetType().GetField(field, flags)!.GetValue(state)!;
        int count = Read("EvacuationDrills");
        int cooldown = Read("NextDrillTick");
        Building_Bed Station(int x)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.size = new IntVec2(1, 1);
            def.building = new BuildingProperties { bed_humanlike = true };
            var bed = new Building_Bed { def = def };
            Set(bed, "positionInt", new IntVec3(x, 0, 8));
            Set(bed, "mapIndexOrState", (sbyte)0);
            Set(bed, "factionInt", actor.Faction);
            return bed;
        }
        var first = Station(8);
        var second = Station(19);
        var job = new Job { def = new JobDef { defName = "TM_SniperEvacuationDrill" }, targetA = first, targetB = second };
        var driver = new JobDriver_SniperEvacuationDrill { pawn = actor, job = job };
        try
        {
            actor.Map.lordManager = (Verse.AI.Group.LordManager)RuntimeHelpers.GetUninitializedObject(typeof(Verse.AI.Group.LordManager));
            actor.Map.lordManager.lords = [];
            Set(Current.Game, "worldInt", world);
            Check.Equal(false, driver.TryMakePreToilReservations(false), "drill rejects two nearby beds");
            Set(second, "positionInt", new IntVec3(20, 0, 8));
            Check.Equal(true, driver.TryMakePreToilReservations(false), "12 cells between colony stations");
            var steps = ((IEnumerable<Toil>)driver.GetType().GetMethod("MakeNewToils", flags)!.Invoke(driver, null)!).ToArray();
            Check.Equal(300, steps[1].defaultDuration, "first station preparation");
            Check.Equal(300, steps[4].defaultDuration, "second station preparation");
            Set(actor, "positionInt", first.Position);
            steps[2].initAction();
            Check.Equal(count, Read("EvacuationDrills"), "no credit after one station");
            Set(actor, "positionInt", second.Position);
            steps[5].initAction();
            Check.Equal(count + 1, Read("EvacuationDrills"), "credit after both stations");
            Check.Equal(false, driver.TryMakePreToilReservations(false), "daily limit prevents immediate repeat");
        }
        finally
        {
            actor.Map.lordManager = oldLords;
            Set(Current.Game, "worldInt", previousWorld);
            Set(actor, "positionInt", origin);
            Set(state, "EvacuationDrills", count);
            Set(state, "NextDrillTick", cooldown);
        }
    }
}
