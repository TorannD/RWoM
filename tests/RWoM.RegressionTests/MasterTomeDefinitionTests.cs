using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using Verse;

namespace RWoM.RegressionTests;

internal static class MasterTomeDefinitionTests
{
    
    static XElement Definition(RuntimeFixture f, string Id) => XDocument.Load(Path.Combine(f.Repository,
        Id == "TM_MasterTomeEmpath" ? "v1.7/Defs/ThingDefs/TM_EmpathBooks.xml" : "v1.7/Defs/ThingDefs/TM_MasteryTomes.xml")).Root!.Elements("ThingDef").Single(e => (string?)e.Element("defName") == Id);

    public static IEnumerable<TestCase> Cases()
    {
        foreach (var (id, texture) in new[] { ("TM_MasterTomeChaos", "Books/chaos"), ("TM_MasterTomePriest", "Books/priest"), ("TM_MasterTomeNecromancer", "Books/necromancer"), ("TM_MasterTomeFire", "Books/fire"), ("TM_MasterTomeIce", "Books/ice"), ("TM_MasterTomeLightning", "Books/lightning"), ("TM_MasterTomeArcanist", "Books/arcane"), ("TM_MasterTomePaladin", "Books/paladin"), ("TM_MasterTomeSummoner", "Books/summoner"), ("TM_MasterTomeDruid", "Books/druid"), ("TM_MasterTomeBard", "Books/bard"), ("TM_MasterTomeWarlock", "Books/warlock"), ("TM_MasterTomeSuccubus", "Books/warlock"), ("TM_MasterTomeGeomancer", "Books/earth"), ("TM_MasterTomeTechnomancer", "Books/technomancer"), ("TM_MasterTomeBloodMage", "Books/bloodmage"), ("TM_MasterTomeEnchanter", "Books/enchanter"), ("TM_MasterTomeChronomancer", "Books/chronomancer"), ("TM_MasterTomeBrightmage", "Books/brightmage"), ("TM_MasterTomeShaman", "Books/shaman2"), ("TM_MasterTomeGolemancer", "Books/golemancer"), ("TM_MasterTomeEmpath", "Books/tome") })
            foreach (var test in ForTome(id, texture)) yield return test;
    }

