using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;
internal static class PsionicCrucibleTests
{
    static readonly Dictionary<Thing,int> impacts=[];
    public static bool Record(Thing __instance,DamageInfo dinfo,ref DamageWorker.DamageResult __result)
    {
        impacts.TryGetValue(__instance,out int count);impacts[__instance]=count+1;
        Check.Equal(14f,dinfo.Amount,"single kinetic pulse");
        __result=new DamageWorker.DamageResult{totalDamageDealt=dinfo.Amount};return false;
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might)
    {
        var map=pawn.Map;var pos=pawn.Position;var jobs=pawn.jobs;var energy=pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_PsionicHD);
        var oldQuests=Find.QuestManager;
        var defs=DefDatabase<HediffDef>.AllDefsListForReading.ToArray();var oldEnergy=energy.Severity;
        pawn.ageTracker??=new Pawn_AgeTracker(pawn);Set(pawn.ageTracker,"ageBiologicalTicksInt",20L*3600000L);
        pawn.needs??=new Pawn_NeedsTracker(pawn);
        if(might.Stamina==null)pawn.needs.AllNeeds.Add(new Need_Stamina(pawn){def=TorannMagicDefOf.TM_Stamina,CurLevel=1});
        var stam=might.Stamina;var oldStam=stam.CurLevel;
        const BindingFlags h=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var path=ClassIdentity.Component(pawn).State.Find("rwom.psionic")!;
        var data=path.GetType().GetField("MartialData",h)!.GetValue(path)!;
        var oldRelations=typeof(Faction).GetField("relations",h)!.GetValue(pawn.Faction);
        var enemyFaction=new Faction{def=new FactionDef()};
        var enemies=new List<Pawn>();
        var damage=typeof(PsionicCrucibleAbility).Assembly.GetType("TorannMagic.TMDamageDefOf+DamageDefOf",true)!.GetField("TM_PsionicInjury")!;
        var oldDef=damage.GetValue(null);
        var lib=Assembly.Load("0Harmony");var ht=lib.GetType("HarmonyLib.Harmony")!;var id="rwom.test.crucible."+Guid.NewGuid();
        var patch=Activator.CreateInstance(ht,id)!;
        try
        {
            Set(Current.Game,"questManager",new QuestManager());
            var prefix=Activator.CreateInstance(lib.GetType("HarmonyLib.HarmonyMethod")!,typeof(PsionicCrucibleTests).GetMethod(nameof(Record))!)!;
            ht.GetMethod("Patch")!.Invoke(patch,[typeof(Thing).GetMethod(nameof(Thing.TakeDamage))!,prefix,null,null,null]);
            damage.SetValue(null,new DamageDef{defName="TestPsionicStrike"});
            Set(pawn.Faction,"relations",new List<FactionRelation>{new(){other=enemyFaction,kind=FactionRelationKind.Hostile}});
            Set(enemyFaction,"relations",new List<FactionRelation>{new(){other=pawn.Faction,kind=FactionRelationKind.Hostile}});
            var ward=new HediffDef{defName="TM_PsionicCrucibleWard",hediffClass=typeof(Hediff_CrucibleWard),initialSeverity=1};
            if(DefDatabase<HediffDef>.GetNamedSilentFail(ward.defName)==null)DefDatabase<HediffDef>.Add(ward);
            map.info=new MapInfo{Size=new IntVec3(40,1,40)};map.cellIndices=new CellIndices(map);map.thingGrid=new ThingGrid(map);map.fogGrid=new FogGrid(map);map.edificeGrid=new EdificeGrid(map);
            Set(pawn,"positionInt",new IntVec3(16,0,16));pawn.def.category=ThingCategory.Pawn;pawn.def.size=new IntVec2(1,1);map.thingGrid.Register(pawn);
            for(int i=0;i<4;i++)
            {
                var e=PawnWithComps(false,false).pawn;e.thingIDNumber=92050+i;e.def.category=ThingCategory.Pawn;e.def.size=new IntVec2(1,1);
                e.kindDef=new PawnKindDef{race=e.def};e.guest=new Pawn_GuestTracker(e);e.mindState=new Pawn_MindState(e);
                Set(e,"factionInt",enemyFaction);Set(e,"mapIndexOrState",(sbyte)0);Set(e,"positionInt",pawn.Position);map.thingGrid.Register(e);enemies.Add(e);
            }
            var jd=new JobDef{driverClass=typeof(JobDriver_PsionicCrucible),defName="TM_PsionicCrucibleCast"};
            var job=new Job{def=jd,targetA=pawn.Position,targetB=pawn.Position};
            var driver=new JobDriver_PsionicCrucible{pawn=pawn,job=job};
            pawn.jobs=new Pawn_JobTracker(pawn);Set(pawn.jobs,"curDriver",driver);Set(pawn.jobs,"curJob",job);
            object? Call(string name,params object[] args)=>typeof(JobDriver_PsionicCrucible).GetMethod(name,h)!.Invoke(driver,args);
            void Near(float expect,float actual,string what)=>Check.Equal(true,Math.Abs(expect-actual)<.005f,what);
            Set(driver,"mapId",map.uniqueID);Set(driver,"begun",true);
            energy.Severity=100;stam.CurLevel=.25f*stam.MaxLevel;
            Check.Equal(false,(bool)Call("Commit")!,"insufficient stamina prevents spending either pool");
            Check.Equal(100f,energy.Severity,"no debit on failed commit");
            Call("Finish");Call("Finish");Near(94,energy.Severity,"interrupt costs six psi once");
            Near(.22f*stam.MaxLevel,stam.CurLevel,"interrupt costs three percent stamina once");
            Set(data,"CrucibleReadyAt",0);Set(driver,"finished",false);stam.CurLevel=stam.MaxLevel;energy.Severity=100;
            Check.Equal(true,(bool)Call("Ready",pawn)!,"funded native channel ready");
            var pacifist=new Trait(new TraitDef{defName="CruciblePacifistTest",disabledWorkTags=WorkTags.Violent,degreeDatas=[new TraitDegreeData()]});
            pawn.story.traits.allTraits.Add(pacifist);
            try {
                Check.Equal(true,pawn.WorkTagIsDisabled(WorkTags.Violent),"fixture forbids violence");
                Check.Equal(false,(bool)Call("Ready",pawn)!,"violent channel rejects a pacifist actor");
            } finally {pawn.story.traits.allTraits.Remove(pacifist);}
            Check.Equal(true,(bool)Call("Commit")!,"both pools paid once");
            Near(40,energy.Severity,"psi cost");Near(.7f*stam.MaxLevel,stam.CurLevel,"stamina cost");
            Check.Equal(true,(bool)Call("Protects",pawn)!,"ally acquired and in range");
            var hit=new DamageInfo(DamageDefOf.Blunt,50);object[] args=[hit,false];
            Call("Shield",args);Check.Equal(true,(bool)args[1],"first damage blocked");
            var file=Path.Combine(f.WorkingDirectory,"psionic-channel.xml");
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref driver,"driver");Scribe.saver.FinalizeSaving();}finally{Scribe.ForceStop();}
            driver=null!;
            try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref driver,"driver");driver.pawn=pawn;driver.job=job;
                var dir=(LoadedObjectDirectory)typeof(CrossRefHandler).GetField("loadedObjectDirectory",h)!.GetValue(Scribe.loader.crossRefs)!;dir.RegisterLoaded(pawn);foreach(var e in enemies)dir.RegisterLoaded(e);
                Scribe.loader.FinalizeLoading();}finally{Scribe.ForceStop();}
            Set(pawn.jobs,"curDriver",driver);
            Check.Equal(false,(bool)Call("Commit")!,"no repeat payment after reload");
            args=[new DamageInfo(DamageDefOf.Blunt,40),false];Call("Shield",args);
            Near(30,((DamageInfo)args[0]).Amount,"shared ten-point remainder after reload");
            Call("Pulse",0);Call("Pulse",0);Check.Equal(1,(int)driver.GetType().GetField("pulses",h)!.GetValue(driver)!,"no repeated pulse");
            Call("Pulse",1);Call("Pulse",2);Call("Pulse",3);Call("Pulse",3);
            Check.Equal(3,impacts.Count,"three hostile targets maximum");Check.Equal(12,impacts.Values.Sum(),"four hits each, once");
            Call("Finish");Check.Equal(false,pawn.health.hediffSet.HasHediff(ward),"no lingering ward");
            Check.Equal(Find.TickManager.TicksGame+600,(int)data.GetType().GetField("DisruptedUntil",h)!.GetValue(data)!,"recovery quiet period");
        }
        finally{Set(Current.Game,"questManager",oldQuests);impacts.Clear();ht.GetMethod("UnpatchAll")!.Invoke(patch,[id]);Set(pawn.Faction,"relations",oldRelations);damage.SetValue(null,oldDef);foreach(var e in enemies)map.thingGrid.Deregister(e);map.thingGrid.Deregister(pawn);Set(pawn,"positionInt",pos);pawn.jobs=jobs;energy.Severity=oldEnergy;stam.CurLevel=oldStam;
            DefDatabase<HediffDef>.Clear();foreach(var def in defs)DefDatabase<HediffDef>.Add(def);}
    }
}
