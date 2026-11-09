using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.AutoCast;
using TorannMagic.TMDefs;
using Verse;
using Verse.AI;
using AbilityDef = AbilityUser.AbilityDef;
using AbilityDefOf = AbilityUser.AbilityDefOf;

namespace RWoM.RegressionTests;

internal static class AutocastTests
{
    private static NeedDef MoodDef = new NeedDef { defName = "Mood" };

    public static void Initialize(string repo)
    {
        ushort index = 1;
        foreach (var field in typeof(TorannMagicDefOf).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(TraitDef) && field.GetValue(null) == null)
                field.SetValue(null, new TraitDef { defName = field.Name, index = index++ });
        var document = XDocument.Load(Path.Combine(repo, "v1.7/Defs/CustomDefs/TM_HediffCategoryDef.xml"));
        var category = new HediffCategoryList { defName = "TM_Category_Hediffs" };
        foreach (var field in typeof(HediffCategoryList).GetFields().Where(field => field.FieldType == typeof(List<TM_CategoryHediff>)))
        {
            var entries = new List<TM_CategoryHediff>();
            foreach (var node in document.Root!.Elements().First().Element(field.Name)!.Elements("li"))
            {
                var entry = new TM_CategoryHediff();
                foreach (var valueField in typeof(TM_CategoryHediff).GetFields())
                {
                    var value = node.Element(valueField.Name);
                    if (value != null && !string.IsNullOrWhiteSpace(value.Value))
                        valueField.SetValue(entry, Convert.ChangeType(value.Value, valueField.FieldType, CultureInfo.InvariantCulture));
                }
                entries.Add(entry);
            }
            field.SetValue(category, entries);
        }
        DefDatabase<HediffCategoryList>.Add(category);
        // Need's static initialization requests two UI textures. They are never used
        // by these tests; account for exactly those headless diagnostics, not arbitrary logs.
        RuntimeHelpers.RunClassConstructor(typeof(Need).TypeHandle);
        RuntimeLog.AcceptHeadlessNeedInitialization();
        RuntimeHelpers.RunClassConstructor(typeof(AbilityDefOf).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(HediffDefOf).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(JobDefOf).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(RulePackDefOf).TypeHandle);
        RuntimeLog.AcceptHeadlessDefOfInitialization();
        HediffDefOf.Scaria = new HediffDef { defName = "Scaria", isBad = true, makesSickThought = true };
        HediffDefOf.FoodPoisoning = new HediffDef { defName = "FoodPoisoning", isBad = true, makesSickThought = true };
        DefDatabase<HediffDef>.Add(HediffDefOf.Scaria);
        DefDatabase<HediffDef>.Add(HediffDefOf.FoodPoisoning);
        DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_SyrriumSenseHD" });
    }

