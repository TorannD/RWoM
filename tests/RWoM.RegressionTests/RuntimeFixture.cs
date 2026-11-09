using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using Verse;
using AbilityDef = AbilityUser.AbilityDef;

namespace RWoM.RegressionTests;

internal sealed class RuntimeFixture(string repo, string directory)
{
    public static readonly string[] ShippedPowers =
    [
        "TM_Empathy", "TM_SuppressiveAura", "TM_HarvestPassion", "TM_DistortSpirit",
        "TM_SpiritDrain", "TM_SpiritPossession", "TM_SpiritStorm"
    ];

    private int saveNumber;
    public string Repository => repo;
    public string WorkingDirectory => directory;
    public List<TM_CustomPowerDef> Definitions { get; } = [];

    public static void Initialize()
    {
        RuntimeLog.Initialize();
        Prefs.UseDefaultsWithoutFile(); // In-memory defaults only; never read/apply/save user prefs.
        // ModContentPack's constructor mounts assets and scans mod metadata. Only its
        // assembly registry is needed by the real Scribe type resolver in this process.
        var pack = (ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
        pack.assemblies = new ModAssemblyHandler(pack);
        pack.assemblies.loadedAssemblies.Add(typeof(MagicData).Assembly);
        // Scribe must resolve the test envelope just as it resolves mod IExposable types.
        pack.assemblies.loadedAssemblies.Add(typeof(RuntimeFixture).Assembly);
        pack.assemblies.loadedAssemblies.Add(typeof(AbilityDef).Assembly);
        LoadedModManager.RunningModsListForReading.Add(pack);
        ScribeMetaHeaderUtility.loadedGameVersion = VersionControl.CurrentVersionString;
        ScribeMetaHeaderUtility.loadedGameVersionMajor = VersionControl.CurrentMajor;
        ScribeMetaHeaderUtility.loadedGameVersionMinor = VersionControl.CurrentMinor;
        ScribeMetaHeaderUtility.loadedGameVersionBuild = VersionControl.CurrentBuild;
        ScribeMetaHeaderUtility.modListChanged = false;
    }

    public void ResetDefinitions()
    {
        Scribe.ForceStop();
        DefDatabase<TM_CustomPowerDef>.Clear();
        DefDatabase<TMAbilityDef>.Clear();
        DefDatabase<AbilityDef>.Clear();
        // Production caches a deferred query over this same real DefDatabase list.
        Definitions.Clear();
        Directory.CreateDirectory(directory);
        var document = XDocument.Load(Path.Combine(repo, "v1.7", "Defs", "CustomDefs", "TM_CustomPowerDef.xml"));
        XElement defaults = document.Root!.Elements().Single(element => (string?)element.Attribute("Name") == "CustomPower").Element("customPower")!;
        foreach (string name in ShippedPowers)
        {
            XElement power = document.Root.Elements().Single(element => (string?)element.Element("defName") == name).Element("customPower")!;
            var definition = new TM_CustomPowerDef { defName = name };
            definition.customPower = new TM_CustomPower
            {
                forMage = bool.Parse(Value("forMage")),
                forFighter = bool.Parse(Value("forFighter")),
                maxLevel = int.Parse(Value("maxLevel"), CultureInfo.InvariantCulture),
                learnCost = int.Parse(Value("learnCost"), CultureInfo.InvariantCulture),
                costToLevel = int.Parse(Value("costToLevel"), CultureInfo.InvariantCulture),
                requiresScroll = bool.Parse(Value("requiresScroll")),
                chaosMageUseable = bool.Parse(Value("chaosMageUseable")),
                abilityDefs = power.Element("abilityDefs")!.Elements("li").Select(element => (AbilityDef)AddAbility(element.Value)).ToList(),
                skills = power.Element("skills")!.Elements("li").Select(element => new TM_CustomSkill
                {
                    label = element.Element("label")!.Value,
                    description = element.Element("description")!.Value,
                    levelMax = (int)element.Element("levelMax")!,
                    costToLevel = (int)element.Element("costToLevel")!
                }).ToList(),
                autocasting = ReadAutocast(power.Element("autocasting"), defaults.Element("autocasting")!)
            };
            AddDefinition(definition);
            string Value(string field) => (power.Element(field) ?? defaults.Element(field))!.Value;
        }
    }

    public TMAbilityDef Ability(string name) => DefDatabase<TMAbilityDef>.GetNamed(name);

    public TMAbilityDef AddAbility(string name)
    {
        // The actual AbilityDef constructor initializes BaseContent.BadTex. Bypass only
        // that graphics side effect, keeping real runtime identity and lookup/serialization.
        var ability = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        ability.defName = name;
        ability.label = name;
        ability.childAbilities = [];
        ability.abilitiesRemovedWhenUsed = [];
        ability.MainVerb = new AbilityUser.VerbProperties_Ability();
        DefDatabase<TMAbilityDef>.Add(ability);
        DefDatabase<AbilityDef>.Add(ability);
        return ability;
    }

    public void AddDefinition(TM_CustomPowerDef definition)
    {
        Definitions.Add(definition);
        DefDatabase<TM_CustomPowerDef>.Add(definition);
    }

    public string Save(MagicData data)
    {
        string path = Path.Combine(directory, $"save-{++saveNumber}.xml");
        try
        {
            Scribe.saver.InitSaving(path, "regression");
            Scribe_Deep.Look(ref data, "magicData");
            Scribe.saver.FinalizeSaving();
        }
        finally
        {
            Scribe.ForceStop();
        }
        RuntimeLog.AssertClean();
        return path;
    }

    public MagicData Load(string path)
    {
        // Save recovery now fills missing builtin lists as well as custom ones.
        // The game loads these defs before Scribe; isolated payload tests must too.
        GrimoireProgressionTests.LoadBaseDefinitions(this);
        MagicData data = null!;
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref data, "magicData");
            Scribe.loader.FinalizeLoading(); // Includes real cross-reference and PostLoadInit passes.
        }
        finally
        {
            Scribe.ForceStop();
        }
        RuntimeLog.AssertClean();
        return Check.NotNull(data, "Scribe loaded MagicData");
    }

    public MagicData LoadLegacy(params MagicPowerSkill[] skills)
    {
        // Hand-authored old save shape: no invocation of the current serializer to
        // create the input, so a fixed Saving pass cannot hide a legacy-load bug.
        string path = Path.Combine(directory, $"legacy-{++saveNumber}.xml");
        new XDocument(new XElement("regression", new XElement("magicData",
            new XElement("initialized", true), new XElement("magicPowerCustom"),
            new XElement("magicPowerSkill_Custom", skills.Select(skill => new XElement("li",
                new XElement("label", skill.label), new XElement("desc", skill.desc),
                new XElement("level", skill.level), new XElement("levelMax", skill.levelMax),
                new XElement("costToLevel", skill.costToLevel))))))).Save(path);
        return Load(path);
    }

    private static TM_Autocast ReadAutocast(XElement? specific, XElement defaults)
    {
        var result = new TM_Autocast();
        foreach (FieldInfo field in typeof(TM_Autocast).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            XElement? element = specific?.Element(field.Name) ?? defaults.Element(field.Name);
            if (element == null)
                continue;
            object value = field.FieldType == typeof(List<string>) ? element.Elements("li").Select(item => item.Value).ToList()
                : field.FieldType.IsEnum ? Enum.Parse(field.FieldType, element.Value)
                : Convert.ChangeType(element.Value, field.FieldType, CultureInfo.InvariantCulture);
            field.SetValue(result, value);
        }
        return result;
    }
}
