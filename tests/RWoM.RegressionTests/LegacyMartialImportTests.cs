using System.Reflection;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class LegacyMartialImportTests
{
    public sealed class Envelope : IExposable
    {
        public MightData might=null!;
        public CompClassProgression progression=new();
        public void ExposeData()
        {
            Scribe_Deep.Look(ref might,"mightData");
            progression.PostExposeData();
        }
    }
    internal static IEnumerable<TestCase> Cases()
    {
        yield return new("LegacyMartialImport.NativeSources",Matrix);
        yield return new("LegacyMartialImport.OwnershipAndRollback",f=>Run(f,null));
        string? copy=Environment.GetEnvironmentVariable("RWOM_MARTIAL_IMPORT_COPY");
        if(!string.IsNullOrWhiteSpace(copy)) yield return new("LegacyMartialImport.RealSave",f=>Run(f,copy));
    }
    private static void Matrix(RuntimeFixture f)
    {
        var enabled=ClassProgressionFeature.Enabled;
        var installed=TM_ClassUtility.CustomClasses;
        try
        {
            ClassProgressionFeature.Enabled=true;LoadBaseDefinitions(f);TM_ClassUtility.CustomClasses=[];
            var fn=typeof(ClassInitiation).Assembly.GetType("TorannMagic.ClassProgression.LegacyClassConversion")!.GetMethod("TryImport",BindingFlags.NonPublic|BindingFlags.Static)!;
            var entries=new (string trait,string family,string id)[] {
                ("Gladiator","G","gladiator"),("TM_Sniper","S","sniper"),("Bladedancer","B","bladedancer"),
                ("Ranger","R","ranger"),("Faceless","F","faceless"),("TM_Psionic","P","psionic"),
                ("DeathKnight","DK","deathknight"),("TM_Monk","M","monk"),("TM_Commander","C","commander"),
                ("TM_SuperSoldier","SS","supersoldier"),("TM_Apothecary","Apothecary","apothecary")
            };
            foreach(var e in entries)
            {
                var p=PawnWithComps(false,true);var trait=DefDatabase<TraitDef>.GetNamed(e.trait);
                p.pawn.story.traits.allTraits.Add(new Trait(trait));
                var comp=new CompClassProgression{parent=p.pawn};p.pawn.AllComps.Add(comp);
                var data=p.might.MightData;
                var owners=(List<MightPower>)typeof(MightData).GetProperty("MightPowers"+e.family)!.GetValue(data)!;
                foreach(var o in owners)o.learned=false;
                owners[0].learned=true;
                if(e.id=="supersoldier")
                {
                    owners[1].learned=true;
                    Check.Equal(false,(bool)fn.Invoke(null,[p.pawn])!,"ambiguous specialization stays classic");owners[1].learned=false;
                }
                if(e.id=="apothecary") TM_ClassUtility.CustomClasses.Add(new TM_CustomClass{
                    classTrait=trait,isFighter=true,fullScript=(ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef))});
                if(e.id=="apothecary") TM_ClassUtility.CustomClasses.Last().fullScript.defName="BookOfApothecary";
                Check.Equal(true,(bool)fn.Invoke(null,[p.pawn])!,e.id+" recognized through normal import dispatch");
                Check.Equal(true,comp.State.Find("rwom."+e.id)!=null,e.id+" stable identity");
                Check.Equal(1,owners.Count(o=>o.learned),e.id+" sparse owners preserved");
            }
        }
        finally {ClassProgressionFeature.Enabled=enabled;TM_ClassUtility.CustomClasses=installed;}
    }
    private static void Run(RuntimeFixture f,string? copy)
    {
        var enabled=ClassProgressionFeature.Enabled;var current=Current.Game;
        try
        {
            ClassProgressionFeature.Enabled=true;Current.Game=(Game)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game));Set(Current.Game,"maps",new List<Map>{(Map)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Map))});Set(Current.Game,"tickManager",new TickManager());Set(Current.Game,"uniqueIDsManager",new UniqueIDsManager());
            LoadBaseDefinitions(f);
            var p=PawnWithComps(false,true);
            p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.Gladiator));
            var comp=new CompClassProgression{parent=p.pawn};p.pawn.AllComps.Add(comp);
            var fn=typeof(ClassInitiation).Assembly.GetType("TorannMagic.ClassProgression.LegacyClassConversion")!.GetMethod("TryImport",BindingFlags.NonPublic|BindingFlags.Static)!;
            bool Import()=>(bool)fn.Invoke(null,[p.pawn])!;
            var data=p.might.MightData;
            if(copy!=null)
            {
                var xml=XDocument.Load(copy);
                var pawn=xml.Descendants("li").Single(x=>(string?)x.Element("id")=="Human915");
                Check.Equal(true,pawn.Element("story")!.Descendants("def").Any(x=>x.Value=="Gladiator"),"actual saved trait");
                p.pawn.def.defName="Human";p.pawn.thingIDNumber=915;
                var original=pawn.Element("mightData")!;
                foreach(var name in original.Descendants("TMabilityDefs").SelectMany(l=>l.Elements("li")).Select(x=>x.Value).Distinct())
                    if(DefDatabase<TMAbilityDef>.GetNamedSilentFail(name)==null) f.AddAbility(name);
                string input=Path.Combine(f.WorkingDirectory,"extracted.xml");
                new XDocument(new XElement("regression",new XElement("pawnData",new XElement(original)))).Save(input);
                data=Read(input,p.pawn).might;
                Set(p.might,"mightData",data);
            }
            else
            {
                var owners=data.MightPowersG;
                foreach(var o in owners)o.learned=false;
                owners[0].learned=true;owners[0].level=2;owners[0].autocast=true;owners[0].ticksUntilNextCast=413;
                data.MightPowerSkill_Sprint[0].level=2;
                data.MightUserLevel=17;data.MightUserXP=173;data.MightAbilityPoints=6;
            }
            var source=p.pawn.story.traits.allTraits.Last();
            AbilityUser.PawnAbility? existing=null;
            if(copy==null)
            {
                existing=new AbilityUser.PawnAbility(p.pawn,data.mightPowerG[0].abilityDef){CooldownTicksLeft=711};
                p.might.AbilityData.Powers.Add(existing);
            }
            var runtime=p.might.AbilityData.Powers.ToArray();
            var native=data.mightPowerG.ToArray();
            string before=Path.Combine(f.WorkingDirectory,"before.xml"),after=Path.Combine(f.WorkingDirectory,"after.xml");
            Write(before,new Envelope{might=data,progression=comp});
            ClassProgressionFeature.Enabled=false;Check.Equal(false,Import(),"beta off cannot import");
            ClassProgressionFeature.Enabled=true;
            var ambiguous=new Trait(TorannMagicDefOf.Priest);p.pawn.story.traits.allTraits.Add(ambiguous);
            Check.Equal(false,Import(),"ambiguous dual class left intact");p.pawn.story.traits.allTraits.Remove(ambiguous);
            Check.Equal(true,Import(),"native warrior imported once");
            Check.Equal(false,Import(),"retry idempotent");
            Check.Equal(true,p.pawn.story.traits.allTraits.Contains(source),"classic source remains");
            Check.Equal(true,native.SequenceEqual(data.mightPowerG),"same owners");
            Check.Equal(true,runtime.SequenceEqual(p.might.AbilityData.Powers),"same runtime abilities");
            var path=comp.State.Find("rwom.gladiator")!;
            Check.Equal(copy==null?InitiationStage.Foundation:InitiationStage.Master,path.Stage,"earned stage only");
            Write(after,new Envelope{might=data,progression=comp});
            XElement Saved(string file)=>XDocument.Load(file).Descendants("mightData").Single();
            Check.Equal(true,XNode.DeepEquals(Saved(before),Saved(after)),"all serialized might fields exactly preserved");
            TM_ClassState.RestoreCurrentMightRuntime(p.might);
            if(existing!=null)
            {
                Check.Equal(true,p.might.AbilityData.Powers.Contains(existing),"production recovery keeps runtime owner");
                Check.Equal(711,existing.CooldownTicksLeft,"runtime cooldown untouched by recovery");
            }
            Check.Equal(copy!=null,data.mightPowerG[1].learned,"production recovery respects original family");
            if(copy!=null)
            {
                var blocked=typeof(ClassInitiation).Assembly.GetType("TorannMagic.ClassProgression.LegacyMartialConversion")!.GetMethod("AlternativeConflicts",BindingFlags.NonPublic|BindingFlags.Static)!;
                Check.Equal(true,(bool)blocked.Invoke(null,[p.pawn,true])!,"grandfathered ultimate prevents two final rewards");
                Check.Equal(false,(bool)blocked.Invoke(null,[p.pawn,false])!,"native ascension remains reachable");
            }
            var restored=Read(after,p.pawn);
            Check.Equal(path.Stage,restored.progression.State.Find("rwom.gladiator")!.Stage,"career roundtrip");
            Check.Equal(data.MightUserLevel,restored.might.MightUserLevel,"saved level");
            Check.Equal(data.MightUserXP,restored.might.MightUserXP,"saved XP");
            Check.Equal(data.MightAbilityPoints,restored.might.MightAbilityPoints,"saved points");
            if(copy==null)
            {
                Check.Equal(2,restored.might.MightPowerSkill_Sprint[0].level,"paid passive received");
                Check.Equal(2,restored.might.mightPowerG[0].level,"native ranks");
                Check.Equal(413,restored.might.mightPowerG[0].ticksUntilNextCast,"native cooldown");
                Check.Equal(true,restored.might.mightPowerG[0].autocast,"native autocast");
                Check.Equal(false,restored.might.mightPowerG[1].learned,"native locked family");
            }
        }
        finally {ClassProgressionFeature.Enabled=enabled;Current.Game=current;}
    }
    private static void Write(string file,Envelope envelope)
    {
        try{Scribe.saver.InitSaving(file,"regression");Scribe_Deep.Look(ref envelope,"pawnData");Scribe.saver.FinalizeSaving();}
        finally{Scribe.ForceStop();}
    }
    private static Envelope Read(string file,Pawn owner)
    {
        Envelope envelope=null!;
        try
        {
            Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref envelope,"pawnData");
            var refs=Scribe.loader.crossRefs;
            var directory=refs.GetType().GetField("loadedObjectDirectory",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(refs)!;
            directory.GetType().GetMethod("RegisterLoaded")!.Invoke(directory,[owner]);Scribe.loader.FinalizeLoading();
        }
        finally{Scribe.ForceStop();}
        return envelope;
    }
}