    private static void AreaSelection(RuntimeFixture fixture)
    {
        var area = new Area_Allowed();
        var grid = new BoolGrid(4, 4);
        typeof(Area).GetField("innerGrid", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(area, grid);
        var origin = new IntVec3(0, 0, 0);
        Check.Equal(false, TM_Calc.TryPickAutocastAreaCell(null, out _), "missing area skips cast");
        Check.Equal(false, TM_Calc.TryPickAutocastAreaCell(area, out var empty), "empty area skips cast");
        Check.Equal(IntVec3.Invalid, empty, "empty zone must never target map origin");
        grid[origin] = true;
        Check.Equal(true, TM_Calc.TryPickAutocastAreaCell(area, out var picked), "painted origin is valid");
        Check.Equal(origin, picked, "selects a designated cell");
        grid[origin] = false;
        Check.Equal(false, TM_Calc.TryPickAutocastAreaCell(area, out empty), "erased zone skips cast");
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Autocast.AreaSelection", AreaSelection);
        foreach (string family in new[] { "TM_Meteor", "TM_OrbitalStrike", "TM_Blink" })
            foreach (string suffix in new[] { "", "_I", "_II", "_III" })
            {
                string name = family + suffix;
                yield return new("Autocast.DefaultPolicy." + name, fixture => DefaultPolicy(fixture, name, family == "TM_Blink"));
            }
        yield return new("Autocast.StaleDestructiveConfigsAreNotCandidatesOrToggles", StaleDestructiveConfigs);
        yield return new("Autocast.DestructiveAIAdmissionLeavesManualChecksAlone", DestructiveAIAdmission);
        yield return new("Autocast.BlinkUsesExecutingJobMarkerAfterDelay", BlinkJobMarker);
        yield return new("Autocast.LearnedBlinkWithoutDestinationNeverCreatesSelfJob", BlinkWithoutDestination);
        yield return new("Autocast.HerbalClarityStandaloneExecutionAndToggle", f => HerbalExecution(f, "TM_HerbalClarity"));
        yield return new("Autocast.HerbalBlissStandaloneExecutionAndToggle", f => HerbalExecution(f, "TM_HerbalBliss"));
        yield return new("Autocast.SpecializedRoutesKeepModeBounds", SpecializedRoutes);
        yield return new("Autocast.CustomFirstAidUndraftedReachesSpecializedExecution", f => CustomFirstAid(f, false));
        yield return new("Autocast.CustomFirstAidDraftedReachesSpecializedExecution", f => CustomFirstAid(f, true));
        yield return new("Autocast.CustomRegenerateKeepsLearnedSavedOwner", CustomRegenerate);
        yield return new("Autocast.FailedShadowBoltCustomConsumedOnce", f => FailedDedicatedOnce(f, false, true));
        yield return new("Autocast.FailedShadowBoltClassConsumedOnce", f => FailedDedicatedOnce(f, false, false));
        yield return new("Autocast.FailedDraftedHealCustomConsumedOnce", f => FailedDedicatedOnce(f, true, true));
        yield return new("Autocast.FailedDraftedHealStandaloneConsumedOnce", f => FailedDedicatedOnce(f, true, false));
        yield return new("Autocast.ChaosMageUnlearnedHealDraftedCompatibility", f => FailedDedicatedOnce(f, true, false, chaos: true));
        yield return new("Autocast.ChaosMageUnlearnedHealUndraftedKeepsLearnedGate", f => FailedDedicatedOnce(f, true, false, drafted: false, chaos: true));
        yield return new("Autocast.FailedUndraftedHealCustomConsumedOnce", f => FailedDedicatedOnce(f, true, true, drafted: false));
        yield return new("Autocast.FailedUndraftedHealStandaloneConsumedOnce", f => FailedDedicatedOnce(f, true, false, drafted: false));
        yield return new("Autocast.FailedSummonMinionCustomConsumedOnce", f => FailedDedicatedOnce(f, false, true, drafted: false, summon: true));
        yield return new("Autocast.FailedSummonMinionStandaloneConsumedOnce", f => FailedDedicatedOnce(f, false, false, drafted: false, summon: true));
        yield return new("Autocast.DedicatedFamilyHonorsKnowledgeGate", f => DedicatedKnowledgeGate(f, drafted: false));
        yield return new("Autocast.ChaosMageUnlearnedSummonMinionKeepsLearnedGate", f => FailedDedicatedOnce(f, false, false, drafted: false, chaos: true, summon: true));
        yield return new("Autocast.CustomOwnersCoverAllReservedFamilies", ReservedOwners);
        yield return new("Autocast.CureDiseaseRejectsInjury", f => CureTarget(f, "Cut", 0, false, injury: true));
        yield return new("Autocast.CureDiseaseAcceptsUninjuredFlu", f => CureTarget(f, "Flu", 0, true));
        yield return new("Autocast.CureDiseaseRejectsLowLevelMalaria", f => CureTarget(f, "Malaria", 0, false));
        yield return new("Autocast.CureDiseaseAcceptsSupportedMalaria", f => CureTarget(f, "Malaria", 1, true));
        yield return new("Autocast.CureDiseaseRejectsUnsupportedSickness", f => CureTarget(f, "Regression_Sickness", 0, false));
        yield return new("Autocast.CureDiseaseRejectsAbasiaAtMaxLevel", f => CureTarget(f, "Abasia", 3, false));
        yield return new("Autocast.MechanitesAreNotOrdinaryDiseases", MechaniteTargets);
        yield return new("Autocast.CorpseTargetsStayCorpses", CorpseTargets);
        yield return new("Autocast.SharedMagicPathRejectsInjuryBeforeJob", SharedMagicRejectsInjury);
        yield return new("Autocast.SharedMightPathRejectsWrongMechaniteTarget", SharedMightRejectsFlu);
        yield return new("Autocast.StandaloneAndLeveledPowersKeepSavedOwners", FullCollections);
        yield return new("Autocast.HerbalClarityAndBlissKeepXmlConditions", HerbalConditions);
        yield return new("Autocast.CommanderAurasStayManualToggles", CommanderAurasManual);
        yield return new("Autocast.BrandsStayTargetedManualCasts", BrandsManual);
    }

    // Brands are semi-permanent upkeep links between two chosen pawns; autocasting
    // one onto whoever walks past (prisoners, allies, colonists — player report)
    // drains the caster and misplaces a permanent effect. They must stay targeted
    // manual casts with no autocast affordance and no candidate.
    private static void BrandsManual(RuntimeFixture fixture)
    {
        var might = EmptyMight();
        var magic = EmptyMagic();
        foreach (string family in new[] { "TM_Branding", "TM_SiphonBrand", "TM_FitnessBrand", "TM_EmotionBrand",
                 "TM_VitalityBrand", "TM_ProtectionBrand", "TM_AwarenessBrand", "TM_DispelBranding" })
        {
            var def = fixture.AddAbility(family);
            var policy = TM_AutocastDefaults.For(def, false);
            Check.Equal(AutocastType.Null, policy.type, family + " stays a targeted manual cast");
            Check.Equal(true, TM_AutocastDefaults.IsManualOnly(def), family + " is manual only");
            Check.Same(null, MagicToggle(magic, def), family + " offers no AC affordance");
            var mp = new MagicPower { TMabilityDefs = [def], learned = true, autocast = true };
            magic.magicPowerIF.Add(mp);
        }
        Check.Equal(0, MagicCandidates(magic).Count(), "toggled-on brands are never autocast candidates");
    }

    // The commander auras are toggles: casting the active aura dispels it (player
    // report — a drafted commander's aura switched itself off every time its icon
    // cooldown expired, because the toggle verb enables autocast and the generic
    // loop recast the toggle). They must stay manual-only like the other
    // MaintainedNoAutocast powers, with no autocast affordance and no candidate.
    private static void CommanderAurasManual(RuntimeFixture fixture)
    {
        var might = EmptyMight();
        var magic = EmptyMagic();
        foreach (string family in new[] { "TM_CommanderAura", "TM_ProvisionerAura", "TM_TaskMasterAura" })
            foreach (string suffix in new[] { "", "_I", "_II", "_III" })
            {
                var def = fixture.AddAbility(family + suffix);
                var policy = TM_AutocastDefaults.For(def, true);
                Check.Equal(AutocastType.Null, policy.type, family + suffix + " stays a manual toggle");
                Check.Equal(true, TM_AutocastDefaults.NeverAutocast(def), family + suffix + " never autocasts");
                Check.Same(null, MagicToggle(magic, def), family + suffix + " offers no AC affordance");
                Check.Same(null, MightToggle(might, def), family + suffix + " offers no might AC affordance");
                var mp = new MightPower { TMabilityDefs = [def], learned = true, autocast = true };
                might.mightPowerG.Add(mp);
                var gp = new MagicPower { TMabilityDefs = [def], learned = true, autocast = true };
                magic.magicPowerIF.Add(gp);
            }
        Check.Equal(0, MightCandidates(might).Count(), "toggled-on commander auras are never might autocast candidates");
        Check.Equal(0, MagicCandidates(magic).Count(), "toggled-on commander auras are never magic autocast candidates");
    }

    private static void DefaultPolicy(RuntimeFixture fixture, string name, bool blink)
    {
        var def = fixture.AddAbility(name);
        foreach (bool might in new[] { false, true })
        {
            var policy = TM_AutocastDefaults.For(def, might);
            Check.Equal(blink ? AutocastType.OnSelf : AutocastType.Null, policy.type, name + " default type");
            Check.Equal(false, policy.drafted, name + " not a generic combat cast");
            Check.Equal(blink, policy.undrafted, name + " travel toggle availability");
            Check.Equal(false, policy.AIUsable, name + " AI permission");
        }
    }

    private static void StaleDestructiveConfigs(RuntimeFixture fixture)
    {
        var magic = EmptyMagic();
        var might = EmptyMight();
        foreach (string family in new[] { "TM_Meteor", "TM_OrbitalStrike", "TM_Summon" })
            foreach (string suffix in new[] { "", "_I", "_II", "_III" })
            {
                var def = fixture.AddAbility(family + suffix);
                var stale = new TM_Autocast { type = AutocastType.OnSelf, magicUser = true, mightUser = true, drafted = true, undrafted = true, AIUsable = true };
                var mp = new MagicPower { TMabilityDefs = [def], learned = true, autocast = true, autocasting = stale };
                var fp = new MightPower { TMabilityDefs = [def], learned = true, autocast = true, autocasting = stale };
                magic.magicPowerIF.Add(mp);
                might.mightPowerG.Add(fp);
            }
        Check.Equal(0, MagicCandidates(magic).Count(), "saved unsafe configs cannot admit magic auto jobs");
        Check.Equal(0, MightCandidates(might).Count(), "saved unsafe configs cannot admit might auto jobs");
        foreach (var power in magic.magicPowerIF)
            Check.Equal<MagicPower?>(null, MagicToggle(magic, (TMAbilityDef)power.abilityDef), "no unsafe AC affordance");
        foreach (var power in might.mightPowerG)
            Check.Equal<MightPower?>(null, MightToggle(might, (TMAbilityDef)power.abilityDef), "no unsafe might AC affordance");
    }

    private static void DestructiveAIAdmission(RuntimeFixture fixture)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        foreach (string family in new[] { "TM_Meteor", "TM_OrbitalStrike" })
            foreach (string suffix in new[] { "", "_I", "_II", "_III" })
            {
                var def = fixture.AddAbility(family + suffix);
                var ability = RealAbility<MagicAbility>(comp, def);
                Check.Equal(false, ability.CanCastPowerCheck(AbilityContext.AI, out _), "legacy AI admission for " + def.defName);
                // Player checking is exercised by a narrow policy seam rather than a UI/game launch.
                var policy = typeof(TM_AutocastDefaults).GetMethod("AllowsContext");
                Check.NotNull(policy, "context admission policy");
                Check.Equal(true, (bool)policy!.Invoke(null, [def, AbilityContext.Player])!, "manual admission remains unchanged");
            }
    }

