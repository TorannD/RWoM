using System.Reflection;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.AutoCast;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;

namespace RWoM.RegressionTests;

internal static class SpiritRebirthCastingTests
{
    public static IEnumerable<TestCase> Cases() => CastSafetyTests.SpiritRebirthCases();
}

// Reuse the actual engine map/verb fixture and production safety patches.
internal static partial class CastSafetyTests
{
    public static IEnumerable<TestCase> SpiritRebirthCases()
    {
        yield return new("SpiritRebirthCasting.ExactNativeFamilyAndInactiveForms", f => World(f, w =>
        {
            using var definitions = new SpiritCastingDefinitions(f);
            var oldTrait = new Trait(TorannMagicDefOf.TM_Possessor);
            w.Caster.story.traits.allTraits.Add(oldTrait);
            // Former-class MagicData registered before the first gate call:
            // GetCompAbilityUserMagic negatively caches pawns without the comp.
            var formerMagic = new CompAbilityUserMagic { parent = w.Caster };
            w.Caster.AllComps.Add(formerMagic);
            var formerData = new MagicData();
            Set(formerMagic, "magicData", formerData);
            var learnedFormer = new MagicPower(new List<AbilityUser.AbilityDef> { w.Def }, false) { learned = false };
            formerData.magicPowerCustom = [learnedFormer];
            Check.Equal(true, SpiritRebirthCasting.Allows(w.Caster, w.Def), "legacy Spirit trait alone does not opt in");
            var state = MarkSpirit(w.Caster);
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, w.Def), "ethereal former-class ability denied");
            Check.Equal(true, SpiritRebirthCasting.Allows(w.Caster, TorannMagicDefOf.TM_SpiritPossession), "native entry ability allowed");
            Check.Equal(true, SpiritRebirthCasting.Allows(w.Caster, DefDatabase<TMAbilityDef>.GetNamed("TM_DistortSpirit_III")), "explicit native ranked ability allowed");
            var spoof = Blank<TMAbilityDef>(); spoof.defName = TorannMagicDefOf.TM_SpiritDrain.defName;
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, spoof), "matching defName alone cannot grant entry");
            var child = f.AddAbility("TM_SpiritCastingRegressionChild");
            var parent = TorannMagicDefOf.TM_SpiritDrain; var oldChildren = parent.childAbilities;
            try
            {
                parent.childAbilities = [child]; child.childAbilities = [parent];
                Check.Equal(true, SpiritRebirthCasting.Allows(w.Caster, child), "actual child traversed safely through cycle");
                Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, w.Def), "child cycle does not grant unrelated abilities");
            }
            finally { parent.childAbilities = oldChildren; }
            // A reborn spirit keeps its former class: a power it had already
            // learned stays castable, read-only from the retained MagicData;
            // unlearned powers still need the grimoire (classed-spirit report).
            learnedFormer.learned = true;
            Check.Equal(true, SpiritRebirthCasting.Allows(w.Caster, w.Def), "learned former-class ability stays castable");
            learnedFormer.learned = false;
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, w.Def), "unlearned former-class ability still needs the grimoire");
            foreach (var form in new[] { SpiritRebirthForm.Dormant, SpiritRebirthForm.Possessed, (SpiritRebirthForm)937 })
            {
                Set(state, "form", form);
                Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, TorannMagicDefOf.TM_SpiritDrain), "inactive/unsupported form cannot cast Spirit abilities");
                Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, w.Def), "inactive/unsupported form cannot cast former abilities");
            }
            Set(state, "form", SpiritRebirthForm.Ethereal); Set(state, "version", 937);
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, TorannMagicDefOf.TM_SpiritDrain), "future marker version fails closed");
            Set(state, "version", SpiritRebirthState.CurrentVersion); Set(state, "original", w.Target);
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, TorannMagicDefOf.TM_SpiritDrain), "foreign marker identity fails closed");
        }));
        foreach (bool might in new[] { false, true })
            yield return new("SpiritRebirthCasting.NativeAdmissionAndQueuedCast." + (might ? "Might" : "Magic"), f => World(f, w =>
            {
                using var definitions = new SpiritCastingDefinitions(f);
                var magic = new CompAbilityUserMagic { parent = w.Caster }; var warrior = new CompAbilityUserMight { parent = w.Caster };
                w.Caster.AllComps.Add(magic); w.Caster.AllComps.Add(warrior);
                PawnAbility ability = might ? new MightAbility(warrior) { Def = w.Def } : new MagicAbility(magic) { Def = w.Def };
                w.Verb.Ability = ability; Set(ability, "verb", w.Verb); ability.CooldownTicksLeft = 317;
                // The ability is deliberately not inserted into permanent powers:
                // item/summoned-item runtimes must obey the same form restriction.
                MarkSpirit(w.Caster);
                foreach (var context in new[] { AbilityContext.Player, AbilityContext.AI })
                {
                    Check.Equal(false, ability.CanCastPowerCheck(context, out _), "real magic/might preflight denies former ability");
                    Check.Equal(false, ability.TryCastAbility(context, w.Target), "direct ability admission denies former ability");
                    Check.Equal(true, ability.UseAbility(context, w.Target) == null, "manual and AI job admission denied");
                }
                Check.Equal(false, TM_AutocastCombat.AllowsAutomaticCast(w.Caster, w.Def, w.Target), "automatic selection policy denies former ability");
                Check.Equal(false, w.Verb.TryStartCastOn(w.Target), "queued direct start denied");
                Check.Equal(false, w.Verb.PreCastShotCheck(w.Target, LocalTargetInfo.Invalid), "pre-shot boundary denied");
                Check.Equal(false, w.Verb.TryLaunchProjectileCheck(Blank<ThingDef>(), w.Target), "queued projectile denied");
                w.Verb.state = VerbState.Bursting;
                typeof(Verb).GetMethod("TryCastNextBurstShot", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(w.Verb, null);
                Check.Equal(VerbState.Idle, w.Verb.state, "already queued burst stopped");
                Check.Equal(0, w.Verb.Shots, "no effect occurs through any rejected path");
                Check.Equal(317, ability.CooldownTicksLeft, "rejection preserves actual cooldown");
                Check.Equal(0, SpecialCostsSeen, "rejected execution charges no special resource");
            }));
        yield return new("SpiritRebirthCasting.NativeSpiritCastAndUnmarkedLegacyRemainUsable", f => World(f, w =>
        {
            using var definitions = new SpiritCastingDefinitions(f);
            w.Job.count = 1;
            Check.Equal(true, w.Verb.TryStartCastOn(w.Target), "unmarked former ability retains native execution");
            Check.Equal(1, w.Verb.Shots, "legacy shot executed");
            MarkSpirit(w.Caster); w.Verb.Ability.Def = TorannMagicDefOf.TM_SpiritDrain;
            Check.Equal(true, w.Verb.TryStartCastOn(w.Target), "marked native Spirit ability retains native execution");
            Check.Equal(2, w.Verb.Shots, "Spirit shot executed");
        }));
        yield return new("SpiritRebirthCasting.NativeSavedMarkerKeepsDormantOwnership", f => World(f, w =>
        {
            using var definitions = new SpiritCastingDefinitions(f);
            var state = MarkSpirit(w.Caster); Set(state, "form", SpiritRebirthForm.Dormant);
            w.Caster.thingIDNumber = 94137;
            string file = Path.Combine(f.WorkingDirectory, "spirit-casting-state.xml");
            try { Scribe.saver.InitSaving(file, "regression"); Scribe_Deep.Look(ref state, "spirit"); Scribe.saver.FinalizeSaving(); } finally { Scribe.ForceStop(); }
            SpiritRebirthState? loaded = null;
            try
            {
                Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref loaded, "spirit");
                var directory = (LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Scribe.loader.crossRefs)!;
                directory.RegisterLoaded(w.Caster); Scribe.loader.FinalizeLoading();
            }
            finally { Scribe.ForceStop(); }
            Set(ClassIdentity.Component(w.Caster), "spiritRebirth", loaded!);
            Check.Equal(true, SpiritRebirthState.TryGet(w.Caster, out var actual), "native Scribe resolves actual original reference");
            Check.Equal(SpiritRebirthForm.Dormant, actual.Form, "load does not promote dormant form");
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, w.Def), "loaded marker denies former class");
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, TorannMagicDefOf.TM_SpiritDrain), "loaded dormant marker denies Spirit abilities");
            Set(loaded!, "form", SpiritRebirthForm.Ethereal);
            Check.Equal(true, SpiritRebirthCasting.Allows(w.Caster, TorannMagicDefOf.TM_SpiritDrain), "same saved owner can use Spirit family when explicitly ethereal");
            Check.Equal(false, SpiritRebirthCasting.Allows(w.Caster, w.Def), "same saved owner still cannot cast former class");
        }));
    }

    static SpiritRebirthState MarkSpirit(Pawn pawn)
    {
        var comp = new CompClassProgression { parent = pawn }; pawn.AllComps.Add(comp);
        if (pawn.Spawned)
        {
            pawn.Map.spawnedThings ??= new ThingOwner<Thing>(pawn.Map);
            pawn.def.stackLimit = 1;
            Check.Equal(true, pawn.Map.spawnedThings.TryAdd(pawn, false), "native map owns marked cast original");
        }
        pawn.needs ??= new Pawn_NeedsTracker(pawn);
        pawn.needs.AllNeeds.Add(new Need_Spirit(pawn) { def = TorannMagicDefOf.TM_SpiritND, CurLevel = 65f });
        typeof(CompClassProgression).GetMethod("BeginSpiritRebirth", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(comp, null);
        return Check.NotNull(comp.SpiritRebirth, "test explicitly opts original pawn in");
    }

    sealed class SpiritCastingDefinitions : IDisposable
    {
        readonly NeedDef priorSpiritNeed = TorannMagicDefOf.TM_SpiritND;
        readonly TM_CustomClassDef[] previous = DefDatabase<TM_CustomClassDef>.AllDefsListForReading.ToArray();
        public SpiritCastingDefinitions(RuntimeFixture f)
        {
            TorannMagicDefOf.TM_SpiritND = new NeedDef { defName = "SpiritCastingEnergy", needClass = typeof(Need_Spirit) };
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var xml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomClassDef.xml"));
            var native = xml.Descendants("li").Single(e => (string?)e.Element("classTrait") == "TM_Possessor");
            var spec = new TM_CustomClass { classTrait = TorannMagicDefOf.TM_Possessor, isMage = true,
                classMageAbilities = native.Element("classMageAbilities")!.Elements().Select(e => DefDatabase<TMAbilityDef>.GetNamedSilentFail(e.Value) ?? f.AddAbility(e.Value)).ToList() };
            DefDatabase<TM_CustomClassDef>.Clear(); DefDatabase<TM_CustomClassDef>.Add(new TM_CustomClassDef { defName = "TM_CustomClasses", customClasses = [spec] });
            LanguageDatabase.activeLanguage.keyedReplacements["TM_SpiritRebirthCastUnavailable"] = new LoadedLanguage.KeyedReplacement { key = "TM_SpiritRebirthCastUnavailable", value = "Unavailable in current spirit form" };
        }
        public void Dispose() { TorannMagicDefOf.TM_SpiritND = priorSpiritNeed; DefDatabase<TM_CustomClassDef>.Clear(); foreach (var definition in previous) DefDatabase<TM_CustomClassDef>.Add(definition); }
    }
}
