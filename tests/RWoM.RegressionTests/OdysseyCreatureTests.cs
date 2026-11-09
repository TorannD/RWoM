using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.Odyssey;
using Verse;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    public static IEnumerable<TestCase> OdysseyCreatureCases()
    {
        yield return new("Odyssey.Creatures.GolemAndUnrelatedFlesh", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1, Temperature = 21 };
            using var patches = new CreaturePatchScope();
            var golem = new TorannMagic.Golems.TMPawnGolem { def = w.Caster.def };
            golem.health = new Pawn_HealthTracker(golem);
            golem.ageTracker = new Pawn_AgeTracker(golem); Set(golem.ageTracker, "cachedLifeStageIndex", 0);
            CreatureProtected(golem);
            w.Caster.def.race.canBeVacuumBurnt = false;
            Check.Equal(true, w.Caster.HarmedByVacuum, "burn-only resistance does not become race-wide exposure immunity");
            w.Caster.health.hediffSet.hediffs.Add(new Hediff { pawn = w.Caster, def = new HediffDef { defName = "ROM_VampUnrelated" } });
            Check.Equal(true, w.Caster.HarmedByVacuum, "unrelated vampire hediff does not grant immunity");
            var oldSpiritRace = TorannMagicDefOf.TM_SpiritTD;
            try
            {
                TorannMagicDefOf.TM_SpiritTD = w.Caster.def;
                CreatureProtected(w.Caster);
            }
            finally { TorannMagicDefOf.TM_SpiritTD = oldSpiritRace; }
            env.Odyssey = false;
            Check.Equal(0f, golem.GetStatValue(StatDefOf.VacuumResistance), "compatibility inactive without Odyssey");
        }));
        foreach (string id in new[] { "TM_UndeadHD", "TM_UndeadAnimalHD", "TM_LichHD", "TM_SpiritPossessorHD" })
        yield return new("Odyssey.Creatures.NativeStateAndRemoval." + id, f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1, Temperature = 21 };
            using var patches = new CreaturePatchScope();
            using var healthBoundaries = w.HealingBoundaries();
            var pawn = w.Caster;
            var def = (HediffDef)typeof(TorannMagicDefOf).GetField(id)!.GetValue(null)!;
            Check.Equal(true, pawn.HarmedByVacuum, "prime native unprotected cache");
            var oldExposure = HediffDefOf.VacuumExposure;
            var exposureDef = new HediffDef { defName = "VacuumExposure", maxSeverity = 1 };
            HediffDefOf.VacuumExposure = exposureDef;
            var exposure = new Hediff { pawn = pawn, def = exposureDef, Severity = .1f };
            pawn.health.hediffSet.hediffs.Add(exposure);
            try
            {
                VacuumUtility.PawnVacuumTickInterval(pawn, 60);
                Check.Equal(true, exposure.Severity > .1f, "native exposure increases before transformation");
                float severity = exposure.Severity;
                var hd = new Hediff { pawn = pawn, def = def };
                pawn.health.hediffSet.hediffs.Add(hd);
                CreatureProtected(pawn);
                for (int i = 0; i < 3; i++) VacuumUtility.PawnVacuumTickInterval(pawn, 60);
                Check.Equal(severity, exposure.Severity, "native repeated exposure stops without healing existing injury");
                env.Shell(1);
                Check.Equal(true, TravelEnvironmentSafety.IsSafe(pawn, w.Map, pawn.Position), "natural protection survives projected shell expiry");
                pawn.health.hediffSet.hediffs.Clear();
                Check.Equal(true, pawn.HarmedByVacuum, "removal restores vulnerability in same tick");
                Check.Equal(true, pawn.ConcernedByVacuum, "removal restores native avoidance");
                Check.Equal(0f, pawn.GetStatValue(StatDefOf.VacuumResistance, cacheStaleAfterTicks: 60), "no protected value leaked into cache");
                pawn.health.hediffSet.hediffs.Add(exposure);
                VacuumUtility.PawnVacuumTickInterval(pawn, 60);
                Check.Equal(true, exposure.Severity > severity, "native exposure resumes after removal");
            }
            finally { HediffDefOf.VacuumExposure = oldExposure; }
        }));
        yield return new("Odyssey.Creatures.PossessionOwnershipAndSave", f => World(f, w =>
        {
            using var env = new TravelEnvironmentFixture(w.Caster, w.Map) { Vacuum = 1, Temperature = 21 };
            using var patches = new CreaturePatchScope();
            var host = w.Caster; var spirit = w.Patient;
            using var spiritDef = new DefinitionScope<HediffDef>(TorannMagicDefOf.TM_SpiritPossessorHD);
            var state = new Hediff_Possessor { pawn = spirit, def = TorannMagicDefOf.TM_SpiritPossessorHD };
            spirit.health.hediffSet.hediffs.Add(state);
            var possession = new HediffWithComps { pawn = host, def = TorannMagicDefOf.TM_SpiritPossessionHD };
            var holder = new HediffComp_SpiritPossession { parent = possession, props = new HediffCompProperties() };
            possession.comps = [holder]; host.health.hediffSet.hediffs.Add(possession);
            var container = new ThingOwner<Thing>(holder);
            Set(holder, "innerContainer", container);
            Set(spirit, "mapIndexOrState", (sbyte)-1); Set(spirit, "comps", null);
            Check.Equal(true, container.TryAdd(spirit), "real owner accepts spirit");
            Check.Equal(true, host.HarmedByVacuum, "living host remains vulnerable despite contained spirit");
            state.wasDead = true;
            CreatureProtected(host);
            string path = Path.Combine(f.WorkingDirectory, "dead-host-state.xml");
            var saved = new CreatureSpiritSave { State = state };
            try { Scribe.saver.InitSaving(path, "regression"); saved.ExposeData(); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            state.wasDead = false;
            Check.Equal(true, host.HarmedByVacuum, "changing provenance cannot leave stale immunity");
            var restored = new CreatureSpiritSave();
            try { Scribe.loader.InitLoading(path); restored.ExposeData(); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            spirit.health.hediffSet.hediffs.Remove(state);
            restored.State.pawn = spirit; spirit.health.hediffSet.hediffs.Add(restored.State);
            CreatureProtected(host);
            container.Remove(spirit);
            Check.Equal(true, host.HarmedByVacuum, "detached spirit cannot protect host even with stale possession hediff");
            Check.Equal(true, container.TryAdd(spirit), "spirit reattached");
            host.health.hediffSet.hediffs.Remove(possession);
            Check.Equal(true, host.HarmedByVacuum, "removed possession hediff cannot grant protection");
        }));
    }

    public sealed class CreatureSpiritSave : IExposable
    {
        public Hediff_Possessor State = null!;
        public void ExposeData() => Scribe_Deep.Look(ref State, "spiritState");
    }

    static void CreatureProtected(Pawn pawn)
    {
        Check.Equal(1f, pawn.GetStatValue(StatDefOf.VacuumResistance, cacheStaleAfterTicks: 60), "native effective resistance");
        Check.Equal(false, pawn.HarmedByVacuum, "native exposure gate");
        Check.Equal(false, pawn.ConcernedByVacuum, "native AI avoidance");
        Check.Equal(false, VacuumUtility.CanBeVacuumBurnt(pawn), "no burns from retained exposure");
        Check.Equal(0f, pawn.GetStatValue(StatDefOf.ComfyTemperatureMin), "no added cold immunity");
    }

    sealed class CreaturePatchScope : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony"); readonly object harmony;
        readonly string id = "rwom.creature-vacuum." + Guid.NewGuid();
        readonly Dictionary<FieldInfo, object?> previous = [];
        public CreaturePatchScope()
        {
            foreach (string name in new[] { "TM_UndeadHD", "TM_UndeadAnimalHD", "TM_LichHD", "TM_SpiritPossessorHD", "TM_SpiritPossessionHD" })
            {
                var field = typeof(TorannMagicDefOf).GetField(name)!;
                previous[field] = field.GetValue(null); field.SetValue(null, new HediffDef { defName = name });
            }
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            foreach (string name in new[] { "MagicalCreatureVacuumStatPatch", "MagicalCreatureVacuumBurnPatch" })
            {
                var type = typeof(TravelEnvironmentSafety).Assembly.GetType("TorannMagic.Odyssey." + name);
                if (type == null) throw new InvalidOperationException("Missing production compatibility patch " + name);
                var processor = harmony.GetType().GetMethod("CreateClassProcessor")!.Invoke(harmony, [type])!;
                processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            }
        }
        public void Dispose()
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
            foreach (var item in previous) item.Key.SetValue(null, item.Value);
        }
    }
}
