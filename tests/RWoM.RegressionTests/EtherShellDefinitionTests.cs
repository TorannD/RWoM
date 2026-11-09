using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static class EtherShellDefinitionTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("Odyssey.EtherShell.Definitions", Definitions);
        yield return new("Odyssey.EtherShell.GrimoireGenericFilter", Grimoire);
        yield return new("Odyssey.EtherShell.SevenLanguages", Languages);
        yield return new("Odyssey.EtherShell.SavedProgressionAndGrimoire", Progression);
    }
    static void Progression(RuntimeFixture f)
    {
        var oldGame=Current.Game;
        Current.Game=(Game)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game));
        GrimoireProgressionTests.Set(Current.Game,"maps",new List<Map>());
        GrimoireProgressionTests.Set(Current.Game,"tickManager",new TickManager());
        var ability=f.AddAbility("TM_EtherShell");
        var xml=new XmlDocument();xml.LoadXml(Document(f).Root!.Elements().Single(d=>(string?)d.Element("defName")=="TM_EtherShellPower").ToString());
        try
        {
            var powerDef=DirectXmlToObject.ObjectFromXml<TM_CustomPowerDef>(xml.DocumentElement!,false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            f.AddDefinition(powerDef);
            var data=f.LoadLegacy();
            var power=data.MagicPowersCustom.Single(p=>p.TMabilityDefs.Contains(ability));
            Check.Equal(false,power.learned,"old save gains locked power without granting spell");
            var skill=Check.NotNull(data.GetSkill_Efficiency(ability),"efficiency discovered from actual XML");
            Check.Equal(3,skill.levelMax,"native skill cap");
            var pawn=GrimoireProgressionTests.PawnWithComps(true,false);
            pawn.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Warlock));
            GrimoireProgressionTests.Set(pawn.magic,"magicData",data);
            pawn.pawn.needs.AllNeeds.Add((Need_Mana)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Need_Mana)));
            var abilityXml=Document(f).Root!.Elements().Single(d=>(string?)d.Element("defName")=="TM_EtherShell");
            var learnDef=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            learnDef.defName=abilityXml.Element("learnItem")!.Value;learnDef.thingClass=typeof(ThingWithComps);learnDef.stackLimit=25;learnDef.destroyable=true;
            ability.learnItem=learnDef;
            ability.abilityClass=typeof(MagicData).Assembly.GetType(abilityXml.Element("abilityClass")!.Value)!;
            ability.shouldInitialize=true;
            xml.LoadXml(abilityXml.Element("MainVerb")!.ToString());
            // Native parser resolves the verb implementation; external sound/projectile refs are checked separately.
            foreach(var name in new[]{"soundCast","defaultProjectile"}){var node=xml.DocumentElement!.SelectSingleNode(name);if(node!=null)xml.DocumentElement.RemoveChild(node);}
            ability.MainVerb=DirectXmlToObject.ObjectFromXml<AbilityUser.VerbProperties_Ability>(xml.DocumentElement!,false);
            DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
            var scroll=new ThingWithComps { def=learnDef, stackCount=1 };
            var learner=new CompUseEffect_LearnSpell { parent=scroll };
            learner.DoEffect(pawn.pawn);
            Check.Equal(true,power.learned,"actual learning-scroll component marks power learned");
            Check.Equal(true,scroll.Destroyed,"actual learning scroll consumed");
            Check.Equal(true,pawn.magic.AbilityData.Powers.Any(p=>p.Def==ability && p.GetType()==ability.abilityClass),"native ability grant uses payment-safe subclass");
            skill.level=2;data.MagicAbilityPoints=4;
            var duration=Check.NotNull(data.GetSkill_Versatility(ability),"new duration skill added to legacy data");
            Check.Equal(0,duration.level,"new duration starts untrained");
            duration.level=3;
            var loaded=f.Load(f.Save(data));
            var restored=loaded.MagicPowersCustom.Single(p=>p.TMabilityDefs.Contains(ability));
            Check.Equal(true,restored.learned,"learning survives native Scribe");
            Check.Equal(2,loaded.GetSkill_Efficiency(ability).level,"investment survives native Scribe");
            Check.Equal(3,loaded.GetSkill_Versatility(ability).level,"duration investment survives native Scribe");
            Check.Equal(4,loaded.MagicAbilityPoints,"discovery never pays/refunds points");
            GrimoireProgressionTests.Set(pawn.magic,"magicData",loaded);
            var rows=GrimoireProgressionTests.MagicRows(GrimoireProgressionTests.Build(pawn.pawn));
            Check.Equal(true,rows.Contains(restored),"learned generic spell renders with saved owner for a base mage");
        }
        finally { DirectXmlCrossRefLoader.Clear();Current.Game=oldGame; }
    }
    static void Grimoire(RuntimeFixture f)
    {
        var prior=DefDatabase<TMAbilityDef>.AllDefsListForReading.ToArray();
        try
        {
            DefDatabase<TMAbilityDef>.Clear();
            var shell=(TMAbilityDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef)); shell.defName="TM_EtherShell";
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
    static XDocument Document(RuntimeFixture f) => XDocument.Load(Path.Combine(f.Repository,"v1.7/Odyssey/Defs/EtherShell.xml"));
    static void Definitions(RuntimeFixture f)
    {
        var doc=Document(f);
        XElement Def(string name)=>doc.Root!.Elements().Single(e=>(string?)e.Element("defName")==name);
        var ability=Def("TM_EtherShell");
        Check.Equal("0.25",ability.Element("manaCost")!.Value,"base mana");
        Check.Equal("0.1",ability.Element("efficiencyReductionPercent")!.Value,"native efficiency scaling");
        Check.Equal("SpellOf_EtherShell",ability.Element("learnItem")!.Value,"learning item");
        var verb=ability.Element("MainVerb")!;
        foreach(var pair in new[]{("range","12"),("warmupTime","1"),("SecondsToRecharge","60"),("verbClass","TorannMagic.Verb_EtherShell"),("isViolent","false")})
            Check.Equal(pair.Item2,verb.Element(pair.Item1)!.Value,pair.Item1);
        var hediff=Def("TM_EtherShellHD");
        var xml=new XmlDocument(); xml.LoadXml(hediff.ToString());
        var stage=DirectXmlToObject.ObjectFromXml<HediffStage>(xml.SelectSingleNode("HediffDef/stages/li")!,false);
        Check.Equal(true,stage.preventVacuumBurns,"native vacuum burn protection");
        var offsets=hediff.Element("stages")!.Element("li")!.Element("statOffsets")!;
        Check.Equal("1",offsets.Element("VacuumResistance")!.Value,"complete vacuum resistance");
        Check.Equal("-300",offsets.Element("ComfyTemperatureMin")!.Value,"cold resistance");
        Check.Equal(2,offsets.Elements().Count(),"no damage/healing/heat modifiers");
        var disappear=DirectXmlToObject.ObjectFromXml<HediffCompProperties>(xml.SelectSingleNode("HediffDef/comps/li")!,false) as HediffCompProperties_Disappears;
        Check.NotNull(disappear,"native expiry comp"); Check.Equal(60000,disappear!.disappearsAfterTicks.min,"one day minimum"); Check.Equal(60000,disappear.disappearsAfterTicks.max,"one day maximum");
        DirectXmlCrossRefLoader.Clear();
        var power=Def("TM_EtherShellPower").Element("customPower")!;
        Check.Equal("true",power.Element("forMage")!.Value,"generic mage registration");
        Check.Equal("true",power.Element("requiresScroll")!.Value,"scroll required");
        Check.Equal("0",power.Element("maxLevel")!.Value,"single fixed-duration ability");
        var skill=power.Element("skills")!.Elements().Single(e => e.Element("label")!.Value.EndsWith("_eff"));
        Check.Equal("TM_EtherShell_eff",skill.Element("label")!.Value,"efficiency skill retained"); Check.Equal("3",skill.Element("levelMax")!.Value,"three levels");
        foreach(var pair in new[]{("TM_CastScroll_EtherShell","1"),("TM_SpellStaff_EtherShell","10")})
        {
            var item=Def(pair.Item1); var props=item.Element("comps")!.Elements().Single();
            var itemXml=new XmlDocument();itemXml.LoadXml(props.ToString());
            var native=DirectXmlToObject.ObjectFromXml<TorannMagic.SpellItems.CompProperties_SpellItem>(itemXml.DocumentElement!,false);
            Check.Equal(TorannMagic.SpellItems.SpellItemKind.EtherShell,native.spell,"native enum resolves");
            Check.Equal(typeof(TorannMagic.SpellItems.CompSpellItem),native.compClass,"native item component resolves");
            Check.Equal(pair.Item1.StartsWith("TM_SpellStaff"),native.requiresEquipped,"native ownership mode");
            Check.Equal(3600,native.cooldownTicks,"native cooldown");
            Check.Equal("EtherShell",props.Element("spell")!.Value,"shared fixed effect");
            Check.Equal(pair.Item2,props.Element("maxUses")!.Value,"uses");
        }
        foreach(var name in new[]{"TM_MakeCastScroll_EtherShell","TM_MakeSpellStaff_EtherShell"})
            Check.Equal("TM_EtherShell",Def(name).Element("modExtensions")!.Element("li")!.Element("requiredSpell")!.Value,"crafting requires knowledge");
        Check.Equal("ArcaneSpellBase",Def("SpellOf_EtherShell").Attribute("ParentName")!.Value,"established learn-scroll behavior");
        var oldResearch=DefDatabase<ResearchProjectDef>.AllDefsListForReading.ToArray();
        string game=Path.GetDirectoryName(typeof(Pawn).Assembly.Location)!;
        var known=new[]{Path.Combine(f.Repository,"v1.7/Defs"),Path.Combine(game,"Data/Core/Defs"),Path.Combine(game,"Data/Odyssey/Defs")}
            .SelectMany(dir=>Directory.GetFiles(dir,"*.xml",SearchOption.AllDirectories)).SelectMany(path=>XDocument.Load(path).Root!.Elements()).Concat(doc.Root!.Elements()).ToArray();
        foreach(var id in new[]{"TM_AdvancedScribing","Projectile_TM_SpawnThing","TM_Vibration","RawMagicyte","Cloth","DevilstrandCloth","WoodLog","Jade","TableMagicPrinter","Smelt","UnfinishedWeapon","WorkSpeedGlobal","Intellectual","Crafting","VacuumResistance","ComfyTemperatureMin"})
            Check.Equal(true,known.Any(d=>(string?)d.Element("defName")==id),"real external definition "+id);
        foreach(var parent in doc.Root.Elements().Attributes("ParentName").Select(a=>a.Value).Distinct())
            Check.Equal(true,known.Any(d=>(string?)d.Attribute("Name")==parent),"real XML parent "+parent);
        foreach(var texture in new[]{"UI/Abilities/EtherShell","Items/SpellItems/Scroll_EtherShell","Items/SpellItems/Staff_EtherShell"})
            Check.Equal(true,File.Exists(Path.Combine(f.Repository,"v1.7/Odyssey/Textures",texture+".png")),"dedicated texture "+texture);
        var research=DefDatabase<ResearchProjectDef>.GetNamedSilentFail("TM_AdvancedScribing");
        bool added=research==null;
        if(added){research=new ResearchProjectDef { defName="TM_AdvancedScribing" };DefDatabase<ResearchProjectDef>.Add(research);}
        try
        {
            xml.LoadXml(Def("SpellOf_EtherShell").Element("recipeMaker")!.ToString());
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
            var keys=XDocument.Load(Path.Combine(root,"Keyed/TM_EtherShell.xml")).Root!;
            foreach(var key in new[]{"TM_EtherShell_eff","TM_EtherShell_eff_desc","TM_EtherShell_AlreadyProtected","TM_EtherShell_Unavailable"})
                Check.Equal(false,string.IsNullOrWhiteSpace((string?)keys.Element(key)),language+" "+key);
            foreach(var def in defs)
            foreach(var field in def.Elements().Where(e=>e.Name.LocalName is "label" or "description" or "jobString"))
            {
                var type=def.Name.LocalName=="TorannMagic.TMAbilityDef"?"TMAbilityDef":def.Name.LocalName;
                var translation=XDocument.Load(Path.Combine(root,"DefInjected",type,"TM_EtherShell.xml")).Root!.Element(def.Element("defName")!.Value+"."+field.Name.LocalName);
                Check.Equal(false,string.IsNullOrWhiteSpace((string?)translation),language+" "+def.Element("defName")!.Value+"."+field.Name);
            }
        }
    }
}
