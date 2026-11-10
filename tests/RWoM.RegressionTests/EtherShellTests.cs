using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.SpellItems;
using Verse;
using TorannMagic.Odyssey;

namespace RWoM.RegressionTests;

internal static partial class SpellItemEffectTests
{
    static IEnumerable<TestCase> EtherShellCases()
    {
        yield return new("Odyssey.SpaceSpells.ResearchVisibilityAndRetainedKnowledge", f => World(f, w =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
            var old = flag.GetValue(null);
            var research = Current.Game.researchManager;
            var orbit = new ResearchProjectDef { defName = "OrbitalTech", baseCost = 100 };
            var scribing = new ResearchProjectDef { defName = "TM_AdvancedScribing", baseCost = 100 };
            using var defs = new DefinitionScope<ResearchProjectDef>(orbit);
            using var scribingDefs = new DefinitionScope<ResearchProjectDef>(scribing);
            RuntimeLog.AssertClean();
            try
            {
                flag.SetValue(null, true);
                Current.Game.researchManager = Blank<ResearchManager>();
                var progress = new Dictionary<ResearchProjectDef,float>();
                Set(Current.Game.researchManager, "progress", progress);
                RuntimeLog.AssertClean();
                var spell = Blank<TMAbilityDef>(); spell.defName = "TM_EtherShell";
                using var spellDef = new DefinitionScope<TMAbilityDef>(spell);
                var power = new MagicPower(new List<AbilityUser.AbilityDef> { spell });
                RuntimeLog.AssertClean();
                var mage = GrimoireProgressionTests.PawnWithComps(true, false);
                mage.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
                mage.magic.MagicData.MagicPowersCustom.Add(power);
                var grimoire = GrimoireProgressionTests.Build(mage.pawn);
                Check.Equal(false, GrimoireProgressionTests.MagicRows(grimoire).Contains(power), "actual grimoire hides locked research");
                Check.Equal(false, SpaceSpellProgression.Visible(power), "hidden before technology");
                progress[scribing] = 100;
                Check.Equal(false, SpaceSpellProgression.Visible(power), "scribing alone cannot unlock space spell");
                progress[orbit] = 100;
                Check.Equal(true, SpaceSpellProgression.Visible(power), "both technologies unlock learning");
                typeof(Window_TMGrimoire).GetMethod("EnsureFilters", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(grimoire, null);
                Check.Equal(true, GrimoireProgressionTests.MagicRows(grimoire).Contains(power), "open grimoire refreshes after research");
                progress.Clear(); power.learned = true;
                Check.Equal(true, SpaceSpellProgression.Visible(power), "existing learned spell remains usable and visible");
                power.learned = false; var fireball = Blank<TMAbilityDef>(); fireball.defName = "TM_Fireball"; power.TMabilityDefs[0] = fireball;
                Check.Equal(true, SpaceSpellProgression.Visible(power), "unrelated class spell is unaffected");
            }
            finally { Current.Game.researchManager = research; flag.SetValue(null, old); }
        }));
        yield return new("Odyssey.SpaceSpells.DurationSkillsAndRecipeResearch", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            foreach (string name in new[] { "EtherShell", "BreachSeal" })
            {
                var xml = System.Xml.Linq.XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs/" + name + ".xml"));
                var node = xml.Root!.Element("TorannMagic.TM_CustomPowerDef")!;
                var ability = Blank<TMAbilityDef>(); ability.defName = "TM_" + name;
                using var abilities = new DefinitionScope<TMAbilityDef>(ability);
                var skill = node.Descendants("skills").Elements("li").Single(e => e.Element("label")!.Value.EndsWith("_ver"));
                var custom = new TM_CustomPowerDef { defName = "TM_" + name + "Power", customPower = new TorannMagic.TMDefs.TM_CustomPower
                {
                    forMage = true, abilityDefs = [ability], skills = [new TorannMagic.TMDefs.TM_CustomSkill
                    { label = skill.Element("label")!.Value, description = skill.Element("description")!.Value, levelMax = (int)skill.Element("levelMax")!, costToLevel = 1 }]
                } };
                using var powers = new DefinitionScope<TM_CustomPowerDef>(custom);
                var pawn = GrimoireProgressionTests.PawnWithComps(true, false);
                var duration = pawn.magic.MagicData.GetSkill_Versatility(ability);
                Check.NotNull(duration, "new duration skill discovered without deleting old data");
                for (int level = 0; level <= 3; level++)
                {
                    duration.level = level;
                    Check.Equal(60000 + 15000 * level, SpaceSpellProgression.Duration(pawn.pawn, ability.defName), "duration per rank");
                }
                foreach (var recipe in xml.Root.Elements("RecipeDef").Concat(xml.Root.Elements("ThingDef").Elements("recipeMaker")))
                {
                    var requirements = recipe.Element("researchPrerequisites")!.Elements("li").Select(e => e.Value)
                        .Append(recipe.Element("researchPrerequisite")!.Value).ToArray();
                    Check.Equal(true, requirements.Contains("OrbitalTech") && requirements.Contains("TM_AdvancedScribing"), "craft and grimoire unlock together");
                }
            }
        });
        yield return new("Odyssey.EtherShell.NativeManaAndCooldownSettleOnce", f => World(f, w =>
        {
            var previousSettings = TorannMagic.ModOptions.Settings.Instance;
            var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = flag.GetValue(null);
            try
            {
                flag.SetValue(null, true);
                TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings();
                GrimoireProgressionTests.LoadBaseDefinitions(f);
                w.Map.gameConditionManager = new GameConditionManager(w.Map);
                // The native payment method may already have inlined Find's getter in a
                // prior test. Supply its real backing state instead of patching the getter.
                Current.Game.history = Blank<History>();
                Current.Game.history.historyEventsManager = Blank<HistoryEventsManager>();
                var pawn = w.Caster;
                pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
                pawn.needs = new Pawn_NeedsTracker(pawn); pawn.apparel = new Pawn_ApparelTracker(pawn);
                pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000);
                var magic = new CompAbilityUserMagic { parent = pawn }; pawn.AllComps.Add(magic); TM_Calc.InvalidateCompCache(pawn);
                Set(magic, "magicData", new MagicData());
                var mana = new Need_Mana(pawn); pawn.needs.AllNeeds.Add(mana); mana.CurLevel = .9f;
                using var syrrium = DefDatabase<HediffDef>.GetNamedSilentFail("TM_SyrriumSenseHD") == null
                    ? new DefinitionScope<HediffDef>(new HediffDef { defName = "TM_SyrriumSenseHD" }) : null;
                var def = new HediffDef { defName = "TM_EtherShellHD", hediffClass = typeof(Hediff_EtherShell), initialSeverity = 1,
                    comps = [new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange(60000, 60000) }] };
                using var definitions = new DefinitionScope<HediffDef>(def);
                using var patches = w.HealingBoundaries();
                patches.Prefix(typeof(HistoryEventsManager).GetMethod(nameof(HistoryEventsManager.RecordEvent))!, nameof(SkipSeerVisual));
                patches.Prefix(typeof(GameConditionManager).GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetMethod!, nameof(ShellNoParentConditions));
                // Admission is separately exercised above; this test preserves the full native settlement path.
                patches.Prefix(typeof(MagicAbility).GetMethod(nameof(MagicAbility.CanCastPowerCheck))!, nameof(ShellResourcesAvailable));
                shellResources = true;
                var abilityDef = f.AddAbility("TM_EtherShellPayment"); abilityDef.manaCost = .25f; abilityDef.consumeEnergy = true;
                abilityDef.MainVerb.SecondsToRecharge = 60;
                var ability = new EtherShellAbility(magic) { Def = abilityDef };
                var verb = new ShellTestVerb { caster = pawn, Ability = ability,
                    verbProps = new AbilityUser.VerbProperties_Ability { range = 12, targetParams = new TargetingParameters { canTargetSelf = true } } };
                Check.Same(pawn, ability.Pawn, "native payment owns caster");
                Check.Same(magic, ability.MagicUser, "native payment uses current caster component");
                Check.Equal(true, verb.Fire(pawn), "real shell applied");
                Check.Equal(true, Math.Abs(.65f - mana.CurLevel) < .00001f, "real mana charged exactly quarter pool synchronously");
                int cooldown = ability.CooldownTicksLeft;
                Check.Equal(true, cooldown > 0, "native cooldown started immediately");
                ability.PostAbilityAttempt(); ability.PostAbilityAttempt();
                Check.Equal(true, Math.Abs(.65f - mana.CurLevel) < .00001f, "finish callbacks leave real mana unchanged");
                Check.Equal(cooldown, ability.CooldownTicksLeft, "finish callbacks do not restart cooldown");
                Check.Equal(1, magic.MagicUsed.Count, "one native magic-use record");
            }
            finally { flag.SetValue(null, previous); TorannMagic.ModOptions.Settings.Instance = previousSettings; }
        }));
        yield return new("Odyssey.EtherShell.VerbSettlesBeforeInterruptionExactlyOnce", f => World(f, w =>
        {
            var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = flag.GetValue(null);
            try
            {
                flag.SetValue(null, true);
                var def = new HediffDef { defName = "TM_EtherShellHD", hediffClass = typeof(Hediff_EtherShell), initialSeverity = 1,
                    comps = [new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange(60000, 60000) }] };
                using var definitions = new DefinitionScope<HediffDef>(def);
                using var patches = w.HealingBoundaries();
                patches.Prefix(typeof(MagicAbility).GetMethod(nameof(MagicAbility.PostAbilityAttempt))!, nameof(RecordShellPayment));
                patches.Prefix(typeof(MagicAbility).GetMethod(nameof(MagicAbility.CanCastPowerCheck))!, nameof(ShellResourcesAvailable));
                shellPayments = 0;
                var ability = new EtherShellAbility();
                var verb = new ShellTestVerb { caster = w.Caster, Ability = ability,
                    verbProps = new AbilityUser.VerbProperties_Ability { range = 12, targetParams = new TargetingParameters { canTargetSelf = true } } };
                shellResources = false;
                Check.Equal(false, verb.Fire(w.Caster), "resource loss during warmup prevents effect");
                Check.Equal(0, w.Caster.health.hediffSet.hediffs.Count, "no free shell with unavailable resources");
                shellResources = true;
                Check.Equal(true, verb.Fire(w.Caster), "native verb applies self shell");
                Check.Equal(1, shellPayments, "paid before job cleanup or interruption");
                ability.PostAbilityAttempt(); ability.PostAbilityAttempt();
                Check.Equal(1, shellPayments, "finish callbacks cannot duplicate payment");
                Check.Equal(false, verb.Fire(w.Caster), "already protected target rejects at completion");
                ability.PostAbilityAttempt();
                Check.Equal(1, shellPayments, "rejected repeat creates no payment receipt");
            }
            finally { flag.SetValue(null, previous); }
        }));
        yield return new("Odyssey.EtherShell.DlcGuardAndNoDuplicatePayment", f => World(f, w =>
        {
            var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = flag.GetValue(null);
            try
            {
                var type = typeof(CompAbilityUserMagic).Assembly.GetType("TorannMagic.Odyssey.Hediff_EtherShell");
                Check.Equal(true, type != null, "shell native hediff exists");
                var def = new HediffDef { defName = "TM_EtherShellHD", hediffClass = type!, initialSeverity = 1, maxSeverity = 1,
                    comps = [new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange(60000, 60000) }] };
                using var definitions = new DefinitionScope<HediffDef>(def);
                using var patches = w.HealingBoundaries();
                var props = Props((SpellItemKind)10);
                flag.SetValue(null, false);
                Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "no effect without Odyssey");
                flag.SetValue(null, true);
                Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "ally gains shell");
                var shell = w.Patient.health.hediffSet.GetFirstHediffOfDef(def);
                Check.Equal(60000, shell.TryGetComp<HediffComp_Disappears>().ticksToDisappear, "one day duration");
                Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "duplicate not paid");
                Check.Equal(1, w.Patient.health.hediffSet.hediffs.Count(h => h.def == def), "no stack");
                Check.Equal(true, SpellItemEffects.TryApply(w.Caster, w.Caster, props), "self cast supported");
                w.Wall();
                w.Patient.health.hediffSet.hediffs.Clear();
                Check.Equal(false, SpellItemEffects.TryApply(w.Caster, w.Patient, props), "wall prevents shell");
            }
            finally { flag.SetValue(null, previous); }
        }));
        foreach (int duration in new[] { 60000, 105000 })
        yield return new("Odyssey.EtherShell.NativeStatsCacheExpiryAndSave." + duration, f => World(f, w =>
        {
            var flag = typeof(ModsConfig).GetField("odysseyActive", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = flag.GetValue(null);
            var oldVacuum = StatDefOf.VacuumResistance;
            var oldCold = StatDefOf.ComfyTemperatureMin;
            try
            {
                flag.SetValue(null, true);
                var vacuum = new StatDef { defName = "VacuumResistance", defaultBaseValue = 0, cacheable = true, minValue = 0, maxValue = 1 };
                var cold = new StatDef { defName = "ComfyTemperatureMin", defaultBaseValue = 0, cacheable = true, minValue = -2000, maxValue = 2000 };
                vacuum.Worker.SetCacheability(false); cold.Worker.SetCacheability(false);
                using var vacuumScope = new DefinitionScope<StatDef>(vacuum);
                using var coldScope = new DefinitionScope<StatDef>(cold);
                StatDefOf.VacuumResistance = vacuum; StatDefOf.ComfyTemperatureMin = cold;
                var pawn = w.Patient;
                pawn.def.race.lifeStageAges = [new LifeStageAge { def = new LifeStageDef() }];
                pawn.ageTracker = new Pawn_AgeTracker(pawn); Set(pawn.ageTracker, "cachedLifeStageIndex", 0);
                var def = new HediffDef { defName = "TM_EtherShellHD", hediffClass = typeof(Hediff_EtherShell), initialSeverity = 1, maxSeverity = 1,
                    comps = [new HediffCompProperties_Disappears { disappearsAfterTicks = new IntRange(60000, 60000) }],
                    stages = [new HediffStage { preventVacuumBurns = true,
                        statOffsets = [new StatModifier { stat = vacuum, value = 1 }, new StatModifier { stat = cold, value = -300 }] }] };
                using var definitions = new DefinitionScope<HediffDef>(def);
                using var patches = w.HealingBoundaries();
                var wound = Injury(pawn, Part("Scar"), 9);
                Check.Equal(true, pawn.HarmedByVacuum, "prime unprotected native cache");
                Check.Equal(0f, pawn.GetStatValue(cold, cacheStaleAfterTicks: 60), "prime cold cache");
                Check.Equal(true, EtherShell.TryApply(w.Caster, pawn, duration), "apply selected duration");
                Check.Equal(false, pawn.HarmedByVacuum, "immediate native vacuum immunity");
                Check.Equal(false, pawn.ConcernedByVacuum, "native AI sees protection");
                Check.Equal(-300f, pawn.GetStatValue(cold, cacheStaleAfterTicks: 60), "immediate native cold stat");
                Check.Equal(9f, wound.Severity, "shell does not heal existing wounds");
                var shell = (HediffWithComps)pawn.health.hediffSet.GetFirstHediffOfDef(def);
                var expiry = shell.TryGetComp<HediffComp_Disappears>();
                for (int i = 0; i < 1234; i++) shell.PostTick();
                var saved = new ShellExpirySave { Expiry = expiry };
                string path = Path.Combine(f.WorkingDirectory, "ether-shell-expiry.xml");
                try { Scribe.saver.InitSaving(path, "regression"); saved.ExposeData(); Scribe.saver.FinalizeSaving(); }
                finally { Scribe.ForceStop(); }
                var restored = new ShellExpirySave { Expiry = new HediffComp_Disappears { parent = shell, props = expiry.props } };
                try { Scribe.loader.InitLoading(path); restored.ExposeData(); Scribe.loader.FinalizeLoading(); }
                finally { Scribe.ForceStop(); }
                Check.Equal(duration - 1234, restored.Expiry.ticksToDisappear, "save preserves remaining duration");
                Check.Equal(duration, restored.Expiry.disappearsAfterTicks, "save preserves original duration");
                for (int i = 1234; i < duration; i++) shell.PostTick();
                Check.Equal(true, shell.ShouldRemove, "native expiry requests removal at one day");
                pawn.health.RemoveHediff(shell);
                Check.Equal(true, pawn.HarmedByVacuum, "same tick expiration clears native cached immunity");
                Check.Equal(true, pawn.ConcernedByVacuum, "native AI resumes concern");
                Check.Equal(0f, pawn.GetStatValue(cold, cacheStaleAfterTicks: 60), "same tick expiration removes cold offset");
            }
            finally { StatDefOf.VacuumResistance = oldVacuum; StatDefOf.ComfyTemperatureMin = oldCold; flag.SetValue(null, previous); }
        }));
    }

    public sealed class ShellExpirySave : IExposable
    {
        public HediffComp_Disappears Expiry = null!;
        public void ExposeData() => Expiry.CompExposeData();
    }
    static int shellPayments;
    static bool ShellNoParentConditions(ref GameConditionManager __result) { __result = null!; return false; }
    static bool shellResources;
    static bool ShellResourcesAvailable(ref bool __result, ref string reason) { reason = ""; __result = shellResources; return false; }
    static bool RecordShellPayment() { shellPayments++; return false; }
    sealed class ShellTestVerb : Verb_EtherShell
    {
        public bool Fire(Pawn target) { currentTarget = target; return TryCastShot(); }
    }
}


