using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using TorannMagic.Odyssey;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> StellarPoolCases()
    {
        yield return new("Odyssey.Stellar.InstalledPoolMetadataNativeEligibilityAndWeight", f => Run(f, () => {
            var flag=typeof(ModsConfig).GetField("odysseyActive",BindingFlags.Static|BindingFlags.NonPublic)!;var old=flag.GetValue(null);var previous=DefDatabase<QuestScriptDef>.AllDefsListForReading.ToArray();
            try {
                using var seam=new StellarPoolSeam();flag.SetValue(null,true);Current.Game.Scenario=new Scenario();Set(Find.TickManager,"ticksGameInt",1);
                var world=(World)RuntimeHelpers.GetUninitializedObject(typeof(World));Set(world,"storyState",new StoryState(world));world.factionManager=new FactionManager();Set(Current.Game,"worldInt",world);
                map.info=new MapInfo{parent=new MapParent{Tile=PlanetTile.Invalid}};
                var slate=new Slate();slate.Set("points",500f);DefDatabase<QuestScriptDef>.Clear();
                string steam="C:/Program Files (x86)/Steam/steamapps";
                string[] files={Path.Combine(steam,"common/RimWorld/Data/Odyssey/Defs/QuestScriptDefs/Script_SpaceSites.xml"),Path.Combine(steam,"workshop/content/294100/3609835606/1.7/Defs/QuestScriptDefs/Script_SpaceSites.xml"),Path.Combine(steam,"workshop/content/294100/3799737423/1.7/Defs/QuestScriptDefs/Script_SpaceSites.xml")};
                var names=new List<string>();
                foreach(var file in files) {
                    if(!File.Exists(file)){Console.WriteLine("CAPABILITY SKIPPED: optional installed quest metadata "+file);continue;}
                    foreach(var node in XDocument.Load(file).Root!.Elements("QuestScriptDef").Where(n=>n.Element("givenBy")?.Elements("li").Any(x=>x.Value=="OrbitalScanner")==true)) {
                        string name=node.Element("defName")!.Value;
                        // This adapter contract test loads actual installed pool metadata. Site feasibility
                        // uses the native asteroid node with tile lookup stubbed; it is not VGE mapgen.
                        var def=new QuestScriptDef{defName=name,rootSelectionWeight=(float?)node.Element("rootSelectionWeight")??1f,rootMinPoints=(float?)node.Element("rootMinPoints")??0f,root=new QuestNode_Root_Asteroid(),givenBy=new List<QuestGiverTag>{QuestGiverTag.OrbitalScanner},autoAccept=true,canGiveRoyalFavor=true};
                        DefDatabase<QuestScriptDef>.Add(def);names.Add(name);
                    }
                    var eligible=StellarDivinationQuestUtility.EligibleQuests(map,slate);
                    Check.Equal(names.Count,eligible.Count,"new installed scanner definitions join existing eligible pool dynamically");
                    Check.Equal(true,eligible.All(q=>names.Contains(q.defName)),"only installed scanner definitions selected");
                }
                Check.Equal(true,names.Count>=6,"native orbital opportunities loaded");
                var blocked=new QuestScriptDef{defName="StellarTooHard",rootSelectionWeight=1,rootMinPoints=1000,root=new QuestNode_Root_Asteroid(),givenBy=new List<QuestGiverTag>{QuestGiverTag.OrbitalScanner},autoAccept=true,canGiveRoyalFavor=true};DefDatabase<QuestScriptDef>.Add(blocked);
                Check.Equal(false,StellarDivinationQuestUtility.EligibleQuests(map,slate).Contains(blocked),"native threat weighting excludes too hard quest");
                stellarOrbitAvailable=false;Set(Find.TickManager,"ticksGameInt",2);Check.Equal(0,StellarDivinationQuestUtility.EligibleQuests(map,slate).Count,"missing native orbit eligibility produces empty pool");
            } finally {flag.SetValue(null,old);DefDatabase<QuestScriptDef>.Clear();foreach(var def in previous)DefDatabase<QuestScriptDef>.Add(def);}
        }));
    }
    static bool stellarOrbitAvailable;
    static bool StellarSite(ref PlanetTile tile,ref bool __result){tile=PlanetTile.Invalid;__result=stellarOrbitAvailable;return false;}
    static bool StellarProgress(ref float __result){__result=0;return false;}
    static bool StellarDays(ref int __result){__result=100;return false;}
    sealed class StellarPoolSeam:IDisposable
    {
        readonly Assembly asm=Assembly.Load("0Harmony");readonly object harmony;readonly string id="rwom.stellar.pool."+Guid.NewGuid();
        public StellarPoolSeam()
        {
            stellarOrbitAvailable=true;harmony=Activator.CreateInstance(asm.GetType("HarmonyLib.Harmony")!,id)!;
            Patch(typeof(QuestNode_Root_Asteroid).GetMethod("TryFindSiteTile",BindingFlags.Instance|BindingFlags.NonPublic)!,nameof(StellarSite));
            Patch(typeof(StorytellerUtility).GetMethod("GetProgressScore")!,nameof(StellarProgress));
            Patch(typeof(GenDate).GetProperty("DaysPassedSinceSettle")!.GetMethod!,nameof(StellarDays));
        }
        void Patch(MethodBase method,string prefix){var hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,typeof(OdysseyBeaconTests).GetMethod(prefix,BindingFlags.Static|BindingFlags.NonPublic)!)!;harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{method,hm,null,null,null});}
        public void Dispose()=>harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,new object[]{id});
    }
}