    private static void BlinkJobMarker(RuntimeFixture fixture)
    {
        var pawn = NewPawn();
        var comp = MagicComp(pawn);
        var def = fixture.AddAbility("TM_Blink_II");
        var ability = RealAbility<PawnAbility>(comp, def);
        var predicate = typeof(AutoCastContext).GetMethod("IsPlayerOrderedAbilityCast", [typeof(Job), typeof(Verb)]);
        Check.NotNull(predicate, "execution-time ability context policy (not temporary evaluator flag)");
        foreach (float warmup in new[] { 0f, 1.5f })
            foreach (bool queued in new[] { false, true })
                foreach (bool manual in new[] { false, true })
                {
                    def.MainVerb.warmupTime = warmup;
                    Job job = ability.GetJob(manual ? AbilityContext.Player : AbilityContext.AI, new IntVec3(4, 0, 4));
                    Check.Equal(true, job.playerForced, "Jecs marks BOTH ability contexts playerForced");
                    Check.Equal(manual ? 1 : 0, job.count, "actual Jecs context encoding");
                    if (queued)
                    {
                        var queue = new JobQueue();
                        queue.EnqueueLast(job, JobTag.Misc);
                        job = queue.Dequeue().job;
                    }
                    AutoCastContext.SelectionRestoreAllowed = true; // resolution already finished before execution
                    Check.Equal(manual, (bool)predicate!.Invoke(null, [job, ability.Verb])!, "queued/warmup execution uses its own marker");
                    Check.Equal(false, (bool)predicate.Invoke(null, [job, new Verb_UseAbility()])!, "other verb cannot authorize selection");
                }
        Check.Equal(false, (bool)predicate!.Invoke(null, [null, ability.Verb])!, "no job cannot authorize selection");
    }

