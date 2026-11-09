using System.Reflection;
using Verse;
using RimWorld;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using TorannMagic.Guidance;

namespace RWoM.RegressionTests;

internal static class MagicGuidanceTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("MagicGuidance.NativeLifecyclePendingSaveAndDeviceCallback", f => Lifecycle(f));
        yield return new("MagicGuidance.SevenLanguageKeys", f =>
        {
            var root = Path.Combine(f.Repository, "v1.7/Languages");
            var expected = XDocument.Load(Path.Combine(root, "English/Keyed/TM_MagicGuidance.xml")).Root!.Elements().Select(x => x.Name.LocalName).Order().ToArray();
            foreach (string language in new[] { "English", "Russian", "Ukrainian", "Spanish", "Portuguese", "PortugueseBrazilian", "ChineseSimplified" })
            {
                var keys = XDocument.Load(Path.Combine(root, language, "Keyed/TM_MagicGuidance.xml")).Root!.Elements().ToArray();
                Check.Equal(true, expected.SequenceEqual(keys.Select(x => x.Name.LocalName).Order()), "localized key set: " + language);
                Check.Equal(true, keys.All(x => !string.IsNullOrWhiteSpace(x.Value)), "nonempty localized connective text");
            }
        });
        yield return new("MagicGuidance.LegacyAndSavedReceipts", f =>
        {
            var type = typeof(TorannMagic.MagicPower).Assembly.GetType("TorannMagic.Guidance.MagicGuideState");
            Check.NotNull(type, "persisted magic guidance state exists");
            var state = (IExposable)Activator.CreateInstance(type!)!;
            List<string> Take(IExposable value, params string[] keys) => (List<string>)type!.GetMethod("TakeNew")!.Invoke(value, new object[] { keys })!;
            Check.Equal(0, Take(state, "forging").Count, "uninitialized legacy state silently seeds current milestones");
            Check.Equal("scribing", Take(state, "forging", "scribing").Single(), "only newly earned milestone is returned");
            var path = Path.Combine(f.WorkingDirectory, "guidance.xml");
            Scribe.saver.InitSaving(path, "regression"); Scribe_Deep.Look(ref state, "state"); Scribe.saver.FinalizeSaving(); Scribe.ForceStop();
            IExposable restored = null!;
            Scribe.loader.InitLoading(path); Scribe_Deep.Look(ref restored, "state"); Scribe.loader.FinalizeLoading(); Scribe.ForceStop();
            Check.Equal(0, Take(restored, "forging", "scribing").Count, "loaded receipts suppress repeated milestones");
            Check.Equal("network", Take(restored, "network", "scribing").Single(), "post-load new research still announces");
            Check.Equal(0, Take(restored, "forging").Count, "temporary missing eligibility never forgets receipt");
        });
        yield return new("MagicGuidance.CombinedResearchBothOrdersAndDlc", f =>
        {
            var type = typeof(TorannMagic.MagicPower).Assembly.GetType("TorannMagic.Guidance.MagicGuides");
            Check.NotNull(type, "magic milestone catalog exists");
            var method = type!.GetMethod("Unlocked")!;
            foreach (bool reverse in new[] {false, true})
            {
                var research = new HashSet<string>();
                string[] Unlocked(bool dlc = true, bool vge = false) => ((IEnumerable<string>)method.Invoke(null, new object[] {(Func<string, bool>)research.Contains, dlc, vge})!).ToArray();
                research.Add(reverse ? "OrbitalTech" : "TM_AdvancedScribing");
                Check.Equal(false, Unlocked().Contains("spaceSpells"), "half the space spell requirements is insufficient");
                research.Add(reverse ? "TM_AdvancedScribing" : "OrbitalTech");
                Check.Equal(true, Unlocked().Contains("spaceSpells"), "space spell milestone works in either completion order");
                Check.Equal(false, Unlocked(false).Contains("spaceSpells"), "space help never unlocks without Odyssey");
                research.Add("TM_MagofuelRefining");
                Check.Equal(false, Unlocked().Contains("network"), "refining alone cannot promise beacon construction");
                research.Add("Electricity");
                Check.Equal(true, Unlocked().Contains("network"), "electricity completes network unlock");
                research.Add("TM_MagicalGravtech");
                Check.Equal(true, Unlocked().Contains("gravtech"), "base magical engine milestone");
                Check.Equal(false, Unlocked(true, true).Contains("gravtech"), "VGE engine additionally waits for StandardGravtech");
                research.Add("StandardGravtech");
                Check.Equal(true, Unlocked(true, true).Contains("gravtech"), "VGE engine ready after both requirements");
            }
        });
    }

    static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    sealed class DefinitionScope<T> : IDisposable where T : Def
    {
        readonly List<T> list = DefDatabase<T>.AllDefsListForReading;
        readonly Dictionary<string,T> names = (Dictionary<string,T>)typeof(DefDatabase<T>).GetField("defsByName", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        readonly T[] oldList;
        readonly KeyValuePair<string,T>[] oldNames;
        public DefinitionScope(T def)
        {
            oldList=list.ToArray();oldNames=names.ToArray();
            if(names.Remove(def.defName,out var old)) list.Remove(old);
            DefDatabase<T>.Add(def);
        }
        public void Dispose() { list.Clear();list.AddRange(oldList);names.Clear();foreach(var pair in oldNames) names.Add(pair.Key,pair.Value); }
    }
    static void Set(object owner, string field, object value) => owner.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(owner, value);
    static readonly List<string> letters = new();
    static int windows;
    static WindowStack windowStack = null!;
    public static bool CaptureLetter(TaggedString text) { letters.Add(text.ToString()); return false; }
    public static bool CaptureWindow(Window window) { Check.Equal(true, window is Dialog_MessageBox, "guide uses native reread dialog"); windows++; return false; }
    public static bool GetWindows(ref WindowStack __result) { __result = windowStack; return false; }

    static void Lifecycle(RuntimeFixture f)
    {
        var oldGame = Current.Game; var oldState = Current.ProgramState;
        var oldLanguage = LanguageDatabase.activeLanguage; var oldDefault = LanguageDatabase.defaultLanguage;
        var harmonyAssembly = Assembly.Load("0Harmony"); var ht = harmonyAssembly.GetType("HarmonyLib.Harmony")!;
        var id = "rwom.guidance." + Guid.NewGuid(); var harmony = Activator.CreateInstance(ht, id)!;
        void Prefix(MethodBase method, string handler)
        {
            var hm = Activator.CreateInstance(harmonyAssembly.GetType("HarmonyLib.HarmonyMethod")!, typeof(MagicGuidanceTests).GetMethod(handler)!)!;
            ht.GetMethod("Patch")!.Invoke(harmony, new object?[] { method, hm, null, null, null });
        }
        try
        {
            RuntimeLog.EnsureHeadlessDefOfInitialized(typeof(LetterDefOf));
            var language = Blank<LoadedLanguage>(); language.folderName = "English";
            Set(language, "dataIsLoaded", true); Set(language, "workerInt", new LanguageWorker_English());
            language.keyedReplacements = XDocument.Load(Path.Combine(f.Repository, "v1.7/Languages/English/Keyed/TM_MagicGuidance.xml"))
                .Root!.Elements().ToDictionary(x => x.Name.LocalName, x => new LoadedLanguage.KeyedReplacement {key=x.Name.LocalName,value=x.Value});
            language.keyedReplacements["OK"] = new LoadedLanguage.KeyedReplacement {key="OK",value="OK"};
            LanguageDatabase.activeLanguage = LanguageDatabase.defaultLanguage = language;
            Prefix(typeof(LetterStack).GetMethod("ReceiveLetter", new[] {typeof(TaggedString),typeof(TaggedString),typeof(LetterDef),typeof(string),typeof(int),typeof(bool)})!, nameof(CaptureLetter));
            Prefix(typeof(Find).GetProperty("WindowStack")!.GetMethod!, nameof(GetWindows));
            Prefix(typeof(WindowStack).GetMethod("Add")!, nameof(CaptureWindow));
            letters.Clear(); windows = 0; windowStack = new WindowStack();
            var game = Blank<Game>(); Current.Game = game; Current.ProgramState = ProgramState.Playing;
            game.letterStack = new LetterStack(); game.researchManager = Blank<ResearchManager>();
            var progress = new Dictionary<ResearchProjectDef,float>(); Set(game.researchManager,"progress",progress);
            game.Scenario = new Scenario(); Set(game.Scenario, "parts", new List<ScenPart> { new TorannMagic.Odyssey.ScenPart_MagicalShipArrival() });
            var research = new ResearchProjectDef {defName="TM_MagicScribing",label="scribing",description="Research details",baseCost=100};
            using var researchScope = new DefinitionScope<ResearchProjectDef>(research);
            var deviceDef = Blank<ThingDef>();deviceDef.defName="TableMagicPrinter";deviceDef.category=ThingCategory.Building;
            deviceDef.label="printer";deviceDef.description="Printer instructions";deviceDef.comps=new();
            using var deviceScope = new DefinitionScope<ThingDef>(deviceDef);
            var guidance = new GameComponent_MagicGuidance(game); game.components = new List<GameComponent>{guidance};
            guidance.StartedNewGame(); guidance.GameComponentTick(); guidance.GameComponentTick();
            Check.Equal(1,letters.Count,"native start callback produces exactly one guidance letter");
            Check.Equal(true,letters[0].Contains("limited fuel reserve"),"start explains finite supplied fuel");
            progress[research]=100;
            MagicGuidanceResearchPatch.Postfix();
            var path=Path.Combine(f.WorkingDirectory,"guidance-component.xml");
            Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref guidance,"component",game);Scribe.saver.FinalizeSaving();Scribe.ForceStop();
            GameComponent_MagicGuidance restored=null!;
            Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref restored,"component",game);Scribe.loader.FinalizeLoading();Scribe.ForceStop();
            game.components = new List<GameComponent>{restored};restored.LoadedGame();restored.GameComponentTick();
            Check.Equal(2,letters.Count,"saved pending completion survives reload");
            Check.Equal(true,letters[1].Contains("Printer instructions"),"letter reuses device description");
            restored.LoadedGame();MagicGuidanceResearchPatch.Postfix();restored.GameComponentTick();
            Check.Equal(2,letters.Count,"reload and duplicate completion do not resend");
            var legacy = new GameComponent_MagicGuidance(game);legacy.LoadedGame();legacy.ResearchFinished();legacy.GameComponentTick();
            Check.Equal(2,letters.Count,"legacy save has no backlog letters");
            game.Scenario = new Scenario();Set(game.Scenario,"parts",new List<ScenPart>());
            var ordinary = new GameComponent_MagicGuidance(game);ordinary.StartedNewGame();ordinary.GameComponentTick();
            Check.Equal(2,letters.Count,"ordinary scenario has no magical start letter");
            MagicGuides.AttachDeviceGuides();MagicGuides.AttachDeviceGuides();
            Check.Equal(1,deviceDef.comps.Count(c=>c.compClass==typeof(CompMagicGuide)),"guide component attached once");
            progress.Clear();
            var device = new ThingWithComps {def=deviceDef};device.InitializeComps();
            var comp=device.GetComp<CompMagicGuide>();Check.NotNull(comp,"real native comp initialization installs guide");
            var gizmo=(Command_Action)comp!.CompGetGizmosExtra().Single();gizmo.action();gizmo.action();
            Check.Equal(2,windows,"obtained device help can be reopened without research");
            Check.Equal(2,letters.Count,"rereading does not resend letters");
            RuntimeLog.AssertClean();
        }
        finally
        {
            ht.GetMethod("UnpatchAll")!.Invoke(harmony,new object[]{id});
            Current.Game=oldGame;Current.ProgramState=oldState;LanguageDatabase.activeLanguage=oldLanguage;LanguageDatabase.defaultLanguage=oldDefault;
        }
    }
}
