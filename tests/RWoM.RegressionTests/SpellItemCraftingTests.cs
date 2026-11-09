using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using AbilityDef = AbilityUser.AbilityDef;
using RimWorld;
using TorannMagic;
using TorannMagic.SpellItems;
using Verse;
using Verse.AI;

namespace RWoM.RegressionTests;

internal static class SpellItemCraftingTests
{
    const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpellItems.Crafting.KnowledgeAndRankedDuplicateOwnership", Knowledge);
        yield return new("SpellItems.Crafting.BillGateKeepsVanillaAndUnrelatedBills", BillGate);
        yield return new("SpellItems.Crafting.UnknownSpellReportsMenuReason", MenuReason);
        yield return new("SpellItems.Crafting.CompletionRechecksBeforeProductsOrPayment", CompletionGate);
        yield return new("SpellItems.Definitions.RealInheritedXmlComponentsAndRefs", InheritedXml);
        yield return new("SpellItems.Definitions.ExtendedRecipesKnowledgeCostAndTranslations", ExtendedCatalog);
    }

    static void ExtendedCatalog(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var maker = GrimoireProgressionTests.PawnWithComps(true, false);
        var document = XDocument.Load(Path.Combine(f.Repository, "v1.7/Defs/SpellItems/SpellItems.xml"));
        var definitions = document.Root!.Elements().Where(e => e.Element("defName") != null).ToDictionary(e => e.Element("defName")!.Value);
        string[] spells = ["Lightning", "Regeneration", "CureDisease", "Purify", "Soothe", "Portal", "Resurrection"];
        var prices = new Dictionary<string, float> { ["RawMagicyte"] = 5, ["Cloth"] = 1.5f, ["WoodLog"] = 1.2f, ["Gold"] = 10, ["Jade"] = 5 };
        float Cost(XElement recipe) => recipe.Element("ingredients")!.Elements("li").Sum(i => prices[i.Element("filter")!.Element("thingDefs")!.Element("li")!.Value] * (float)i.Element("count")!);
        foreach (var spell in spells)
        {
            string scrollName = "TM_CastScroll_" + spell;
            var scroll = definitions["TM_MakeCastScroll_" + spell];
            var required = scroll.Descendants("requiredSpell").Single().Value;
            var ability = Check.NotNull(DefDatabase<TorannMagic.TMAbilityDef>.GetNamedSilentFail(required), "real learned family exists: " + required);
            var requirement = new SpellItemRecipeRequirement { requiredSpell = ability };
            foreach (var power in maker.magic.MagicData.AllMagicPowers) power.learned = false;
            Check.Equal(false, requirement.Allows(maker.pawn), "unlearned family cannot craft " + spell);
            var family = maker.magic.MagicData.AllMagicPowers.First(p => p.TMabilityDefs?.Contains(ability) == true);
            family.learned = true; family.level = 2;
            Check.Equal(true, requirement.Allows(maker.pawn), "learned ranked family can craft " + spell);
            string research = spell is "Portal" or "Resurrection" ? "TM_MasterScribing" : spell is "Lightning" or "Soothe" ? "TM_MagicScribing" : "TM_AdvancedScribing";
            Check.Equal(research, scroll.Element("researchPrerequisite")!.Value, "appropriate existing research tier");
            float cost = Cost(scroll); prices[scrollName] = cost;
            if (spell is "Portal" or "Resurrection") Check.Equal(true, cost >= 1000, "ultimate scroll genuinely expensive");
            foreach (bool staff in spell == "Resurrection" ? new[] { false } : new[] { false, true })
            {
                var name = (staff ? "TM_SpellStaff_" : "TM_CastScroll_") + spell;
                var recipeName = (staff ? "TM_MakeSpellStaff_" : "TM_MakeCastScroll_") + spell;
                var recipe = definitions[recipeName];
                Check.Equal(required, recipe.Descendants("requiredSpell").Single().Value, "staff and scroll share learned gate");
                Check.Equal(research, recipe.Element("researchPrerequisite")!.Value, "same tier");
                if (staff)
                {
                    float ratio = Cost(recipe) / cost;
                    Check.Equal(true, ratio >= 7.5f && ratio <= 8.5f, "staff total raw cost includes ingredient scroll and is about eight casts");
                    var inputs = recipe.Element("ingredients")!.Elements("li").Select(i => i.Element("filter")!.Element("thingDefs")!.Element("li")!.Value).ToArray();
                    Check.Equal(true, inputs.Contains(scrollName) && inputs.Contains("WoodLog") && inputs.Contains("RawMagicyte"), "matching scroll plus wood and magicyte");
                    Check.Equal(true, inputs.Contains(spell == "Lightning" ? "Gold" : "Jade"), "attack gold, otherwise jade");
                }
                foreach (var (kind, id) in new[] { ("ThingDef", name), ("RecipeDef", recipeName) })
                {
                    var language = XDocument.Load(Path.Combine(f.Repository, "Languages/Russian/DefInjected", kind, "TM_SpellItems.xml"));
                    Check.NotNull(language.Root!.Element(id + ".label"), "Russian label for " + id);
                    Check.NotNull(language.Root!.Element(id + ".description"), "Russian description for " + id);
                }
            }
        }
        Check.Equal("Bullet", definitions["TM_ItemLightningProjectile"].Element("thingClass")!.Value, "lightning uses native independent bullet");
        Check.Equal("18", definitions["TM_ItemLightningProjectile"].Element("projectile")!.Element("damageAmountBase")!.Value, "fixed damage");
        var moodNode = new XmlDocument(); moodNode.LoadXml(definitions["TM_ItemSoothed"].ToString());
        var mood = DirectXmlToObject.ObjectFromXml<ThoughtDef>(moodNode.DocumentElement!, false);
        Check.Equal(1f, mood.durationDays, "shipped temporary mood duration"); Check.Equal(6f, mood.stages.Single().baseMoodEffect, "shipped mood magnitude");
        Check.Equal(1, mood.stackLimit, "shipped thought cannot stack");
        DirectXmlCrossRefLoader.Clear();
    }

    static void Knowledge(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var p = GrimoireProgressionTests.PawnWithComps(true, false);
        var data = p.magic.MagicData;
        var requirement = new SpellItemRecipeRequirement { requiredSpell = TorannMagicDefOf.TM_Fireball };
        Check.Equal(false, requirement.Allows(p.pawn), "mage must know spell");
        var family = data.MagicPowersIF.Single(x => x.TMabilityDefs.Contains(requirement.requiredSpell));
        family.learned = true;
        family.level = 2;
        Check.Equal(true, requirement.Allows(p.pawn), "leveled own family accepted");
        GrimoireProgressionTests.Set(p.magic, "isMagicUser", false);
        Check.Equal(false, requirement.Allows(p.pawn), "mere learned data without mage state insufficient");
        GrimoireProgressionTests.Set(p.magic, "isMagicUser", true);
        var duplicate = new MagicPower(new List<AbilityDef> { requirement.requiredSpell }) { learned = false };
        data.MagicPowersCustom.Insert(0, duplicate);
        Check.Equal(true, requirement.Allows(p.pawn), "unlearned first duplicate does not mask learned family");
        requirement.requiredSpell = TorannMagicDefOf.TM_Blink;
        Check.Equal(false, requirement.Allows(p.pawn), "other spell not granted by unrelated knowledge");
        Check.Equal(false, requirement.Allows(null!), "missing maker denied");
        var ordinary = GrimoireProgressionTests.PawnWithComps(false, false);
        Check.Equal(false, requirement.Allows(ordinary.pawn), "ordinary pawn cannot scribe");
    }

    static MethodInfo BillPostfix => typeof(CompSpellItem).Assembly.GetType("TorannMagic.SpellItems.SpellItemBillKnowledgePatch")!
        .GetMethod("Postfix", Flags)!;

    static bool Allowed(RecipeDef recipe, Pawn pawn, bool vanilla)
    {
        var bill = (Bill_Production)RuntimeHelpers.GetUninitializedObject(typeof(Bill_Production)); bill.recipe = recipe;
        object[] args = [bill, pawn, vanilla]; BillPostfix.Invoke(null, args); return (bool)args[2];
    }

    static void BillGate(RuntimeFixture f)
    {
        using var language = new SpellItemEffectTests.LanguageScope(f.Repository);
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var p = GrimoireProgressionTests.PawnWithComps(true, false);
        var requirement = new SpellItemRecipeRequirement { requiredSpell = TorannMagicDefOf.TM_Fireball };
        var recipe = new RecipeDef { defName = "SpellFixture", modExtensions = [requirement] };
        Check.Equal(false, Allowed(recipe, p.pawn, true), "unknown spell bill rejected");
        p.magic.MagicData.MagicPowersIF.Single(x => x.TMabilityDefs.Contains(requirement.requiredSpell)).learned = true;
        Check.Equal(true, Allowed(recipe, p.pawn, true), "learned recipe accepted");
        Check.Equal(false, Allowed(recipe, p.pawn, false), "vanilla denial never overridden");
        Check.Equal(true, Allowed(new RecipeDef { defName = "OrdinaryRecipe" }, p.pawn, true), "other scribing recipes unchanged");
        JobFailReason.Clear();
    }

    static void MenuReason(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var p = GrimoireProgressionTests.PawnWithComps(true, false);
        var requirement = new SpellItemRecipeRequirement { requiredSpell = TorannMagicDefOf.TM_Fireball };
        var recipe = new RecipeDef { defName = "SpellFixture", label = "scribe fireball", modExtensions = [requirement] };
        using var language = new SpellItemEffectTests.LanguageScope(f.Repository);
        try
        {
            foreach (var directory in Directory.GetDirectories(Path.Combine(f.Repository, "Languages")))
            {
                var key = XDocument.Load(Path.Combine(directory, "Keyed/TM_SpellItems.xml"))
                    .Root!.Element("TM_SpellItem_RequiredKnowledge");
                Check.Equal(true, key?.Value.Contains("{0}") == true, "localized reason retains spell name: " + directory);
            }
            JobFailReason.Clear();
            Check.Equal(false, Allowed(recipe, p.pawn, true), "unknown spell stays unavailable");
            Check.Equal(true, JobFailReason.HaveReason, "native work menu needs a reason to show denied recipe");
            Check.Equal(true, JobFailReason.Reason.Contains(requirement.requiredSpell.LabelCap.ToString()), "reason identifies required spell");
            JobFailReason.Clear();
            p.magic.MagicData.MagicPowersIF.Single(x => x.TMabilityDefs.Contains(requirement.requiredSpell)).learned = true;
            Check.Equal(true, Allowed(recipe, p.pawn, true), "learned spell remains available");
            Check.Equal(false, JobFailReason.HaveReason, "available bill does not manufacture denial");
            JobFailReason.Is("existing vanilla restriction");
            Check.Equal(false, Allowed(recipe, p.pawn, false), "vanilla denial retained");
            Check.Equal("existing vanilla restriction", JobFailReason.Reason, "vanilla reason retained");
            JobFailReason.Clear();
            Check.Equal(true, Allowed(new RecipeDef { defName = "OldScroll" }, p.pawn, true), "old recipe unaffected");
            Check.Equal(false, JobFailReason.HaveReason, "old recipe gets no spell restriction");
        }
        finally
        {
            JobFailReason.Clear();
        }
    }

    static void CompletionGate(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var p = GrimoireProgressionTests.PawnWithComps(true, false);
        p.pawn.jobs = new Pawn_JobTracker(p.pawn);
        var requirement = new SpellItemRecipeRequirement { requiredSpell = TorannMagicDefOf.TM_Fireball };
        var recipe = new RecipeDef { defName = "SpellFixture", modExtensions = [requirement] };
        var bill = (Bill_Production)RuntimeHelpers.GetUninitializedObject(typeof(Bill_Production)); bill.recipe = recipe;
        var job = new Job { bill = bill };
        GrimoireProgressionTests.Set(p.pawn.jobs, "curJob", job);
        using var patch = new Patches();
        patch.Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!, nameof(CaptureEnd));
        var actorField = typeof(Toil).GetField("actor", Flags);
        var toil = new Toil(); actorField!.SetValue(toil, p.pawn);
        int productsAndPayment = 0; toil.initAction = () => productsAndPayment++;
        SpellItemRecipeCompletionPatch.Postfix(toil);
        ended = false; toil.initAction();
        Check.Equal(true, ended, "lost spell prevents completion");
        Check.Equal(0, productsAndPayment, "denial precedes all product and ingredient work");
        p.magic.MagicData.MagicPowersIF.Single(x => x.TMabilityDefs.Contains(requirement.requiredSpell)).learned = true;
        ended = false; toil.initAction();
        Check.Equal(false, ended, "qualified maker can finish");
        Check.Equal(1, productsAndPayment, "qualified completion delegates once");
        bill.recipe = new RecipeDef { defName = "Unrestricted" };
        toil.initAction(); Check.Equal(2, productsAndPayment, "other completion unaffected");
    }
    static bool ended;
    static bool CaptureEnd() { ended = true; return false; }

    static void InheritedXml(RuntimeFixture f)
    {
        DirectXmlCrossRefLoader.Clear(); XmlInheritance.Clear();
        var file = new XmlDocument(); file.Load(Path.Combine(f.Repository,"v1.7/Defs/SpellItems/SpellItems.xml"));
        var takeNode = Check.NotNull(file.SelectSingleNode("Defs/OrderedTakeGroupDef[defName='TM_SpellItems']"), "spell item inventory pickup group declared");
        var takeGroup = DirectXmlToObject.ObjectFromXml<OrderedTakeGroupDef>(takeNode, false);
        // The pickup policy does not need ThingDef's graphics-initializing constructor.
        var pickupDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pickupDef.orderedTakeGroup = takeGroup;
        Check.Equal(int.MaxValue, PawnUtility.GetMaxAllowedToPickUp(pickupDef), "no gameplay quantity cap; vanilla pickup still checks mass separately");
        string game = Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        // Register minimal actual parent inheritance declarations rather than generating items.
        var allCore = Directory.GetFiles(Path.Combine(game,"Data/Core/Defs"),"*.xml",SearchOption.AllDirectories)
            .Select(path => { var doc = new XmlDocument(); doc.Load(path); return doc; }).ToList();
        var parentNames = new HashSet<string> { "BaseMeleeWeapon", "BaseBullet" };
        var nodes = allCore.SelectMany(doc => doc.DocumentElement!.ChildNodes.Cast<XmlNode>())
            .Where(n => n.NodeType == XmlNodeType.Element && n.Attributes?["Name"] != null)
            .GroupBy(n => n.Attributes!["Name"]!.Value).ToDictionary(g => g.Key,g => g.First());
        void RegisterParent(string name)
        {
            if (!nodes.TryGetValue(name,out var node)) throw new InvalidOperationException("Missing parent " + name);
            string? parent = node.Attributes?["ParentName"]?.Value;
            if (parent != null) RegisterParent(parent);
            XmlInheritance.TryRegister(node,null);
        }
        RegisterParent("BaseMeleeWeapon"); RegisterParent("BaseBullet");
        foreach(XmlNode node in file.DocumentElement!.ChildNodes)
            if(node.NodeType==XmlNodeType.Element) XmlInheritance.TryRegister(node,null);
        XmlInheritance.Resolve();
        foreach(XmlNode node in file.DocumentElement.ChildNodes)
        {
            if(node.Name!="ThingDef" || node.Attributes?["Abstract"]?.Value=="True")continue;
            var resolved=XmlInheritance.GetResolvedNodeFor(node);
            string id=resolved.SelectSingleNode("defName")!.InnerText;
            if(!id.StartsWith("TM_CastScroll_") && !id.StartsWith("TM_SpellStaff_"))continue;
            var comps=resolved.SelectNodes("comps/li")!.Cast<XmlNode>().ToList();
            var spell=comps.Single(n=>n.Attributes?["Class"]?.Value==typeof(CompProperties_SpellItem).FullName);
            var props=DirectXmlToObject.ObjectFromXml<CompProperties_SpellItem>(spell,false);
            Check.Equal(typeof(CompSpellItem),props.compClass,id+" real XML component class");
            bool staff=id.StartsWith("TM_SpellStaff_");
            Check.Equal(takeGroup.defName, resolved.SelectSingleNode("orderedTakeGroup")?.InnerText, id+" inherits vanilla inventory pickup permission");
            Check.Equal(staff?10:1,props.maxUses,id+" uses");
            Check.Equal(staff,props.requiresEquipped,id+" holder rule");
            Check.Equal(0,comps.Count(n=>n.Attributes?["Class"]?.Value=="CompProperties_Quality"),id+" no quality refill mechanism");
            if(staff) Check.Equal("True",resolved.SelectSingleNode("recipeMaker")?.Attributes?["IsNull"]?.Value,id+" no inherited ungated recipe");
            string tex=resolved.SelectSingleNode("graphicData/texPath")!.InnerText;
            Check.Equal(true,File.Exists(Path.Combine(f.Repository,"Textures",tex+".png")),id+" texture exists");
        }
        DirectXmlCrossRefLoader.Clear();XmlInheritance.Clear();
    }

    sealed class Patches : IDisposable
    {
        readonly Assembly a=Assembly.Load("0Harmony");readonly object harmony;readonly Type type;readonly string id="rwom.spellitems.crafting";
        public Patches(){type=a.GetType("HarmonyLib.Harmony")!;harmony=Activator.CreateInstance(type,id)!;}
        public void Prefix(MethodBase original,string method)
        {
            var prefix=Activator.CreateInstance(a.GetType("HarmonyLib.HarmonyMethod")!,typeof(SpellItemCraftingTests).GetMethod(method,Flags)!)!;
            type.GetMethods().Single(m=>m.Name=="Patch"&&m.GetParameters().Length==5).Invoke(harmony,[original,prefix,null,null,null]);
        }
        public void Dispose()=>type.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);
    }
}
