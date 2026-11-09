using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class EmpathLifecycleTests
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("EmpathLifecycle.MissingOnlyPassivePreservesActualPassionsAndLinks", Passive);
        yield return new("EmpathLifecycle.NativeSavedSourcesAndNeedsSurviveRestore", Save);
        yield return new("EmpathLifecycle.SelectedInciteValidatesActualSourceSkill", Selection);
        yield return new("EmpathLifecycle.NativeSelectedAndRandomInciteTransferOnce", NativeIncite);
        yield return new("EmpathLifecycle.NativeSymbiosisCreatesAndRemovesReciprocalPair", NativeSymbiosis);
        yield return new("EmpathLifecycle.InvalidSymbiosisSourceAndStaleRemovalAreSafe", Removal);
    }
    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
        readonly HediffDef[] defs = DefDatabase<HediffDef>.AllDefsListForReading.ToArray();
        readonly PawnCapacityDef[] capacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        readonly TM_CustomPowerDef[] customs = DefDatabase<TM_CustomPowerDef>.AllDefsListForReading.ToArray();
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        readonly HediffDef oldEmpath = TorannMagicDefOf.TM_EmpathHD, oldHost = TorannMagicDefOf.TM_SymbiosisHD, oldBody = TorannMagicDefOf.TM_OutOfBodyHD;
        readonly object harmony; readonly string patchId = "rwom.empath.lifecycle." + Guid.NewGuid();
        public readonly Pawn Caster, Host, Other;
        public readonly CompAbilityUserMagic Magic;
        public readonly HediffWithComps BodyHD, HostHD, HarvestHD;
        public readonly SkillRecord SourceSkill, HostSkill;
        public readonly Need_Mood Mood;
        public Fixture(RuntimeFixture r)
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            LoadBaseDefinitions(r);
            if (DefDatabase<PawnCapacityDef>.DefCount == 0) DefDatabase<PawnCapacityDef>.Add(new PawnCapacityDef { defName = "EmpathLifecycleCapacity" });
            var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/CustomDefs/TM_CustomPowerDef.xml"));
            DefDatabase<TM_CustomPowerDef>.Clear();
            foreach (var name in new[] { "TM_Empathy", "TM_MindKiller", "TM_SuppressiveAura", "TM_HarvestPassion", "TM_Symbiosis" })
            {
                var node = xml.Root!.Elements().Single(e => (string?)e.Element("defName") == name).Element("customPower")!;
                DefDatabase<TM_CustomPowerDef>.Add(new TM_CustomPowerDef { defName = name, customPower = new TM_CustomPower { forMage = true,
                    abilityDefs = node.Element("abilityDefs")!.Elements().Select(e => (AbilityUser.AbilityDef)DefDatabase<TMAbilityDef>.GetNamed(e.Value)).ToList(),
                    skills = node.Element("skills")!.Elements().Select(e => new TM_CustomSkill { label = e.Element("label")!.Value,
                        description = e.Element("description")!.Value, levelMax = int.Parse(e.Element("levelMax")!.Value) }).ToList() } });
            }
            TM_Data.ResetDefinitionCaches();
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager()); Set(Current.Game, "uniqueIDsManager", new UniqueIDsManager()); Set(Current.Game, "maps", new List<Map>());
            var p = PawnWithComps(true, false); Caster = p.pawn; Magic = p.magic; Caster.thingIDNumber = 78201;
            Host = PawnWithComps(false, false).pawn; Host.thingIDNumber = 78202; Other = PawnWithComps(false, false).pawn; Other.thingIDNumber = 78203;
            foreach (var pawn in new[] { Caster, Host, Other })
            {
                pawn.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "EmpathLifecycleBody" } } };
                pawn.skills = new Pawn_SkillTracker(pawn);
            }
            var skill = new SkillDef { defName = "EmpathLifecycleSkill" };
            SourceSkill = new SkillRecord(Caster) { def = skill, passion = Passion.Major };
            HostSkill = new SkillRecord(Host) { def = skill, passion = Passion.None };
            Caster.skills.skills.Add(SourceSkill); Host.skills.skills.Add(HostSkill);
            Mood = new Need_Mood(Caster); Caster.needs.AllNeeds.Add(Mood); Mood.CurLevel = .63f;
            var progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(progression);
            progression.BeginNewProgression(Vocation.Mage); progression.State.TryAddFoundation("rwom.empath", DisciplineDomain.Magic);
            DefDatabase<ClassDisciplineDef>.Clear(); DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "EmpathLifecycle", profileId = "rwom.empath", domain = DisciplineDomain.Magic });
            TorannMagicDefOf.TM_EmpathHD = Definition("EmpathLifecyclePassive", typeof(HediffComp_Empath));
            TorannMagicDefOf.TM_SymbiosisHD = Definition("EmpathLifecycleHost", typeof(HediffComp_SymbiosisHost));
            TorannMagicDefOf.TM_OutOfBodyHD = Definition("EmpathLifecycleBodyLink", typeof(HediffComp_SymbiosisCaster));
            BodyHD = Add(Caster, TorannMagicDefOf.TM_OutOfBodyHD); HostHD = Add(Host, TorannMagicDefOf.TM_SymbiosisHD);
            BodyHD.TryGetComp<HediffComp_SymbiosisCaster>().symbiosisHost = Host; HostHD.TryGetComp<HediffComp_SymbiosisHost>().symbiote = Caster;
            HarvestHD = Add(Host, Definition("EmpathLifecycleHarvest", typeof(HediffComp_Empath).Assembly.GetType("TorannMagic.HediffComp_HarvestPassion")!));
            Set(HarvestHD.comps.Single(), "caster", Caster);
            var assembly = Assembly.Load("0Harmony"); harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, patchId)!;
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(EmpathLifecycleTests).GetMethod(nameof(SkipHealthBoundary), BindingFlags.Static | BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod("CheckForStateChange")!, hook, null, null, null]);
        }
        static HediffDef Definition(string name, Type comp)
        {
            var def = new HediffDef { defName = name, hediffClass = typeof(HediffWithComps), maxSeverity = 100f, comps = [new HediffCompProperties { compClass = comp }] };
            DefDatabase<HediffDef>.Add(def); return def;
        }
        static HediffWithComps Add(Pawn pawn, HediffDef def)
        { var hd = (HediffWithComps)HediffMaker.MakeHediff(def, pawn); pawn.health.hediffSet.hediffs.Add(hd); return hd; }
        public MagicPower Empathy => EmpathOwners.Resolve(Magic.MagicData)!.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Empathy));
        public void Dispose()
        {
            harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [patchId]); Current.Game = previous;
            System.Globalization.CultureInfo.CurrentCulture = culture;
            TorannMagicDefOf.TM_EmpathHD = oldEmpath; TorannMagicDefOf.TM_SymbiosisHD = oldHost; TorannMagicDefOf.TM_OutOfBodyHD = oldBody;
            DefDatabase<HediffDef>.Clear(); foreach (var d in defs) DefDatabase<HediffDef>.Add(d);
            DefDatabase<PawnCapacityDef>.Clear(); foreach (var d in capacities) DefDatabase<PawnCapacityDef>.Add(d);
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var d in profiles) DefDatabase<ClassDisciplineDef>.Add(d);
            DefDatabase<TM_CustomPowerDef>.Clear(); foreach (var d in customs) DefDatabase<TM_CustomPowerDef>.Add(d); TM_Data.ResetDefinitionCaches();
        }
    }
    static bool SkipHealthBoundary() => false;
    static bool DrawPosition(ref UnityEngine.Vector3 __result) { __result = UnityEngine.Vector3.zero; return false; }
    sealed class IncitePresentation : IDisposable
    {
        readonly Type type = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony")!;
        readonly string id = "rwom.empath.incite." + Guid.NewGuid(); readonly object harmony;
        public IncitePresentation()
        {
            harmony = Activator.CreateInstance(type, id)!;
            Patch(typeof(Pawn).GetProperty("DrawPos")!.GetMethod!, nameof(DrawPosition));
            foreach (var method in typeof(TM_MoteMaker).GetMethods().Where(m => m.Name == "ThrowGenericMote")) Patch(method, nameof(SkipHealthBoundary));
            foreach (var method in typeof(MoteMaker).GetMethods().Where(m => m.Name == "ThrowText")) Patch(method, nameof(SkipHealthBoundary));
            foreach (var method in typeof(Messages).GetMethods().Where(m => m.Name == "Message")) Patch(method, nameof(SkipHealthBoundary));
        }
        void Patch(MethodInfo method, string name)
        { var hook = Activator.CreateInstance(type.Assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(EmpathLifecycleTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!)!; type.GetMethod("Patch")!.Invoke(harmony, [method, hook, null, null, null]); }
        public void Dispose() => type.GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
    }
    static void NativeIncite(RuntimeFixture r)
    {
        foreach (bool selected in new[] { true, false })
        {
            using var f = new Fixture(r); using var presentation = new IncitePresentation(); using var language = new SpellItemEffectTests.LanguageScope(r.Repository);
            using var payment = new NativePayment(r, f, TorannMagicDefOf.TM_IncitePassion);
            f.Magic.incitePassionSkill = selected ? f.SourceSkill : null;
            var ability = new MagicAbility(f.Magic) { Def = TorannMagicDefOf.TM_IncitePassion };
            var verb = new Verb_IncitePassion { Ability = ability }; Set(verb, "caster", f.Caster); Set(verb, "currentTarget", new LocalTargetInfo(f.Host));
            var cast = typeof(Verb_IncitePassion).GetMethod("TryCastShot", Fields)!;
            Check.Equal(true, (bool)cast.Invoke(verb, null)!, "actual successful transfer reports success through native PostCastShot");
            Check.Equal(Passion.None, f.SourceSkill.passion, "native Incite spends actual source passion");
            Check.Equal(Passion.Major, f.HostSkill.passion, "native Incite transfers actual target passion");
            Check.Equal(true, f.Magic.incitePassionSkill == null, "native selection clears after success");
            payment.CompleteAndAssert(ability);
            Check.Equal(false, (bool)cast.Invoke(verb, null)!, "no-improvement repeat reports failure"); EmpathLifecycle.Restore(f.Magic);
            Check.Equal(Passion.None, f.SourceSkill.passion, "repeat/restore does not recreate source passion");
            Check.Equal(Passion.Major, f.HostSkill.passion, "repeat/restore preserves actual transferred passion");
        }
    }
    // The verb and native job-finish payment callback are exercised separately; only presentation and unrelated physiology are suppressed.
    sealed class NativePayment : IDisposable
    {
        readonly TMAbilityDef def;
        readonly Fixture fixture;
        readonly NeedDef oldNeed, oldMoodDef;
        readonly float oldCost, oldXp, oldRecharge;
        readonly TorannMagic.ModOptions.Settings settings = TorannMagic.ModOptions.Settings.Instance;
        public NativePayment(RuntimeFixture r, Fixture f, TMAbilityDef ability)
        {
            fixture = f; def = ability; oldNeed = def.requiredNeed; oldCost = def.needCost; oldXp = def.needXPFactor; oldMoodDef = f.Mood.def; oldRecharge = def.MainVerb.SecondsToRecharge;
            var xml = XDocument.Load(Path.Combine(r.Repository, "v1.7/Defs/AbilityDefs/Abilities_TMagic_Empath.xml")).Root!.Elements().Single(e => (string?)e.Element("defName") == def.defName);
            Check.Equal("Mood", xml.Element("requiredNeed")!.Value, "shipped native need");
            f.Mood.def ??= new NeedDef { defName = "Mood" };
            def.requiredNeed = f.Mood.def; def.needCost = float.Parse(xml.Element("needCost")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            def.needXPFactor = float.Parse(xml.Element("needXPFactor")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            def.MainVerb.SecondsToRecharge = float.Parse(xml.Element("MainVerb")!.Element("SecondsToRecharge")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            f.Mood.CurLevel = 1f;
            TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIAggressiveCasting = false };
        }
        public void CompleteAndAssert(MagicAbility ability)
        {
            float expected = fixture.Mood.CurLevel - MagicAbility.ActualNeedCost(def, fixture.Magic);
            ability.PostAbilityAttempt();
            Check.Equal(expected, fixture.Mood.CurLevel, "native callback consumes required Mood exactly once");
            Check.Equal(UnityEngine.Mathf.RoundToInt(ability.MaxCastingTicks * fixture.Magic.coolDown), ability.CooldownTicksLeft, "native callback sets cooldown");
            Check.Equal(true, ability.CooldownTicksLeft > 0, "shipped cooldown is active");
        }
        public void Dispose() { def.requiredNeed = oldNeed; def.needCost = oldCost; def.needXPFactor = oldXp; fixture.Mood.def = oldMoodDef; def.MainVerb.SecondsToRecharge = oldRecharge; TorannMagic.ModOptions.Settings.Instance = settings; }
    }
    static void NativeSymbiosis(RuntimeFixture r)
    {
        using var f = new Fixture(r); using var presentation = new IncitePresentation(); using var language = new SpellItemEffectTests.LanguageScope(r.Repository);
        using var payment = new NativePayment(r, f, TorannMagicDefOf.TM_Symbiosis);
        f.Caster.health.hediffSet.hediffs.Remove(f.BodyHD); f.Host.health.hediffSet.hediffs.Remove(f.HostHD);
        var ability = new MagicAbility(f.Magic) { Def = TorannMagicDefOf.TM_Symbiosis };
        var verb = new Verb_Symbiosis { Ability = ability }; Set(verb, "caster", f.Caster); Set(verb, "currentTarget", new LocalTargetInfo(f.Host));
        var cast = typeof(Verb_Symbiosis).GetMethod("TryCastShot", Fields)!;
        Set(verb, "currentTarget", new LocalTargetInfo(f.Caster));
        Check.Equal(false, (bool)cast.Invoke(verb, null)!, "self link is rejected");
        Check.Equal(false, f.Caster.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SymbiosisHD), "self rejection creates no host effect");
        Set(verb, "currentTarget", new LocalTargetInfo(f.Host));
        Check.Equal(true, (bool)cast.Invoke(verb, null)!, "native reciprocal creation reports success");
        var body = f.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD);
        var host = f.Host.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD);
        Check.Same(f.Host, body.TryGetComp<HediffComp_SymbiosisCaster>().symbiosisHost, "native cast links caster to actual host");
        Check.Same(f.Caster, host.TryGetComp<HediffComp_SymbiosisHost>().symbiote, "native cast links host to actual caster");
        payment.CompleteAndAssert(ability);
        Check.Equal(false, (bool)cast.Invoke(verb, null)!, "already-linked target reports failure");
        Set(verb, "currentTarget", new LocalTargetInfo(f.Other));
        Check.Equal(false, (bool)cast.Invoke(verb, null)!, "bound caster cannot overwrite its host link");
        Check.Equal(false, f.Other.health.hediffSet.HasHediff(TorannMagicDefOf.TM_SymbiosisHD), "rejected second host has no effect");
        Check.Same(f.Host, body.TryGetComp<HediffComp_SymbiosisCaster>().symbiosisHost, "rejected second cast preserves reciprocal host reference");
        Set(verb, "currentTarget", LocalTargetInfo.Invalid);
        Check.Equal(false, (bool)cast.Invoke(verb, null)!, "invalid target reports failure");
        EmpathLifecycle.Restore(f.Magic);
        Check.Same(body, f.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD), "restore preserves native caster effect");
        Check.Same(host, f.Host.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD), "restore preserves native host effect");
        TM_Action.RemoveSymbiosisCommand(f.Caster);
        Check.Equal(false, f.Caster.health.hediffSet.hediffs.Contains(body), "actual caster effect removed");
        Check.Equal(false, f.Host.health.hediffSet.hediffs.Contains(host), "actual reciprocal host effect removed");
    }
    static void Passive(RuntimeFixture r)
    {
        using var f = new Fixture(r);
        EmpathLifecycle.Restore(f.Magic); Check.Equal(false, f.Caster.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmpathHD), "unlearned passive not invented");
        f.Empathy.learned = true; f.Magic.ResolveClassSkills();
        var hd = f.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmpathHD); Check.Equal(true, hd != null, "native maintenance creates missing passive");
        hd.Severity = .44f; f.Magic.incitePassionSkill = f.SourceSkill;
        EmpathLifecycle.Restore(f.Magic); EmpathLifecycle.Restore(f.Magic);
        Check.Same(hd, f.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmpathHD), "exact passive preserved"); Check.Equal(.44f, hd.Severity, "live severity not reinitialized");
        Check.Equal(.63f, f.Mood.CurLevel, "restore does not share/drain mood");
        Check.Equal(Passion.Major, f.SourceSkill.passion, "source passion retained"); Check.Equal(Passion.None, f.HostSkill.passion, "target passion retained");
        Check.Same(f.SourceSkill, f.Magic.incitePassionSkill, "live selection retained");
        Check.Same(f.BodyHD, f.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_OutOfBodyHD), "caster link retained");
        Check.Same(f.HostHD, f.Host.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SymbiosisHD), "host link retained");
        DefDatabase<ClassDisciplineDef>.Clear(); EmpathLifecycle.Restore(f.Magic); Check.Same(hd, f.Caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EmpathHD), "missing profile preserves existing effect");
        f.Caster.health.hediffSet.hediffs.Remove(hd); EmpathLifecycle.Restore(f.Magic); Check.Equal(false, f.Caster.health.hediffSet.HasHediff(TorannMagicDefOf.TM_EmpathHD), "missing profile adds nothing");
    }
    public sealed class Envelope : IExposable
    {
        public static HediffComp CasterLink = null!, HostLink = null!, Harvest = null!;
        public void ExposeData()
        { Scribe.EnterNode("casterLink"); CasterLink.CompExposeData(); Scribe.ExitNode(); Scribe.EnterNode("hostLink"); HostLink.CompExposeData(); Scribe.ExitNode(); Scribe.EnterNode("harvest"); Harvest.CompExposeData(); Scribe.ExitNode(); }
    }
    static void Save(RuntimeFixture r)
    {
        using var f = new Fixture(r); f.Empathy.learned = true;
        var body = f.BodyHD.TryGetComp<HediffComp_SymbiosisCaster>(); var host = f.HostHD.TryGetComp<HediffComp_SymbiosisHost>(); host.lastDamageTick = 113;
        Envelope.CasterLink = body; Envelope.HostLink = host; Envelope.Harvest = f.HarvestHD.comps.Single();
        var envelope = new Envelope(); string path = Path.Combine(r.WorkingDirectory, "empath-native-sources.xml");
        try
        {
            try { Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref envelope, "native"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            body.symbiosisHost = null; host.symbiote = null; Set(Envelope.Harvest, "caster", null);
            try
            {
                Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref envelope, "native");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", Fields)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(f.Caster); directory.RegisterLoaded(f.Host); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            RuntimeLog.AssertClean(); EmpathLifecycle.Restore(f.Magic);
            Check.Same(f.Host, body.symbiosisHost, "actual native host source saved"); Check.Same(f.Caster, host.symbiote, "actual reciprocal caster saved");
            Check.Same(f.Caster, Envelope.Harvest.GetType().GetField("caster")!.GetValue(Envelope.Harvest)!, "actual harvest channel caster saved");
            Check.Equal(113, host.lastDamageTick, "restore preserves live damage recursion guard"); Check.Equal(.63f, f.Mood.CurLevel, "restore does not replay linked need updates");
            Check.Equal(Passion.Major, f.SourceSkill.passion, "restore does not replay harvest");
        }
        finally { Envelope.CasterLink = null!; Envelope.HostLink = null!; Envelope.Harvest = null!; }
    }
    static void Selection(RuntimeFixture r)
    {
        using var f = new Fixture(r); var verb = new Verb_IncitePassion();
        f.Magic.incitePassionSkill = f.SourceSkill;
        Check.Equal(true, verb.CheckAnyPassions(f.Caster, f.Host, f.Magic), "actual selected source eligible");
        f.Magic.incitePassionSkill = new SkillRecord(f.Other) { def = f.SourceSkill.def, passion = Passion.Major };
        Check.Equal(false, verb.CheckAnyPassions(f.Caster, f.Host, f.Magic), "foreign selected skill rejected");
        f.Magic.incitePassionSkill = null;
        Check.Equal(true, verb.CheckAnyPassions(f.Caster, f.Host, f.Magic), "random source candidates available");
        Check.Same(f.SourceSkill, verb.validSkillPassions.Single(), "random candidates are actual source records");
        Check.Equal(false, verb.CheckAnyPassions(f.Caster, f.Caster, f.Magic), "self transfer cannot erase passion");
    }
    static void Removal(RuntimeFixture r)
    {
        using var f = new Fixture(r); var host = f.HostHD.TryGetComp<HediffComp_SymbiosisHost>();
        host.symbiote = null; float adjustment = 0f; host.CompPostTick(ref adjustment);
        Check.Equal(true, host.CompShouldRemove, "native orphan marks for removal without dereference");
        var caster = f.BodyHD.TryGetComp<HediffComp_SymbiosisCaster>();
        caster.symbiosisHost = f.Other;
        host.symbiote = f.Caster;
        host.RemoveSymbioteHediff();
        Check.Equal(true, f.Caster.health.hediffSet.hediffs.Contains(f.BodyHD), "obsolete partner cannot remove newer caster link");
        caster.symbiosisHost = f.Host;
        host.symbiote = f.Other;
        caster.RemoveHostHediff();
        Check.Equal(true, f.Host.health.hediffSet.hediffs.Contains(f.HostHD), "obsolete caster cannot remove newer recipient link");
        TM_Action.RemoveSymbiosisCommand(f.Caster);
        Check.Equal(true, f.Host.health.hediffSet.hediffs.Contains(f.HostHD), "stale action preserves foreign host effect");
        Check.Equal(false, f.Caster.health.hediffSet.hediffs.Contains(f.BodyHD), "stale own body link safely released");
        TM_Action.RemoveSymbiosisCommand(f.Caster); TM_Action.RemoveSymbiosisCommand(null);
    }
}
