using RimWorld;
using TorannMagic;
using TorannMagic.AutoCast;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class TeleportSpiritTests
{
    public static IEnumerable<TestCase> TravelEnvironmentAutoCases()
    {
        yield return new("TravelSafety.AutomaticDestinationRefusesBeforeManaOrMovement", f => World(f, w =>
        {
            PrepareTravel(w); using var boundaries = TravelBoundaries(w);
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map);
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            using var sense = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null
                ? new DefinitionScope<HediffDef>(new HediffDef { defName = "TM_SyrriumSenseHD" }) : null;
            w.Map.gameConditionManager = new GameConditionManager(w.Map);
            boundaries.Prefix(typeof(GameConditionManager).GetProperty("Parent", Flags)!.GetMethod!, nameof(NoParentConditions));
            w.Caster.story = new Pawn_StoryTracker(w.Caster) { traits = new TraitSet(w.Caster) };
            w.Caster.needs = new Pawn_NeedsTracker(w.Caster);
            var magic = new CompAbilityUserMagic { parent = w.Caster }; w.Caster.AllComps.Add(magic); TM_Calc.InvalidateCompCache(w.Caster);
            Set(magic, "magicData", new MagicData());
            var mana = new Need_Mana(w.Caster); mana.CurLevel = .9f; w.Caster.needs.AllNeeds.Add(mana);
            var def = f.AddAbility("TravelSafetyAutomatic"); def.MainVerb.range = 16; def.MainVerb.warmupTime = 0; def.manaCost = .05f; def.consumeEnergy = true;
            var ability = new MagicAbility(magic) { Def = def };
            var initialCooldown = ability.CooldownTicksLeft;
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 0, out var safePlan), "same structural destination is useful when safe");
            env.Vacuum = 1;
            Check.Equal(false, TM_TravelAutocast.StillValid(w.Caster, safePlan), "fresh environment invalidates previously safe plan");
            var origin = w.Caster.Position;
            Check.Equal(false, TM_TravelAutocast.TryExecute(w.Caster, def, ability, 1, false), "native unsafe automatic route refused");
            Check.Equal(origin, w.Caster.Position, "no movement before refusal");
            Check.Equal(.9f, mana.CurLevel, "real mana unchanged");
            Check.Equal(initialCooldown, ability.CooldownTicksLeft, "cooldown unchanged on refusal");
            Check.Same(w.Job, w.Caster.CurJob, "work ownership unchanged");
        }));
        yield return new("TravelSafety.AutomaticFlightArrivalRetainsPassengerThenRecovers", f => World(f, w =>
        {
            PrepareTravel(w); using var boundaries = FlightBoundaries(w);
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map);
            var cargo = FlightCargo(w);
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 30, out var plan, TM_WorkFlight.TicksPerCell), "safe flight planned");
            var flight = FlightCarrier(w); Check.Equal(true, flight.Begin(w.Caster, plan), "safe takeoff owns actual passenger");
            env.Vacuum = 1;
            Check.Equal(false, flight.TryLand(), "newly dangerous destination and origin both refused");
            flight.Destroy();
            Check.Equal(false, flight.Destroyed, "cleanup retains carrier when every landing is unsafe");
            Check.Same(w.Caster, flight.Passenger, "actual passenger retained");
            Check.Same(flight, w.Caster.ParentHolder, "native owner retained while passenger.Map is null");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "cargo retained");
            env.Vacuum = 0;
            Check.Equal(true, flight.TryLand(), "recovered atmosphere permits later landing");
            Check.Equal(0, flight.GetDirectlyHeldThings().Count, "ownership transferred exactly once");
            Check.Same(w.Job, w.Caster.CurJob, "work resumes unchanged");
            Check.Same(cargo, w.Caster.carryTracker.CarriedThing, "same cargo after recovery");
        }));
        yield return new("TravelSafety.AutomaticFlightHorizonRejectsSoonExpiringShell", f => World(f, w =>
        {
            PrepareTravel(w); using var boundaries = TravelBoundaries(w);
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map);
            env.Vacuum = 1; var shell = env.Shell(601);
            Check.Equal(true, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 0, out _), "instant travel fits remaining shell margin");
            Check.Equal(false, TM_TravelAutocast.TryPlan(w.Caster, 16, 1, 0, out _, TM_WorkFlight.TicksPerCell), "flight horizon makes same protection expire too soon");
            Check.Equal(601, shell.ticksToDisappear, "planning never advances shell timer");
        }));
    }
}
