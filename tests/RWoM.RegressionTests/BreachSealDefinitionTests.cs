using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static class BreachSealDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.BreachSeal.Definitions", Definitions);
        yield return new("Odyssey.BreachSeal.GrimoireGenericFilter", Grimoire);
        yield return new("Odyssey.BreachSeal.SevenLanguages", Languages);
        yield return new("Odyssey.BreachSeal.Artwork", Artwork);
    }
    static void Grimoire(RuntimeFixture f)
    {
        var prior=DefDatabase<TMAbilityDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<TMAbilityDef>.Clear();
            var shell=(TMAbilityDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef)); shell.defName="TM_BreachSeal";
            var other=(TMAbilityDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef)); other.defName="UnrelatedCustomClassPower";
            DefDatabase<TMAbilityDef>.Add(shell); DefDatabase<TMAbilityDef>.Add(other);
            var window=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Window_TMGrimoire));
            var method=typeof(Window_TMGrimoire).GetMethod("MagicAbilityFilter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var filter=(List<TMAbilityDef>)method.Invoke(window,new object?[]{null})!;
            Check.Equal(true,filter.Contains(shell),"generic scroll power is available outside a class group");
            Check.Equal(false,filter.Contains(other),"unrelated class powers remain hidden");
            DefDatabase<TMAbilityDef>.Clear();
            filter=(List<TMAbilityDef>)method.Invoke(window,new object?[]{null})!;
            Check.Equal(0,filter.Count,"DLC absent has no dangling ability");
        }
        finally { DefDatabase<TMAbilityDef>.Clear(); foreach(var d in prior)DefDatabase<TMAbilityDef>.Add(d); }
    }
    static XDocument Document(RuntimeFixture f) => XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/BreachSeal.xml"));
    static void Definitions(RuntimeFixture f)
    {
        var doc=Document(f);
        XElement Def(string name)=>doc.Root!.Elements().Single(e=>(string?)e.Element("defName")==name);
        var ability=Def("TM_BreachSeal");
        Check.Equal("0.3",ability.Element("manaCost")!.Value,"base mana");
        Check.Equal("0.1",ability.Element("efficiencyReductionPercent")!.Value,"native efficiency scaling");
        Check.Equal("SpellOf_BreachSeal",ability.Element("learnItem")!.Value,"learning item");
        var verb=ability.Element("MainVerb")!;
        foreach(var pair in new[]{("range","15"),("warmupTime","1"),("SecondsToRecharge","60"),("verbClass","TorannMagic.Verb_BreachSeal"),("isViolent","false")})
            Check.Equal(pair.Item2,verb.Element(pair.Item1)!.Value,pair.Item1);
        var xml=new XmlDocument(); xml.LoadXml(Def("TM_BreachSealWall").ToString());
        var wall=Def("TM_BreachSealWall");
        var building=DirectXmlToObject.ObjectFromXml<BuildingProperties>(xml.DocumentElement!.SelectSingleNode("building")!,false);
        var wallClass=typeof(Building_TMBarrier).Assembly.GetType(wall.Element("thingClass")!.Value)!;
        Check.Equal(typeof(Building_TMBarrier),wallClass.BaseType!,"reuse original arcane barrier drawing");
        foreach(var value in new[]{("passability","Impassable"),("fillPercent","1"),("holdsRoof","true"),("tickerType","Normal"),("leaveResourcesWhenKilled","false"),("resourcesFractionWhenDeconstructed","0")})
            Check.Equal(value.Item2,wall.Element(value.Item1)!.Value,"native wall property "+value.Item1);
        Check.Equal(true,building.isEdifice && building.isWall && building.isAirtight,"native region and airtight contracts");
        Check.Equal(false,building.isStuffableAirtight,"fixed airtightness without stuff");
        Check.Equal(false,building.IsDeconstructible || building.claimable || building.repairable,"no construction resource exploitation");
        Check.Equal(true,wall.Attribute("ParentName")==null,"standalone wall inherits no hidden material/timer/build settings");
        foreach(var absent in new[]{"minifiedDef","designationCategory","stuffCategories","costList","costStuffCount","killedLeavings"})
            Check.Equal(true,wall.Element(absent)==null,"no "+absent);
        var timer=DirectXmlToObject.ObjectFromXml<CompProperties>(xml.DocumentElement!.SelectSingleNode("comps/li")!,false) as CompProperties_DestroyAfterDelay;
        Check.NotNull(timer,"native expiry comp resolves");
        Check.Equal(1,wall.Element("comps")!.Elements().Count(),"native timer is the only wall comp");
        Check.Equal(60000,timer!.delayTicks,"one day");
        Check.Equal(DestroyMode.Vanish,timer.destroyMode,"clean native expiry");
        Check.Equal(typeof(CompDestroyAfterDelay),timer.compClass,"native save/transport-aware timer");
        Check.Equal("300",wall.Element("statBases")!.Element("MaxHitPoints")!.Value,"fixed wall durability");
        DirectXmlCrossRefLoader.Clear();
        xml.LoadXml(verb.ToString());
        foreach(var name in new[]{"abilityDef","soundCast","defaultProjectile"})
        {
            var reference=xml.DocumentElement!.SelectSingleNode(name);
            if(reference!=null)xml.DocumentElement.RemoveChild(reference);
        }
        var nativeVerb=DirectXmlToObject.ObjectFromXml<AbilityUser.VerbProperties_Ability>(xml.DocumentElement!,false);
        Check.Equal(AbilityUser.AbilityTargetCategory.TargetLocation,nativeVerb.AbilityTargetCategory,"location targeting category resolves");
        Check.Equal("TorannMagic.Verb_BreachSeal",nativeVerb.verbClass.FullName!,"learned verb resolves");
        Check.Equal(true,nativeVerb.targetParams.canTargetLocations && !nativeVerb.targetParams.canTargetPawns && !nativeVerb.targetParams.canTargetBuildings && !nativeVerb.targetParams.canTargetSelf,"targets cells only");
        var abilityClass=typeof(MagicAbility).Assembly.GetType(ability.Element("abilityClass")!.Value);
        Check.Equal("BreachSealAbility",abilityClass!.Name,"payment-safe ability subclass resolves");
        var power=Def("TM_BreachSealPower").Element("customPower")!;
        Check.Equal("true",power.Element("forMage")!.Value,"generic mage registration");
        Check.Equal("true",power.Element("requiresScroll")!.Value,"scroll required");
        Check.Equal("0",power.Element("maxLevel")!.Value,"single fixed-duration ability");
        var skill=power.Element("skills")!.Elements().Single(e => e.Element("label")!.Value.EndsWith("_eff"));
        Check.Equal("TM_BreachSeal_eff",skill.Element("label")!.Value,"efficiency skill retained"); Check.Equal("3",skill.Element("levelMax")!.Value,"three levels");
        foreach(var pair in new[]{("TM_CastScroll_BreachSeal","1"),("TM_SpellStaff_BreachSeal","10")})
        {
            var item=Def(pair.Item1); var props=item.Element("comps")!.Elements().Single();
            var itemXml=new XmlDocument();itemXml.LoadXml(props.ToString());
            var native=DirectXmlToObject.ObjectFromXml<TorannMagic.SpellItems.CompProperties_SpellItem>(itemXml.DocumentElement!,false);
            Check.Equal(TorannMagic.SpellItems.SpellItemKind.BreachSeal,native.spell,"native enum resolves");
            Check.Equal(typeof(TorannMagic.SpellItems.CompSpellItem),native.compClass,"native item component resolves");
            Check.Equal(pair.Item1.StartsWith("TM_SpellStaff"),native.requiresEquipped,"native ownership mode");
            Check.Equal(3600,native.cooldownTicks,"native cooldown");
            Check.Equal("BreachSeal",props.Element("spell")!.Value,"shared fixed effect");
            Check.Equal(pair.Item2,props.Element("maxUses")!.Value,"uses");
        }
        foreach(var name in new[]{"TM_MakeCastScroll_BreachSeal","TM_MakeSpellStaff_BreachSeal"})
        {
            Check.Equal("TM_BreachSeal",Def(name).Element("modExtensions")!.Element("li")!.Element("requiredSpell")!.Value,"crafting requires knowledge");
            var product=Def(name).Element("products")!.Elements().Single();
            Check.Equal(name.Replace("Make", ""),product.Name.LocalName,"recipe produces its own casting item");
            Check.Equal("1",product.Value,"one crafted item");
        }
        Check.Equal("ArcaneSpellBase",Def("SpellOf_BreachSeal").Attribute("ParentName")!.Value,"established learn-scroll behavior");
        var oldResearch=DefDatabase<ResearchProjectDef>.AllDefsListForReading.ToArray();
        string game=Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var known=new[]{Path.Combine(f.Repository,"v1.7/Defs"),Path.Combine(game,"Data/Core/Defs"),Path.Combine(game,"Data/Odyssey/Defs")}
            .SelectMany(dir=>Directory.GetFiles(dir,"*.xml",SearchOption.AllDirectories)).SelectMany(path=>XDocument.Load(path).Root!.Elements()).Concat(doc.Root!.Elements()).ToArray();
        foreach(var id in new[]{"TM_AdvancedScribing","Projectile_TM_SpawnThing","TM_Vibration","RawMagicyte","Cloth","DevilstrandCloth","WoodLog","Jade","TableMagicPrinter","Smelt","UnfinishedWeapon","WorkSpeedGlobal","Intellectual","Crafting"})
            Check.Equal(true,known.Any(d=>(string?)d.Element("defName")==id),"real external definition "+id);
        foreach(var parent in doc.Root.Elements().Attributes("ParentName").Select(a=>a.Value).Distinct())
            Check.Equal(true,known.Any(d=>(string?)d.Attribute("Name")==parent),"real XML parent "+parent);
        // Resolve inherited item nodes through the native loader, not a parallel merge implementation.
        XmlInheritance.Clear();
        try
        {
            var bases=new XmlDocument();bases.Load(Path.Combine(f.Repository,"v1.7/Defs/SpellItems/SpellItems.xml"));
            var registered = new HashSet<string>();
            void RegisterParent(string name)
            {
                if (!registered.Add(name)) return;
                var definition=known.Single(d=>(string?)d.Attribute("Name")==name);
                var parent=(string?)definition.Attribute("ParentName");
                if(parent!=null) RegisterParent(parent);
                var document=new XmlDocument();document.LoadXml(definition.ToString());
                XmlInheritance.TryRegister(document.DocumentElement!,null);
            }
            RegisterParent("BaseMeleeWeapon");
            foreach(var name in new[]{"TM_CastScrollBase","TM_SpellStaffBase"})
                XmlInheritance.TryRegister(bases.SelectSingleNode("Defs/ThingDef[@Name='"+name+"']")!,null);
            var items=new XmlDocument();items.LoadXml(doc.ToString());
            var nodes=new[]{"TM_CastScroll_BreachSeal","TM_SpellStaff_BreachSeal"}.Select(id=>items.SelectSingleNode("Defs/ThingDef[defName='"+id+"']")!).ToArray();
            foreach(var node in nodes) XmlInheritance.TryRegister(node,null);
            XmlInheritance.Resolve();
            foreach(var node in nodes)
            {
                var resolved=XmlInheritance.GetResolvedNodeFor(node);
                Check.Equal(1,resolved.SelectNodes("comps/li[@Class='TorannMagic.SpellItems.CompProperties_SpellItem']")!.Count,"one cast component after inheritance");
                Check.Equal(1,resolved.SelectNodes("comps/li[@Class='CompProperties_Forbiddable']")!.Count,"inherited inventory component");
                Check.Equal("15",resolved.SelectSingleNode("comps/li[@Class='TorannMagic.SpellItems.CompProperties_SpellItem']/range")!.InnerText,"fixed item range");
            }
        }
        finally { XmlInheritance.Clear(); }
        var research=DefDatabase<ResearchProjectDef>.GetNamedSilentFail("TM_AdvancedScribing");
        bool added=research==null;
        if(added){research=new ResearchProjectDef { defName="TM_AdvancedScribing" };DefDatabase<ResearchProjectDef>.Add(research);}
        try
        {
            xml.LoadXml(Def("SpellOf_BreachSeal").Element("recipeMaker")!.ToString());
            // Parse the real scalar research field without unrelated work/user references.
            var gate=xml.CreateElement("recipeMaker");gate.AppendChild(xml.DocumentElement!.SelectSingleNode("researchPrerequisite")!.CloneNode(true));
            var maker=DirectXmlToObject.ObjectFromXml<RecipeMakerProperties>(gate,false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            Check.Same(research!,maker.researchPrerequisite,"learning-scroll recipe research resolves natively");
        }
        finally { DirectXmlCrossRefLoader.Clear();if(added){DefDatabase<ResearchProjectDef>.Clear();foreach(var d in oldResearch)DefDatabase<ResearchProjectDef>.Add(d);} }
    }
    static void Languages(RuntimeFixture f)
    {
        var defs=Document(f).Root!.Elements().Where(d=>d.Element("label")!=null).ToArray();
        foreach(var language in new[]{"English","Russian","Ukrainian","Spanish","ChineseSimplified","Portuguese","PortugueseBrazilian"})
        {
            var root=Path.Combine(f.Repository,"v1.7/Odyssey/Languages",language);
            var keys=XDocument.Load(Path.Combine(root,"Keyed/TM_BreachSeal.xml")).Root!;
            foreach(var key in new[]{"TM_BreachSeal_eff","TM_BreachSeal_eff_desc","TM_BreachSeal_Unavailable","TM_BreachSeal_NoCore","TM_BreachSeal_AmbiguousCore","TM_BreachSeal_CoreOverlap","TM_BreachSeal_InvalidFloor","TM_BreachSeal_Blocked","TM_BreachSeal_AlreadySealed"})
                Check.Equal(false,string.IsNullOrWhiteSpace((string?)keys.Element(key)),language+" "+key);
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(e=>e.Name.LocalName is "label" or "description" or "jobString"))
            {
                var type=def.Name.LocalName=="TorannMagic.TMAbilityDef"?"TMAbilityDef":def.Name.LocalName;
                var translation=XDocument.Load(Path.Combine(root,"DefInjected",type,"TM_BreachSeal.xml")).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName);
                Check.Equal(false,string.IsNullOrWhiteSpace((string?)translation),language+" "+def.Element("defName")!.Value+"."+field.Name);
            }
        }
    }
    static void Artwork(RuntimeFixture f)
    {
        foreach(var texture in Document(f).Descendants().Where(e=>e.Name.LocalName is "texPath" or "uiIconPath").Select(e=>e.Value))
            Check.Equal(true,new[]{"Textures","v1.7/Textures","v1.7/Odyssey/Textures"}.Any(dir=>File.Exists(Path.Combine(f.Repository,dir,texture+".png"))),"real texture "+texture);
    }
}
