using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static class InitializationTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Initialization.FacelessManaConstructorAndSharedNeedVisibility", ManaConstruction);
        yield return new("Initialization.ShippedEmpathyRealXmlDiscovery", RealEmpathy);
        yield return new("Initialization.ShippedEmpathHediffInitialize", f => EmpathHediff(f));
        yield return new("Initialization.RussianEmpathHediffInitialize", f => EmpathHediff(f, true));
        yield return new("Initialization.LegacyRussianEmpathyPreservesInvestment", LegacyRussianEmpathy);
        yield return new("Initialization.LegacyRussianAllKnownAliases", LegacyAllAliases);
        yield return new("Initialization.LegacyRussianFighterKeepsSavedOwner", LegacyFighter);
        yield return new("Initialization.CustomSkillIdentifiersRejectTranslation", RejectIdentifierTranslation);
        yield return new("Initialization.PreleveledInnerFireReassignment", f => InnerFire(f, 1));
        yield return new("Initialization.UnleveledInnerFireControl", f => InnerFire(f, 0));
        foreach (var item in new[] {
            ("InnerFire", "MagicPowersIF", "TM_Fireball"),
            ("HeartOfFrost", "MagicPowersHoF", "TM_Icebolt"),
            ("StormBorn", "MagicPowersSB", "TM_LightningBolt"),
            ("Arcanist", "MagicPowersA", "TM_MagicMissile"),
            ("Paladin", "MagicPowersP", "TM_Heal"),
            ("Summoner", "MagicPowersS", "TM_SummonMinion"),
            ("Druid", "MagicPowersD", "TM_Regenerate"),
            ("Necromancer", "MagicPowersN", "TM_DeathMark"),
            ("Priest", "MagicPowersPR", "TM_AdvancedHeal"),
            ("TM_Bard", "MagicPowersB", "TM_BardTraining"),
            ("Succubus", "MagicPowersSD", "TM_ShadowBolt"),
            ("Warlock", "MagicPowersWD", "TM_ShadowBolt"),
            ("Geomancer", "MagicPowersG", "TM_EarthSprites"),
            ("Technomancer", "MagicPowersT", "TM_TechnoShield"),
            ("BloodMage", "MagicPowersBM", "TM_IgniteBlood"),
            ("Enchanter", "MagicPowersE", "TM_Transmutate") })
        {
            var test = item;
            yield return new("Initialization.LeveledClass." + test.Item1, f => LeveledClass(f, test.Item1, test.Item2, test.Item3));
        }
    }

    static void Set(object target, string field, object value)
    {
        for (Type? type = target as Type ?? target.GetType(); type != null; type = type.BaseType)
        {
            var member = type.GetField(field, Flags);
            if (member != null) { member.SetValue(target is Type ? null : target, value); return; }
        }
        throw new InvalidOperationException("Missing fixture field " + field);
    }

    static Pawn Pawn(bool faceless = false)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "InitializationHuman";
        def.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = def, Name = new NameSingle("InitializationPawn") };
        pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        pawn.ageTracker = new Pawn_AgeTracker(pawn);
        Set(pawn.ageTracker, "ageBiologicalTicksInt", 20L * 3600000L);
        Set(pawn, "comps", new List<ThingComp> { new CompCanBeDormant { parent = pawn } });
        if (faceless) pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless));
        pawn.needs = new Pawn_NeedsTracker(pawn);
        return pawn;
    }

    static void ManaConstruction(RuntimeFixture f)
    {
        TorannMagicDefOf.Faceless = new TraitDef { defName = "Faceless" };
        var shared = new NeedDef { defName = "TM_Mana", needClass = typeof(Need_Mana), showOnNeedList = true };
        var faceless = Pawn(true);
        typeof(Pawn_NeedsTracker).GetMethod("AddNeed", Flags)!.Invoke(faceless.needs, [shared]);
        var hidden = faceless.needs.AllNeeds.Single();
        RuntimeLog.AssertClean();
        Check.Equal(.8f, hidden.CurLevel, "Faceless mana initializes during the base constructor");
        Check.Equal(false, hidden.ShowOnNeedList, "Faceless mana is hidden for this pawn");
        Check.Equal(true, shared.showOnNeedList, "Pawn-specific visibility never mutates a shared NeedDef");
        var ordinary = Pawn();
        typeof(Pawn_NeedsTracker).GetMethod("AddNeed", Flags)!.Invoke(ordinary.needs, [shared]);
        Check.Equal(true, ordinary.needs.AllNeeds.Single().ShowOnNeedList, "Other pawns retain visible mana");
    }

    static void RealEmpathy(RuntimeFixture f)
    {
        LoadCustomDefinitions(f);
        TorannMagicDefOf.TM_Empathy = f.Ability("TM_Empathy");
        var data = new MagicData();
        var power = Check.NotNull(data.GetSkill_Power(TorannMagicDefOf.TM_Empathy), "Real XML Empathy power skill");
        Check.Equal(5, power.levelMax, "Shipped XML skill maximum");
        Check.NotNull(data.GetSkill_Efficiency(TorannMagicDefOf.TM_Empathy), "Real XML Empathy efficiency");
        Check.NotNull(data.GetSkill_Versatility(TorannMagicDefOf.TM_Empathy), "Real XML Empathy versatility");
    }

    static void LoadCustomDefinitions(RuntimeFixture f)
    {
        DefDatabase<TM_CustomPowerDef>.Clear();
        DirectXmlCrossRefLoader.Clear();
        XmlInheritance.Clear();
        var doc = new XmlDocument();
        doc.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomPowerDef.xml"));
        foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
            if (node.NodeType == XmlNodeType.Element) XmlInheritance.TryRegister(node, null);
        XmlInheritance.Resolve();
        foreach (XmlNode node in doc.DocumentElement!.ChildNodes)
        {
            if (node.NodeType != XmlNodeType.Element || node.Attributes?["Abstract"]?.Value == "True") continue;
            foreach (XmlNode ability in XmlInheritance.GetResolvedNodeFor(node).SelectNodes("customPower/abilityDefs/li")!)
                if (DefDatabase<TMAbilityDef>.GetNamedSilentFail(ability.InnerText) == null) f.AddAbility(ability.InnerText);
            DefDatabase<TM_CustomPowerDef>.Add(DirectXmlToObject.ObjectFromXml<TM_CustomPowerDef>(node, true));
        }
        DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
        RuntimeLog.AssertClean();
    }

    static void EmpathHediff(RuntimeFixture f, bool russian = false)
    {
        LoadCustomDefinitions(f);
        if (russian) InjectRussianSkills(f);
        TorannMagicDefOf.TM_Empathy = f.Ability("TM_Empathy");
        var pawn = Pawn();
        pawn.health = new Pawn_HealthTracker(pawn);
        var comp = new CompAbilityUserMagic { parent = pawn, customIndex = -1 };
        pawn.AllComps.Add(comp);
        TM_Calc.InvalidateCompCache(pawn);
        Set(comp, "initializedIsMagicUser", true);
        Set(comp, "isMagicUser", true);
        var data = new MagicData(comp);
        Set(comp, "magicData", data);
        Current.Game ??= (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        Set(Current.Game, "maps", new List<Map> { (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map)) });
        Set(pawn, "mapIndexOrState", (sbyte)0);
        Set(pawn.needs, "mood", RuntimeHelpers.GetUninitializedObject(typeof(Need_Mood)));
        var hediff = new HediffWithComps { pawn = pawn, def = new HediffDef { defName = "TM_EmpathHD" } };
        var empathy = new HediffComp_Empath { parent = hediff };
        typeof(HediffComp_Empath).GetMethod("Initialize", Flags)!.Invoke(empathy, null);
        Check.Equal(.1f, hediff.Severity, "Fresh real-XML Empath initializes without losing its hediff");
        var pwr = Check.NotNull(data.GetSkill_Power(TorannMagicDefOf.TM_Empathy), "Empath canonical skill");
        pwr.level = 4;
        typeof(HediffComp_Empath).GetMethod("Initialize", Flags)!.Invoke(empathy, null);
        Check.Equal(.1f + .17f * 4, hediff.Severity, "Empath uses the invested canonical level");
        Check.Same(pwr, data.magicPowerSkill_Custom.Single(skill => skill.label == "TM_Empathy_pwr"), "Empath does not replace the save owner");
    }

    static void InjectRussianSkills(RuntimeFixture f)
    {
        var package = new DefInjectionPackage(typeof(TM_CustomPowerDef));
        string file = Path.Combine(f.Repository, "Languages/Russian/DefInjected/TorannMagic.TM_CustomPowerDef/TM_CustomPowerDef_ExtractedMissing.xml");
        foreach (var element in XDocument.Load(file).Root!.Elements())
            package.injections.Add(element.Name.LocalName, new DefInjectionPackage.DefInjection {
                path = element.Name.LocalName, injection = element.Value, fileSource = file
            });
        package.InjectIntoDefs(true);
        Check.Equal(0, package.loadErrors.Count, "Shipped Russian injections resolve without errors");
        Check.Equal(true, package.injections.Values.All(value => value.injected), "Actual engine injects every shipped custom skill translation");
    }

    static void LegacyRussianEmpathy(RuntimeFixture f)
    {
        var localized = new MagicPowerSkill("Позитивное мышление", "saved description") { level = 4, levelMax = 9, costToLevel = 7 };
        var canonical = new MagicPowerSkill("TM_Empathy_pwr", "canonical description") { level = 1 };
        var data = f.LoadLegacy(localized, canonical);
        var skill = Check.NotNull(data.GetSkill_Power(f.Ability("TM_Empathy")), "Known Russian saved alias resolves after a language switch");
        Check.Equal(4, skill.level, "Highest saved Russian investment survives canonical duplicate");
        Check.Equal(9, skill.levelMax, "Saved maximum remains attached to invested owner");
        Check.Equal(7, skill.costToLevel, "Saved cost remains attached to invested owner");
        Check.Equal(1, data.magicPowerSkill_Custom.Count(s => s.label == "TM_Empathy_pwr"), "Canonical persistence has one saved owner");
        var reloaded = f.Load(f.Save(data));
        Check.Equal(4, reloaded.GetSkill_Power(f.Ability("TM_Empathy")).level, "Canonical investment survives the next save");
    }

    static void LegacyAllAliases(RuntimeFixture f)
    {
        var aliases = XDocument.Load(Path.Combine(f.Repository, "tests/fixtures/legacy-russian-custom-skill-labels.xml")).Root!.Elements().ToList();
        var skills = aliases.Select(a => new MagicPowerSkill(a.Value, "saved metadata") { level = 4, levelMax = 8, costToLevel = 6 }).ToArray();
        var data = f.LoadLegacy(skills.Concat(new[] { new MagicPowerSkill("unknown third-party skill", "unknown") { level = 17 } }).ToArray());
        foreach (var alias in aliases)
        {
            string canonical = (string)alias.Attribute("canonical")!;
            var skill = data.AllMagicPowerSkills.Single(s => s.label == canonical);
            Check.Equal(4, skill.level, canonical + " retains the historical investment");
            Check.Equal(8, skill.levelMax, canonical + " retains saved maximum");
            Check.Equal(6, skill.costToLevel, canonical + " retains saved cost");
        }
        Check.Equal(17, data.magicPowerSkill_Custom.Single(s => s.label == "unknown third-party skill").level, "Unknown aliases remain untouched");
        Check.Equal(false, data.magicPowerSkill_Custom.Any(s => aliases.Any(a => a.Value == s.label)), "Known labels become stable save keys");
        var reloaded = f.Load(f.Save(data));
        foreach (var alias in aliases)
        {
            string canonical = (string)alias.Attribute("canonical")!;
            var skill = reloaded.AllMagicPowerSkills.Single(s => s.label == canonical);
            Check.Equal(4, skill.level, canonical + " persists through its actual saved owner");
            Check.Equal(8, skill.levelMax, canonical + " persists saved maximum");
            Check.Equal(6, skill.costToLevel, canonical + " persists saved cost");
        }
    }

    static void LegacyFighter(RuntimeFixture f)
    {
        string path = Path.Combine(f.WorkingDirectory, "legacy-might.xml");
        new XDocument(new XElement("regression", new XElement("data", new XElement("mightPowerCustom"),
            new XElement("mightPowerSkill_Custom", new[] {
                ("Соперник", 4, 9, 7), ("TM_Taunt_pwr", 1, 3, 1), ("unknown fighter skill", 11, 15, 5)
            }.Select(s => new XElement("li", new XElement("label", s.Item1), new XElement("desc", "saved fighter metadata"),
                new XElement("level", s.Item2), new XElement("levelMax", s.Item3), new XElement("costToLevel", s.Item4))))))).Save(path);
        MightData data = null!;
        try { Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref data, "data"); Scribe.loader.FinalizeLoading(); }
        finally { Scribe.ForceStop(); }
        var skill = data.MightPowerSkill_Custom.Single(s => s.label == "TM_Taunt_pwr");
        Check.Equal(4, skill.level, "Historical fighter investment migrates to canonical owner");
        Check.Equal(9, skill.levelMax, "Fighter maximum survives migration");
        Check.Equal(7, skill.costToLevel, "Fighter cost survives migration");
        Check.Equal(11, data.MightPowerSkill_Custom.Single(s => s.label == "unknown fighter skill").level, "Unknown fighter skill remains untouched");
        string saved = Path.Combine(f.WorkingDirectory, "canonical-might.xml");
        try { Scribe.saver.InitSaving(saved, "regression"); Scribe_Deep.Look(ref data, "data"); Scribe.saver.FinalizeSaving(); }
        finally { Scribe.ForceStop(); }
        Check.Equal(false, File.ReadAllText(saved).Contains("Соперник"), "Fighter next save uses stable identifiers");
        Check.Same(skill, data.MightPowerSkill_Custom.Single(s => s.label == "TM_Taunt_pwr"), "Saving retains invested fighter owner");
    }

    static void RejectIdentifierTranslation(RuntimeFixture f)
    {
        var package = new DefInjectionPackage(typeof(TM_CustomPowerDef));
        const string path = "TM_Empathy.customPower.skills.0.label";
        var injection = new DefInjectionPackage.DefInjection { path = path, injection = "Позитивное мышление", fileSource = "historical invalid injection" };
        package.injections.Add(path, injection);
        package.InjectIntoDefs(true);
        Check.Equal(false, injection.injected, "Engine rejects translating a stable skill identifier");
        Check.Equal(1, package.loadErrors.Count, "Engine reports the invalid machine identifier injection");
        Check.Equal("TM_Empathy_pwr", f.Definitions.Single(d => d.defName == "TM_Empathy").customPower.skills[0].label, "Rejected translation preserves lookup key");
    }

    static void LoadAbilityIdentities(RuntimeFixture f)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs"), "*.xml"))
        foreach (var element in XDocument.Load(file).Root!.Elements("TorannMagic.TMAbilityDef"))
        {
            string? name = (string?)element.Element("defName");
            if (name == null) continue;
            var ability = DefDatabase<TMAbilityDef>.GetNamedSilentFail(name) ?? f.AddAbility(name);
            // Identity is taken from every shipped concrete ability. Only Initialize's
            // abilityClass is required by JecsLite's real AddPawnAbility in this test.
            ability.abilityClass = typeof(MagicAbility);
        }
        foreach (var field in typeof(TorannMagicDefOf).GetFields(BindingFlags.Static | BindingFlags.Public))
            if (field.FieldType == typeof(TMAbilityDef))
                field.SetValue(null, DefDatabase<TMAbilityDef>.GetNamedSilentFail(field.Name));
    }

    static void InnerFire(RuntimeFixture f, int level)
    {
        LoadAbilityIdentities(f);
        TorannMagicDefOf.InnerFire = new TraitDef { defName = "InnerFire" };
        var pawn = Pawn();
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.InnerFire));
        var comp = new CompAbilityUserMagic { parent = pawn, customIndex = -1 };
        pawn.AllComps.Add(comp);
        TM_Calc.InvalidateCompCache(pawn);
        Set(comp, "initializedIsMagicUser", true);
        Set(comp, "isMagicUser", true);
        var data = new MagicData();
        Set(comp, "magicData", data);
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIHardMode = false };
        foreach (var power in data.MagicPowersIF) power.level = level;
        typeof(CompAbilityUserMagic).GetMethod("AssignAbilities", Flags)!.Invoke(comp, null);
        Check.Equal(4, data.MagicPowersIF.Count(power => power.learned), "Reassignment retains all four ordinary class powers");
        Check.Equal(level, data.MagicPowersIF[0].level, "Reassignment preserves the rolled power level");
        Check.Equal(4, comp.AbilityData.Powers.Count, "Real framework receives the four assigned powers");
    }

    static void LeveledClass(RuntimeFixture f, string traitName, string property, string ordinaryPower)
    {
        LoadAbilityIdentities(f);
        var trait = new TraitDef { defName = traitName };
        typeof(TorannMagicDefOf).GetField(traitName)!.SetValue(null, trait);
        var pawn = Pawn();
        pawn.health = new Pawn_HealthTracker(pawn);
        pawn.story.traits.allTraits.Add(new Trait(trait));
        var comp = new CompAbilityUserMagic { parent = pawn, customIndex = -1 };
        pawn.AllComps.Add(comp);
        TM_Calc.InvalidateCompCache(pawn);
        Set(comp, "initializedIsMagicUser", true);
        Set(comp, "isMagicUser", true);
        var data = new MagicData();
        Set(comp, "magicData", data);
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIHardMode = false };
        var powers = (List<MagicPower>)typeof(MagicData).GetProperty(property)!.GetValue(data)!;
        foreach (var power in powers) power.level = Math.Min(2, power.TMabilityDefs.Count - 1);
        var saved = powers.Single(power => power.GetAbilityDef(0)?.defName == ordinaryPower);
        int level = saved.level;
        Rand.PushState(12345);
        try { typeof(CompAbilityUserMagic).GetMethod("AssignAbilities", Flags)!.Invoke(comp, null); }
        finally { Rand.PopState(); }
        Check.Equal(true, saved.learned, "Leveled ordinary class power is learned");
        Check.Equal(level, saved.level, "Class assignment preserves the existing level");
        Check.Same(saved, powers.Single(power => power.GetAbilityDef(0)?.defName == ordinaryPower), "Class assignment retains the existing power owner");
    }
}
