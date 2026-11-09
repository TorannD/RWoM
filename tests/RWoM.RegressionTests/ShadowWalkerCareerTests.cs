using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;
namespace RWoM.RegressionTests;

internal static class ShadowWalkerCareerTests
{
    private sealed class DefScope : IDisposable
    {
        private readonly Dictionary<TMAbilityDef,Type> types=new();
        private readonly JobDef[] previous = DefDatabase<JobDef>.AllDefsListForReading.ToArray();
        internal DefScope()
        {
            foreach(var job in new[]{"TM_ReadShadowWalkerStrike","TM_ReadShadowWalkerPoison"})
                if(DefDatabase<JobDef>.GetNamedSilentFail(job)==null)DefDatabase<JobDef>.Add(new JobDef{defName=job});
            foreach(var def in new[]{TorannMagicDefOf.TM_ShadowStrike,TorannMagicDefOf.TM_Nightshade,TorannMagicDefOf.TM_VeilOfShadows})
            { types[def]=def.abilityClass;def.abilityClass=typeof(PawnAbility); }
        }
        public void Dispose(){foreach(var(def,type) in types)def.abilityClass=type; DefDatabase<JobDef>.Clear();foreach(var def in previous)DefDatabase<JobDef>.Add(def);}
    }
    internal static IDisposable Setup(RuntimeFixture f)=>new DefScope();
    public static bool SkipFontLayout() => false;
    internal static void BookMenu(Pawn pawn, ThingDef definition)
    {
        var effect=new CompUseEffect_LearnMagic { parent=new ThingWithComps{def=definition,stackCount=1} };
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;
        string id="rwom.regression.shadowmenu."+Guid.NewGuid();var harmony=Activator.CreateInstance(ht,id)!;
        try {
            var prefix=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(ShadowWalkerCareerTests).GetMethod(nameof(SkipFontLayout))!)!;
            ht.GetMethod("Patch")!.Invoke(harmony,[typeof(FloatMenuOption).GetMethod("SetSizeMode",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance),prefix,null,null,null]);
            var choices=effect.CompFloatMenuOptions(pawn).ToList();
            Check.Equal(2,choices.Count(x=>!x.Disabled),"physical options available from the native arcane-book comp");
        } finally {ht.GetMethod("UnpatchAll")!.Invoke(harmony,[id]);}
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        var api=typeof(ClassIdentity).Assembly.GetType("TorannMagic.ClassProgression.ShadowWalkerMartialPath",true)!;
        object? Call(string name,params object[] args)=>api.GetMethod(name,flags)!.Invoke(null,args);
        bool Claim()=>(bool)Call("Claim",pawn,alternate)!;
        var owner=might.MightData.MightPowersShadow;
        bool Has(TMAbilityDef def)=>might.AbilityData.Powers.Any(p=>p.Def==def);
        var first=alternate?TorannMagicDefOf.TM_Nightshade:TorannMagicDefOf.TM_ShadowStrike;
        var second=alternate?TorannMagicDefOf.TM_ShadowStrike:TorannMagicDefOf.TM_Nightshade;
        Check.Equal(true,Has(first),"first martial family");Check.Equal(false,Has(second),"second family staged");
        Check.Equal(true,comp.State.Find("rwom.shadow")==null,"magic profile not granted by physical book reading");
        var file=Path.Combine(f.WorkingDirectory,"shadowwalker.xml");
        void Reload()
        {
            var state=comp.State;
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            state=null!;
            try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Set(comp,"state",state);
        }
        var defs=Path.Combine(f.Repository,"v1.7","Defs");
        var native=XDocument.Load(Path.Combine(defs,"CustomDefs","TM_CustomClassDef.xml")).Descendants("li")
            .Single(e=>(string?)e.Element("classTrait")=="TM_TheShadow" && (string?)e.Element("fullScript")=="BookOfShadow");
        Check.Equal(true,native.Element("classFighterAbilities")!.Elements().Select(e=>e.Value).ToHashSet().SetEquals(owner.Select(x=>x.abilityDef.defName)),"physical side matches shipped custom class");
        Check.Equal(true,native.Element("classMageAbilities")!.Elements().Any(e=>e.Value=="TM_ShadowWalk"),"same native book also serves magicians");
        var extra=(object)Activator.CreateInstance(api,true)!;
        var supports=api.GetMethod("SupportsBook",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var book=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));book.defName="BookOfShadow";
        var baseline=new TM_CustomClass {classTrait=new TraitDef{defName="TM_TheShadow"},fullScript=book,isMage=true,isFighter=true,
            classFighterAbilities=native.Element("classFighterAbilities")!.Elements().Select(e=>DefDatabase<TMAbilityDef>.GetNamedSilentFail(e.Value)??f.AddAbility(e.Value)).ToList(),
            classMageAbilities=native.Element("classMageAbilities")!.Elements().Select(e=>DefDatabase<TMAbilityDef>.GetNamedSilentFail(e.Value)??f.AddAbility(e.Value)).ToList()};
        var old=TM_ClassUtility.CustomClasses;
        try {
            TM_ClassUtility.CustomClasses=[baseline];
            Check.Equal(true,(bool)supports.Invoke(extra,[book])!,"shipped native hybrid signature accepted");
            baseline.classFighterAbilities.Add(TorannMagicDefOf.TM_Cleave);
            Check.Equal(false,(bool)supports.Invoke(extra,[book])!,"addon override stays classic");
        } finally {TM_ClassUtility.CustomClasses=old;}
        var patient=PawnWithComps(false,false).pawn;
        for(int stage=0;stage<3;stage++)
        {
            int n=alternate?new[]{3,4,6}[stage]:new[]{3,5,6}[stage];
            for(int i=0;i<n;i++)
            {
                Check.Equal(false,Claim(),"no early advancement");int id=88100+i;
                if(alternate){patient.thingIDNumber=id;Call("Rescue",pawn,patient);Call("Rescue",pawn,patient);}
                else
                {
                    if(stage==0)Call("Hit",pawn,id+100,true,false);
                    if(stage==2)Call("Hit",pawn,id+100,false,false);
                    Call("Hit",pawn,id,false,stage==2);Call("Hit",pawn,id,false,stage==2);
                }
                if(i==1)Reload();
            }
            if(stage==2){might.MightData.MightUserLevel=29;Check.Equal(false,Claim(),"level gate");might.MightData.MightUserLevel=30;}
            Check.Equal(true,Claim(),"stage claim");
            if(stage==0)Check.Equal(true,Has(second),"second family");
            if(stage==1)Check.Equal(true,Has(TorannMagicDefOf.TM_VeilOfShadows),"veil owner");
        }
        Reload();Check.Equal(false,Claim(),"one capstone");
        Check.Equal(alternate?100:120,(int)Call("StrikeDamage",pawn,100)!,"blade specialisation");
        Check.Equal(alternate?900:600,(int)Call("VeilTicks",pawn,600)!,"veil specialisation");
    }
}
