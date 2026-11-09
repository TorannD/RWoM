using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
using AbilityDef = AbilityUser.AbilityDef;

namespace RWoM.RegressionTests;

internal static class GrimoireKureTests
{
    static readonly string[] KureClasses = (
        "TM_RedMage TM_Ninja TM_ElementalGunner TM_Bombardier TM_Railbreaker TM_Samurai TM_Gadgeteer " +
        "TM_WindMage TM_Medic TM_ChromaMage TM_Harbinger TM_Reaper TM_Berserker TM_Scion TM_Plaguedoctor " +
        "TM_Cleric TM_Arbormancer TM_Chainwarden TM_Ambusher TM_Saboteur TM_Keeper TM_Pridestalker " +
        "TM_Starseeker TM_Aspirant TM_Hawkeye TM_Atronach TM_SupportSpecialist TM_Scrapper TM_Oracle " +
        "TM_Mystic TM_Spellbreaker TM_Duskbringer TM_Thunderblessed TM_Deadeye TM_Spellthief TM_Dragoon " +
        "TM_BlackMage TM_Scholar TM_Gunbreaker TM_Gunner TM_Warrior TM_Nomad TM_Guardian TM_Holosmith " +
        "TM_Troubadour TM_Gambler TM_Chef TM_Miner TM_Striker TM_RuneKnight TM_BlossomBlader TM_Ravenous " +
        "TM_MechHunter TM_Gunslinger TM_Shadowbringer TM_Warden TM_Chempunk TM_Explorer TM_Architect").Split(' ');