    static IEnumerable<TestCase> ForTome(string Id, string expectedTexture)
    {
        yield return new("MasterTome.DefinitionAndNoCrafting." + Id, f =>
        {
            var node = Definition(f, Id);
            Check.Equal("ResourceBase", (string?)node.Attribute("ParentName"), "no legacy class-book parent");
            Check.Equal(expectedTexture, node.Element("graphicData")!.Element("texPath")!.Value, "existing class book art reused");
            Check.Equal(true, File.Exists(Path.Combine(f.Repository, "Textures", expectedTexture + ".png")), "class book texture exists");
            Check.Equal("False", (string?)node.Element("comps")!.Attribute("Inherit"), "explicit comp list has stable localization indexes");
            var effects = node.Element("comps")!.Elements().Where(e => (string?)e.Attribute("Class") == "CompProperties_UseEffect").ToList();
            Check.Equal(1, effects.Count, "one payment/grant owner");
            Check.Equal("TorannMagic.ClassProgression.CompUseEffect_MasterTome", effects[0].Element("compClass")!.Value, "dedicated mastery effect");
            Check.Equal(false, node.Descendants().Any(e => e.Value == "TorannMagic.CompUseEffect_LearnMagic"), "no random legacy class effect");
            Check.Equal(false, node.Descendants("recipeMaker").Any(), "no automatic crafting recipe");
            foreach (var file in Directory.EnumerateFiles(Path.Combine(f.Repository, "v1.7/Defs"), "*.xml", SearchOption.AllDirectories))
            {
                var doc = XDocument.Load(file);
                Check.Equal(false, doc.Descendants("products").Elements().Any(e => e.Name.LocalName == Id), "no recipe produces mastery tome: " + file);
            }
            var langs = new[] { "English", "Russian", "Ukrainian", "ChineseSimplified", "Spanish", "Portuguese", "PortugueseBrazilian" };
            foreach (var lang in langs)
            {
                var nodes = XDocument.Load(Path.Combine(f.Repository, "v1.7/Languages", lang, Id == "TM_MasterTomeEmpath" ? "DefInjected/ThingDef/TM_EmpathBooks.xml" : "DefInjected/ThingDef/TM_MasteryTomes.xml")).Root!.Elements().Where(e => e.Name.LocalName.StartsWith(Id + ".")).ToList();
                Check.Equal(3, nodes.Count, "localized label, description and use action: " + lang);
                Check.Equal(3, nodes.Select(e => e.Name.LocalName).Distinct().Count(), "unique locale keys: " + lang);
                foreach (string suffix in new[] { "label", "description", "comps.1.useLabel" })
                    Check.Equal(true, nodes.Any(e => e.Name.LocalName == Id + "." + suffix && !string.IsNullOrWhiteSpace(e.Value)), "translation present: " + lang + "/" + suffix);
                Check.Equal(true, nodes.Single(e => e.Name.LocalName.EndsWith("useLabel")).Value.Contains("{0_label}"), "item placeholder retained");
            }
        });
        yield return new("MasterTome.NativeTradeAndQuestEligibility." + Id, f =>
        {
            var node = Definition(f, Id);
            // ThingDef's constructor loads renderer shaders; native eligibility only
            // needs the authored item fields, not a Unity graphics environment.
            var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            def.defName = Id;
            def.thingClass = typeof(ThingWithComps);
            def.category = Enum.Parse<ThingCategory>(node.Element("category")!.Value);
            def.alwaysHaulable = bool.Parse(node.Element("alwaysHaulable")!.Value);
            def.graphicData = (GraphicData)RuntimeHelpers.GetUninitializedObject(typeof(GraphicData));
            def.graphicData.texPath = node.Element("graphicData")!.Element("texPath")!.Value;
            def.tradeability = Enum.Parse<Tradeability>(node.Element("tradeability")!.Value);
            def.techLevel = Enum.Parse<TechLevel>(node.Element("techLevel")!.Value);
            def.tradeTags = node.Element("tradeTags")!.Elements().Select(e => e.Value).ToList();
            def.thingSetMakerTags = node.Element("thingSetMakerTags")!.Elements().Select(e => e.Value).ToList();
            def.comps = [];
            def.virtualDefs = [];
            Check.Equal(true, def.PlayerAcquirable, "native player acquisition allowed");
            Check.Equal(true, def.tradeability.TraderCanSell(), "native trader sale allowed");
            var traders = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/TraderKindDefs/TraderKinds_Arcane.xml"));
            var tags = traders.Descendants("li").Where(e => (string?)e.Attribute("Class") == "StockGenerator_Tag")
                .Select(e => (string?)e.Element("tradeTag")).Where(t => t != null).Distinct();
            Check.Equal(true, tags.Any(tag => new StockGenerator_Tag { tradeTag = tag! }.HandlesThingDef(def)), "existing native stock generator accepts tome");
            Check.Equal(true, ThingSetMakerUtility.CanGenerate(def), "native reward item shape accepted");

            string[] args = Environment.GetCommandLineArgs();
            int gameArg = Array.IndexOf(args, "--game");
            Check.Equal(true, gameArg >= 0 && gameArg + 1 < args.Length, "harness provides native game directory");
            var rewards = XDocument.Load(Path.Combine(args[gameArg + 1], "Data/Core/Defs/ThingSetMakerDefs/ThingSetMakers_Reward.xml"));
            var standard = rewards.Root!.Elements().Single(e => (string?)e.Element("defName") == "Reward_ItemsStandard");
            var itemBranch = standard.Element("root")!.Element("options")!.Elements("li").First();
            float maxReward = float.Parse(itemBranch.Element("maxMarketValue")!.Value, CultureInfo.InvariantCulture);
            float price = float.Parse(node.Element("statBases")!.Element("MarketValue")!.Value, CultureInfo.InvariantCulture);
            Check.Equal(true, price > 3300f && price <= maxReward, "rarer than full tome but within native standard reward budget");
            string rewardTag = def.thingSetMakerTags.Single();
            Check.Equal(true, itemBranch.Descendants("thingSetMakerTagsToAllow").Elements().Any(e => e.Value == rewardTag), "native standard quest rewards include tome tag");
            var previous = DefDatabase<ThingDef>.AllDefsListForReading.ToList();
            try
            {
                DefDatabase<ThingDef>.Add(def);
                var filter = new ThingFilter();
                typeof(ThingFilter).GetField("thingSetMakerTagsToAllow", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(filter, new List<string> { rewardTag });
                filter.ResolveReferences();
                Check.Equal(true, filter.Allows(def), "native reward tag resolves to tome");
            }
            finally { DefDatabase<ThingDef>.Clear(); foreach (var old in previous) DefDatabase<ThingDef>.Add(old); }
        });
    }
}
