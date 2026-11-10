using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class TeachingTests
{
    private static bool Neutral(ref int __result){__result=0;return false;}
    private static int studentJobsEnded;
    private static bool EndStudentJob(){studentJobsEnded++;return false;}
    private static bool SkipText()=>false;
    private static bool DrawPosition(ref UnityEngine.Vector3 __result){__result=UnityEngine.Vector3.zero;return false;}
    public static IEnumerable<TestCase> Cases()
    {
        foreach(bool magic in new[]{true,false})
        foreach(string mode in new[]{"BeginOnce","Cooldown","OffMap","SavePaid","SavePending","Finish","Interrupted","Expired","OwnedWait","ForeignWait","NativeReady","NativeEmpty","LegacyRunning"})
            yield return new($"Teaching.{(magic?"Magic":"Might")}.{mode}",r=>Run(r,magic,mode));
    }
    private static void Run(RuntimeFixture r,bool magic,string mode)
    {
        LoadBaseDefinitions(r);
        var oldManaNeed=TorannMagicDefOf.TM_Mana;var oldStaminaNeed=TorannMagicDefOf.TM_Stamina;
        TorannMagicDefOf.TM_Mana??=new NeedDef{defName="TM_Mana",needClass=typeof(Need_Mana)};
        TorannMagicDefOf.TM_Stamina??=new NeedDef{defName="TM_Stamina",needClass=typeof(Need_Stamina)};
        if(DefDatabase<HediffDef>.GetNamedSilentFail("TM_PsionicHD")==null)
            DefDatabase<HediffDef>.Add(new HediffDef{defName="TM_PsionicHD"});
        var def=magic?TorannMagicDefOf.TM_TeachMagic:TorannMagicDefOf.TM_TeachMight;
        float oldMana=def.manaCost,oldStamina=def.staminaCost;
        bool oldConsume=def.consumeEnergy;
        bool oldViolent=def.MainVerb.isViolent;
        float oldRecharge=def.MainVerb.SecondsToRecharge;
        var xml=System.Xml.Linq.XDocument.Load(Path.Combine(r.Repository,"v1.7/Defs/AbilityDefs/Abilities_TMagic_Standalone.xml"))
            .Root!.Elements().Single(e=>(string?)e.Element("defName")==def.defName);
        def.manaCost=(float?)xml.Element("manaCost")??0;
        def.staminaCost=(float?)xml.Element("staminaCost")??0;
        def.consumeEnergy=true;
        def.MainVerb.SecondsToRecharge=(float)xml.Element("MainVerb")!.Element("SecondsToRecharge")!;
        def.MainVerb.isViolent=(bool?)xml.Element("MainVerb")!.Element("isViolent")??true;
        var oldGame=Current.Game;var oldSettings=TorannMagic.ModOptions.Settings.Instance;
        var oldLanguage=LanguageDatabase.activeLanguage;var oldDefaultLanguage=LanguageDatabase.defaultLanguage;
        var lib=Assembly.Load("0Harmony");var id="rwom.teach."+Guid.NewGuid();
        var harmony=Activator.CreateInstance(lib.GetType("HarmonyLib.Harmony")!,id)!;
        try {
            var language=(LoadedLanguage)RuntimeHelpers.GetUninitializedObject(typeof(LoadedLanguage));
            language.folderName="English";Set(language,"dataIsLoaded",true);Set(language,"workerInt",new LanguageWorker_English());
            var translations=System.Xml.Linq.XDocument.Load(Path.Combine(r.Repository,"Languages/English/Keyed/TM_English.xml"));
            language.keyedReplacements=new Dictionary<string,LoadedLanguage.KeyedReplacement>();
            foreach(string key in new[]{"TM_NotEnoughMana","TM_NotEnoughStamina"})
                language.keyedReplacements[key]=new(){key=key,value=translations.Root!.Element(key)!.Value};
            LanguageDatabase.activeLanguage=LanguageDatabase.defaultLanguage=language;
            _=new TorannMagic.ModOptions.Settings{AIAggressiveCasting=false};
            Current.Game=(Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            var map=(Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
            map.gameConditionManager=new GameConditionManager((Map)null!);
            Set(Current.Game,"maps",new List<Map>{map});
            Set(Current.Game,"tickManager",new TickManager());
            Set(Current.Game,"uniqueIDsManager",new UniqueIDsManager());
            var world=(RimWorld.Planet.World)RuntimeHelpers.GetUninitializedObject(typeof(RimWorld.Planet.World));
            world.factionManager=new FactionManager();Set(Current.Game,"worldInt",world);
            var faction=new Faction{def=new FactionDef{defName="TeachingPlayer",isPlayer=true}};
            world.factionManager.AllFactionsListForReading.Add(faction);Set(world.factionManager,"ofPlayer",faction);
            Current.Game.history=(History)RuntimeHelpers.GetUninitializedObject(typeof(History));
            Current.Game.history.historyEventsManager=(HistoryEventsManager)RuntimeHelpers.GetUninitializedObject(typeof(HistoryEventsManager));
            var hook=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(TeachingTests).GetMethod(nameof(Neutral),BindingFlags.Static|BindingFlags.NonPublic)!)!;
            harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[typeof(Pawn_RelationsTracker).GetMethod("OpinionOf")!,hook,null,null,null]);
            void Prefix(MethodInfo method,string name) {
                var prefix=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(TeachingTests).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!)!;
                harmony.GetType().GetMethod("Patch")!.Invoke(harmony,[method,prefix,null,null,null]);
            }
            Prefix(typeof(Pawn_JobTracker).GetMethod("EndCurrentJob")!,nameof(EndStudentJob));
            foreach(var method in typeof(MoteMaker).GetMethods().Where(m=>m.Name=="ThrowText"))Prefix(method,nameof(SkipText));
            Prefix(typeof(Pawn).GetProperty("DrawPos")!.GetMethod!,nameof(DrawPosition));
            foreach(var method in typeof(HistoryEventsManager).GetMethods().Where(m=>m.Name=="RecordEvent"))Prefix(method,nameof(SkipText));
            studentJobsEnded=0;
            var mentor=PawnWithComps(true,true);var student=PawnWithComps(true,true);
            foreach(var p in new[]{mentor.pawn,student.pawn}) {
                Set(p,"mapIndexOrState",(sbyte)0);p.relations=new Pawn_RelationsTracker(p);
                p.jobs=new Pawn_JobTracker(p);
                p.apparel=new Pawn_ApparelTracker(p);
                Set(p,"factionInt",faction);
                p.ageTracker=new Pawn_AgeTracker(p);Set(p.ageTracker,"ageBiologicalTicksInt",20L*3600000);
                p.needs.AllNeeds.Add(new Need_Mana(p){def=TorannMagicDefOf.TM_Mana});
                p.needs.AllNeeds.Add(new Need_Stamina(p){def=TorannMagicDefOf.TM_Stamina});
            }
            PawnAbility power=magic?new MagicAbility(mentor.magic):new MightAbility(mentor.might);
            Set(power,"powerDef",def);power.CooldownTicksLeft=mode=="Cooldown"?100:-1;
            Set(power,"verb",new Verb_Teach{caster=mentor.pawn,Ability=power,verbProps=def.MainVerb});
            (magic?mentor.magic.AbilityData:mentor.might.AbilityData).Powers.Add(power);
            var need=magic?(Need)mentor.magic.Mana:mentor.might.Stamina;need.CurLevel=mode=="NativeEmpty"?0:1;
            var type=typeof(Verb_Teach).Assembly.GetType("TorannMagic.JobDriver_Teach")!;
            var driver=(JobDriver)Activator.CreateInstance(type)!;driver.pawn=mentor.pawn;
            driver.job=new Job(TorannMagicDefOf.JobDriver_TM_Teach,student.pawn){count=magic?1:2};
            if(mode=="OffMap")Set(student.pawn,"mapIndexOrState",(sbyte)-1);
            if(mode=="Expired")Set(driver,"age",1261);
            var begin=type.GetMethod("BeginTeaching",BindingFlags.NonPublic|BindingFlags.Instance)!;
            bool Begin()=>(bool)begin.Invoke(driver,[student.pawn])!;
            if(mode=="SavePending")driver=RoundTrip(r,driver);
            if(mode=="LegacyRunning") {
                Set(driver,"curToilIndex",1);driver.ticksLeftThisToil=600;
                // Old saves did not persist chosen domain or lesson-local state.
                driver.job.count=-1;
                if(!magic) {
                    Set(student.magic,"isMagicUser",false);
                    student.pawn.needs.AllNeeds.RemoveAll(n=>n is Need_Mana);
                }
                driver=RoundTrip(r,driver,true);
                Check.Equal(660,(int)type.GetField("age",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(driver)!,"old running lesson retains elapsed work");
                Set(student.pawn.jobs,"curJob",new Job(JobDefOf.Wait,student.pawn));
                var resumed=((IEnumerable<Toil>)type.GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null)!).Last();
                resumed.actor=mentor.pawn;resumed.tickAction();
                Check.Equal(.75f,need.CurLevel,"loaded legacy toil pays on resumed tick without initAction");
                Check.Equal(661,(int)type.GetField("age",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(driver)!,"resumed tick advances retained lesson");
            }
            bool accepted=mode!="Cooldown"&&mode!="OffMap"&&mode!="Expired"&&mode!="NativeEmpty";
            Check.Equal(accepted,Begin(),"only eligible lesson starts");
            if(!accepted) {Check.Equal(mode=="NativeEmpty"?0f:1f,need.CurLevel,"rejected lesson cannot spend");Check.Equal(mode=="Cooldown"?100:-1,power.CooldownTicksLeft,"rejected timer unchanged");return;}
            Check.Equal(72000,power.CooldownTicksLeft,"native cooldown follows XML 1200 seconds");
            Check.Equal(.75f,need.CurLevel,"native payment consumes XML quarter resource");
            float remaining=need.CurLevel;int cooldown=power.CooldownTicksLeft;
            if(mode=="SavePaid") {
                Set(driver,"age",512);Set(driver,"lastEffect",400);
                driver=RoundTrip(r,driver);
                Check.Equal(512,(int)type.GetField("age",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(driver)!,"Scribe retains lesson progress");
                Check.Equal(400,(int)type.GetField("lastEffect",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(driver)!,"Scribe retains effect progress");
            }
            Check.Equal(true,Begin(),"same lesson can resume");
            Check.Equal(remaining,need.CurLevel,"resume pays once");Check.Equal(cooldown,power.CooldownTicksLeft,"resume does not reset cooldown");
            Check.Equal(magic,(bool)type.GetField("isMageTeaching",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(driver)!,"hybrid follows chosen spell");
            if(mode=="Finish"||mode=="Interrupted"||mode=="OwnedWait"||mode=="ForeignWait") {
                bool completed=mode!="Interrupted";
                Set(driver,"age",1259);Set(driver,"success",completed);
                if(mode=="OwnedWait"||mode=="ForeignWait") {
                    var wait=new Job(JobDefOf.Wait,student.pawn);
                    Set(student.pawn.jobs,"curJob",wait);
                    Set(driver,"studentWaitJobId",mode=="OwnedWait"?wait.loadID:wait.loadID+1);
                }
                int Before()=>magic?student.magic.MagicUserXP:student.might.MightUserXP;
                int xp=Before();
                var toil=((IEnumerable<Toil>)type.GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null)!).Last();
                toil.actor=mentor.pawn;toil.Cleanup(1,driver);
                Check.Equal(completed,Before()>xp,"only successful lesson grants XP");
                int rewarded=Before();
                driver=RoundTrip(r,driver);
                toil=((IEnumerable<Toil>)type.GetMethod("MakeNewToils",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(driver,null)!).Last();
                toil.actor=mentor.pawn;toil.Cleanup(1,driver);
                Check.Equal(rewarded,Before(),"finish cannot grant twice across Scribe");
                Check.Equal(mode=="OwnedWait"?1:0,studentJobsEnded,"cleanup releases owned wait once, never unrelated student work");
            }
        } finally {TorannMagicDefOf.TM_Mana=oldManaNeed;TorannMagicDefOf.TM_Stamina=oldStaminaNeed;LanguageDatabase.activeLanguage=oldLanguage;LanguageDatabase.defaultLanguage=oldDefaultLanguage;def.MainVerb.isViolent=oldViolent;def.consumeEnergy=oldConsume;def.MainVerb.SecondsToRecharge=oldRecharge;def.manaCost=oldMana;def.staminaCost=oldStamina;harmony.GetType().GetMethod("UnpatchAll")!.Invoke(harmony,[id]);Current.Game=oldGame;TorannMagic.ModOptions.Settings.Instance=oldSettings;}
    }
    private static JobDriver RoundTrip(RuntimeFixture r,JobDriver source,bool legacy=false)
    {
        string path=Path.Combine(r.WorkingDirectory,"teaching-job.xml");
        JobDriver saved=source,loaded=null!;
        try {Scribe.saver.InitSaving(path,"regression");Scribe_Deep.Look(ref saved,"driver");Scribe.saver.FinalizeSaving();}
        finally {Scribe.ForceStop();}
        if(legacy) {
            var xml=System.Xml.Linq.XDocument.Load(path);
            foreach(var element in xml.Root!.Element("driver")!.Elements().Where(e=>e.Name.LocalName.StartsWith("teach")).ToArray())element.Remove();
            xml.Save(path);
        }
        try {
            Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref loaded,"driver");
            // Native PostLoadInit builds toils and needs the containing tracker's references.
            loaded.pawn=source.pawn;loaded.job=source.job;
            Scribe.loader.FinalizeLoading();
        }
        finally {Scribe.ForceStop();}
        // Pawn_JobTracker supplies these references; this test roundtrips the driver only.
        loaded.pawn=source.pawn;loaded.job=source.job;return loaded;
    }
}
