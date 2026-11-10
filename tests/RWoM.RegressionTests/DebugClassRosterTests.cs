using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using TorannMagic.TMDefs;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class DebugClassRosterTests
{
    static readonly Assembly asm=typeof(CompClassProgression).Assembly;
    static readonly Type api=asm.GetType("TorannMagic.ModOptions.DebugClassRoster",true)!;
    static readonly Type mode=asm.GetType("TorannMagic.ModOptions.RosterMode",true)!;
    const BindingFlags h=BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("DebugClassRoster.NativeAndKureCatalog",Catalog);
        yield return new("DebugClassRoster.ExplicitClassicSurvivesRestart",State);
    }
    static void Catalog(RuntimeFixture f)
    {
        using var compatibility = new PridestalkerCompatibilityTests.Hooks();
        LoadBaseDefinitions(f);var traits=DefDatabase<TraitDef>.AllDefsListForReading.ToArray();
        var profiles=DefDatabase<ClassDisciplineDef>.AllDefsListForReading.ToArray();var customs=TM_ClassUtility.CustomClasses;
        var enabled=ClassProgressionFeature.Enabled;
        try
        {
            ClassProgressionFeature.Enabled=true;
            var plant=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));plant.defName="RosterFoundationBook";
            var magic=new ClassDisciplineDef{defName="RosterFire",legacyTrait=TorannMagicDefOf.InnerFire,foundationBook=plant,profileId="rwom.fire",domain=DisciplineDomain.Magic};
            if(!profiles.Any(p=>p.profileId==magic.profileId))DefDatabase<ClassDisciplineDef>.Add(magic);
            var dir=Environment.GetEnvironmentVariable("RWOM_KURE_CANDIDATE")??Path.GetFullPath(Path.Combine(f.Repository,"../artifacts/kure-pridestalker/candidate"));
            var xml=Path.Combine(dir,"Common/Patches/RFS_CustomClasses.xml");var addon=(ModContentPack)RuntimeHelpers.GetUninitializedObject(typeof(ModContentPack));
            var pridestalker=traits.FirstOrDefault(t=>t.defName=="TM_Pridestalker")??new TraitDef{defName="TM_Pridestalker",degreeDatas=[new TraitDegreeData{degree=4,label="pridestalker"}]};
            if(!traits.Contains(pridestalker))DefDatabase<TraitDef>.Add(pridestalker);
            var oldOrigin=pridestalker.modContentPack;pridestalker.modContentPack=addon;
            var oldDegrees=pridestalker.degreeDatas;
            pridestalker.degreeDatas=[new TraitDegreeData{degree=4,label="pridestalker"}];
            try
            {
                var registrations=XDocument.Load(xml).Descendants("li").Where(x=>x.Element("classTrait")!=null).ToArray();
                var kure=registrations.Select(x=>{
                    var id=(string)x.Element("classTrait")!;var degree=(int?)x.Element("traitDegree")??0;
                    var trait=id=="TM_Pridestalker"?pridestalker:new TraitDef{defName=id,modContentPack=addon,degreeDatas=[new TraitDegreeData{degree=degree,label=id}]};
                    List<TMAbilityDef> Abilities(string tag)=>x.Element(tag)?.Elements("li").Select(i=>{var power=(TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));power.defName=i.Value;return power;}).ToList()??[];
                    List<ThingDef> Scrolls(string tag)=>x.Element(tag)?.Elements("li").Select(i=>plant).ToList()??[];
                    return new TM_CustomClass{classTrait=trait,traitDegree=degree,isMage=(bool?)x.Element("isMage")??false,
                        isFighter=(bool?)x.Element("isFighter")??false,classMageAbilities=Abilities("classMageAbilities"),
                        classFighterAbilities=Abilities("classFighterAbilities"),learnableSpells=Scrolls("learnableSpells"),learnableSkills=Scrolls("learnableSkills")};
                }).ToList();
                var d=kure.Single(k=>k.classTrait==pridestalker);
                TM_ClassUtility.CustomClasses=[new TM_CustomClass{classTrait=pridestalker,traitDegree=0,isMage=true},..kure];
                List<object> Rows(string choice)=>((IEnumerable)api.GetMethod("Catalog",h)!.Invoke(null,[Enum.Parse(mode,choice)])!).Cast<object>().ToList();
                T Field<T>(object e,string name)=>(T)e.GetType().GetField(name,h)!.GetValue(e)!;
                var all=Rows("Preferred");var classes=Rows("ClassicNative");var addons=Rows("Addons");var foundations=Rows("Foundations");
                Check.Equal(kure.Count,addons.Count,"one row for every Kure registration");var pRow=addons.Single(e=>Field<TraitDef>(e,"Trait")==pridestalker);
                var explorer=addons.Single(e=>Field<TraitDef>(e,"Trait").defName=="TM_Explorer");
                Check.Equal(true,(int)explorer.GetType().GetProperty("ProfileSize",h)!.GetValue(explorer)!>0,"Explorer has learnable scrolls despite no starting abilities");
                Check.Equal(4,Field<int>(pRow,"Degree"),"working degree");
                Check.Equal(d,Field<TM_CustomClass>(pRow,"Custom"),"full mixed Kure class keeps exact registration");
                Check.Equal(true,all.Any(e=>Field<TraitDef>(e,"Trait")==pridestalker),"all includes Kure");
                Check.Equal(false,classes.Any(e=>Field<TraitDef>(e,"Trait")==pridestalker),"native list excludes Kure");
                Check.Equal(true,foundations.Any(e=>Field<TraitDef>(e,"Trait")==TorannMagicDefOf.InnerFire),"native fire foundation");
                Check.Equal(1,foundations.Count(e=>Field<bool>(e,"Wanderer")),"one hybrid wanderer foundation");
                Check.Equal(2,classes.Count(e=>Field<bool>(e,"Wanderer")),"both original classic routes");
                ClassProgressionFeature.Enabled=false;Check.Equal(0,Rows("Foundations").Count,"beta disabled cannot grant new paths");
            }
            finally {pridestalker.modContentPack=oldOrigin;pridestalker.degreeDatas=oldDegrees;}
        }
        finally
        {
            ClassProgressionFeature.Enabled=enabled;TM_ClassUtility.CustomClasses=customs;
            DefDatabase<TraitDef>.Clear();foreach(var d in traits)DefDatabase<TraitDef>.Add(d);
            DefDatabase<ClassDisciplineDef>.Clear();foreach(var d in profiles)DefDatabase<ClassDisciplineDef>.Add(d);
        }
    }
    internal static void Grant(RuntimeFixture f,Pawn pawn,ThingDef form,CompClassProgression comp)
    {
        var defs=DefDatabase<ThingDef>.AllDefsListForReading.ToArray();
        try
        {
            if(DefDatabase<ThingDef>.GetNamedSilentFail(form.defName)==null)DefDatabase<ThingDef>.Add(form);
            var t=asm.GetType("TorannMagic.ModOptions.RosterEntry")!;
            var row=Activator.CreateInstance(t,true)!;
            t.GetField("Trait",h)!.SetValue(row,TorannMagicDefOf.Gladiator);
            t.GetField("Foundation",h)!.SetValue(row,"rwom.gladiator");
            t.GetField("Book",h)!.SetValue(row,"BookOfGladiator");
            t.GetField("Choice",h)!.SetValue(row,"TM_ReadGladiatorSprint");
            bool Apply(bool modern)=>(bool)api.GetMethod("Grant",h)!.Invoke(null,[pawn,row,modern,true])!;
            Check.Equal(true,Apply(true),"real generated-book grant");
            Check.Equal(InitiationStage.Foundation,comp.State.Find("rwom.gladiator")!.Stage,"one new foundation");
            Check.Equal(0,pawn.inventory.innerContainer.Count,"no temporary or donated book remains");
            Check.Equal(false,Apply(true),"cannot overwrite existing foundation");
            Check.Equal(false,Apply(false),"classic debug route cannot overwrite existing foundation");
            var map=pawn.Map;map.info=new MapInfo{Size=new IntVec3(40,1,40)};map.cellIndices=new CellIndices(map);
            map.thingGrid=new ThingGrid(map);map.fogGrid=new FogGrid(map);map.edificeGrid=new EdificeGrid(map);
            var context=new PathingContext(map,new PathGrid(map,new PathGridDef{defName="DebugRosterWalk"}));
            map.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));Set(map.pathing,"normal",context);Set(map.pathing,"fenceBlocked",context);
            pawn.def.category=ThingCategory.Pawn;pawn.def.size=new IntVec2(1,1);Set(pawn,"positionInt",new IntVec3(10,0,10));
            bool Clear(IntVec3 c)=>(bool)api.GetMethod("ClearCell",h)!.Invoke(null,[c,map,pawn.def])!;
            Check.Equal(false,Clear(IntVec3.Invalid),"invalid cell");
            Check.Equal(true,Clear(pawn.Position),"open cell");
            map.thingGrid.Register(pawn);
            Check.Equal(false,Clear(pawn.Position),"existing colonist prevents placement");
            Check.Equal(pawn,pawn.Position.GetFirstPawn(map),"existing colonist remains in place");
        }
        finally{DefDatabase<ThingDef>.Clear();foreach(var d in defs)DefDatabase<ThingDef>.Add(d);}
    }
    public sealed class Carrier:IExposable
    {
        public CompClassProgression Comp=new();
        public void ExposeData()=>Comp.PostExposeData();
    }
    static void State(RuntimeFixture f)
    {
        var carrier=new Carrier();var field=typeof(CompClassProgression).GetField("DebugKeepClassic",h)!;
        field.SetValue(carrier.Comp,true);
        var path=Path.Combine(f.WorkingDirectory,"pinned.xml");
        try {Scribe.saver.InitSaving(path,"test");Scribe_Deep.Look(ref carrier,"carrier");Scribe.saver.FinalizeSaving();}
        finally {Scribe.ForceStop();}
        carrier=null!;
        try {Scribe.loader.InitLoading(path);Scribe_Deep.Look(ref carrier,"carrier");Scribe.loader.FinalizeLoading();}
        finally {Scribe.ForceStop();}
        Check.Equal(true,(bool)field.GetValue(carrier.Comp)!,"explicit classic sample remains pinned without beta state");
        Check.Equal(null,carrier.Comp.State,"serializing pin does not enroll");
    }
}
