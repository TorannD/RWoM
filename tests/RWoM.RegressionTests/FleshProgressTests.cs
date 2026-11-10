using System.Xml.Linq;
using TorannMagic;
using TorannMagic.FleshDevelopment;
using Verse;

namespace RWoM.RegressionTests;

internal static class FleshProgressTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("FleshProgress.PawnSaveHookDiscovered", _ => {
            var patchType = typeof(FleshProgress).Assembly.GetType("TorannMagic.FleshDevelopment.FleshSavePatches")!;
            var patch = patchType.CustomAttributes.Single(a => a.AttributeType.Name == "HarmonyPatch");
            Check.Equal(typeof(Pawn), patch.ConstructorArguments[0].Value, "save target type");
            Check.Equal(nameof(Pawn.ExposeData), patch.ConstructorArguments[1].Value as string, "save target method");
            Check.NotNull(typeof(Pawn).GetMethod(nameof(Pawn.ExposeData)), "installed engine save method exists");
            Check.NotNull(patchType.GetMethod("Postfix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic), "scanner finds postfix");
        });
        yield return new("FleshProgress.FreshAndExperienceCarry", _ => {
            var data = new FleshProgress();
            Fresh(data);
            data.AddExperience(250);
            Check.Equal(3, data.Level, "two levels");
            Check.Equal(25f, data.Experience, "XP carry");
            Check.Equal(2, data.SkillPoints, "points");
            Check.Equal(150f, data.RequiredExperience, "next threshold");
        });
        yield return new("FleshProgress.AllCapsAndUltimateGate", _ => {
            var data = new FleshProgress { SkillPoints = 100 };
            foreach (FleshSkill skill in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Feast)) {
                Check.Equal(false, data.TryUpgrade(FleshSkill.Feast), "ultimate gated until every rank");
                while (data.GetRank(skill) < FleshProgress.MaxRank(skill)) Check.Equal(true, data.TryUpgrade(skill), "upgrade");
                int points = data.SkillPoints;
                Check.Equal(false, data.TryUpgrade(skill), "rank cap");
                Check.Equal(points, data.SkillPoints, "cap consumes nothing");
            }
            Check.Equal(true, data.CanUnlockFeast, "fully unlocked");
            Check.Equal(true, data.TryUpgrade(FleshSkill.Feast), "ultimate purchase");
            Check.Equal(false, data.TryUpgrade(FleshSkill.Feast), "ultimate cap");
            Check.Equal(100f, data.MaxThirst, "capacity");
            data.AddThirst(1000); Check.Equal(100f, data.Thirst, "upper clamp");
            data.AddThirst(-1000); Check.Equal(0f, data.Thirst, "lower clamp");
        });
        yield return new("FleshProgress.RepositoryIdentityAndEligibility", _ => {
            var ordinary = PawnWith(null);
            Check.Equal<FleshProgress?>(null, FleshRepository.TryGet(ordinary), "read does not create");
            Check.Equal<FleshProgress?>(null, FleshRepository.Ensure(ordinary), "ordinary excluded");
            foreach (string marker in new[] { "TM_UndeadHD", "TM_UndeadAnimalHD" }) {
                var pawn = PawnWith(marker); var data = FleshRepository.Ensure(pawn); Fresh(data);
                data.AddExperience(125); pawn.health.hediffSet.hediffs.Clear();
                Check.Same(data, FleshRepository.Ensure(pawn), "hediff removal retains state");
                Check.Equal<FleshProgress?>(null, FleshRepository.TryGet(PawnWith(marker)), "new identity no stale cache");
            }
        });
        yield return new("FleshProgress.OwnerUsesActualUndeadLink", _ => {
            var pawn = PawnWith(null); var owner = PawnWith(null);
            var type = typeof(MagicData).Assembly.GetType("TorannMagic.HediffComp_Undead")!;
            var comp = (HediffComp)Activator.CreateInstance(type, true)!;
            type.GetField("linkedPawn")!.SetValue(comp, owner);
            var marker = new HediffWithComps { pawn = pawn, def = new HediffDef { defName = "TM_UndeadAnimalHD" }, comps = [comp] };
            comp.parent = marker; pawn.health.hediffSet.hediffs.Add(marker);
            Check.Same(owner, FleshRepository.Owner(pawn), "actual owner regardless of class");
            type.GetField("linkedPawn")!.SetValue(comp, null);
            Check.Equal<Pawn?>(null, FleshRepository.Owner(pawn), "removed link");
        });
        yield return new("FleshProgress.OldSaveMissingMigratesExactlyOnce", fixture => {
            foreach (string? marker in new string?[] { "TM_UndeadHD", "TM_UndeadAnimalHD", null }) {
                var pawn = PawnWith(marker);
                Load(fixture, "<regression><data /></regression>", pawn);
                var data = FleshRepository.TryGet(pawn);
                if (marker == null) { Check.Equal<FleshProgress?>(null, data, "ordinary old pawn unchanged"); continue; }
                Fresh(data!); data!.AddExperience(155);
                string xml = Save(fixture, pawn);
                Load(fixture, xml, pawn);
                Check.Equal(2, FleshRepository.TryGet(pawn).Level, "reload keeps level");
                Check.Equal(55f, FleshRepository.TryGet(pawn).Experience, "reload keeps XP");
            }
        });
        yield return new("FleshProgress.ScribePreservesProgressAndTimers", fixture => {
            var pawn = PawnWith("TM_UndeadHD"); var data = FleshRepository.Ensure(pawn);
            data.Level = 12; data.Experience = 123; data.SkillPoints = 8;
            data.TryUpgrade(FleshSkill.Restore); data.TryUpgrade(FleshSkill.Capacity);
            data.Thirst = 27; data.LastTick = 5432; data.EmpowerUntil = 20000; data.HardenUntil = 23000;
            data.FeastUntil = 7000; data.FeastRage = true; data.SetReadyTick(FleshSkill.Feast, 65432);
            var loadedPawn = PawnWith(null); // Removal of undead must not discard the saved tree.
            Load(fixture, Save(fixture, pawn), loadedPawn);
            var loaded = FleshRepository.TryGet(loadedPawn);
            Check.Equal(12, loaded.Level, "level"); Check.Equal(123f, loaded.Experience, "experience");
            Check.Equal(6, loaded.SkillPoints, "unspent points"); Check.Equal(27f, loaded.Thirst, "thirst");
            Check.Equal(1, loaded.GetRank(FleshSkill.Restore), "rank"); Check.Equal(30f, loaded.MaxThirst, "capacity rank");
            Check.Equal(5432, loaded.LastTick, "last tick"); Check.Equal(20000, loaded.EmpowerUntil, "empower deadline");
            Check.Equal(23000, loaded.HardenUntil, "harden deadline"); Check.Equal(7000, loaded.FeastUntil, "feast deadline");
            Check.Equal(true, loaded.FeastRage, "own rage"); Check.Equal(65432, loaded.GetReadyTick(FleshSkill.Feast), "cooldown");
        });
        yield return new("FleshProgress.PartialAndNullContainersPreserveSavedFields", fixture => {
            var pawn = PawnWith("TM_UndeadHD");
            Load(fixture, "<regression><data><TM_FleshDevelopment><level>9</level><experience>44</experience><skillPoints>3</skillPoints><ranks IsNull=\"True\"/><readyTicks IsNull=\"True\"/></TM_FleshDevelopment></data></regression>", pawn);
            var data = FleshRepository.TryGet(pawn);
            Check.Equal(9, data.Level, "missing schema does not reset"); Check.Equal(44f, data.Experience, "XP retained");
            Check.Equal(1, data.GetRank(FleshSkill.Devour), "missing free rank");
            Check.Equal(true, data.TryUpgrade(FleshSkill.Restore), "null ranks usable");
            data.SetReadyTick(FleshSkill.Harden, 500);
            var xml = XDocument.Parse(Save(fixture, pawn));
            xml.Descendants("schemaVersion").Remove(); xml.Descendants("thirst").Remove();
            Load(fixture, xml.ToString(), pawn);
            data = FleshRepository.TryGet(pawn);
            Check.Equal(9, data.Level, "partial load retains level"); Check.Equal(1, data.GetRank(FleshSkill.Restore), "partial ranks retained");
            Check.Equal(500, data.GetReadyTick(FleshSkill.Harden), "partial timers retained");
        });
    }

    private static void Fresh(FleshProgress data) {
        Check.NotNull(data, "initial data"); Check.Equal(1, data.Level, "initial level");
        Check.Equal(0f, data.Experience, "initial XP"); Check.Equal(0, data.SkillPoints, "initial points");
        Check.Equal(20f, data.Thirst, "initial thirst"); Check.Equal(1, data.GetRank(FleshSkill.Devour), "free rank");
        foreach (FleshSkill skill in Enum.GetValues<FleshSkill>().Where(s => s != FleshSkill.Devour)) Check.Equal(0, data.GetRank(skill), "other initial rank");
    }
    private static Pawn PawnWith(string? marker) {
        var def = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.race = new RaceProperties();
        var pawn = new Pawn { def = def };
        pawn.health = new Pawn_HealthTracker(pawn);
        if (marker != null) pawn.health.hediffSet.hediffs.Add(new Hediff { pawn = pawn, def = new HediffDef { defName = marker } });
        return pawn;
    }
    public sealed class RepositoryEnvelope : IExposable {
        private readonly Pawn pawn;
        public RepositoryEnvelope(Pawn pawn) { this.pawn = pawn; }
        public void ExposeData() => FleshRepository.Expose(pawn);
    }
    private static string Save(RuntimeFixture fixture, Pawn pawn) {
        string path = Path.Combine(fixture.WorkingDirectory, "flesh-save.xml");
        var envelope = new RepositoryEnvelope(pawn);
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "data", pawn); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        return File.ReadAllText(path);
    }
    private static void Load(RuntimeFixture fixture, string xml, Pawn pawn) {
        string path = Path.Combine(fixture.WorkingDirectory, "flesh-load.xml"); File.WriteAllText(path, xml);
        RepositoryEnvelope? envelope = null;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "data", pawn); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        Check.NotNull(envelope, "loaded envelope");
    }
}
