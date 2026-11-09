using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;

namespace RWoM.RegressionTests;

internal static class GrimoireProgressionTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Initialize()
    {
        RuntimeHelpers.RunClassConstructor(typeof(SoundDefOf).TypeHandle);
        RuntimeLog.AcceptHeadlessDefOfInitialization("SoundDefOf");
    }

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GrimoireProgression.StageTitlesPreserveLearnedAndHybridAccess", f =>
        {
            var previous = DefDatabase<TorannMagic.ClassProgression.ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var oldLanguage = LanguageDatabase.activeLanguage; var oldDefault = LanguageDatabase.defaultLanguage;
            try
            {
                var language = (LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
                language.folderName = "English";
                Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
                language.keyedReplacements = Directory.GetFiles(Path.Combine(f.Repository,"Languages/English/Keyed"),"*.xml")
                    .SelectMany(p=>System.Xml.Linq.XDocument.Load(p).Root!.Elements()).GroupBy(e=>e.Name.LocalName)
                    .ToDictionary(g=>g.Key,g=>new LoadedLanguage.KeyedReplacement {key=g.Key,value=g.First().Value});
                LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
                DefDatabase<TorannMagic.ClassProgression.ClassDisciplineDef>.Clear();
                var ability = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
                ability.defName = "StageTitleTest";
                var state = new TorannMagic.ClassProgression.ClassProgressionState(TorannMagic.ClassProgression.Vocation.Mage);
                state.TryAddFoundation("rwom.fire", TorannMagic.ClassProgression.DisciplineDomain.Magic);
                DefDatabase<TorannMagic.ClassProgression.ClassDisciplineDef>.Add(new TorannMagic.ClassProgression.ClassDisciplineDef() { defName="StageFire", profileId="rwom.fire", label="Fire",
                    spells=[new() {ability=ability, stage=TorannMagic.ClassProgression.InitiationStage.Master}] });
                var method = typeof(Window_TMGrimoire).GetMethod("StageSuffix", Flags)!;
                string Suffix(bool learned) => (string)method.Invoke(null, [state, new AbilityUser.AbilityDef[] { ability }, learned])!;
                Check.Equal(true, Suffix(false).Contains("TM_GrimoireStageTooLow".Translate().ToString()), "future ability explains stage lock");
                Check.Equal(false, Suffix(true).Contains("TM_GrimoireStageTooLow".Translate().ToString()), "retained learned spell never labelled locked");
                state.TryAddFoundation("rwom.ice", TorannMagic.ClassProgression.DisciplineDomain.Magic);
                DefDatabase<TorannMagic.ClassProgression.ClassDisciplineDef>.Add(new TorannMagic.ClassProgression.ClassDisciplineDef() {defName="StageIce",profileId="rwom.ice",label="Ice",
                    spells=[new() {ability=ability,stage=TorannMagic.ClassProgression.InitiationStage.Foundation}]});
                Check.Equal(false, Suffix(false).Contains("TM_GrimoireStageTooLow".Translate().ToString()), "either hybrid path can grant shared spell");
                Check.Equal("", (string)method.Invoke(null, [null, new AbilityUser.AbilityDef[] {ability}, false])!, "classic addon has no invented stage");
            }
            finally { LanguageDatabase.activeLanguage=oldLanguage; LanguageDatabase.defaultLanguage=oldDefault; DefDatabase<TorannMagic.ClassProgression.ClassDisciplineDef>.Clear(); foreach(var p in previous) DefDatabase<TorannMagic.ClassProgression.ClassDisciplineDef>.Add(p); }
        });
        yield return new("GrimoireProgression.NoviceUpgradeLabelsSevenLanguages", f =>
        {
            foreach (var language in new[] { "English", "Russian", "ChineseSimplified", "Portuguese", "PortugueseBrazilian", "Spanish", "Ukrainian" })
            {
                var entries = Directory.GetFiles(Path.Combine(f.Repository, "Languages", language, "Keyed"), "*.xml")
                    .SelectMany(path => System.Xml.Linq.XDocument.Load(path).Root!.Elements()).ToList();
                foreach (var spell in new[] { "ArcaneBolt", "MageLight", "Extinguish", "Heater", "Cooler", "SmokeCloud" })
                foreach (var upgrade in new[] { "pwr", "eff", "ver" })
                {
                    var key = "TM_" + spell + "_" + upgrade;
                    var labels = entries.Where(e => e.Name.LocalName == key).ToList();
                    Check.Equal(1, labels.Count, language + " has one label for " + key);
                    Check.Equal(true, !string.IsNullOrWhiteSpace(labels[0].Value) && labels[0].Value != key, "translated upgrade label");
                }
            }
        });
        yield return new("GrimoireProgression.MageWithInactiveMightComp", MageWithInactiveMight);
        yield return new("GrimoireProgression.FighterWithInactiveMagicComp", FighterWithInactiveMagic);
        yield return new("GrimoireProgression.LeveledSharedAbilityDeduplicates", LeveledSharedAbility);
        yield return new("GrimoireProgression.ClassWithoutBuiltinGroup", WithoutBuiltinGroup);
        yield return new("GrimoireProgression.FailedBuildCanRetry", FailedBuildCanRetry);
        yield return new("GrimoireProgression.MissingOppositeComp", MissingOppositeComp);
        yield return new("GrimoireProgression.InactiveSavedDataDoesNotRender", InactiveSavedData);
        yield return new("GrimoireProgression.CustomOverrideKeepsAggregateOwner", CustomOverride);
        foreach (var entry in TM_ClassGroups.MagicEntriesField)
        {
            var row = entry;
            foreach (int level in new[] { 0, 2 })
            {
                int rank = level;
                yield return new("GrimoireProgression.BuiltinMagic." + row.trait + ".Rank" + rank,
                    f => BuiltinMagic(f, row.trait, row.field, rank));
            }
        }
        foreach (var entry in TM_ClassGroups.MightEntriesField)
        {
            var row = entry;
            foreach (int level in new[] { 0, 2 })
            {
                int rank = level;
                yield return new("GrimoireProgression.BuiltinMight." + row.trait + ".Rank" + rank,
                    f => BuiltinMight(f, row.trait, row.list, rank));
            }
        }
    }

    internal static void Set(object target, string name, object? value)
    {
        for (Type? type = target as Type ?? target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Flags);
            if (field == null) continue;
            field.SetValue(target is Type ? null : target, value);
            return;
        }
        throw new InvalidOperationException("Missing fixture field " + name);
    }

    internal static void LoadBaseDefinitions(RuntimeFixture f)
    {
        foreach (var field in typeof(TorannMagicDefOf).GetFields(Flags))
        {
            if (field.FieldType == typeof(TMAbilityDef))
                field.SetValue(null, DefDatabase<TMAbilityDef>.GetNamedSilentFail(field.Name) ?? f.AddAbility(field.Name));
            else if (field.FieldType == typeof(TraitDef))
            {
                var trait = DefDatabase<TraitDef>.GetNamedSilentFail(field.Name);
                if (trait == null)
                {
                    trait = new TraitDef { defName = field.Name };
                    DefDatabase<TraitDef>.Add(trait);
                }
                field.SetValue(null, trait);
            }
        }
    }

    internal static (Pawn pawn, CompAbilityUserMagic magic, CompAbilityUserMight might) PawnWithComps(bool mage, bool fighter)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "GrimoireProgressionHuman";
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = def, Name = new NameSingle("ProgressionPawn") };
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.needs = new Pawn_NeedsTracker(pawn);
        var magic = new CompAbilityUserMagic { parent = pawn, customIndex = -1 };
        var might = new CompAbilityUserMight { parent = pawn, customIndex = -1 };
        Set(pawn, "comps", new List<ThingComp> { magic, might });
        Set(magic, "initializedIsMagicUser", true);
        Set(magic, "isMagicUser", mage);
        Set(might, "initializedIsMightUser", true);
        Set(might, "isMightUser", fighter);
        if (mage) Set(magic, "magicData", new MagicData(magic));
        if (fighter) Set(might, "mightData", new MightData(might));
        TM_Calc.InvalidateCompCache(pawn);
        return (pawn, magic, might);
    }

    internal static Window_TMGrimoire Build(Pawn pawn)
    {
        var window = new Window_TMGrimoire(pawn);
        try { typeof(Window_TMGrimoire).GetMethod("EnsureFilters", Flags)!.Invoke(window, null); }
        catch (TargetInvocationException e) { throw e.InnerException!; }
        Check.Equal(true, (bool)typeof(Window_TMGrimoire).GetField("filtersBuilt", Flags)!.GetValue(window)!, "build finishes");
        return window;
    }

    internal static HashSet<MagicPower> MagicRows(Window_TMGrimoire window) =>
        Check.NotNull((HashSet<MagicPower>?)typeof(Window_TMGrimoire).GetField("magicRenderable", Flags)!.GetValue(window), "magic rows published");
    internal static HashSet<MightPower> MightRows(Window_TMGrimoire window) =>
        Check.NotNull((HashSet<MightPower>?)typeof(Window_TMGrimoire).GetField("mightRenderable", Flags)!.GetValue(window), "might rows published");

    static void MageWithInactiveMight(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        var window = Build(p.pawn);
        Check.Equal(0, MightRows(window).Count, "inactive might side stays empty");
        Check.Equal(true, MagicRows(window).Count > 0, "Warlock progression exists");
        Build(p.pawn);
    }

    static void FighterWithInactiveMagic(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(false, true);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Commander));
        var window = Build(p.pawn);
        Check.Equal(0, MagicRows(window).Count, "inactive magic side stays empty");
        Check.Equal(true, MightRows(window).Count > 0, "Commander progression exists");
    }

    static void BuiltinMagic(RuntimeFixture f, string traitName, string fieldName, int rank)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(DefDatabase<TraitDef>.GetNamed(traitName)));
        var data = p.magic.MagicData;
        Check.Same(null!, typeof(MagicData).GetField(fieldName)!.GetValue(data), "class list starts lazy");
        if (rank > 0)
            foreach (var power in TM_ClassGroups.GetMagicClassGroup(p.pawn, data))
                power.level = Math.Min(rank, power.TMabilityDefs.Count - 1);
        var window = Build(p.pawn);
        var rows = MagicRows(window);
        var expected = Check.NotNull(TM_ClassGroups.GetMagicClassGroup(p.pawn, data), traitName + " class group");
        Check.Equal(true, expected.Count > 0, traitName + " has class powers");
        foreach (var power in expected)
            Check.Equal(true, rows.Contains(power), traitName + " exposes " + power.TMabilityDefs[0].defName);
        Check.Equal(rows.Count, rows.Select(x => x.TMabilityDefs[0]).Distinct().Count(), traitName + " no duplicate families");
        Check.Equal(true, rows.Any(x => x.maxLevel > 0 || data.GetSkill_Power((TMAbilityDef)x.abilityDef) != null), traitName + " has upgradeable skills");
    }

    static void BuiltinMight(RuntimeFixture f, string traitName, string propertyName, int rank)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(false, true);
        p.pawn.story.traits.allTraits.Add(new Trait(DefDatabase<TraitDef>.GetNamed(traitName)));
        if (rank > 0)
            foreach (var power in TM_ClassGroups.GetMightClassGroup(p.pawn, p.might.MightData))
                power.level = Math.Min(rank, power.TMabilityDefs.Count - 1);
        var window = Build(p.pawn);
        var rows = MightRows(window);
        var expected = Check.NotNull(TM_ClassGroups.GetMightClassGroup(p.pawn, p.might.MightData), traitName + " class group");
        Check.Equal(true, expected.Count > 0, traitName + " has class powers");
        foreach (var power in expected)
            Check.Equal(true, rows.Contains(power), traitName + " exposes " + power.TMabilityDefs[0].defName);
        Check.Equal(rows.Count, rows.Select(x => x.TMabilityDefs[0]).Distinct().Count(), traitName + " no duplicate families");
        Check.Equal(true, rows.Any(x => x.maxLevel > 0 || p.might.MightData.GetSkill_Power((TMAbilityDef)x.abilityDef) != null), traitName + " has upgradeable skills");
    }

    static void MissingOppositeComp(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var mage = PawnWithComps(true, false);
        mage.pawn.AllComps.Remove(mage.might);
        TM_Calc.InvalidateCompCache(mage.pawn);
        mage.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        Check.Equal(true, MagicRows(Build(mage.pawn)).Count > 0, "mage needs no might comp");
        var fighter = PawnWithComps(false, true);
        fighter.pawn.AllComps.Remove(fighter.magic);
        TM_Calc.InvalidateCompCache(fighter.pawn);
        fighter.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Commander));
        Check.Equal(true, MightRows(Build(fighter.pawn)).Count > 0, "fighter needs no magic comp");
    }

    static void InactiveSavedData(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        Set(p.might, "mightData", new MightData(p.might));
        var window = Build(p.pawn);
        Check.Equal(0, MightRows(window).Count, "stale saved opposite-side data does not expose a class section");
    }

    static void CustomOverride(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        p.magic.customClass = new TM_CustomClass { classMageAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_ShadowBolt } };
        var expected = p.magic.MagicData.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        var selected = MagicRows(Build(p.pawn)).Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        Check.Same(expected, selected, "custom assignment takes precedence over builtin trait owner");
    }

    static void FailedBuildCanRetry(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
        Set(p.magic.MagicData, "magicPowerSkill_global_regen", new List<MagicPowerSkill>());
        var window = new Window_TMGrimoire(p.pawn);
        bool failed = false;
        try { typeof(Window_TMGrimoire).GetMethod("EnsureFilters", Flags)!.Invoke(window, null); }
        catch (TargetInvocationException) { failed = true; }
        Check.Equal(true, failed, "incomplete data triggers fixture failure");
        Check.Equal(false, (bool)typeof(Window_TMGrimoire).GetField("filtersBuilt", Flags)!.GetValue(window)!, "failed build not published");
        Set(p.magic.MagicData, "magicPowerSkill_global_regen", null);
        typeof(Window_TMGrimoire).GetMethod("EnsureFilters", Flags)!.Invoke(window, null);
        Check.Equal(true, MagicRows(window).Count > 0, "next build recovers on same window");
    }

    static void WithoutBuiltinGroup(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, true);
        p.magic.customClass = new TM_CustomClass { classMageAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_SoulBond } };
        p.might.customClass = new TM_CustomClass { classFighterAbilities = new List<TMAbilityDef> { TorannMagicDefOf.TM_Sprint } };
        var window = Build(p.pawn);
        Check.Equal(true, MagicRows(window).Any(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_SoulBond)), "non-builtin magic progression exists");
        Check.Equal(true, MightRows(window).Any(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_Sprint)), "non-builtin might progression exists");
    }

    static void LeveledSharedAbility(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Succubus));
        var data = p.magic.MagicData;
        var own = data.MagicPowersSD.Single(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt);
        own.level = 2;
        own.learned = true;
        var window = Build(p.pawn);
        var rows = MagicRows(window).Where(x => x.TMabilityDefs[0] == TorannMagicDefOf.TM_ShadowBolt).ToList();
        Check.Equal(1, rows.Count, "leveled family renders exactly once");
        Check.Same(own, rows[0], "saved Succubus owner retained");
        Check.Equal(2, own.level, "saved investment untouched");
    }
}
