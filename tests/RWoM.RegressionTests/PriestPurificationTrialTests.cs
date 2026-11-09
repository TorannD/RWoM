using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class PriestPurificationTrialTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("PriestPurificationTrial.CompleteScarOnlyAndIdempotent", Completion);
        yield return new("PriestPurificationTrial.OwnedAdeptSpellOnly", Eligibility);
        yield return new("PriestPurificationTrial.NativeHookAttaches", Hook);
        yield return new("Purify.HealsPermanentInjuriesButNotNaturalWounds", HealsPermanentInjuries);
    }

    // Player report: Purify removed toxic buildup but did nothing to scars or a lost
    // arm. Vanilla GetInjuredParts only yields bleeding parts, so the old loop never
    // saw permanent injuries. The spell must heal permanent injuries (scars) per its
    // description and leave fresh naturally-healing wounds to normal medicine.
    static void HealsPermanentInjuries(RuntimeFixture f)
    {
        using var w = new Fixture(f);
        var savedSettings = TorannMagic.ModOptions.Settings.Instance;
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
        var savedCategories = DefDatabase<HediffCategoryList>.AllDefsListForReading.ToArray();
        DefDatabase<HediffCategoryList>.Clear();
        DefDatabase<HediffCategoryList>.Add(new HediffCategoryList
        {
            defName = "TM_Category_Hediffs",
            ailments = new List<TorannMagic.TMDefs.TM_CategoryHediff>(),
            addictions = new List<TorannMagic.TMDefs.TM_CategoryHediff>(),
            diseases = new List<TorannMagic.TMDefs.TM_CategoryHediff>(),
            mechanites = new List<TorannMagic.TMDefs.TM_CategoryHediff>()
        });
        w.Magic.MagicData.MagicPowerSkill_Purify[0].level = 3;  // 2 + 2x3 heal per use covers severity 8 in one cast
        w.Magic.MagicData.MagicPowerSkill_Purify[2].level = 10; // budget 1 + 0.4x10 = 5 injuries per cast
        // TM_Calc.GetSkillPowerLevel resolves through IsMagicUser, which requires the
        // mana need to be present on the caster; the need ctor reads adult age.
        Set(w.Caster.def.race, "lifeStageAges", new List<LifeStageAge> { new LifeStageAge { minAge = 0f, def = new LifeStageDef { defName = "PurifyCasterStage", healthScaleFactor = 1f, bodySizeFactor = 1f, developmentalStage = DevelopmentalStage.Adult } } });
        w.Caster.ageTracker = new Pawn_AgeTracker(w.Caster);
        Set(w.Caster.ageTracker, "cachedLifeStageIndex", 0);
        var mana = new Need_Mana(w.Caster);
        var allNeeds = (List<Need>)typeof(Pawn_NeedsTracker).GetField("needs", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(w.Caster.needs)!;
        allNeeds.Add(mana);

        // Injury healing refreshes the hediff set cache, which needs a body model
        // plus an initialized capacities cache (CoreReturnWorld pattern).
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(BodyPartTagDefOf));
        RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(PawnCapacityDefOf));
        var savedCapacities = DefDatabase<PawnCapacityDef>.AllDefsListForReading.ToArray();
        var moving = new PawnCapacityDef { defName = "PurifyMoving", minForCapable = .1f };
        var consciousness = new PawnCapacityDef { defName = "PurifyConsciousness", minForCapable = .1f };
        DefDatabase<PawnCapacityDef>.Clear(); DefDatabase<PawnCapacityDef>.Add(moving); DefDatabase<PawnCapacityDef>.Add(consciousness);
        try
        {
            PawnCapacityDefOf.Moving = moving; PawnCapacityDefOf.Consciousness = consciousness;
            w.Target.def.race.body = new BodyDef { corePart = new BodyPartRecord { def = new BodyPartDef { defName = "PurificationCore" } } };
            Set(w.Target.def.race, "fleshType", new RimWorld.FleshTypeDef { defName = "PurifyFlesh" });
            Set(w.Target.def.race, "lifeStageAges", new List<LifeStageAge> { new LifeStageAge { minAge = 0f, def = new LifeStageDef { defName = "PurifyStage", healthScaleFactor = 1f, bodySizeFactor = 1f, developmentalStage = DevelopmentalStage.Adult } } });
            w.Target.ageTracker = new Pawn_AgeTracker(w.Target);
            Set(w.Target.ageTracker, "cachedLifeStageIndex", 0);
            var capacityField = typeof(PawnCapacitiesHandler).GetField("cachedCapacityLevels", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            var cache = Activator.CreateInstance(capacityField.FieldType)!;
            foreach (var cap in new[] { PawnCapacityDefOf.Moving, PawnCapacityDefOf.Consciousness })
            {
                var entry = capacityField.FieldType.GetProperty("Item", [typeof(PawnCapacityDef)])!.GetValue(cache, [cap])!;
                Set(entry, "status", Enum.Parse(entry.GetType().GetField("status")!.FieldType, "Cached"));
                Set(entry, "value", 1f);
            }
            capacityField.SetValue(w.Target.health.capacities, cache);

            // Regeneration motes probe the map bounds and mote budget.
            var map = Find.Maps[0];
            map.info = new MapInfo { Size = new IntVec3(20, 1, 20) };
            map.cellIndices = new CellIndices(map);
            map.moteCounter = new MoteCounter();

            // A realistic located permanent scar plus a partless fresh wound.
            var armPart = new BodyPartRecord { def = new BodyPartDef { defName = "PurificationArm" } };
            var scar = new Hediff_Injury { pawn = w.Target, def = new HediffDef { defName = "PurificationScar", injuryProps = new InjuryProps() } };
            scar.comps = [new HediffComp_GetsPermanent { parent = scar, props = new HediffCompProperties_GetsPermanent() }];
            Set(scar.comps[0], "isPermanentInt", true);
            Set(scar, "part", armPart);
            w.Target.health.hediffSet.hediffs.Add(scar);
            Set(scar, "severityInt", 8f);

            var fresh = new Hediff_Injury { pawn = w.Target, def = new HediffDef { defName = "PurificationFreshCut", injuryProps = new InjuryProps() } };
            w.Target.health.hediffSet.hediffs.Add(fresh);
            Set(fresh, "severityInt", 5f);

            var verb = new Verb_Purify { Ability = w.Ability, verbProps = TorannMagicDefOf.TM_Purify.MainVerb };
            Set(verb, "caster", w.Caster);
            Set(verb, "currentTarget", new LocalTargetInfo(w.Target));
            var cast = typeof(Verb_Purify).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
            // Heal recalculates capacities and downed state; only hediff removal is
            // under test, so skip the physiological recalculation like CoreReturn does.
            var assembly = Assembly.Load("0Harmony");
            var id = "rwom.regression.purifyheal." + Guid.NewGuid();
            var harmony = Activator.CreateInstance(assembly.GetType("HarmonyLib.Harmony")!, id)!;
            var hook = Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(PriestPurificationTrialTests).GetMethod(nameof(SkipStateChange), BindingFlags.NonPublic | BindingFlags.Static))!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony, [typeof(Pawn_HealthTracker).GetMethod(nameof(Pawn_HealthTracker.CheckForStateChange), BindingFlags.Public | BindingFlags.Instance)!, hook, null, null, null]);
            try
            {
                Check.Equal(true, (bool)cast.Invoke(verb, null)!, "native cast succeeds on the injured target");
                Check.Equal(0f, w.Scar.Severity, "fixture permanent scar healed to zero by the cast");
                Check.Equal(0f, scar.Severity, "located permanent scar healed to zero by the cast");
                Check.Equal(5f, fresh.Severity, "fresh naturally-healing wound is untouched");
            }
            finally
            {
                harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony, [id]);
            }
        }
        finally
        {
            TorannMagic.ModOptions.Settings.Instance = savedSettings;
            TorannMagic.TM_Data.ResetCaches();
            DefDatabase<HediffCategoryList>.Clear();
            foreach (var def in savedCategories) DefDatabase<HediffCategoryList>.Add(def);
            // Restore the pre-test capacity set. When it was empty (no earlier test
            // initialized capacities yet), keep the registered defs in place: an
            // emptied database makes every later DefMap constructor throw
            // "without defs being initialized" (ManaSiphon cluster in the full run).
            if (savedCapacities.Length > 0)
            {
                DefDatabase<PawnCapacityDef>.Clear();
                foreach (var def in savedCapacities) DefDatabase<PawnCapacityDef>.Add(def);
            }
        }
    }

    static bool SkipStateChange(DamageInfo? dinfo, Hediff hediff) => false;

    sealed class Fixture : IDisposable
    {
        readonly Game previous = Current.Game;
        readonly ClassDisciplineDef[] profiles = DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
        public Pawn Caster, Target;
        public CompAbilityUserMagic Magic;
        public CompClassProgression Progression;
        public DisciplineProgress Path;
        public PawnAbility Ability;
        public Hediff_Injury Scar;
        public Fixture(RuntimeFixture f, bool adept = true)
        {
            LoadBaseDefinitions(f);
            DefDatabase<ClassDisciplineDef>.Clear();
            DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef { defName = "PurificationPriest", profileId = "rwom.priest", domain = DisciplineDomain.Magic });
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
            var c = PawnWithComps(true, false); Caster = c.pawn; Magic = c.magic;
            Target = PawnWithComps(false, false).pawn;
            var faction = new Faction { def = new FactionDef { isPlayer = true } };
            foreach (var pawn in new[] { Caster, Target })
            {
                pawn.mindState = new Verse.AI.Pawn_MindState(pawn);
                pawn.kindDef = new PawnKindDef { defName = "PurificationPawn" };
                Set(pawn, "factionInt", faction); Set(pawn, "mapIndexOrState", (sbyte)0);
            }
            Progression = new CompClassProgression { parent = Caster }; Caster.AllComps.Add(Progression);
            Progression.BeginNewProgression(Vocation.Mage); Progression.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
            Path = Progression.State.Find("rwom.priest");
            if (adept)
            {
                Path.RecordCompletedTrial(PriestHealingTrial.TrialId);
                Progression.State.TryAdvanceAfterTrial("rwom.priest", InitiationStage.Foundation, PriestHealingTrial.TrialId);
            }
            Ability = new PawnAbility(Magic); Set(Ability, "powerDef", TorannMagicDefOf.TM_Purify);
            Ability.CooldownTicksLeft = 77; Magic.AbilityData.Powers.Add(Ability);
            Scar = new Hediff_Injury { pawn = Target, def = new HediffDef { defName = "PurificationScar", injuryProps = new InjuryProps() } };
            var permanent = new HediffComp_GetsPermanent { parent = Scar, props = new HediffCompProperties_GetsPermanent() };
            Set(permanent, "isPermanentInt", true); Scar.comps = [permanent];
            Target.health.hediffSet.hediffs.Add(Scar); Severity(8f);
        }
        public void Severity(float value) => Set(Scar, "severityInt", value);
        public PriestPurificationTrial.Snapshot Capture() => PriestPurificationTrial.Capture(Caster, Target, Ability);
        public void Dispose()
        {
            Current.Game = previous;
            DefDatabase<ClassDisciplineDef>.Clear(); foreach (var profile in profiles) DefDatabase<ClassDisciplineDef>.Add(profile);
        }
    }

    static void Completion(RuntimeFixture f)
    {
        foreach (bool remove in new[] { false, true })
        {
            using var w = new Fixture(f);
            w.Magic.MagicData.MagicUserXP = 67; w.Magic.MagicData.MagicAbilityPoints = 4;
            var skill = w.Magic.MagicData.MagicPowerSkill_Purify[0]; skill.level = 2;
            Check.Equal(false, PriestPurificationTrial.Complete(w.Capture(), true), "successful no-op cannot finish trial");
            var partial = w.Capture(); w.Severity(4f);
            Check.Equal(false, PriestPurificationTrial.Complete(partial, true), "partly healed scar cannot finish trial");
            var failed = w.Capture(); w.Severity(0f);
            Check.Equal(false, PriestPurificationTrial.Complete(failed, false), "failed spell cannot credit scar removal");
            w.Severity(2f); var cured = w.Capture();
            if (remove) w.Target.health.hediffSet.hediffs.Remove(w.Scar); else w.Severity(0f);
            Check.Equal(true, PriestPurificationTrial.Complete(cured, true), "pre-existing scar removed or zeroed completes trial");
            Check.Equal(false, PriestPurificationTrial.Complete(cured, true), "duplicate callback is inert");
            Check.Equal(1, w.Path.CompletedTrials.Count(t => t == PriestPurificationTrial.TrialId), "one persisted purification marker");
            Check.Equal(InitiationStage.Adept, w.Path.Stage, "trial does not advance stage");
            Check.Equal(67, w.Magic.MagicData.MagicUserXP, "XP unchanged");
            Check.Equal(4, w.Magic.MagicData.MagicAbilityPoints, "points unchanged");
            Check.Equal(2, skill.level, "skill unchanged");
            Check.Equal(77, w.Ability.CooldownTicksLeft, "cooldown unchanged");
        }
    }

    static void Eligibility(RuntimeFixture f)
    {
        using var w = new Fixture(f, false);
        Check.Same(null!, w.Capture()!, "Foundation not eligible for Adept trial");
        w.Path.RecordCompletedTrial(PriestHealingTrial.TrialId);
        w.Progression.State.TryAdvanceAfterTrial("rwom.priest", InitiationStage.Foundation, PriestHealingTrial.TrialId);
        Check.Same(null!, PriestPurificationTrial.Capture(w.Caster, w.Caster, w.Ability)!, "self treatment excluded");
        var fake = new PawnAbility(w.Magic); Set(fake, "powerDef", TorannMagicDefOf.TM_Purify);
        Check.Same(null!, PriestPurificationTrial.Capture(w.Caster, w.Target, fake)!, "definition-equal fake instance excluded");
        w.Magic.AbilityData.Powers.Clear(); w.Magic.AbilityData.TemporaryWeaponPowers.Add(w.Ability);
        Check.Same(null!, w.Capture()!, "item-only spell excluded");
        w.Magic.AbilityData.TemporaryWeaponPowers.Clear(); w.Magic.AbilityData.Powers.Add(w.Ability);
        var comps = w.Scar.comps; w.Scar.comps = [];
        Check.Same(null!, w.Capture()!, "ordinary injury is not a permanent scar");
        w.Scar.comps = comps;
        var faction = w.Target.Faction; Set(w.Target, "factionInt", null);
        Check.Same(null!, w.Capture()!, "factionless target excluded");
        Set(w.Target, "factionInt", faction);
        var stale = w.Capture(); w.Severity(0f); w.Magic.AbilityData.Powers.Clear();
        Check.Equal(false, PriestPurificationTrial.Complete(stale, true), "lost ownership cannot finish trial");
        Check.Equal(false, w.Path.CompletedTrials.Contains(PriestPurificationTrial.TrialId), "rejected casts preserve trial state");
    }

    static void Hook(RuntimeFixture f)
    {
        var native = typeof(Verb_Purify).GetMethod("TryCastShot", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;
        Check.Equal(typeof(bool), native.ReturnType, "hook reads native cast success");
        var assembly = Assembly.Load("0Harmony"); var ht = assembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.regression.purification." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        try
        {
            var processor = ht.GetMethod("CreateClassProcessor")!.Invoke(harmony, [typeof(PriestPurificationTrialPatch)])!;
            processor.GetType().GetMethod("Patch")!.Invoke(processor, null);
            var patched = ((IEnumerable<MethodBase>)ht.GetMethod("GetAllPatchedMethods")!.Invoke(null, null)!).ToList();
            Check.Equal(true, patched.Contains(native), "prefix/postfix emit and attach to original Purify");
        }
        finally { ht.GetMethod("UnpatchAll")!.Invoke(harmony, [id]); }
    }
}
