using System.Xml;
using System.Xml.Linq;
using RimWorld;
using Verse;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RWoM.RegressionTests;

internal static class OdysseyMagofuelTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.Magofuel.OptionalLoadingAndRecipe", Recipe);
        yield return new("Odyssey.Magofuel.NativeRecipeAndResearchXml", NativeXml);
        yield return new("Odyssey.Magofuel.SevenLanguageCoverage", Languages);
        yield return new("Odyssey.Magofuel.ResolvedRecipeProducesAndConsumesNativeItems", Production);
    }

    static XDocument Catalog(RuntimeFixture f) => XDocument.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Magofuel.xml"));

    static void Recipe(RuntimeFixture f)
    {
        var folders = XDocument.Load(Path.Combine(f.Repository, "LoadFolders.xml"));
        var folder = folders.Root!.Element("v1.7")!.Elements("li").SingleOrDefault(x => x.Value == "v1.7/Odyssey");
        Check.Equal("ludeon.rimworld.odyssey", (string?)folder?.Attribute("IfModActive"), "content only loaded with Odyssey");
        Check.Equal(false, folders.Root.Elements().Where(x => x.Name != "v1.7").Descendants("li").Any(x => x.Value == "v1.7/Odyssey"), "older versions unaffected");
        var defs = Catalog(f).Root!.Elements().ToDictionary(x => x.Element("defName")!.Value);
        var recipe = defs["TM_RefineMagofuel"];
        var amounts = recipe.Element("ingredients")!.Elements("li").ToDictionary(x => x.Element("filter")!.Element("thingDefs")!.Element("li")!.Value, x => (int)x.Element("count")!);
        Check.Equal(2, amounts.Count, "exactly two physical inputs");
        Check.Equal(100, amounts["Chemfuel"], "chemfuel payment");
        Check.Equal(50, amounts["RawMagicyte"], "magicyte payment");
        Check.Equal(100, (int)recipe.Element("products")!.Element("TM_Magofuel")!, "batch yield");
        Check.Equal("BiofuelRefinery", recipe.Element("recipeUsers")!.Elements().Single().Value, "crafted at the vanilla biofuel refinery");
        Check.Equal("TM_MagofuelRefining", recipe.Element("researchPrerequisite")!.Value, "research gates recipe");
        var requirements = defs["TM_MagofuelRefining"].Element("prerequisites")!.Elements().Select(x => x.Value).ToHashSet();
        Check.Equal(true, requirements.SetEquals(["BiofuelRefining", "BasicGravtech"]), "vanilla fuel chain required, no arcane forge");
        var forge = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/ThingDefs/Buildings_Magic_Production.xml"));
        Check.Equal(true, forge.Descendants("defName").Any(x => x.Value == "TableArcaneForge"), "real forge exists");
        var work = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/WorkGiverDefs/WorkGivers_Magic.xml")).Root!.Elements().Single(x => (string?)x.Element("defName") == "DoBillsArcaneForge");
        Check.Equal("MagicManipulation", work.Element("requiredCapacities")!.Elements().Single().Value, "retains magical labor gate");
        var fuel = defs["TM_Magofuel"];
        Check.Equal("ResourceBase", (string?)fuel.Attribute("ParentName"), "independent resource, not chemfuel replacement");
        Check.Equal(false, fuel.Descendants("recipeMaker").Any(), "no ungated automatic recipe");
        string texture = fuel.Element("graphicData")!.Element("texPath")!.Value;
        Check.Equal(true, File.Exists(Path.Combine(f.Repository, "v1.7/Odyssey/Textures", texture + ".png")), "dedicated fuel texture shipped with optional module");
        var stat = fuel.Element("statBases")!;
        Check.Equal(true, (float)stat.Element("Mass")! > 0, "carried fuel has mass");
        Check.Equal(true, (int)fuel.Element("stackLimit")! > 0, "valid stack size");
        string game = Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var sources = new[] { Path.Combine(f.Repository, "v1.7/Defs"), Path.Combine(game, "Data/Core/Defs"), Path.Combine(game, "Data/Odyssey/Defs") };
        var known = sources.SelectMany(dir => Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories))
            .SelectMany(path => XDocument.Load(path).Root!.Elements())
            .Concat(defs.Values).Where(x => x.Element("defName") != null).ToList();
        foreach (var id in amounts.Keys.Append("TableArcaneForge").Append("BiofuelRefinery"))
            Check.Equal(true, known.Any(x => x.Name.LocalName == "ThingDef" && (string?)x.Element("defName") == id), "real resource/station reference " + id);
        foreach (var id in requirements)
            Check.Equal(true, known.Any(x => x.Name.LocalName == "ResearchProjectDef" && (string?)x.Element("defName") == id), "real research reference " + id);
        var researchNode = defs["TM_MagofuelRefining"];
        Check.Equal(false, known.Where(x => x.Name.LocalName == "ResearchProjectDef" && (string?)x.Element("defName") != "TM_MagofuelRefining")
            .Any(x => (string?)x.Element("tab") == "TMagic" && (float?)x.Element("researchViewX") == (float?)researchNode.Element("researchViewX")
                && (float?)x.Element("researchViewY") == (float?)researchNode.Element("researchViewY")), "research does not overlap existing magic nodes");
    }

    static void NativeXml(RuntimeFixture f)
    {
        DirectXmlCrossRefLoader.Clear();
        // The recipe now targets the vanilla biofuel refinery; register it so the
        // isolated single-file parse can resolve the recipeUsers cross-reference.
        try
        {
            var document = new XmlDocument(); document.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Magofuel.xml"));
            // recipeUsers points at the vanilla biofuel refinery, which is not part of
            // this single-file parse; the XML-level case checks that reference.
            document.SelectSingleNode("Defs/RecipeDef/recipeUsers")?.ParentNode?.RemoveChild(
                document.SelectSingleNode("Defs/RecipeDef/recipeUsers")!);
            var recipe = DirectXmlToObject.ObjectFromXml<RecipeDef>(document.SelectSingleNode("Defs/RecipeDef")!, false);
            Check.Equal(12000f, recipe.workAmount, "native parser accepts work amount");
            Check.Equal(2, recipe.ingredients.Count, "native ingredient records");
            Check.Equal(100f, recipe.ingredients[0].GetBaseCount(), "native chemfuel count");
            Check.Equal(50f, recipe.ingredients[1].GetBaseCount(), "native magicyte count");
            var research = DirectXmlToObject.ObjectFromXml<ResearchProjectDef>(document.SelectSingleNode("Defs/ResearchProjectDef")!, false);
            Check.Equal(2000f, research.baseCost, "native research cost");
        }
        finally { DirectXmlCrossRefLoader.Clear(); }
    }

    static void Languages(RuntimeFixture f)
    {
        string[] languages = ["English", "Russian", "Ukrainian", "Spanish", "ChineseSimplified", "Portuguese", "PortugueseBrazilian"];
        foreach (var language in languages)
        foreach (var def in Catalog(f).Root!.Elements())
        {
            var fields = def.Elements().Where(x => x.Name.LocalName is "label" or "description" or "jobString").ToList();
            if (language == "English") continue; // English comes from source definitions.
            string path = Path.Combine(f.Repository, "v1.7/Odyssey/Languages", language, "DefInjected", def.Name.LocalName, "TM_Magofuel.xml");
            var entries = XDocument.Load(path).Root!.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
            foreach (var field in fields)
            {
                string key = def.Element("defName")!.Value + "." + field.Name.LocalName;
                Check.Equal(true, entries.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value), language + " translated " + key);
            }
        }
    }

    static void Production(RuntimeFixture f)
    {
        using var things = new DefScope<ThingDef>();
        using var stats = new DefScope<StatDef>();
        using var skills = new DefScope<SkillDef>();
        using var effects = new DefScope<EffecterDef>();
        using var sounds = new DefScope<SoundDef>();
        using var tabs = new DefScope<ResearchTabDef>();
        using var projects = new DefScope<ResearchProjectDef>();
        ThingDef Resource(string name)
        {
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = name; def.label = name; def.thingClass = typeof(ThingWithComps);
            def.category = ThingCategory.Item; def.stackLimit = 150; def.comps = [];
            def.statBases = []; def.virtualDefs = []; def.useHitPoints = false; def.destroyable = true;
            things.Add(def); return def;
        }
        var chem = Resource("Chemfuel"); var magic = Resource("RawMagicyte");
        var fuel = Resource("TM_Magofuel"); var refinery = Resource("BiofuelRefinery");
        stats.Add(new StatDef { defName = "WorkSpeedGlobal" });
        skills.Add(new SkillDef { defName = "Intellectual" });
        effects.Add(new EffecterDef { defName = "Smelt" });
        sounds.Add(new SoundDef { defName = "Recipe_Machining" });
        tabs.Add(new ResearchTabDef { defName = "TMagic" });
        projects.Add(new ResearchProjectDef { defName = "TM_ArcaneForging" });
        projects.Add(new ResearchProjectDef { defName = "BasicGravtech" });
        projects.Add(new ResearchProjectDef { defName = "BiofuelRefining" });
        var oldGame = Current.Game;
        Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
        typeof(Game).GetField("maps", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Current.Game, new List<Map>());
        typeof(Game).GetField("uniqueIDsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(Current.Game, new UniqueIDsManager());
        DirectXmlCrossRefLoader.Clear();
        try
        {
            var xml = new XmlDocument(); xml.Load(Path.Combine(f.Repository, "v1.7/Odyssey/Defs/Magofuel.xml"));
            var research = DirectXmlToObject.ObjectFromXml<ResearchProjectDef>(xml.SelectSingleNode("Defs/ResearchProjectDef")!, false);
            projects.Add(research);
            var recipe = DirectXmlToObject.ObjectFromXml<RecipeDef>(xml.SelectSingleNode("Defs/RecipeDef")!, false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            recipe.fixedIngredientFilter.ResolveReferences();
            foreach (var ingredient in recipe.ingredients) ingredient.filter.ResolveReferences();
            Check.Same(research, recipe.researchPrerequisite, "research link resolved");
            Check.Same(refinery, recipe.recipeUsers.Single(), "recipeUsers resolve to the vanilla biofuel refinery");
            Check.Same(fuel, recipe.products.Single().thingDef, "product link resolved");
            Check.Equal(true, recipe.ingredients[0].filter.Allows(chem), "chemfuel accepted by first ingredient");
            Check.Equal(false, recipe.ingredients[0].filter.Allows(magic), "magicyte cannot replace chemfuel");
            Check.Equal(true, recipe.ingredients[1].filter.Allows(magic), "magicyte accepted by second ingredient");
        }
        finally { DirectXmlCrossRefLoader.Clear(); Current.Game = oldGame; }

    }
    sealed class DefScope<T> : IDisposable where T : Def
    {
        readonly T[] old = DefDatabase<T>.AllDefsListForReading.ToArray();
        public void Add(T def)
        {
            // Test-local database; restore the exact previous objects on disposal.
            var keep = DefDatabase<T>.AllDefsListForReading.Where(x => x.defName != def.defName).ToArray();
            DefDatabase<T>.Clear(); foreach (var item in keep) DefDatabase<T>.Add(item);
            DefDatabase<T>.Add(def);
        }
        public void Dispose() { DefDatabase<T>.Clear(); foreach (var def in old) DefDatabase<T>.Add(def); }
    }
}

