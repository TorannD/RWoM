using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic.ClassProgression;
using UnityEngine;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SoulStoneDefinitionTests
{
    const string Id = "TM_SoulStone";
    static readonly string[] Languages = ["English", "Russian", "Ukrainian", "ChineseSimplified", "Spanish", "Portuguese", "PortugueseBrazilian"];
    static string DefinitionPath(RuntimeFixture f) => Path.Combine(f.Repository, "v1.7/Defs/ThingDefs/TM_SoulStone.xml");
    static XElement Definition(RuntimeFixture f) => XDocument.Load(DefinitionPath(f)).Root!.Element("ThingDef")!;
    static T Blank<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SoulStoneDefinition.NativeItemConfigAndNoActivation", NativeConfig);
        yield return new("SoulStoneDefinition.ApprovedArtworkAndSevenLocales", Assets);
        yield return new("SoulStoneDefinition.ActualContainedPawnPresentation", Presentation);
    }

    static ThingDef NativeDef(RuntimeFixture f)
    {
        var node = Definition(f);
        // ThingDef construction initializes graphics shaders in the headless host.
        // Reconstruct authored fields only, then run real native ConfigErrors below.
        var def = Blank<ThingDef>();
        def.defName = node.Element("defName")!.Value;
        def.label = node.Element("label")!.Value; def.description = node.Element("description")!.Value;
        def.thingClass = typeof(Thing_SoulStone);
        def.category = Enum.Parse<ThingCategory>(node.Element("category")!.Value);
        def.altitudeLayer = Enum.Parse<AltitudeLayer>(node.Element("altitudeLayer")!.Value);
        def.selectable = bool.Parse(node.Element("selectable")!.Value);
        def.alwaysHaulable = bool.Parse(node.Element("alwaysHaulable")!.Value);
        def.rotatable = bool.Parse(node.Element("rotatable")!.Value);
        def.useHitPoints = bool.Parse(node.Element("useHitPoints")!.Value);
        def.destroyable = bool.Parse(node.Element("destroyable")!.Value);
        def.stackLimit = int.Parse(node.Element("stackLimit")!.Value, CultureInfo.InvariantCulture);
        def.tradeability = Enum.Parse<Tradeability>(node.Element("tradeability")!.Value);
        def.generateCommonality = float.Parse(node.Element("generateCommonality")!.Value, CultureInfo.InvariantCulture);
        def.tradeTags = []; def.thingSetMakerTags = []; def.virtualDefs = []; def.size = IntVec2.One;
        var category = Blank<ThingCategoryDef>();
        category.defName = node.Element("thingCategories")!.Element("li")!.Value;
        def.thingCategories = [category];
        def.statBases = node.Element("statBases")!.Elements().Select(e => new StatModifier {
            stat = (StatDef)typeof(StatDefOf).GetField(e.Name.LocalName)!.GetValue(null)!,
            value = float.Parse(e.Value, CultureInfo.InvariantCulture) }).ToList();
        var xml = new XmlDocument(); xml.Load(DefinitionPath(f));
        def.comps = xml.SelectNodes("Defs/ThingDef/comps/li")!.Cast<XmlNode>()
            .Select(n => DirectXmlToObject.ObjectFromXml<CompProperties>(n, false)).ToList();
        def.graphicData = Blank<GraphicData>();
        def.graphicData.texPath = node.Element("graphicData")!.Element("texPath")!.Value;
        def.graphicData.graphicClass = typeof(Graphic_Single);
        def.graphicData.drawSize = Vector2.one;
        return def;
    }

    sealed class NativeStats : IDisposable
    {
        readonly Dictionary<FieldInfo, object?> previous = new();
        readonly Game? priorGame = Current.Game;
        readonly Scenario? priorScenario = Current.Game?.Scenario;
        public NativeStats()
        {
            if (Current.Game == null) Current.Game = Blank<Game>();
            Current.Game.Scenario = new Scenario();
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(StatDefOf));
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(ShaderTypeDefOf));
            foreach (string name in new[] { "Mass", "MaxHitPoints", "MarketValue", "DeteriorationRate", "Nutrition", "Flammability", "Beauty", "BeautyOutdoors" })
            {
                var field = typeof(StatDefOf).GetField(name)!; previous[field] = field.GetValue(null);
                field.SetValue(null, new StatDef { defName = name, workerClass = typeof(StatWorker), defaultBaseValue = 0 });
            }
        }
        public void Dispose()
        {
            foreach (var entry in previous) entry.Key.SetValue(null, entry.Value);
            Current.Game.Scenario = priorScenario;
            Current.Game = priorGame;
        }
    }

    static void NativeConfig(RuntimeFixture f)
    {
        using var stats = new NativeStats();
        var node = Definition(f); var def = NativeDef(f);
        Check.Equal(true, node.Attribute("ParentName") == null, "no resource or magicyte inheritance");
        Check.Equal(typeof(Thing_SoulStone).FullName!, node.Element("thingClass")!.Value, "native holder item class");
        Check.Equal("", string.Join("; ", def.ConfigErrors()), "native item configuration is valid");
        Check.Equal(1, def.stackLimit, "singleton item");
        Check.Equal(false, def.tradeability.TraderCanSell(), "native trader cannot sell");
        Check.Equal(Tradeability.None, def.tradeability, "no buying or selling");
        Check.Equal(0f, def.generateCommonality, "no random generation weight");
        Check.Equal("ItemsMisc", def.thingCategories.Single().defName, "miscellaneous item, not material ingredient category");
        Check.Equal(0.2f, def.statBases.Single(s => s.stat == StatDefOf.Mass).value, "authored mass");
        Check.Equal(150f, def.statBases.Single(s => s.stat == StatDefOf.MaxHitPoints).value, "empty stone hit points");
        Check.Equal(0f, def.statBases.Single(s => s.stat == StatDefOf.MarketValue).value, "no market value");
        Check.Equal(0f, def.statBases.Single(s => s.stat == StatDefOf.DeteriorationRate).value, "no deterioration");
        Check.Equal(true, def.destroyable && def.useHitPoints, "empty shell remains normally damageable");
        Check.Equal(1, def.comps.Count, "no usable or effect component");
        Check.Equal(typeof(CompForbiddable), def.comps.Single().compClass, "only native forbidding control");
        Check.Equal(false, node.Descendants("recipeMaker").Any(), "no automatic recipe");
        Check.Equal(false, node.Element("tradeTags")!.Elements().Any() || node.Element("thingSetMakerTags")!.Elements().Any(), "no trader or reward tags");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(f.Repository, "v1.7/Defs"), "*.xml", SearchOption.AllDirectories))
        {
            if (Path.GetFullPath(file) == Path.GetFullPath(DefinitionPath(f))) continue;
            Check.Equal(false, XDocument.Load(file).Descendants().Any(e => e.Name.LocalName == Id || (!e.HasElements && e.Value == Id)), "no recipe/trader/quest/source reference: " + file);
        }
        RuntimeLog.AssertClean();
    }

    static void Assets(RuntimeFixture f)
    {
        string texture = Definition(f).Element("graphicData")!.Element("texPath")!.Value;
        byte[] png = File.ReadAllBytes(Path.Combine(f.Repository, "v1.7/Textures", texture + ".png"));
        Check.Equal("211472E6194B36DD9F653B76094DFAB5A8D5E0DC77C786053BBD80E2359BAF67", Convert.ToHexString(SHA256.HashData(png)), "approved art copied byte-for-byte");
        Check.Equal("89504E470D0A1A0A", Convert.ToHexString(png.AsSpan(0, 8)), "PNG signature");
        Check.Equal(1254, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)), "approved width unchanged");
        Check.Equal(1254, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)), "approved height unchanged");
        Check.Equal((byte)6, png[25], "RGBA alpha channel retained");
        foreach (var language in Languages)
        {
            var translated = XDocument.Load(Path.Combine(f.Repository, "v1.7/Languages", language, "DefInjected/ThingDef/TM_SoulStone.xml")).Root!.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
            Check.Equal(2, translated.Count, "label and description: " + language);
            Check.Equal(true, translated.TryGetValue(Id + ".label", out var label) && !string.IsNullOrWhiteSpace(label), "nonempty label " + language);
            Check.Equal(true, translated.TryGetValue(Id + ".description", out var description) && !string.IsNullOrWhiteSpace(description), "nonempty description " + language);
            var keys = XDocument.Load(Path.Combine(f.Repository, "v1.7/Languages", language, "Keyed/TM_SoulStone.xml")).Root!.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value);
            Check.Equal(4, keys.Count, "presentation and abandonment keys " + language);
            Check.Equal(true, keys["TM_SoulStoneAbandonWarning"].Contains("{0}"), "warning names souls " + language);
            Check.Equal(true, keys["TM_SoulStoneOccupiedLabel"].Contains("{0}") && keys["TM_SoulStoneOccupiedLabel"].Contains("{1}"), "label placeholders " + language);
            Check.Equal(true, keys["TM_SoulStoneContainedSpirit"].Contains("{0}"), "actual soul placeholder " + language);
            Check.Equal(false, string.IsNullOrWhiteSpace(keys["TM_SoulStoneRecoveryDebt"]), "recovery debt translated " + language);
        }
    }

    static void Presentation(RuntimeFixture f)
    {
        using var fixture = new SpiritRebirthStateTests.Fixture(f);
        Set(Current.Game, "tickManager", new TickManager());
        Set(Current.Game, "questManager", new QuestManager());
        using var stats = new NativeStats();
        var previous = LanguageDatabase.activeLanguage; var previousDefault = LanguageDatabase.defaultLanguage;
        try
        {
            var language = Blank<LoadedLanguage>();
            language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = XDocument.Load(Path.Combine(f.Repository, "v1.7/Languages/English/Keyed/TM_SoulStone.xml"))
                .Root!.Elements().ToDictionary(e => e.Name.LocalName, e => new LoadedLanguage.KeyedReplacement { key = e.Name.LocalName, value = e.Value });
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
            var stone = new Thing_SoulStone { def = NativeDef(f), HitPoints = 150 };
            string emptyLabel = stone.LabelNoCount;
            Check.Equal("soul stone", emptyLabel, "empty shell keeps normal item label");
            fixture.Original.Name = new NameSingle("Original Soul");
            Check.Equal(true, stone.GetDirectlyHeldThings().TryAdd(fixture.Original, false), "presentation reads actual native owner");
            Check.Equal(true, stone.LabelNoCount.Contains("Original Soul"), "occupied label identifies actual pawn");
            string inspect = stone.GetInspectString();
            Check.Equal(true, inspect.Contains("Original Soul") && inspect.Contains("seven full days"), "inspect reports exact soul and full next-implant debt");
            stone.GetDirectlyHeldThings().Remove(fixture.Original);
            Check.Equal(emptyLabel, stone.LabelNoCount, "empty shell never retains stale soul name");
            Check.Equal(false, stone.GetInspectString().Contains("Original Soul"), "no stale inspect name");
            RuntimeLog.AssertClean();
        }
        finally { LanguageDatabase.activeLanguage = previous; LanguageDatabase.defaultLanguage = previousDefault; }
    }
}
