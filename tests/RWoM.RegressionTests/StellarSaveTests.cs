using System.Runtime.CompilerServices;
using RimWorld;
using RimWorld.Planet;
using Verse;
namespace RWoM.RegressionTests;
internal static partial class OdysseyBeaconTests
{
    public static IEnumerable<TestCase> StellarSaveCases()
    {
        yield return new("Odyssey.Stellar.PendingQuestScribePreservesIdentity", f => Run(f, () => {
            var previous=DefDatabase<QuestScriptDef>.AllDefsListForReading.ToArray();
            try {
                var root=new QuestScriptDef{defName="StellarPendingSave",sendAvailableLetter=false};DefDatabase<QuestScriptDef>.Add(root);
                var c=Make("StellarObservatory",10,10);var search=Activator.CreateInstance(T("StellarDivinationSearch"))!;Call(search,"Begin",5,0);Set(c,"search",search);
                Set(c,"pendingQuest",new Quest{id=400,root=root,name="Saved orbital destination"});
                StellarSaveEnvelope.Comp=c;StellarSaveEnvelope.Controller=(IExposable)Activator.CreateInstance(T("WorldComponent_StellarDivination"),new object?[]{null})!;var envelope=new StellarSaveEnvelope();string path=Path.Combine(f.WorkingDirectory,"stellar-pending.xml");
                try {Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref envelope,"stellar");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
                Set(c,"pendingQuest",null!);
                try {Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref envelope,"stellar");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
                var loaded=(Quest)c.GetType().GetField("pendingQuest",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(c)!;
                Check.Equal(400,loaded.id,"unregistered quest ID preserved");Check.Same(root,loaded.root,"quest script reference preserved");Check.Equal("Saved orbital destination",loaded.name,"same generated destination retained");Check.Equal(true,(bool)Get(c,"IsSearching"),"delivery remains pending");
            }finally {StellarSaveEnvelope.Comp=null;StellarSaveEnvelope.Controller=null;DefDatabase<QuestScriptDef>.Clear();foreach(var def in previous)DefDatabase<QuestScriptDef>.Add(def);}
        }));
        yield return new("Odyssey.Stellar.NullGenerationBoundedRetryWithoutRepayment", f => Run(f, () => {
            using var seam = new StellarLetterSeam(true);
            var flag=typeof(ModsConfig).GetField("odysseyActive",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;var old=flag.GetValue(null);
            try {
                flag.SetValue(null,true);Set(Current.Game,"questManager",new QuestManager());Set(Current.Game,"signalManager",new SignalManager());Set(Current.Game,"uniqueIDsManager",new UniqueIDsManager());
                var c=Make("StellarObservatory",10,10);var search=Activator.CreateInstance(T("StellarDivinationSearch"))!;Call(search,"Begin",9,0);Set(c,"search",search);
                c.CompTick();Set(Find.TickManager,"ticksGameInt",2499);c.CompTick();Check.Equal(1,stellarGenerations,"empty destination retries bounded");Check.Equal(true,(bool)Get(c,"IsSearching"),"paid search retained");
                var q=new Quest();q.root=new QuestScriptDef{defName="StellarRetry"};q.AddPart(new StellarReenterPart{Comp=c});stellarGeneratedQuest=q;
                Set(Find.TickManager,"ticksGameInt",2500);c.CompTick();Set(Find.TickManager,"ticksGameInt",5000);c.CompTick();
                Check.Equal(2,stellarGenerations,"retry finishes once");Check.Equal(1,stellarLetters,"one available letter");Check.Equal(1,Find.QuestManager.QuestsListForReading.Count,"one native quest without repaying absent tanks");RuntimeLog.Clear();
            }finally {flag.SetValue(null,old);stellarGeneratedQuest=null;}
        }));
        yield return new("Odyssey.Stellar.RealScribeProgressAndWorldCooldown", f => Run(f, () => {
            var controller=(IExposable)Activator.CreateInstance(T("WorldComponent_StellarDivination"),new object?[]{null})!;
            Check.Equal(true,(bool)Call(controller,"TryStart",100,new Func<bool>(()=>true))!,"purchase controller");
            var c=Make("StellarObservatory",10,10);var search=Activator.CreateInstance(T("StellarDivinationSearch"))!;Call(search,"Begin",1,600000);Call(search,"Advance",100,60000,true);Set(c,"search",search);
            StellarSaveEnvelope.Comp=c;StellarSaveEnvelope.Controller=controller;var envelope=new StellarSaveEnvelope();
            string path=Path.Combine(f.WorkingDirectory,"stellar-progress.xml");
            try {Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref envelope,"stellar");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            Set(c,"search",null!);Set(controller,"nextStartTick",0L);
            try {Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref envelope,"stellar");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Check.Equal(540000,(int)Get(c,"RemainingTicks"),"paid progress saved");Check.Equal(false,(bool)Call(controller,"CanStart",1080099)!,"saved exact cooldown");Check.Equal(true,(bool)Call(controller,"CanStart",1080100)!,"saved boundary");
            var loaded=c.GetType().GetField("search",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(c)!;Call(loaded,"Advance",100,60000,true);Check.Equal(540000,(int)Get(c,"RemainingTicks"),"saved receipt blocks duplicate");
            StellarSaveEnvelope.Comp=null;StellarSaveEnvelope.Controller=null;
        }));
        yield return new("Odyssey.Stellar.NativeQuestRegistrationCallbackNoDuplicate", f => Run(f, () => {
            using var seam = new StellarLetterSeam();
            var flag=typeof(ModsConfig).GetField("odysseyActive",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;var old=flag.GetValue(null);
            try {
                flag.SetValue(null,true);Set(Current.Game,"questManager",new QuestManager());Set(Current.Game,"signalManager",new SignalManager());Set(Current.Game,"uniqueIDsManager",new UniqueIDsManager());
                var c=Make("StellarObservatory",10,10);var search=Activator.CreateInstance(T("StellarDivinationSearch"))!;Call(search,"Begin",1,0);Set(c,"search",search);
                var q=new Quest();q.root=new QuestScriptDef{defName="StellarRegression",sendAvailableLetter=false};q.AddPart(new StellarReenterPart{Comp=c});Set(c,"pendingQuest",q);
                c.CompTick();c.CompTick();Set(Find.TickManager,"ticksGameInt",5000);c.CompTick();
                Check.Equal(1,Find.QuestManager.QuestsListForReading.Count,"real manager registers once despite reentrant throwing mod callback");Check.Equal(false,(bool)Get(c,"IsSearching"),"registered reward completed before reentry");Check.Equal(0,(int)Get(c,"LastDiscoveryTick"),"completion timestamp");
                Check.Equal(1,stellarLetters,"registered quest announces once even when native callback throws");
                RuntimeLog.Clear();
            }finally {flag.SetValue(null,old);}
        }));
    }
    static int stellarLetters;
    static int stellarGenerations;
    static Quest? stellarGeneratedQuest;
    static bool StellarGenerate(ref Quest? __result){stellarGenerations++;__result=stellarGeneratedQuest;return false;}
    static bool StellarLetter(){stellarLetters++;return false;}
    sealed class StellarLetterSeam:IDisposable
    {
        readonly System.Reflection.Assembly asm=System.Reflection.Assembly.Load("0Harmony");readonly object harmony;readonly string id="rwom.stellar.letter."+Guid.NewGuid();
        public StellarLetterSeam(bool patchGenerate = false)
        {
            stellarLetters=stellarGenerations=0;stellarGeneratedQuest=null;harmony=Activator.CreateInstance(asm.GetType("HarmonyLib.Harmony")!,id)!;
            var hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,typeof(OdysseyBeaconTests).GetMethod(nameof(StellarLetter),System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{T("StellarDivinationQuestUtility").GetMethod("SendLetter"),hm,null,null,null});
            if(patchGenerate) {
                hm=Activator.CreateInstance(asm.GetType("HarmonyLib.HarmonyMethod")!,typeof(OdysseyBeaconTests).GetMethod(nameof(StellarGenerate),System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!)!;
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony,new object?[]{T("StellarDivinationQuestUtility").GetMethod("Generate"),hm,null,null,null});
            }
        }
        public void Dispose()=>harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,new object[]{id});
    }
    public sealed class StellarReenterPart:QuestPart
    {
        public ThingComp Comp=null!;
        public override void PostQuestAdded(){Comp.CompTick();throw new InvalidOperationException("stellar test callback after native registration");}
    }
    public sealed class StellarSaveEnvelope:IExposable
    {
        public static ThingComp? Comp; public static IExposable? Controller;
        public void ExposeData(){Comp!.PostExposeData();Controller!.ExposeData();}
    }
}