    static readonly string[] NativeClasses =
        "TM_TheShadow TM_Brightmage TM_Shaman TM_Golemancer TM_Apothecary TM_Empath TM_Possessor TM_Possessed".Split(' ');

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("GrimoireKure.DeclarationCoverage", DeclarationCoverage);
        yield return new("GrimoireKure.StaminaPotionAddictionNeed", f =>
        {
            var source=new XmlDocument();source.Load(IntegrationPackageTests.Patch(f,"KureStaminaPotion.xml"));
            var op=DirectXmlToObject.ObjectFromXml<PatchOperation>(source.SelectSingleNode("/Patch/Operation")!,false);
            var definitions=new XmlDocument();definitions.LoadXml("<Defs/>");
            Check.Equal(true,op.Apply(definitions),"absent Kure is a no-op");
            definitions.LoadXml("<Defs><HediffDef><defName>RFSStaminaPotionAddiction</defName><hediffClass>Hediff_Addiction</hediffClass></HediffDef></Defs>");
            Check.Equal(true,op.Apply(definitions),"absent named need does not introduce unresolved references");
            Check.Equal(0,definitions.GetElementsByTagName("chemicalNeed").Count,"partial installation untouched");
            var need=definitions.CreateElement("NeedDef");need.InnerXml="<defName>RFSChemical_StaminaPotion</defName>";definitions.DocumentElement!.AppendChild(need);
            Check.Equal(true,op.Apply(definitions),"authored chemical dependency linked");
            Check.Equal("RFSChemical_StaminaPotion",definitions.SelectSingleNode("Defs/HediffDef/chemicalNeed")!.InnerText,"native need selection on load");
            definitions.SelectSingleNode("Defs/HediffDef/chemicalNeed")!.InnerText="AnotherNeed";
            Check.Equal(true,op.Apply(definitions),"other adapter's link remains authoritative");
            Check.Equal("AnotherNeed",definitions.SelectSingleNode("Defs/HediffDef/chemicalNeed")!.InnerText,"existing reference preserved");
            Check.Equal(1,definitions.GetElementsByTagName("chemicalNeed").Count,"idempotent patch");
        });
        yield return new("LegacyConversion.NativeShadowKeepsBothDomains", f =>
        {
            var loaded = Load(f);
            var cls = loaded.Native.Single(c=>c.classTrait.defName=="TM_TheShadow");
            var book = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            book.defName="BookOfShadow"; cls.fullScript=book;
            var profiles=DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();
            var classes=TM_ClassUtility.CustomClasses;
            var trait=TorannMagicDefOf.TM_TheShadow;
            try
            {
                TorannMagicDefOf.TM_TheShadow=cls.classTrait;
                TM_ClassUtility.CustomClasses=[cls];
                DefDatabase<ClassDisciplineDef>.Clear();
                DefDatabase<ClassDisciplineDef>.Add(new ClassDisciplineDef {defName="ImportShadow",profileId="rwom.shadow",
                    legacyTrait=cls.classTrait,foundationBook=book,domain=DisciplineDomain.Magic});
                var p=PawnWithComps(true,true);
                var comp=new CompClassProgression {parent=p.pawn};p.pawn.AllComps.Add(comp);
                p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait,cls.traitDegree));
                var walk=p.magic.MagicData.MagicPowersShadow.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowWalk));
                var strike=p.might.MightData.MightPowersShadow.Single(x=>x.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowStrike));
                walk.learned=strike.learned=true; walk.ticksUntilNextCast=81;strike.ticksUntilNextCast=95;
                var import=typeof(ClassInitiation).Assembly.GetType("TorannMagic.ClassProgression.LegacyClassConversion")!
                    .GetMethod("TryImport",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Equal(true,(bool)import.Invoke(null,[p.pawn])!,"native hybrid accepted");
                Check.Equal(Vocation.WarriorMage,comp.State.Vocation,"both resource domains");
                Check.Equal(2,comp.State.Disciplines.Count,"both paths attached to the same pawn");
                Check.Equal(DisciplineDomain.Magic,comp.State.Find("rwom.shadow").Domain,"arcane path");
                Check.Equal(DisciplineDomain.Might,comp.State.Find("rwom.shadowwalker").Domain,"physical path");
                Check.Equal(81,walk.ticksUntilNextCast,"spell receipt"); Check.Equal(95,strike.ticksUntilNextCast,"technique receipt");
            }
            finally
            {
                TorannMagicDefOf.TM_TheShadow=trait;TM_ClassUtility.CustomClasses=classes;
                DefDatabase<ClassDisciplineDef>.Clear();foreach(var def in profiles)DefDatabase<ClassDisciplineDef>.Add(def);
            }
        });
        foreach (string id in KureClasses)
        {
            string name = id;
            yield return new("GrimoireKure.Rank0." + name, f => ClassRows(f, name, false, false));
            yield return new("GrimoireKure.Leveled." + name, f => ClassRows(f, name, false, true));
        }
        foreach (string id in NativeClasses)
        {
            string name = id;
            yield return new("GrimoireKure.Native.Rank0." + name, f => ClassRows(f, name, true, false));
            yield return new("GrimoireKure.Native.Leveled." + name, f => ClassRows(f, name, true, true));
        }
        yield return new("GrimoireKure.AdvancedPossessedWithHybridBase", AdvancedPossessedWithHybridBase);
    }

    sealed record Family(string Name, HashSet<AbilityDef> Abilities);
    sealed record Loaded(List<TM_CustomClass> Native, List<TM_CustomClass> Kure, List<TM_CustomPowerDef> Powers,
        Dictionary<string, XElement> Abilities);

    static string KureRoot()
    {
        string game = Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        string local = Path.Combine(game, "Mods/Kures AROM 1.7 (Unofficial)");
        if (File.Exists(Path.Combine(local, "Common/Patches/RFS_CustomClasses.xml"))) return local;
        string workshop = Path.GetFullPath(Path.Combine(game, "../../workshop/content/294100/3823397177"));
        if (File.Exists(Path.Combine(workshop, "Common/Patches/RFS_CustomClasses.xml"))) return workshop;
        throw new FileNotFoundException("Kure dependency missing; expected local mod " + local + " or workshop " + workshop);
    }

    static Loaded Load(RuntimeFixture f)
    {
        string kure = KureRoot();
        var nativeXml = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomClassDef.xml"))
            .Root!.Elements("TorannMagic.TM_CustomClassDef").Single().Element("customClasses")!.Elements("li").ToList();
        var kureXml = XDocument.Load(Path.Combine(kure, "Common/Patches/RFS_CustomClasses.xml"))
            .Root!.Elements("Operation").Where(x => x.Element("xpath")?.Value.Contains("TM_CustomClasses") == true)
            .SelectMany(x => x.Element("value")!.Elements("li")).ToList();
        var abilityXml = Directory.GetFiles(Path.Combine(f.Repository, "v1.7/Defs/AbilityDefs"), "*.xml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(kure, "Common/Defs"), "*.xml", SearchOption.AllDirectories))
            .SelectMany(path => XDocument.Load(path).Root!.Elements("TorannMagic.TMAbilityDef"))
            .Where(x => x.Element("defName") != null)
            .GroupBy(x => x.Element("defName")!.Value).ToDictionary(x => x.Key, x => x.Last());
        foreach (string name in abilityXml.Keys) RegisterAbility(f, name);
        LoadBaseDefinitions(f); // Bind DefOf to these same real registered identities.

        DefDatabase<TM_CustomPowerDef>.Clear();
        DirectXmlCrossRefLoader.Clear();
        XmlInheritance.Clear();
        var powers = new List<TM_CustomPowerDef>();
        try
        {
            // Register the shared CustomPower parent and Kure children together.
            // RFSCustomPowers also contains nonpower defs: deliberately do not load
            // those unrelated hediff/autocast-condition dependency graphs.
            var nodes = new List<XmlNode>();
            foreach (string path in new[] {
                Path.Combine(f.Repository, "v1.7/Defs/CustomDefs/TM_CustomPowerDef.xml"),
                Path.Combine(kure, "Common/Defs/CustomClassesRFS/RFSCustomPowers.xml") })
            {
                var document = new XmlDocument();
                document.Load(path);
                foreach (XmlNode node in document.DocumentElement!.ChildNodes)
                {
                    if (node.Name != "TorannMagic.TM_CustomPowerDef") continue;
                    nodes.Add(node);
                    XmlInheritance.TryRegister(node, null);
                }
            }
            XmlInheritance.Resolve();
            foreach (XmlNode node in nodes)
            {
                if (string.Equals(node.Attributes?["Abstract"]?.Value, "True", StringComparison.OrdinalIgnoreCase)) continue;
                foreach (XmlNode ability in XmlInheritance.GetResolvedNodeFor(node).SelectNodes("customPower/abilityDefs/li")!)
                {
                    string name = ability.InnerText.Trim();
                    Check.Equal(true, abilityXml.ContainsKey(name), "actual ability declaration exists for custom-power reference " + name);
                    Check.NotNull(DefDatabase<TMAbilityDef>.GetNamedSilentFail(name), "registered actual custom-power ability " + name);
                }
                var definition = DirectXmlToObject.ObjectFromXml<TM_CustomPowerDef>(node, true);
                DefDatabase<TM_CustomPowerDef>.Add(definition);
                powers.Add(definition);
            }
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            RuntimeLog.AssertClean();
        }
        finally
        {
            DirectXmlCrossRefLoader.Clear();
            XmlInheritance.Clear();
        }
        return new Loaded(nativeXml.Select(x => ReadClass(x, abilityXml)).ToList(), kureXml.Select(x => ReadClass(x, abilityXml)).ToList(), powers, abilityXml);
    }

    static TMAbilityDef RegisterAbility(RuntimeFixture f, string name) =>
        DefDatabase<TMAbilityDef>.GetNamedSilentFail(name) ?? f.AddAbility(name);

    static TM_CustomClass ReadClass(XElement source, Dictionary<string, XElement> abilityXml)
    {
        string name = source.Element("classTrait")!.Value;
        var trait = DefDatabase<TraitDef>.GetNamedSilentFail(name);
        if (trait == null)
        {
            trait = new TraitDef { defName = name };
            DefDatabase<TraitDef>.Add(trait);
        }
        // Class membership needs no book, texture or hediff construction.
        return new TM_CustomClass {
            classTrait = trait,
            traitDegree = (int?)source.Element("traitDegree") ?? 0,
            isMage = (bool?)source.Element("isMage") ?? false,
            isFighter = (bool?)source.Element("isFighter") ?? false,
            isAdvancedClass = (bool?)source.Element("isAdvancedClass") ?? false,
            classMageAbilities = ReadAbilities("classMageAbilities"),
            classFighterAbilities = ReadAbilities("classFighterAbilities")
        };
        List<TMAbilityDef> ReadAbilities(string field) => source.Element(field)?.Elements("li")
            .Select(x => {
                string ability = x.Value.Trim();
                Check.Equal(true, abilityXml.ContainsKey(ability), name + " actual class ability declaration exists: " + ability);
                return Check.NotNull(DefDatabase<TMAbilityDef>.GetNamedSilentFail(ability), name + " registered actual class ability " + ability);
            }).ToList() ?? [];
    }

    static void DeclarationCoverage(RuntimeFixture f)
    {
        var loaded = Load(f);
        Check.Equal(59, loaded.Kure.Count, "installed Kure classes are all covered");
        Check.Equal(8, loaded.Native.Count, "native custom classes are all covered");
        Check.Equal(string.Join(",", KureClasses.Order()), string.Join(",", loaded.Kure.Select(c => c.classTrait.defName).Order()), "Kure case inventory");
        Check.Equal(string.Join(",", NativeClasses.Order()), string.Join(",", loaded.Native.Select(c => c.classTrait.defName).Order()), "native case inventory");
        Check.Equal(747, loaded.Powers.Count, "31 native and 716 installed Kure inherited power definitions loaded");
        Check.Equal(true, loaded.Powers.All(p => p.customPower.abilityDefs.Count > 0 && p.customPower.abilityDefs.All(a => a != null)), "all real ability crossrefs resolve");
    }

    static void ClassRows(RuntimeFixture f, string name, bool native, bool leveled)
    {
        var loaded = Load(f);
        var cls = (native ? loaded.Native : loaded.Kure).Single(c => c.classTrait.defName == name);
        VerifyRows(loaded, [cls], leveled);
    }

    static void AdvancedPossessedWithHybridBase(RuntimeFixture f)
    {
        var loaded = Load(f);
        var hybrid = loaded.Kure.Single(c => c.classTrait.defName == "TM_Reaper");
        var advanced = loaded.Native.Single(c => c.classTrait.defName == "TM_Possessed");
        Check.Equal(true, hybrid.isMage && hybrid.isFighter, "Reaper exercises both resource sides");
        Check.Equal(true, advanced.isAdvancedClass, "Possessed uses AdvancedClasses rather than customClass");
        VerifyRows(loaded, [hybrid, advanced], true);
    }

    static void VerifyRows(Loaded loaded, List<TM_CustomClass> classes, bool leveled)
    {
        string context = string.Join("+", classes.Select(c => c.classTrait.defName));
        var p = PawnWithComps(classes.Any(c => c.isMage), classes.Any(c => c.isFighter));
        foreach (var cls in classes)
        {
            p.pawn.story.traits.allTraits.Add(new Trait(cls.classTrait, cls.traitDegree));
            if (cls.isMage)
            {
                if (cls.isAdvancedClass) p.magic.AdvancedClasses.Add(cls);
                else p.magic.customClass = cls;
            }
            if (cls.isFighter)
            {
                if (cls.isAdvancedClass) p.might.AdvancedClasses.Add(cls);
                else p.might.customClass = cls;
            }
        }
        List<MagicPower> magic = p.magic.IsMagicUser ? p.magic.MagicData.AllMagicPowers : [];
        List<MightPower> might = p.might.IsMightUser ? p.might.MightData.AllMightPowers : [];
        var magicFamilies = ExpectedFamilies(loaded, classes.SelectMany(c => c.classMageAbilities).ToList(), true,
            magic.Select(x => (x.TMabilityDefs, x.TMabilityDefs.Count > 1 || HasMagicSkills(p.magic.MagicData, x))).ToList(), context);
        var mightFamilies = ExpectedFamilies(loaded, classes.SelectMany(c => c.classFighterAbilities).ToList(), false,
            might.Select(x => (x.TMabilityDefs, x.TMabilityDefs.Count > 1 || HasMightSkills(p.might.MightData, x))).ToList(), context);
        var savedMagic = magic.Select(x => {
            bool own = magicFamilies.Any(family => family.Abilities.Overlaps(x.TMabilityDefs));
            return (Power: x, Level: leveled && own ? Math.Min(2, x.TMabilityDefs.Count - 1) : 0, Learned: leveled && own);
        }).ToList();
        var savedMight = might.Select(x => {
            bool own = mightFamilies.Any(family => family.Abilities.Overlaps(x.TMabilityDefs));
            return (Power: x, Level: leveled && own ? Math.Min(2, x.TMabilityDefs.Count - 1) : 0, Learned: leveled && own);
        }).ToList();
        foreach (var saved in savedMagic) { saved.Power.level = saved.Level; saved.Power.learned = saved.Learned; }
        foreach (var saved in savedMight) { saved.Power.level = saved.Level; saved.Power.learned = saved.Learned; }
        if (classes.Any(c => loaded.Kure.Contains(c)))
        {
            var progression = new TorannMagic.ClassProgression.CompClassProgression { parent = p.pawn };
            p.pawn.AllComps.Add(progression);
            var import = typeof(MagicData).Assembly.GetType("TorannMagic.ClassProgression.LegacyClassConversion")!
                .GetMethod("TryImport", BindingFlags.Static | BindingFlags.NonPublic)!;
            Check.Equal(true, TorannMagic.ClassProgression.ClassProgressionFeature.Enabled, "exercise beta-on import");
            Check.Equal(false, (bool)import.Invoke(null, [p.pawn])!, context + " addon remains classic on load");
            Check.Equal(true, progression.UsesClassicProgression, context + " no hidden imported discipline");
        }
        if (leveled && magicFamilies.Concat(mightFamilies).Any(family => family.Abilities.Count > 1))
            Check.Equal(true, savedMagic.Any(x => x.Level > 0) || savedMight.Any(x => x.Level > 0), context + " actually exercises a later rank");
        else if (leveled)
            Console.WriteLine("INFO " + context + " declares no higher-rank family; checking learned cards without inventing ranks");
        if (context == "TM_Explorer")
        {
            Check.Equal(0, magicFamilies.Count + mightFamilies.Count, "Explorer intentionally declares no abilities");
        }
        else Check.Equal(true, magicFamilies.Count + mightFamilies.Count > 0, context + " has upgradeable ability families");

        var window = Build(p.pawn);
        var magicRows = MagicRows(window);
        var mightRows = MightRows(window);
        AssertFamilies(magicFamilies, magicRows.Select(x => x.TMabilityDefs).ToList(),
            classes.SelectMany(c => c.classMageAbilities).ToList(), context + " magic");
        AssertFamilies(mightFamilies, mightRows.Select(x => x.TMabilityDefs).ToList(),
            classes.SelectMany(c => c.classFighterAbilities).ToList(), context + " might");
        foreach (var saved in savedMagic)
        {
            Check.Equal(saved.Level, saved.Power.level, context + " magic investment unchanged");
            Check.Equal(saved.Learned, saved.Power.learned, context + " magic learned flag unchanged");
        }
        foreach (var saved in savedMight)
        {
            Check.Equal(saved.Level, saved.Power.level, context + " might investment unchanged");
            Check.Equal(saved.Learned, saved.Power.learned, context + " might learned flag unchanged");
        }
        Check.Equal(true, magicRows.All(magic.Contains), context + " magic cards retain real data owners");
        Check.Equal(true, mightRows.All(might.Contains), context + " might cards retain real data owners");
    }

    static bool HasMagicSkills(MagicData data, MagicPower power) => power.TMabilityDefs.OfType<TMAbilityDef>().Any(a =>
        data.GetSkill_Power(a) != null || data.GetSkill_Efficiency(a) != null || data.GetSkill_Versatility(a) != null);
    static bool HasMightSkills(MightData data, MightPower power) => power.TMabilityDefs.OfType<TMAbilityDef>().Any(a =>
        data.GetSkill_Power(a) != null || data.GetSkill_Efficiency(a) != null || data.GetSkill_Versatility(a) != null);

    static List<Family> ExpectedFamilies(Loaded loaded, List<TMAbilityDef> declared, bool mage,
        List<(List<AbilityDef> Abilities, bool Upgradeable)> runtime, string context)
    {
        var result = new List<Family>();
        foreach (var ability in declared.Distinct())
        {
            // Expectations for custom powers come from inherited external XML,
            // not the production discovery list whose omissions we are testing.
            var custom = loaded.Powers.Where(p => (mage ? p.customPower.forMage : p.customPower.forFighter) &&
                p.customPower.abilityDefs.Contains(ability)).ToList();
            if (custom.Count > 0)
            {
                foreach (var definition in custom)
                {
                    var cp = definition.customPower;
                    // A blank skill label intentionally suppresses a subskill,
                    // not its declared custom-power card. A completely skill-less,
                    // single-rank declaration is only a standalone helper.
                    if (cp.abilityDefs.Count > 1 || cp.skills?.Any(s => s != null) == true)
                        Add(definition.defName, cp.abilityDefs);
                    else Console.WriteLine("INFO " + context + " standalone XML helper not required: " + ability.defName);
                }
                continue;
            }
            var builtin = runtime.Where(x => x.Abilities.Contains(ability)).ToList();
            if (builtin.Count > 0)
            {
                foreach (var family in builtin.Where(x => x.Upgradeable)) Add(ability.defName, family.Abilities);
                if (!builtin.Any(x => x.Upgradeable))
                    Console.WriteLine("INFO " + context + " standalone native helper not required: " + ability.defName);
                continue;
            }
            // Helpers without any power are allowed only when an actual declared
            // parent names them in childAbilities (e.g. Harvest/Incite Passion).
            bool childHelper = declared.Any(parent => loaded.Abilities.TryGetValue(parent.defName, out var xml) &&
                xml.Element("childAbilities")?.Elements("li").Any(x => x.Value.Trim() == ability.defName) == true);
            Check.Equal(true, childHelper, context + " declared ability has no configured power family: " + ability.defName);
            Console.WriteLine("INFO " + context + " child ability excluded: " + ability.defName);
        }
        return result;
        void Add(string name, List<AbilityDef> abilities)
        {
            var defs = abilities.ToHashSet();
            if (!result.Any(x => x.Abilities.Overlaps(defs))) result.Add(new Family(name, defs));
        }
    }

    static void AssertFamilies(List<Family> expected, List<List<AbilityDef>> rows, List<TMAbilityDef> declared, string context)
    {
        foreach (var family in expected)
            Check.Equal(1, rows.Count(row => family.Abilities.Overlaps(row)), context + " exactly one card for " + family.Name);
        // Presence of upgrade families is mandatory; a declared static family
        // (e.g. PistolWhip) may also render, but never an unrelated or second card.
        foreach (var row in rows)
        {
            Check.Equal(true, declared.Any(ability => row.Contains(ability)), context + " card belongs to a declared class family: " + row[0].defName);
            Check.Equal(1, rows.Count(other => row.Intersect(other).Any()), context + " no duplicate card for " + row[0].defName);
        }
    }
}