    private static void BlinkWithoutDestination(RuntimeFixture fixture)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        var def = fixture.AddAbility("TM_Blink_II");
        var power = new MagicPower([def]) { learned = true, autocast = true };
        var ability = BoundaryAbility(comp, def);
        bool success = false;
        try { MagicAbility_OnSelf.Evaluate(comp, def, ability, power, out success); }
        catch (JobBoundaryReached) { }
        Check.Equal(0, ability.JobRequests, "generic self path must never preempt travel Blink");
        var handler = typeof(CompAbilityUserMagic).GetMethod("TryHandleSpecializedAutocast", BindingFlags.Instance | BindingFlags.NonPublic);
        Check.NotNull(handler, "learned upgraded Blink is routed before generic targeting");
        object?[] args = [power, def, ability, false, false];
        Check.Equal(true, (bool)handler!.Invoke(comp, args)!, "no destination remains handled, with no generic fallback");
        Check.Equal(false, (bool)args[4]!, "idle Blink is not a successful cast");
        Check.Equal(0, ability.JobRequests, "idle Blink leaves work/queue untouched");
    }

    private static void HerbalExecution(RuntimeFixture fixture, string name)
    {
        var definition = LoadHerbal(fixture, name);
        var caster = NewPawn();
        var comp = MightComp(caster);
        var power = comp.MightData.MightPowersCustomAll.Single(item => item.TMabilityDefs.Contains(definition.customPower.abilityDefs[0]));
        power.learned = true;
        power.autocast = true;
        var def = (TMAbilityDef)power.abilityDef;
        Check.Equal(1, MightCandidates(comp.MightData).Count(item => ReferenceEquals(item, power)), name + " admitted from standalone collection");
        Check.Same(power, MightToggle(comp.MightData, def), name + " toggle controls executed saved power");
        DefDatabase<TM_AutocastConditionDef>.Clear();
        LoadConditions(fixture, power.autocasting.advancedConditionDefs);
        var target = NewPawn();
        Need need = name == "TM_HerbalClarity" ? NewNeed<Need_Mana>(target, TorannMagicDefOf.TM_Mana) : NewNeed<Need_Mood>(target, MoodDef);
        need.CurLevel = .01f;
        Check.Equal(true, power.autocasting.ValidConditions(caster, target), "shipped herb conditions permit this target");
        var ability = BoundaryAbility(comp, def);
        try { CombatAbility_OnTarget.TryExecute(comp, def, ability, power, target, 0, out _); }
        catch (JobBoundaryReached) { }
        Check.Equal(1, ability.JobRequests, name + " reaches actual shared job creation boundary once");
    }

    private static void SpecializedRoutes(RuntimeFixture fixture)
    {
        // Mode bounds are deliberate: reserve the existing legacy evaluator only in
        // its original modes, without hiding its toggle or disabling all AI abilities.
        (string Name, bool Might, bool Drafted, bool Hunting, bool Expected)[] cases =
        [
            ("TM_Regenerate", false, false, false, true), ("TM_Regenerate", false, true, false, true),
            ("TM_Shield_III", false, false, false, true), ("TM_Shield_III", false, true, false, true),
            ("TM_Purify", false, false, false, true), ("TM_Purify", false, true, false, false),
            ("TM_Transmutate", false, false, false, true), ("TM_RegrowLimb", false, false, false, true),
            ("TM_SummonMinion", false, false, false, true), ("TM_TimeMark", false, false, false, true),
            ("TM_SpellMending", false, false, false, true), ("TM_SpellMending", false, true, false, false),
            ("TM_TeachMagic", false, false, false, true), ("TM_DirtDevil", false, false, false, true),
            ("TM_TransferMana", false, false, false, true), ("TM_SiphonMana", false, true, false, true),
            ("TM_CauterizeWound", false, true, false, true), ("TM_MagicMissile_II", false, true, false, true),
            ("TM_PhaseStrike_II", true, false, false, true), ("TM_PhaseStrike_II", true, true, false, false),
            ("TM_FirstAid", true, true, false, true), ("TM_Elixir", true, false, false, true),
            ("TM_TeachMight", true, false, false, true), ("TM_ArrowStorm", true, false, true, true),
            ("TM_ArrowStorm", true, false, false, false), ("TM_BladeSpin", true, true, false, true),
            ("TM_HerbalClarity", true, false, false, false), ("TM_HerbalBliss", true, false, false, false)
        ];
        foreach (var item in cases)
        {
            var def = DefDatabase<TMAbilityDef>.GetNamedSilentFail(item.Name) ?? fixture.AddAbility(item.Name);
            Check.Equal(item.Expected, LegacyRoute(def, item.Might, item.Drafted, item.Hunting), item.Name + " legacy ownership in this mode");
        }
    }

    private static IEnumerable<T> LegacyOwners<T>(object data)
    {
        string name = typeof(T) == typeof(MagicPower) ? "LegacyMagicPowers" : "LegacyMightPowers";
        var method = typeof(AutoCastContext).Assembly.GetType("TorannMagic.AutoCast.AutocastPolicy")!.GetMethod(name);
        // Baseline evaluators read only these original class lists.
        if (method == null)
            return (IEnumerable<T>)(object)(data is MagicData magic ? (object)magic.MagicPowersD : ((MightData)data).MightPowersSS);
        return (IEnumerable<T>)method.Invoke(null, [data])!;
    }

    private static void CustomFirstAid(RuntimeFixture fixture, bool drafted)
    {
        var caster = NewPawn();
        var comp = MightComp(caster);
        var def = fixture.AddAbility("TM_FirstAid");
        SetField(caster.def.race, "fleshType", new FleshTypeDef { defName = "Regression_Flesh" });
        TorannMagicDefOf.TM_FirstAid = def;
        comp.MightData.mightPowerSS.Add(new MightPower([def]));
        var saved = new MightPower([def]) { learned = true, autocast = true };
        comp.MightData.MightPowersCustomStandalone.Add(saved);
        var owners = LegacyOwners<MightPower>(comp.MightData).Where(power => power.abilityDef == def).ToList();
        Check.Equal(1, owners.Count, "same-def built-in cannot cause duplicate specialized attempts");
        Check.Same(saved, owners[0], "FirstAid uses custom saved owner without a SuperSoldier trait or mimic");
        var injury = new TendableInjury { def = new HediffDef { defName = "Regression_Tendable", tendable = true }, pawn = caster };
        SetField(injury, "severityInt", 1f);
        caster.health.hediffSet.hediffs.Add(injury);
        var ability = BoundaryAbility(comp, def);
        caster.drafter = new Pawn_DraftController(caster);
        SetField(caster.drafter, "draftedInt", drafted);
        caster.mindState = new Pawn_MindState(caster);
        caster.equipment = new Pawn_EquipmentTracker(caster);
        SetField(caster.jobs, "curJob", new Job { def = new JobDef { defName = "Regression_Work" } });
        comp.AbilityData.Powers.Add(ability);
        TorannMagic.ModOptions.Settings.Instance.autocastEnabled = true;
        var route = typeof(CompAbilityUserMight).GetMethod("ResolvePlayerAutocastPowers", BindingFlags.Instance | BindingFlags.NonPublic);
        Check.NotNull(route, "dedicated scheduler routing is exercised after outer game admission");
        try { route!.Invoke(comp, null); }
        catch (TargetInvocationException ex) when (ex.InnerException is JobBoundaryReached) { }
        catch (TargetInvocationException ex) { throw new InvalidOperationException("Scheduler failure: " + ex.InnerException, ex.InnerException); }
        Check.Equal(1, ability.JobRequests, "custom FirstAid retains specialized self tending execution");
        caster.health.hediffSet.hediffs.Clear();
        route!.Invoke(comp, null);
        Check.Equal(1, ability.JobRequests, "uninjured custom FirstAid never falls back to a generic self cast");
    }

    private static void FailedDedicatedOnce(RuntimeFixture fixture, bool heal, bool custom, bool drafted = true, bool chaos = false, bool summon = false)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        var def = fixture.AddAbility(summon ? "TM_SummonMinion" : heal ? "TM_Heal" : "TM_ShadowBolt");
        def.manaCost = 2f; // Reject inside the real dedicated evaluator, before target lookup.
        if (summon) TorannMagicDefOf.TM_SummonMinion = def;
        else if (heal) TorannMagicDefOf.TM_Heal = def;
        else TorannMagicDefOf.TM_ShadowBolt = def;
        var saved = new MagicPower([def]) { learned = !chaos, autocast = false };
        if (chaos)
        {
            caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.ChaosMage));
            var tradition = fixture.AddAbility("TM_ChaosTradition");
            TorannMagicDefOf.TM_ChaosTradition = tradition;
            comp.MagicData.magicPowerCM.Add(new MagicPower([tradition]) { learned = true });
        }
        if (summon)
        {
            comp.spell_SummonMinion = true;
            comp.MagicData.magicPowerS.Add(custom ? new MagicPower([def]) : saved);
        }
        else
        if (heal)
        {
            comp.spell_Heal = true; // Non-Paladin standalone compatibility route.
            comp.MagicData.magicPowerP.Add(custom ? new MagicPower([def]) : saved);
        }
        else
        {
            comp.MagicData.magicPowerSD.Add(custom ? new MagicPower([def]) : saved);
            comp.MagicData.magicPowerWD.Add(new MagicPower([def]));
            if (!custom) caster.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Succubus));
        }
        if (custom) comp.MagicData.MagicPowersCustomStandalone.Add(saved);
        caster.drafter = new Pawn_DraftController(caster);
        SetField(caster.drafter, "draftedInt", drafted);
        caster.drafter.FireAtWill = true;
        caster.equipment = new Pawn_EquipmentTracker(caster);
        SetField(caster.jobs, "curJob", new Job { def = new JobDef { defName = "Regression_Work" } });
        var ability = BoundaryAbility(comp, def);
        comp.AbilityData.Powers.Add(ability);
        var route = typeof(CompAbilityUserMagic).GetMethod("ResolvePlayerAutocastPowers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        // Observe entry into the actual evaluator; leave its body and false result intact.
        var harmonyAssembly = Assembly.Load("0Harmony");
        var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony")!;
        var harmonyMethod = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!;
        var harmony = Activator.CreateInstance(harmonyType, "rwom.regression.once")!;
        var evaluator = (summon ? typeof(MagicAbility_OnSelfPosition) : heal ? typeof(HealSpell) : typeof(DamageSpell)).GetMethod("Evaluate")!;
        var observer = Activator.CreateInstance(harmonyMethod, typeof(AutocastTests).GetMethod(nameof(ObserveDedicated), BindingFlags.Static | BindingFlags.NonPublic)!)!;
        var patch = harmonyType.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5);
        patch.Invoke(harmony, [evaluator, observer, null, null, null]);
        try
        {
            saved.autocast = true;
            dedicatedAttempts = 0;
            route.Invoke(comp, null);
            Check.Equal(chaos && (summon || !drafted) ? 0 : 1, dedicatedAttempts, "failed dedicated family is consumed once per scheduler pass");
            Check.Equal(0, ability.JobRequests, "failed evaluator never creates a job or falls back");
            dedicatedAttempts = 0;
            route.Invoke(comp, null);
            Check.Equal(chaos && (summon || !drafted) ? 0 : 1, dedicatedAttempts, "next scheduler pass gets exactly one fresh attempt");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, ["rwom.regression.once"]);
        }
    }

    private static void InvokeRoute(System.Reflection.MethodInfo route, CompAbilityUserMagic comp)
    {
        try { route.Invoke(comp, null); }
        catch (System.Reflection.TargetInvocationException ex)
        {
            throw new InvalidOperationException("Scheduler failure: " + ex.InnerException, ex.InnerException);
        }
    }

    private static int dedicatedAttempts;
    private static void ObserveDedicated() => dedicatedAttempts++;
    // Ф4 remainder: the dedicated class evaluators (heal/purify/summon/...)
    // gate through the knowledge store like the generic loops — the store
    // overrides the legacy learned/autocast flags in both directions.
    private static void DedicatedKnowledgeGate(RuntimeFixture fixture, bool drafted)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        var def = fixture.AddAbility("TM_Heal");
        def.manaCost = 2f; // reject inside the real evaluator, before target lookup
        TorannMagicDefOf.TM_Heal = def;
        var saved = new MagicPower([def]) { learned = true, autocast = true };
        comp.spell_Heal = true; // non-Paladin standalone compatibility route
        comp.MagicData.magicPowerP.Add(saved);
        var knowledge = new AbilityKnowledgeComp { parent = caster };
        caster.AllComps.Add(knowledge);
        var entry = knowledge.GetOrCreate(TorannMagic.SpellIds.Of(def), 0);
        entry.learned = true;
        entry.autocast = false;
        caster.drafter = new Pawn_DraftController(caster);
        SetField(caster.drafter, "draftedInt", drafted);
        caster.drafter.FireAtWill = true;
        caster.equipment = new Pawn_EquipmentTracker(caster);
        SetField(caster.jobs, "curJob", new Job { def = new JobDef { defName = "Regression_Work" } });
        var ability = BoundaryAbility(comp, def);
        comp.AbilityData.Powers.Add(ability);
        var route = typeof(CompAbilityUserMagic).GetMethod("ResolvePlayerAutocastPowers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var harmonyAssembly = Assembly.Load("0Harmony");
        var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony")!;
        var harmonyMethod = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!;
        var harmony = Activator.CreateInstance(harmonyType, "rwom.regression.knowledge")!;
        var evaluator = typeof(HealSpell).GetMethod("Evaluate")!;
        var observer = Activator.CreateInstance(harmonyMethod, typeof(AutocastTests).GetMethod(nameof(ObserveDedicated), BindingFlags.Static | BindingFlags.NonPublic)!)!;
        var patch = harmonyType.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5);
        patch.Invoke(harmony, [evaluator, observer, null, null, null]);
        try
        {
            // The store says OFF while legacy says ON — the store wins.
            dedicatedAttempts = 0;
            InvokeRoute(route, comp);
            Check.Equal(0, dedicatedAttempts, "knowledge autocast=false suppresses the dedicated family");

            // The store says ON while legacy says OFF — the store wins.
            saved.autocast = false;
            entry.autocast = true;
            dedicatedAttempts = 0;
            InvokeRoute(route, comp);
            Check.Equal(1, dedicatedAttempts, "knowledge autocast=true enables the dedicated family");
            Check.Equal(0, ability.JobRequests, "failed evaluator never creates a job or falls back");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll")!.Invoke(harmony, ["rwom.regression.knowledge"]);
        }
    }


    private static void CustomRegenerate(RuntimeFixture fixture)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        var def = fixture.AddAbility("TM_Regenerate");
        TorannMagicDefOf.TM_Regenerate = def;
        comp.MagicData.magicPowerD.Add(new MagicPower([def]));
        var saved = new MagicPower([def]) { learned = true, autocast = true };
        comp.MagicData.MagicPowersCustomStandalone.Add(saved);
        var owners = LegacyOwners<MagicPower>(comp.MagicData).Where(power => power.abilityDef == def).ToList();
        Check.Equal(1, owners.Count, "unlearned druid entry cannot duplicate a custom cast");
        Check.Same(saved, owners[0], "Regenerate specialized evaluation reads learned saved owner");
        // No mana: the real specialized evaluator must reject before querying map targets.
        comp.Mana.CurLevel = 0f;
        def.manaCost = .5f;
        var ability = BoundaryAbility(comp, def);
        HediffHealSpell.EvaluateMinSeverity(comp, def, ability, owners[0], new HediffDef { defName = "TM_Regeneration" }, 10f, out bool success);
        Check.Equal(false, success, "Regenerate keeps its resource gate");
        Check.Equal(0, ability.JobRequests, "insufficient mana cannot create a regeneration job");
        comp.Mana.CurLevel = 1f;
        var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
        map.mapPawns = new MapPawns(map);
        map.gameConditionManager = new GameConditionManager((Map)null!);
        SetField(Current.Game, "maps", new List<Map> { map });
        SetField(caster, "mapIndexOrState", (sbyte)0);
        SetField(caster, "factionInt", new Faction());
        SetField(caster, "positionInt", new IntVec3(5, 0, 5));
        caster.health.hediffSet.hediffs.Add(new TendableInjury
        {
            def = new HediffDef { defName = "Regression_RegenInjury", injuryProps = new() }, pawn = caster
        });
        SetField(caster.health.hediffSet.hediffs.Last(), "severityInt", 11f);
        ((IList<Pawn>)map.mapPawns.AllPawnsSpawned).Add(caster);
        DefDatabase<HediffDef>.Add(new HediffDef { defName = "TM_Regeneration" });
        caster.drafter = new Pawn_DraftController(caster);
        caster.equipment = new Pawn_EquipmentTracker(caster);
        SetField(caster.jobs, "curJob", new Job { def = new JobDef { defName = "Regression_Work" } });
        def.MainVerb.range = 30f;
        comp.AbilityData.Powers.Add(ability);
        var route = typeof(CompAbilityUserMagic).GetMethod("ResolvePlayerAutocastPowers", BindingFlags.Instance | BindingFlags.NonPublic);
        Check.NotNull(route, "magic scheduler routes the saved owner to its specialized evaluator");
        try { route!.Invoke(comp, null); }
        catch (TargetInvocationException ex) when (ex.InnerException is JobBoundaryReached) { }
        catch (TargetInvocationException ex) { throw new InvalidOperationException("Scheduler failure: " + ex.InnerException, ex.InnerException); }
        Check.Equal(1, ability.JobRequests, "custom Regenerate reaches specialized nearby injury job boundary");
        SetField(caster.health.hediffSet.hediffs.Last(), "severityInt", 1f);
        route!.Invoke(comp, null);
        Check.Equal(1, ability.JobRequests, "Regenerate retains its ten severity threshold without a generic fallback");
    }

    private static void ReservedOwners(RuntimeFixture fixture)
    {
        string[] magicNames = ["TM_SummonMinion", "TM_TimeMark", "TM_Regenerate", "TM_RegrowLimb", "TM_Heal", "TM_Shield", "TM_AdvancedHeal", "TM_Purify", "TM_Transmutate", "TM_TransferMana", "TM_SiphonMana", "TM_CauterizeWound", "TM_SpellMending", "TM_TeachMagic", "TM_DirtDevil", "TM_Firebolt", "TM_Icebolt", "TM_FrostRay", "TM_LightningBolt", "TM_MagicMissile", "TM_Poison", "TM_ShadowBolt", "TM_ArcaneBolt"];
        string[] mightNames = ["TM_PhaseStrike", "TM_FirstAid", "TM_Elixir", "TM_TeachMight", "TM_ArrowStorm", "TM_PistolWhip", "TM_SuppressingFire", "TM_Buckshot", "TM_Headshot", "TM_Spite", "TM_ThrowingKnife", "TM_TempestStrike", "TM_BladeSpin", "TM_TigerStrike", "TM_ThunderStrike", "TM_Grapple", "TM_AntiArmor", "TM_DisablingShot", "TM_PommelStrike"];
        var magic = EmptyMagic();
        var might = EmptyMight();
        var expectedMagic = new List<MagicPower>();
        var expectedMight = new List<MightPower>();
        foreach (string name in magicNames)
        {
            var def = fixture.AddAbility(name);
            magic.magicPowerD.Add(new MagicPower([def]));
            var saved = new MagicPower([def]) { learned = true, autocast = true };
            magic.MagicPowersCustomStandalone.Add(saved);
            expectedMagic.Add(saved);
        }
        foreach (string name in mightNames)
        {
            var def = fixture.AddAbility(name);
            might.mightPowerSS.Add(new MightPower([def]));
            var saved = new MightPower([def]) { learned = true, autocast = true };
            might.MightPowersCustomStandalone.Add(saved);
            expectedMight.Add(saved);
        }
        var shield = (TMAbilityDef)magic.magicPowerD.Single(power => power.abilityDef.defName == "TM_Shield").abilityDef;
        var upgraded = fixture.AddAbility("TM_Shield_III");
        var savedLevel = new MagicPower([shield, upgraded]) { learned = true, autocast = true, level = 1 };
        magic.magicPowerD.Insert(0, savedLevel);
        magic.MagicPowersCustomStandalone.Add(savedLevel);
        foreach (var saved in expectedMagic.Where(power => power.abilityDef != shield))
            Check.Same(saved, LegacyOwners<MagicPower>(magic).Single(power => power.abilityDef == saved.abilityDef), saved.abilityDef.defName + " dedicated route uses saved owner");
        foreach (var saved in expectedMight)
            Check.Same(saved, LegacyOwners<MightPower>(might).Single(power => power.abilityDef == saved.abilityDef), saved.abilityDef.defName + " dedicated route uses saved owner");
        Check.Equal(1, LegacyOwners<MagicPower>(magic).Count(power => power.TMabilityDefs.Contains(shield)), "aliases, levels and shared saved references cannot duplicate specialized attempts");
    }

    private static void CureTarget(RuntimeFixture fixture, string hediff, int versatility, bool expected, bool injury = false)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        var cure = fixture.AddAbility("TM_CureDisease");
        comp.MagicData.MagicPowerSkill_CureDisease.Single(skill => skill.label == "TM_CureDisease_ver").level = versatility;
        var target = NewPawn();
        Afflict(target, hediff, injury);
        Check.Equal(expected, Acceptable(caster, cure, target), hediff + " at actual caster versatility " + versatility);
    }

    private static void MechaniteTargets(RuntimeFixture fixture)
    {
        var def = fixture.AddAbility("TM_MechaniteReprogramming");
        var caster = NewPawn();
        MagicComp(caster);
        foreach (var item in new[] { ("Flu", false), ("Cut", false), ("SensoryMechanites", true), ("FibrousMechanites", true), ("LymphaticMechanites", true) })
        {
            var target = NewPawn();
            Afflict(target, item.Item1, item.Item1 == "Cut");
            Check.Equal(item.Item2, Acceptable(caster, def, target), "mechanite target " + item.Item1);
        }
    }

    private static void CorpseTargets(RuntimeFixture fixture)
    {
        var caster = NewPawn();
        var corpse = new Corpse();
        foreach (string name in new[] { "TM_ConsumeCorpse", "TM_CorpseExplosion", "TM_RaiseUndead" })
        {
            var def = fixture.AddAbility(name);
            Check.Equal(true, Acceptable(caster, def, corpse), name + " accepts corpse, not its dead inner pawn");
            Check.Equal(false, Acceptable(caster, def, caster), name + " does not consume a live pawn");
            Check.Equal(false, Acceptable(caster, def, new IntVec3(3, 0, 3)), name + " requires a corpse, not a cell");
        }
    }

    private static void SharedMagicRejectsInjury(RuntimeFixture fixture)
    {
        var caster = NewPawn();
        var comp = MagicComp(caster);
        var def = fixture.AddAbility("TM_CureDisease");
        var target = NewPawn();
        Afflict(target, "Cut", true);
        var ability = BoundaryAbility(comp, def);
        bool success = false;
        try { MagicAbility_OnTarget.TryExecute(comp, def, ability, new MagicPower(), target, 0, out success); }
        catch (JobBoundaryReached) { }
        Check.Equal(0, ability.JobRequests, "ordinary wound rejected BEFORE the job boundary");
        Check.Equal(false, success, "ordinary wound must not report an automatic cure cast");
        Check.Equal(1f, comp.Mana.CurLevel, "rejection does not spend mana");
    }

    private static void SharedMightRejectsFlu(RuntimeFixture fixture)
    {
        var caster = NewPawn();
        var comp = MightComp(caster);
        var def = fixture.AddAbility("TM_MechaniteReprogramming");
        var target = NewPawn();
        Afflict(target, "Flu");
        var ability = BoundaryAbility(comp, def);
        bool success = false;
        try { CombatAbility_OnTarget.TryExecute(comp, def, ability, new MightPower(), target, 0, out success); }
        catch (JobBoundaryReached) { }
        Check.Equal(0, ability.JobRequests, "unsupported target rejected BEFORE the job boundary");
        Check.Equal(false, success, "might shared path cannot reprogram Flu");
        Check.Equal(1f, comp.Stamina.CurLevel, "rejection does not spend stamina");
    }

    private static void FullCollections(RuntimeFixture fixture)
    {
        var clarity = LoadHerbal(fixture, "TM_HerbalClarity");
        var bliss = LoadHerbal(fixture, "TM_HerbalBliss");
        var magic = EmptyMagic();
        var might = EmptyMight();
        _ = magic.MagicPowersCustom;
        _ = might.MightPowersCustom;
        // Shipped herbal powers are fighter-only. An actual hybrid may also have
        // a learned saved magic standalone power with the same XML AC conditions.
        foreach (var definition in new[] { clarity, bliss })
        {
            var mp = new MagicPower(definition.customPower.abilityDefs) { learned = true, autocast = true, autocasting = definition.customPower.autocasting };
            magic.MagicPowersCustomStandalone.Add(mp);
            var fp = might.MightPowersCustomStandalone.Single(power => power.TMabilityDefs.Contains(definition.customPower.abilityDefs[0]));
            fp.learned = true;
            fp.autocast = true;
        }
        var leveled = fixture.Ability("TM_DistortSpirit_II");
        var custom = magic.MagicPowersCustom.Single(power => power.TMabilityDefs.Contains(leveled));
        custom.learned = true;
        custom.autocast = true;
        custom.level = 2;
        var learnedBlink = new MagicPower([fixture.AddAbility("TM_Blink"), fixture.AddAbility("TM_Blink_I")]) { learned = true, autocast = true, level = 1 };
        magic.magicPowerA.Add(learnedBlink);
        // A shared saved reference must not cause duplicate attempts; an unlearned
        // same-def object must not suppress a learned custom power.
        magic.magicPowerIF.Add(custom);
        magic.magicPowerIF.Insert(0, new MagicPower(custom.TMabilityDefs));
        // DistortSpirit inherits an explicit Null XML policy: collection expansion
        // must not invent a toggle for it. The upgraded learned Blink has one.
        Check.Equal<MagicPower?>(null, MagicToggle(magic, leveled), "explicit XML Null policy preserved");
        foreach (var mp in new[] { custom, learnedBlink }.Concat(magic.MagicPowersCustomStandalone))
        {
            Check.Equal(1, MagicCandidates(magic).Count(power => ReferenceEquals(power, mp)), mp.abilityDef.defName + " execution candidate once");
            if (!ReferenceEquals(mp, custom))
                Check.Same(mp, MagicToggle(magic, (TMAbilityDef)mp.abilityDef), mp.abilityDef.defName + " toggle is the saved owner");
        }
        foreach (var fp in might.MightPowersCustomStandalone)
        {
            Check.Equal(1, MightCandidates(might).Count(power => ReferenceEquals(power, fp)), fp.abilityDef.defName + " execution candidate once");
            Check.Same(fp, MightToggle(might, (TMAbilityDef)fp.abilityDef), fp.abilityDef.defName + " toggle is the saved owner");
        }
    }

    private static void HerbalConditions(RuntimeFixture fixture)
    {
        DefDatabase<TM_AutocastConditionDef>.Clear();
        foreach (string name in new[] { "TM_HerbalClarity", "TM_HerbalBliss" })
        {
            var def = LoadHerbal(fixture, name);
            var ac = def.customPower.autocasting;
            var target = NewPawn();
            LoadConditions(fixture, ac.advancedConditionDefs);
            Need need = name == "TM_HerbalClarity" ? NewNeed<Need_Mana>(target, TorannMagicDefOf.TM_Mana) : NewNeed<Need_Mood>(target, MoodDef);
            need.CurLevel = .01f;
            Check.Equal(true, ac.ValidConditions(target, target), name + " accepts low need without buff");
            need.CurLevel = .9f;
            Check.Equal(false, ac.ValidConditions(target, target), name + " rejects sufficient need");
            need.CurLevel = .01f;
            Afflict(target, name == "TM_HerbalClarity" ? "TM_HerbalClarityHD" : "TM_HerbalBlissHD");
            Check.Equal(false, ac.ValidConditions(target, target), name + " rejects existing buff");
        }
    }

    // These adapters allow the SAME tests to run against the reviewed pre-fix DLL.
    // Only the absent seam falls back to the old compiled entry point/collection;
    // fixed builds invoke the production seams used by the scheduler and gizmos.
    private static bool Acceptable(Pawn caster, TMAbilityDef def, LocalTargetInfo target)
    {
        var method = typeof(TM_Calc).GetMethod("AutocastTargetAcceptable", [typeof(Pawn), typeof(TMAbilityDef), typeof(LocalTargetInfo)]);
        return method != null ? (bool)method.Invoke(null, [caster, def, target])! : TM_Calc.AutocastTargetAcceptable(def, target.Pawn);
    }

    private static IEnumerable<MagicPower> MagicCandidates(MagicData data)
    {
        var method = typeof(AutoCastContext).Assembly.GetType("TorannMagic.AutoCast.AutocastPolicy")?.GetMethod("MagicPowers", [typeof(MagicData)]);
        return method != null ? (IEnumerable<MagicPower>)method.Invoke(null, [data])! : data.AllMagicPowersWithSkills.Where(power => power.learned);
    }

    private static IEnumerable<MightPower> MightCandidates(MightData data)
    {
        var method = typeof(AutoCastContext).Assembly.GetType("TorannMagic.AutoCast.AutocastPolicy")?.GetMethod("MightPowers", [typeof(MightData)]);
        return method != null ? (IEnumerable<MightPower>)method.Invoke(null, [data])! : data.AllMightPowersWithSkills.Where(power => power.learned);
    }

    private static MagicPower? MagicToggle(MagicData data, TMAbilityDef def)
    {
        var method = typeof(TM_Action).GetMethod("GetAutocastMagicPower");
        return method != null ? (MagicPower?)method.Invoke(null, [data, def]) : data.AllMagicPowersWithSkills.FirstOrDefault(power =>
            power.TMabilityDefs?.Contains(def) == true && power.autocasting != null && power.autocasting.type != AutocastType.Null && (power.autocasting.drafted || power.autocasting.undrafted));
    }

    private static MightPower? MightToggle(MightData data, TMAbilityDef def)
    {
        var method = typeof(TM_Action).GetMethod("GetAutocastMightPower");
        return method != null ? (MightPower?)method.Invoke(null, [data, def]) : data.AllMightPowersWithSkills.FirstOrDefault(power =>
            power.TMabilityDefs?.Contains(def) == true && power.autocasting != null && power.autocasting.type != AutocastType.Null && (power.autocasting.drafted || power.autocasting.undrafted));
    }

    private static bool LegacyRoute(TMAbilityDef def, bool might, bool drafted, bool hunting)
    {
        var method = typeof(AutoCastContext).Assembly.GetType("TorannMagic.AutoCast.AutocastPolicy")?.GetMethod("UsesLegacyEvaluator");
        return method != null && (bool)method.Invoke(null, [def, might, drafted, hunting])!;
    }

    private static MagicData EmptyMagic()
    {
        var data = new MagicData();
        foreach (var field in typeof(MagicData).GetFields())
            if (field.FieldType == typeof(List<MagicPower>) && !field.Name.Contains("Custom"))
                field.SetValue(data, new List<MagicPower>());
        data.magicPowerP.AddRange(Enumerable.Range(0, 6).Select(_ => new MagicPower { TMabilityDefs = [] }));
        return data;
    }

    private static MightData EmptyMight()
    {
        var data = new MightData();
        foreach (var field in typeof(MightData).GetFields())
            if (field.FieldType == typeof(List<MightPower>) && !field.Name.Contains("Custom"))
                field.SetValue(data, new List<MightPower>());
        return data;
    }

    private static Pawn NewPawn()
    {
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIHardMode = false };
        if (Current.Game == null)
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            SetField(Current.Game, "tickManager", new TickManager());
            SetField(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
        }
        AbilityDefOf.CastAbilityVerb ??= new JobDef { defName = "Regression_CastVerb" };
        AbilityDefOf.CastAbilitySelf ??= new JobDef { defName = "Regression_CastSelf" };
        TorannMagicDefOf.TM_Mana ??= new NeedDef { defName = "TM_Mana" };
        MoodDef ??= new NeedDef { defName = "Mood" };
        // ThingDef's constructor loads BaseContent textures, not needed for these
        // real Pawn/health/job tests in the headless runtime.
        var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawnDef.defName = "Regression_Human";
        pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = pawnDef };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        pawn.needs = new Pawn_NeedsTracker(pawn);
        pawn.jobs = new Pawn_JobTracker(pawn);
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        SetField(pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000L);
        pawn.apparel = new Pawn_ApparelTracker(pawn);
        SetField(pawn, "comps", new List<ThingComp>());
        return pawn;
    }

    private static CompAbilityUserMagic MagicComp(Pawn pawn)
    {
        var comp = new CompAbilityUserMagic { parent = pawn };
        SetField(comp, "magicData", EmptyMagic());
        SetField(comp, "initializedIsMagicUser", true);
        SetField(comp, "isMagicUser", true);
        pawn.AllComps.Add(comp);
        TM_Calc.InvalidateCompCache(pawn);
        NewNeed<Need_Mana>(pawn, TorannMagicDefOf.TM_Mana).CurLevel = 1f;
        Check.NotNull(comp.Mana, "fixture mana pool");
        return comp;
    }

    private static CompAbilityUserMight MightComp(Pawn pawn)
    {
        var comp = new CompAbilityUserMight { parent = pawn };
        SetField(comp, "mightData", EmptyMight());
        SetField(comp, "initializedIsMightUser", true);
        SetField(comp, "isMightUser", true);
        pawn.AllComps.Add(comp);
        TM_Calc.InvalidateCompCache(pawn);
        NewNeed<Need_Stamina>(pawn, new NeedDef { defName = "TM_Stamina" }).CurLevel = 1f;
        return comp;
    }

    private static T NewNeed<T>(Pawn pawn, NeedDef def) where T : Need
    {
        // Need constructors initialize UI thresholds/age curves. The real getter,
        // setter, tracker and condition evaluation are used, without graphics startup.
        var need = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        SetField(need, "pawn", pawn);
        need.def = def;
        pawn.needs.AllNeeds.Add(need);
        return need;
    }

    private static void Afflict(Pawn pawn, string name, bool injury = false)
    {
        var def = DefDatabase<HediffDef>.GetNamedSilentFail(name);
        if (def == null)
        {
            def = new HediffDef { defName = name, isBad = true, makesSickThought = !injury };
            DefDatabase<HediffDef>.Add(def);
        }
        Hediff hediff = injury ? new Hediff_Injury() : new Hediff();
        hediff.def = def;
        hediff.pawn = pawn;
        pawn.health.hediffSet.hediffs.Add(hediff);
    }

    private static T RealAbility<T>(CompAbilityUser comp, TMAbilityDef def) where T : PawnAbility, new()
    {
        def.MainVerb.verbClass = typeof(Verb_UseAbility);
        def.MainVerb.isViolent = false;
        var result = new T { AbilityUser = comp, Def = def };
        _ = result.Verb;
        return result;
    }

    // A narrow observation boundary: a rejected target must never request a cast
    // job. A typed sentinel avoids running the game scheduler after a baseline bug.
    private sealed class JobBoundaryReached : Exception { }
    private sealed class TendableInjury : Hediff_Injury
    {
        public override bool TendableNow(bool ignoreTimer = false) => true;
    }
    private sealed class JobBoundaryAbility : PawnAbility
    {
        public int JobRequests { get; private set; }
        public override Job GetJob(AbilityContext context, LocalTargetInfo target)
        {
            JobRequests++;
            throw new JobBoundaryReached();
        }
    }

    private static JobBoundaryAbility BoundaryAbility(CompAbilityUser comp, TMAbilityDef def) => new() { AbilityUser = comp, Def = def };

    private static TM_CustomPowerDef LoadHerbal(RuntimeFixture fixture, string name)
    {
        var document = XDocument.Load(Path.Combine(fixture.Repository, "v1.7/Defs/CustomDefs/TM_CustomPowerDef.xml"));
        var defaults = document.Root!.Elements().Single(element => (string?)element.Attribute("Name") == "CustomPower").Element("customPower")!;
        var power = document.Root.Elements().Single(element => (string?)element.Element("defName") == name).Element("customPower")!;
        var read = typeof(RuntimeFixture).GetMethod("ReadAutocast", BindingFlags.Static | BindingFlags.NonPublic)!;
        var definition = new TM_CustomPowerDef
        {
            defName = name,
            customPower = new TM_CustomPower
            {
                forFighter = true, forMage = false, skills = null!,
                abilityDefs = power.Element("abilityDefs")!.Elements("li").Select(element => (AbilityDef)fixture.AddAbility(element.Value)).ToList(),
                autocasting = (TM_Autocast)read.Invoke(null, [power.Element("autocasting"), defaults.Element("autocasting")])!
            }
        };
        fixture.AddDefinition(definition);
        return definition;
    }

    private static void LoadConditions(RuntimeFixture fixture, List<string> names)
    {
        var document = XDocument.Load(Path.Combine(fixture.Repository, "v1.7/Defs/CustomDefs/TM_AutocastConditionDefs.xml"));
        foreach (string name in names)
        {
            var node = document.Root!.Elements().Single(element => (string?)element.Element("defName") == name).Element("autocastCondition")!;
            var condition = new TM_AutocastCondition();
            foreach (var field in typeof(TM_AutocastCondition).GetFields())
            {
                var value = node.Element(field.Name);
                if (value == null || string.IsNullOrWhiteSpace(value.Value)) continue;
                object parsed;
                if (field.FieldType == typeof(List<HediffDef>))
                    parsed = value.Elements("li").Select(element =>
                    {
                        var def = DefDatabase<HediffDef>.GetNamedSilentFail(element.Value) ?? new HediffDef { defName = element.Value };
                        if (DefDatabase<HediffDef>.GetNamedSilentFail(element.Value) == null) DefDatabase<HediffDef>.Add(def);
                        return def;
                    }).ToList();
                else if (field.FieldType == typeof(List<NeedDef>))
                    parsed = value.Elements("li").Select(element => element.Value == "TM_Mana" ? TorannMagicDefOf.TM_Mana : MoodDef).ToList();
                else parsed = field.FieldType.IsEnum ? Enum.Parse(field.FieldType, value.Value) : Convert.ChangeType(value.Value, field.FieldType, CultureInfo.InvariantCulture);
                field.SetValue(condition, parsed);
            }
            DefDatabase<TM_AutocastConditionDef>.Add(new TM_AutocastConditionDef { defName = name, autocastCondition = condition });
        }
    }

    private static void SetField(object value, string name, object fieldValue)
    {
        for (Type? type = value.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(value, fieldValue);
            return;
        }
        throw new InvalidOperationException("Fixture field missing: " + value.GetType().Name + "." + name);
    }
}
