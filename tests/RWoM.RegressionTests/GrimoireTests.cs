using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using Verse;
namespace RWoM.RegressionTests;
internal static class GrimoireTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static void Set(object target, string name, object value) {
        for (Type? t = target as Type ?? target.GetType(); t != null; t = t.BaseType) {
            var field = t.GetField(name, Flags); if (field != null) { field.SetValue(target is Type ? null : target, value); return; }
        }
        throw new Exception("Missing fixture field " + name);
    }
    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    public static IEnumerable<TestCase> Cases() {
        foreach (var item in new[] { ("Fighter", false,true,false, false,true), ("Mage",true,false,false,true,false), ("Hybrid",true,true,false,true,false), ("Faceless",true,true,true,false,true), ("Neither",false,false,false,false,false) }) {
            var c = item; yield return new("Grimoire.Visibility." + c.Item1, f => Visibility(c.Item2,c.Item3,c.Item4,c.Item5,c.Item6));
        }
        yield return new("Grimoire.Visibility.FacelessWithSharedRaceMagic", f => Visibility(true,true,true,false,true,true));
        yield return new("Grimoire.DuplicateClassAbilitiesRenderOnce", DuplicateAbilitiesRenderOnce);
        yield return new("Grimoire.VariantRankIsNotSkillLevel", VariantRanks);
        yield return new("Weather.DarkThunderstormPrefixCannotCancelVanilla", LightningPrefix);
        yield return new("Grimoire.ThreeColumnsFitScreen", ThreeColumnsFitScreen);
        yield return new("Grimoire.LearningItemsPreserveCustomOwners", LearningItemsPreserveCustomOwners);
        yield return new("Grimoire.Metadata", Metadata);
        yield return new("Grimoire.TraitIndexShiftKeepsClassAdmission", f => TraitIndexShiftKeepsClassAdmission());
        yield return new("Grimoire.LocalizationContracts", Localization);
        yield return new("Grimoire.InstalledIsekaiExactNameGate", IsekaiGate);
    }
    static void Visibility(bool magic, bool might, bool faceless, bool showMagic, bool showMight, bool sharedRaceMagic = false) {        var root = Blank<Root_Play>(); var ui = Blank<UIRoot_Play>(); var map = Blank<MapInterface>(); var selector = Blank<Selector>();
        root.uiRoot = ui; ui.mapUI = map; map.selector = selector; Set(typeof(Current), "rootInt", root);
        var def = Blank<ThingDef>(); def.defName="GrimoireFixtureHuman"; def.race = new RaceProperties { intelligence=Intelligence.Humanlike };
        var pawn = new Pawn { def=def }; pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        Set(pawn, "factionInt", new Faction { def = new FactionDef { isPlayer=true } }); Set(pawn,"comps",new List<ThingComp>());
        if (faceless) pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Faceless));
        if (magic) { var comp = new CompAbilityUserMagic { parent=pawn }; Set(comp,"initializedIsMagicUser",true); Set(comp,"isMagicUser",true); pawn.AllComps.Add(comp); }
        if (might) { var comp = new CompAbilityUserMight { parent=pawn }; Set(comp,"initializedIsMightUser",true); Set(comp,"isMightUser",true); pawn.AllComps.Add(comp); }
        TM_Calc.InvalidateCompCache(pawn); Set(selector,"selected",new List<object> { pawn });
        var mt = Blank<ITab_Pawn_Magic>(); var ft = Blank<ITab_Pawn_Might>();
        // ResolveMagicTab deliberately skips Faceless; a different mage of the same
        // race can nevertheless have registered the shared Magic tab earlier.
        def.inspectorTabsResolved = new List<InspectTabBase>();
        if ((magic && !faceless) || sharedRaceMagic) def.inspectorTabsResolved.Add(mt);
        if (might) def.inspectorTabsResolved.Add(ft);
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { grimoireOnAbilityBar=false };
        Check.Equal(showMight,ft.IsVisible,"Might owns Faceless even without a race Magic registration"); Check.Equal(showMagic,mt.IsVisible,"Magic owns ordinary hybrids only");
        Check.Equal(magic || might ? 1 : 0,pawn.GetInspectTabs().Count(t => t.IsVisible),"Exactly one visible registered entry in inspect mode");
        TorannMagic.ModOptions.Settings.Instance.grimoireOnAbilityBar=true;
        Check.Equal(false,mt.IsVisible,"Ability bar suppresses Magic tab"); Check.Equal(false,ft.IsVisible,"Ability bar suppresses Might tab");
        Check.Equal(0,pawn.GetInspectTabs().Count(t => t.IsVisible),"Ability-bar mode has no inspect entry");
        TorannMagic.ModOptions.Settings.Instance.grimoireOnAbilityBar=false; Set(pawn,"factionInt",new Faction { def = new FactionDef() });
        Check.Equal(false,mt.IsVisible,"Non-colonist Magic gate"); Check.Equal(false,ft.IsVisible,"Non-colonist Might gate");
        Set(selector,"selected",new List<object>()); Check.Equal(false,mt.IsVisible,"No selection Magic gate"); Check.Equal(false,ft.IsVisible,"No selection Might gate");
    }
    // Live report (RimTraits - Medieval Talents): the mod removes and re-adds
    // TraitDefs inside PawnGenerator.GenerateTraits, which re-indexes the whole
    // trait database. Class admission compared cached database indexes, so the
    // shift silently hid the grimoire button from trait-based classes while
    // custom classes (matched by comp) kept it. Admission now compares def
    // references and must survive the same mutation.
    static void TraitIndexShiftKeepsClassAdmission() {
        var def = Blank<ThingDef>(); def.defName="GrimoireIndexShiftHuman"; def.race = new RaceProperties { intelligence=Intelligence.Humanlike };
        var pawn = new Pawn { def=def }; pawn.story = new Pawn_StoryTracker(pawn) { traits = new TraitSet(pawn) };
        Set(pawn,"comps",new List<ThingComp>());
        pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Bard));
        TM_Calc.InvalidateCompCache(pawn);
        Check.Equal(true,TM_Calc.HasMightOrMagicTrait(pawn),"class trait admits before mutation");
        // A dedicated filler: staged environments may hold a minimal trait DB.
        var shifted = new TraitDef { defName = "TraitIndexShiftFiller", degreeDatas = [new TraitDegreeData { degree = 0 }] };
        DefDatabase<TraitDef>.Add(shifted);
        typeof(DefDatabase<TraitDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, new object?[] { shifted });
        DefDatabase<TraitDef>.Add(shifted);
        Check.Equal(true,TM_Calc.HasMightOrMagicTrait(pawn),"database re-indexing cannot revoke class admission");
    }
    // Warlock, Succubus and Shadow collections each define their own power objects
    // for the same abilities (Soul Bond, Shadow Bolt, Dominate); the grimoire must
    // render one card per ability, preferring the pawn's own class collection.
    static void DuplicateAbilitiesRenderOnce(RuntimeFixture f) {
        var addRenderable = typeof(Window_TMGrimoire).GetMethod("AddRenderable", Flags, null,
            new[] { typeof(HashSet<MagicPower>), typeof(HashSet<AbilityUser.AbilityDef>),
                    typeof(System.Collections.Generic.IEnumerable<MagicPower>),
                    typeof(System.Collections.Generic.List<TMAbilityDef>),
                    typeof(System.Collections.Generic.List<TMAbilityDef>) }, null);
        var dom = f.AddAbility("TM_Dominate");
        var powerWD = new MagicPower(new System.Collections.Generic.List<AbilityUser.AbilityDef> { dom });
        var powerSD = new MagicPower(new System.Collections.Generic.List<AbilityUser.AbilityDef> { dom });
        var allowed = new HashSet<MagicPower>();
        var claimed = new HashSet<AbilityUser.AbilityDef>();
        var filter = new System.Collections.Generic.List<TMAbilityDef> { dom };
        addRenderable.Invoke(null, new object[] { allowed, claimed, new System.Collections.Generic.List<MagicPower> { powerWD }, filter, null });
        addRenderable.Invoke(null, new object[] { allowed, claimed, new System.Collections.Generic.List<MagicPower> { powerSD, powerWD }, filter, null });
        Check.Equal(1, allowed.Count, "shared class ability renders once");
        Check.Same(powerWD, System.Linq.Enumerable.First(allowed), "own class collection object preferred");
    }

    static void VariantRanks(RuntimeFixture f) {
        var progress = typeof(Window_TMGrimoire).GetMethod("PowerRankProgress", Flags)!;
        string Rank(int level, int max, bool learned) => (string)progress.Invoke(null, new object[] { level, max, learned })!;
        Check.Equal("", Rank(0, 0, true), "single variant has no misleading 0/0 header");
        Check.Equal("", Rank(0, 0, false), "locked single variant has no rank counter");
        Check.Equal("1 / 4", Rank(0, 3, true), "base variant is first rank");
        Check.Equal("3 / 4", Rank(2, 3, true), "upgraded variant uses one-based rank");
        Check.Equal("4 / 4", Rank(3, 3, true), "last variant rank");
        Check.Equal("0 / 4", Rank(0, 3, false), "unlearned remains unlearned");
        var data = new MagicData();
        var skill = data.MagicPowerSkill_Firebolt[0]; skill.level = 4;
        var ability = f.AddAbility("TM_Firebolt");
        Check.Same(skill, data.GetSkill_Power(ability), "grimoire uses invested skill owner");
        Check.Equal(4, data.GetSkill_Power(ability).level, "investment unchanged");
        Check.Equal(6, data.GetSkill_Power(ability).levelMax, "independent skill cap unchanged");
        string source = File.ReadAllText(Path.Combine(f.Repository, "RimWorldOfMagic/RimWorldOfMagic/Window_TMGrimoire.cs"));
        var processor = Assembly.Load("0Harmony").GetType("HarmonyLib.PatchProcessor")!;
        var instructions = processor.GetMethods().Single(m => m.Name == "GetOriginalInstructions"
            && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.Name == "ILGenerator");
        var title = typeof(Window_TMGrimoire).GetMethod("PowerTitle", Flags)!;
        List<object?> Operands(string method) =>
            ((System.Collections.IEnumerable)instructions.Invoke(null,
                [typeof(Window_TMGrimoire).GetMethod(method, Flags)!, null])!).Cast<object>()
                .Select(op => op.GetType().GetField("operand")!.GetValue(op)).ToList();
        foreach (var pair in new[] { ("DrawMagicPowerCard", "MagicTitle"), ("DrawMightPowerCard", "MightTitle") })
        {
            Check.Equal(1, Operands(pair.Item1).Count(op => Equals(op,
                typeof(Window_TMGrimoire).GetMethod(pair.Item2, Flags))), pair.Item1 + " uses stage/rank title");
            var ops = Operands(pair.Item2);
            var stageTitle = typeof(Window_TMGrimoire).GetMethod("StageTitle", Flags)!;
            Check.Equal(1, ops.Count(op => Equals(op, stageTitle)), pair.Item2 + " uses common title");
            var inputs = ops.Take(ops.FindIndex(op => Equals(op, stageTitle))).OfType<MemberInfo>()
                .TakeLast(3).Select(m => m.Name.Replace("get_", "")).ToArray();
            Check.Equal(true, inputs.SequenceEqual(new[] { "level", "maxLevel", "learned" }),
                pair.Item2 + " passes variant level, cap, and learned state");
        }
        Check.Equal(1, Operands("StageTitle").Count(op => Equals(op, title)), "common title preserves rank presentation");
        Check.Equal(2, Regex.Matches(source, @"if \(power.maxLevel <= 0\)").Count, "both learned single-variant actions avoid max");
    }
    static void LightningPrefix(RuntimeFixture f) {
        var type = typeof(MagicData).Assembly.GetTypes().Single(t => t.Name == "LightningStrike_DarkThunderstorm_Patch");
        var prefix = type.GetMethod("Prefix")!;
        Check.Equal(typeof(void), prefix.ReturnType, "weather prefix has no cancellation return");
        Check.Equal(false, prefix.GetParameters().Any(p => p.ParameterType.IsByRef), "weather instance cannot be replaced");

    }

    static void ThreeColumnsFitScreen(RuntimeFixture f) {
        var fit = typeof(Window_TMGrimoire).GetMethod("FitWindow", Flags)!;
        var columns = typeof(Window_TMGrimoire).GetMethod("Columns", Flags)!;
        foreach (var screen in new[] { new UnityEngine.Vector2(1920,1080), new UnityEngine.Vector2(1366,768), new UnityEngine.Vector2(1024,768), new UnityEngine.Vector2(800,600) }) {
            var size = (UnityEngine.Vector2)fit.Invoke(null, new object[] { screen.x, screen.y })!;
            Check.Equal(true, size.x <= screen.x - 30 && size.y <= screen.y - 30, "window fits UI-scaled screen");
            if (size.x - 36 < 1040) continue; // compact path has no side columns
            var rect = new UnityEngine.Rect(7, 9, size.x - 36, size.y - 36);
            var parts = (UnityEngine.Rect[])columns.Invoke(null, new object[] { rect })!;
            Check.Equal(true, parts.All(r => r.width > 0 && r.height > 0), "positive column areas");
            Check.Equal(true, parts[0].xMax < parts[1].x && parts[1].xMax < parts[2].x, "no overlap");
            Check.Equal(rect.xMax, parts[2].xMax, "right column stays inside window");
            Check.Equal(true, parts[1].width >= 480, "skill cards retain usable width");
        }
    }
    static void LearningItemsPreserveCustomOwners(RuntimeFixture f) {
        var a = f.AddAbility("AddonLearningFixture"); var b = f.AddAbility("MissingScrollFixture");
        a.learnItem = Blank<ThingDef>(); a.learnItem.defName = "AddonScrollFixture";
        var known = new MagicPower(new List<AbilityUser.AbilityDef> { a }) { learned = true, level = 0 };
        var unknown = new MagicPower(new List<AbilityUser.AbilityDef> { a }) { learned = false };
        var missing = new MagicPower(new List<AbilityUser.AbilityDef> { b }) { requiresScroll = true };
        var fighter = new MightPower(new List<AbilityUser.AbilityDef> { a });
        var window = Blank<Window_TMGrimoire>();
        Set(window, "magicRenderable", new HashSet<MagicPower> { known, unknown, missing });
        Set(window, "mightRenderable", new HashSet<MightPower> { fighter });
        var method = typeof(Window_TMGrimoire).GetMethod("RequiredLearningItems", Flags)!;
        var rows = (List<TMAbilityDef>)method.Invoke(window, null)!;
        Check.Equal(2, rows.Count, "shared addon item appears once; unresolved required item retained");
        Check.Equal(true, rows.Contains(a) && rows.Contains(b), "actual definitions retained");
        Check.Equal(true, known.learned, "known spell remains learned");
        Check.Equal(false, unknown.learned, "view does not teach or migrate addon power");
        unknown.learned = true; fighter.learned = true;
        rows = (List<TMAbilityDef>)method.Invoke(window, null)!;
        Check.Equal(1, rows.Count, "learning refreshes item list without reopening");
        Check.Same(b, rows[0], "only still-unlearned requirement remains");
    }

    static void Metadata(RuntimeFixture f) {
        var doc=XDocument.Load(Path.Combine(f.Repository,"About/About.xml"));
        Check.Equal("A RimWorld of Magic (Continued)",doc.Root!.Element("name")!.Value,"User-approved fork display name");
        Check.Equal("1.6,1.7",string.Join(",",doc.Root.Element("supportedVersions")!.Elements("li").Select(x=>x.Value)),"Supported versions");
        Check.Equal("Torann.ARimworldOfMagic",doc.Root.Element("packageId")!.Value,"Stable package ID");
        Check.Equal(true,System.Text.Encoding.UTF8.GetByteCount(doc.Root.Element("description")!.Value)<=8000,"Workshop description byte budget");
    }
    // Redirect all engine metadata/config paths before first ModLister/ModsConfig access.
    // The real gate runs with controlled active metadata; installation XML is read only.
    public static void Initialize(string repository) {
        string sandbox = Path.Combine(Path.GetDirectoryName(typeof(GrimoireTests).Assembly.Location)!, "metadata-fixture");
        Directory.CreateDirectory(Path.Combine(sandbox,"Data")); Directory.CreateDirectory(Path.Combine(sandbox,"Mods")); Directory.CreateDirectory(Path.Combine(sandbox,"Config"));
        Set(typeof(GenFilePaths),"saveDataPath",sandbox); Set(typeof(GenFilePaths),"modsFolderPath",Path.Combine(sandbox,"Mods")); Set(typeof(GenFilePaths),"officialModsFolderPath",Path.Combine(sandbox,"Data"));
        string aboutDir = Path.Combine(sandbox,"Mods","Fixture","About"); Directory.CreateDirectory(aboutDir);
        var about = XDocument.Load(Path.Combine(repository,"About/About.xml"));
        string name = about.Root!.Element("name")!.Value;
        void WriteIdentity(string identity) => new XDocument(new XElement("ModMetaData",new XElement("name",identity),new XElement("author","Regression"),new XElement("packageId","Torann.ARimworldOfMagic"),new XElement("supportedVersions",new XElement("li","1.7")))).Save(Path.Combine(aboutDir,"About.xml"));
        WriteIdentity(name);
        new XDocument(new XElement("ModsConfigData", new XElement("version",VersionControl.CurrentVersionString), new XElement("activeMods",new XElement("li","torann.arimworldofmagic")),new XElement("knownExpansions"))).Save(Path.Combine(sandbox,"Config","ModsConfig.xml"));
        ModLister.EnsureInit();
    }
    static void IsekaiGate(RuntimeFixture f) {
        string sandbox = Path.Combine(Path.GetDirectoryName(typeof(GrimoireTests).Assembly.Location)!, "metadata-fixture");
        string aboutDir = Path.Combine(sandbox,"Mods","Fixture","About");
        string name = XDocument.Load(Path.Combine(f.Repository,"About/About.xml")).Root!.Element("name")!.Value;
        void WriteIdentity(string identity) => new XDocument(new XElement("ModMetaData",new XElement("name",identity),new XElement("author","Regression"),new XElement("packageId","Torann.ARimworldOfMagic"),new XElement("supportedVersions",new XElement("li","1.7")))).Save(Path.Combine(aboutDir,"About.xml"));
        var mods = (List<ModMetaData>)typeof(ModLister).GetField("mods",Flags)!.GetValue(null)!;
        var original = mods.ToList();
        var originalActive = typeof(ModsConfig).GetField("activeModsHashSet",Flags)!.GetValue(null)!;
        // Jecs assemblies live under <workshop>/<id>/1.7/Assemblies.
        string installed = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Path.GetDirectoryName(typeof(AbilityUser.AbilityDef).Assembly.Location)!)!.FullName)!.FullName)!.FullName,"3657580708/Patches/RoMTreePatches.xml");
        var source = new XmlDocument(); source.Load(installed);
        var gateNode = source.SelectSingleNode("/Patch/Operation[@Class='PatchOperationFindMod']")!;
        Check.Equal("A RimWorld of Magic",gateNode.SelectSingleNode("mods/li")!.InnerText,"Installed native gate identity");
        // Retain the installed gate and its exact-name criteria; use a marker branch
        // instead of importing the entire unrelated Isekai passive-tree definition set.
        var gateDoc = new XmlDocument(); gateDoc.LoadXml(gateNode.OuterXml);
        var branch=gateDoc.SelectSingleNode("/Operation/match")!;
        var marker=gateDoc.CreateElement("match"); marker.SetAttribute("Class","PatchOperationAdd");
        marker.InnerXml="<xpath>/Defs</xpath><value><GateMatched /></value>"; branch.ParentNode!.ReplaceChild(marker,branch);
        try {
            bool Match(string identity, bool active) {
                WriteIdentity(identity); mods.Clear(); mods.Add(new ModMetaData(Path.GetDirectoryName(aboutDir)!));
                Set(typeof(ModsConfig),"activeModsHashSet",active ? new HashSet<string> { "torann.arimworldofmagic" } : new HashSet<string>());
                var operation=DirectXmlToObject.ObjectFromXml<PatchOperation>(gateDoc.DocumentElement!,false);
                var defs=new XmlDocument(); defs.LoadXml("<Defs />"); Check.Equal(true,operation.Apply(defs),"Real PatchOperationFindMod execution");
                return defs.SelectSingleNode("/Defs/GateMatched")!=null;
            }
            Check.Equal(false,Match("A RimWorld of Magic — 1.7 (Unofficial)",true),"Former unofficial identity does not match installed exact-name gate");
            Check.Equal(false,Match(name,false),"Inactive canonical mod does not match");
            Check.Equal(true,Match(name,true),"Current About identity matches installed gate");
        } finally { WriteIdentity(name); mods.Clear(); mods.AddRange(original); Set(typeof(ModsConfig),"activeModsHashSet",originalActive); }
    }
    static void Localization(RuntimeFixture f) {
        string[] keys = "TM_Grimoire TM_Learn TM_GrimoireDesc TM_GrimoireOpen TM_GrimoireTabHint TM_GrimoireIsekaiHint TM_GrimoireTabModeDesc TM_AIMarkingDesc TM_AIFighterMarkingDesc TM_AIFriendlyMarkingDesc XPMultiplierDesc NeedMultiplierDesc MagicyteChanceDesc TM_DeathRetaliationIsLethalDesc TM_DeathRetaliationChanceDesc TM_DeathRetaliationDelayDesc TM_enableAutocastDesc TM_autocastUndraftedThresholdDesc TM_autocastDraftedThresholdDesc TM_autocastEvaluationFrequencyDesc TM_autocastAnimalsDesc AICanCastDesc AIHardModeDesc TM_AggressiveAICastingDesc TM_SlavesCanScribeDesc TM_UndeadUpkeepMultiplierDesc TM_ParacyteSoftCapDesc TM_ParacyteMagesCountDesc unrestrictedBloodTypesForBloodMagicDesc TM_UnrestrictedWeaponCopyDesc TM_ClassOptionsDesc TM_FactionOptionsDesc TM_EventOptionsDesc TM_GolemOptionsDesc TM_DisplayOptionsDesc".Split(' ');
        var languages=Directory.GetDirectories(Path.Combine(f.Repository,"Languages")).ToDictionary(d => Path.GetFileName(d)!, d=>Directory.GetFiles(Path.Combine(d,"Keyed"),"*.xml").SelectMany(p=>XDocument.Load(p).Root!.Elements()).ToList());
        foreach(var language in languages) foreach(string key in keys) {
            var entries=language.Value.Where(x=>x.Name.LocalName==key).ToList(); Check.Equal(1,entries.Count,language.Key+" unique "+key);
            Check.Equal(true,!string.IsNullOrWhiteSpace(entries[0].Value),language.Key+" nonempty "+key);
            string Slots(string text)=>string.Join(",",Regex.Matches(text,@"(?<!\{)\{\d+(?:[^{}]*)\}(?!\})").Select(m=>m.Value).Order());
            Check.Equal(Slots(languages["English"].First(x=>x.Name.LocalName==key).Value),Slots(entries[0].Value),language.Key+" placeholders "+key);
        }
    }
}
