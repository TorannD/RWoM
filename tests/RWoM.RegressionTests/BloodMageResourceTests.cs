using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BloodMageResourceTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("BloodMageResource.NativeSustainAndLoadedStatePreserveBlood", Sustain);
        yield return new("SpiritProjectedCast.NativeBloodPaymentBelongsToHost", f => WithBlood(f, blood =>
        {
            using var fixture = new SpiritRebirthStateTests.Fixture(f);
            var soul = fixture.Original;
            var host = fixture.Host;
            PrepareBody(soul); PrepareBody(host);
            var originalPool = new Hediff { def = blood, pawn = soul };
            var hostPool = new Hediff { def = blood, pawn = host };
            soul.health.hediffSet.hediffs.Add(originalPool);
            host.health.hediffSet.hediffs.Add(hostPool);
            hostPool.Severity = 100; originalPool.Severity = 50;
            var state = fixture.Begin(); fixture.Hold(host, soul);
            Check.Equal(true, SpiritRebirthStateTests.Invoke(state, "TryEnterHost", soul, host), "original bound to host");
            var def = TorannMagicDefOf.TM_BloodShield;
            float oldCost = def.bloodCost;
            var priorSettings = TorannMagic.ModOptions.Settings.Instance;
            try
            {
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIAggressiveCasting = false };
                def.bloodCost = .2f;
                var ability = new MagicAbility(fixture.Magic) { Def = def };
                var pay = typeof(MagicAbility).GetMethod("PayForAttempt", BindingFlags.Instance | BindingFlags.NonPublic)!;
                pay.Invoke(ability, [fixture.Other]);
                Check.Equal(100f, hostPool.Severity, "stale or foreign host cannot pay");
                float expected = 100f - 100f * ability.ActualBloodCost;
                pay.Invoke(ability, [host]);
                Check.Equal(expected, hostPool.Severity, "actual physical blood price paid by host");
                Check.Equal(50f, originalPool.Severity, "soul blood remains unchanged");
            }
            finally { def.bloodCost = oldCost; TorannMagic.ModOptions.Settings.Instance = priorSettings; }
        }));
        yield return new("BloodMageResource.NativeCostAndPayment", Payment);
        yield return new("BloodMageResource.NativeMimicRefusal", Mimic);
    }

    sealed class Boundaries : IDisposable
    {
        readonly Assembly assembly = Assembly.Load("0Harmony");
        readonly object harmony;
        readonly string id = "rwom.blood.resource." + Guid.NewGuid();
        public Boundaries()
        {
            harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
        }
        public void Skip(MethodInfo method, string prefix = nameof(SkipBoundary))
        {
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(BloodMageResourceTests).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, new object?[] { method, hook, null, null, null });
        }
        public void Dispose() => harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, new object[] { id });
    }
    static bool SkipBoundary() => false;
    static bool DrawPosition(ref UnityEngine.Vector3 __result) { __result = UnityEngine.Vector3.zero; return false; }
    static (Pawn pawn, CompAbilityUserMagic magic, CompClassProgression progression) PawnFor(string id)
    {
        var p = PawnWithComps(true, false);
        var progression = new CompClassProgression { parent = p.pawn }; p.pawn.AllComps.Add(progression);
        progression.BeginNewProgression(Vocation.Mage);
        Check.Equal(true, progression.State.TryAddFoundation(id, DisciplineDomain.Magic), "seed saved foundation");
        return (p.pawn, p.magic, progression);
    }
    static void WithBlood(RuntimeFixture f, Action<HediffDef> run)
    {
        LoadBaseDefinitions(f);
        var oldGame = Current.Game;
        var oldDef = TorannMagicDefOf.TM_BloodHD;
        var defs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        using var boundaries = new Boundaries();
        try
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
            DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "BloodResourceCapacity" });
            // Only rendering and unrelated death/downing physiology are replaced.
            // Native ResolveClassSkills, HealthUtility, HediffMaker and Blood comp remain real.
            boundaries.Skip(typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!);
            boundaries.Skip(typeof(Pawn).GetProperty("DrawPos")!.GetMethod!, nameof(DrawPosition));
            foreach (var m in typeof(TM_MoteMaker).GetMethods().Where(m => m.Name == "ThrowBloodSquirt")) boundaries.Skip(m);
            var blood = new HediffDef { defName = "TM_BloodHD", hediffClass = typeof(HediffWithComps), initialSeverity = .5f,
                minSeverity = .001f, maxSeverity = 200f, comps = [new HediffCompProperties { compClass = typeof(HediffComp_Blood) }] };
            DefDatabase<HediffDef>.Clear(); foreach (var d in defs.Where(d => d.defName != blood.defName)) DefDatabase<HediffDef>.Add(d);
            DefDatabase<HediffDef>.Add(blood); TorannMagicDefOf.TM_BloodHD = blood;
            run(blood);
        }
        finally
        {
            Scribe.ForceStop(); Current.Game = oldGame; TorannMagicDefOf.TM_BloodHD = oldDef;
            DefDatabase<HediffDef>.Clear(); foreach (var d in defs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var d in capacities) DefDatabase<PawnCapacityDef>.Add(d);
        }
    }
    static void PrepareBody(Pawn pawn)
    {
        Set(pawn.def.race, "fleshType", new FleshTypeDef { isOrganic = true });
        pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "BloodResourceBody" } } };
    }
    static void Sustain(RuntimeFixture f) => WithBlood(f, blood =>
    {
        var p = PawnFor("rwom.bloodmage"); PrepareBody(p.pawn);
        var other = PawnFor("rwom.fire"); PrepareBody(other.pawn);
        other.magic.ResolveClassSkills();
        Check.Equal(0, other.pawn.health.hediffSet.hediffs.Count(h => h.def == blood), "unrelated discipline receives no blood");
        Check.Equal(true, ClassIdentity.IsBloodMage(p.pawn), "traitless foundation is blood mage");
        p.magic.ResolveClassSkills();
        var resource = p.pawn.health.hediffSet.hediffs.Single(h => h.def == blood);
        Check.Equal(true, resource is HediffWithComps hc && hc.comps.Any(c => c is HediffComp_Blood), "native blood comp inserted by native sustain");
        resource.Severity = 73.25f;
        p.magic.ResolveClassSkills(); p.magic.ResolveClassSkills();
        Check.Same(resource, p.pawn.health.hediffSet.hediffs.Single(h => h.def == blood), "repeated sustain keeps one resource");
        Check.Equal(73.25f, resource.Severity, "repeated sustain does not refill or drain");
        var state = p.progression.State;
        string path = Path.Combine(f.WorkingDirectory, "blood-resource-state.xml");
        try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref state, "state"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
        ClassProgressionState? loaded = null;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref loaded, "state"); Scribe.loader.FinalizeLoading(); } finally { Scribe.ForceStop(); }
        Set(p.progression, "state", loaded);
        p.magic.ResolveClassSkills(); p.magic.ResolveClassSkills();
        Check.Same(resource, p.pawn.health.hediffSet.hediffs.Single(h => h.def == blood), "loaded progression retains existing resource instance");
        Check.Equal(73.25f, resource.Severity, "loaded progression sustain preserves existing severity");
        Check.Equal(0, p.pawn.story.traits.allTraits.Count, "no legacy trait inserted");
    });
    static void Payment(RuntimeFixture f) => WithBlood(f, blood =>
    {
        var p = PawnFor("rwom.bloodmage"); PrepareBody(p.pawn); p.magic.ResolveClassSkills();
        var resource = p.pawn.health.hediffSet.hediffs.Single(h => h.def == blood); resource.Severity = 100f;
        var def = TorannMagicDefOf.TM_BloodShield; var gift = TorannMagicDefOf.TM_BloodGift;
        float oldCost = def.bloodCost, oldEfficiency = def.efficiencyReductionPercent, oldGift = gift.efficiencyReductionPercent;
        var settings = TorannMagic.ModOptions.Settings.Instance;
        try
        {
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIAggressiveCasting = false };
            def.bloodCost = .35f; def.efficiencyReductionPercent = .06f; gift.efficiencyReductionPercent = .06f;
            p.magic.MagicData.GetSkill_Efficiency(def).level = 2;
            p.magic.MagicData.MagicPowerSkill_BloodGift.Single(s => s.label == "TM_BloodGift_eff").level = 3;
            var ability = new MagicAbility(p.magic) { Def = def };
            float expected = .35f * ((1f - .06f * 2) * (1f - (.06f / 2f) * 3));
            Check.Equal(expected, ability.ActualBloodCost, "both native efficiencies apply once");
            ability.PostAbilityAttempt();
            Check.Equal(100f - 100f * expected, resource.Severity, "native successful callback deducts actual blood cost once");
            Check.Equal(1, p.pawn.health.hediffSet.hediffs.Count(h => h.def == blood), "payment preserves single resource");
        }
        finally { def.bloodCost = oldCost; def.efficiencyReductionPercent = oldEfficiency; gift.efficiencyReductionPercent = oldGift; TorannMagic.ModOptions.Settings.Instance = settings; }
    });
    sealed class LanguageScope : IDisposable
    {
        readonly LoadedLanguage previous = LanguageDatabase.activeLanguage, previousDefault = LanguageDatabase.defaultLanguage;
        public LanguageScope(RuntimeFixture f)
        {
            var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            const string key = "TM_CannotMimicBloodMage";
            string value = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository, "Languages/English/Keyed/TM_English.xml")).Root!.Element(key)!.Value;
            language.keyedReplacements = new Dictionary<string, LoadedLanguage.KeyedReplacement> { [key] = new() { key = key, value = value } };
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
        }
        public void Dispose() { LanguageDatabase.activeLanguage = previous; LanguageDatabase.defaultLanguage = previousDefault; }
    }
    static void Mimic(RuntimeFixture f)
    {
        LoadBaseDefinitions(f); RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(MessageTypeDefOf));
        using var language = new LanguageScope(f);
        using var boundary = new Boundaries();
        foreach (var m in typeof(Messages).GetMethods().Where(m => m.Name == "Message")) boundary.Skip(m);
        var caster = PawnWithComps(true, false);
        var modern = PawnFor("rwom.bloodmage"); var legacy = PawnWithComps(true, false);
        legacy.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.BloodMage));
        foreach (var p in new[] { (modern.pawn, modern.magic), (legacy.pawn, legacy.magic) })
        {
            foreach (var power in p.magic.MagicData.MagicPowersBM) power.learned = true;
            Check.Equal<TMAbilityDef?>(null, TM_Calc.GetCopiedMagicAbility(p.pawn, caster.pawn), "native Mimic explicitly refuses learned blood mage spells");
        }
    }
}


