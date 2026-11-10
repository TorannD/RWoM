using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class CommanderCareerTests
{
    private sealed class Definitions : IDisposable
    {
        internal readonly List<(TMAbilityDef def,Type type)> Old=[];
        public void Dispose() {foreach(var item in Old) item.def.abilityClass=item.type;}
    }
    internal static IDisposable Setup(RuntimeFixture f)
    {
        var defs=new Definitions();
        foreach(string name in new[]{"TM_CommanderWorkCrew","TM_CommanderRally","TM_CommanderOperationEvac","TM_CommanderOperationAid","TM_ProvisionerAura","TM_MoveOut","TM_StayAlert","TM_HoldTheLine","TM_CommanderAura"})
        {
            var def=DefDatabase<TMAbilityDef>.GetNamedSilentFail(name)??f.AddAbility(name);
            defs.Old.Add((def,def.abilityClass));
            def.abilityClass=name.StartsWith("TM_CommanderOperation") ? typeof(CommanderOperationAbility) : typeof(PawnAbility);
        }
        return defs;
    }
    internal static void Verify(RuntimeFixture f,Pawn pawn,CompAbilityUserMight might,CompClassProgression comp,bool alternate)
    {
        var type=typeof(CommanderOperationAbility).Assembly.GetType("TorannMagic.ClassProgression.CommanderMartialPath",true)!;
        var instance=Activator.CreateInstance(type,true)!;
        object? Run(string name,params object[] args) => type.GetMethod(name,BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(instance,args);
        void Reload()
        {
            var state=comp.State; string file=Path.Combine(f.WorkingDirectory,"commander-path.xml");
            try{Scribe.saver.InitSaving(file,"test");Scribe_Deep.Look(ref state,"state");Scribe.saver.FinalizeSaving();}
            finally{Scribe.ForceStop();}
            state=null!;
            try{Scribe.loader.InitLoading(file);Scribe_Deep.Look(ref state,"state");Scribe.loader.FinalizeLoading();}
            finally{Scribe.ForceStop();}
            Set(comp,"state",state);
        }
        bool Has(string name)=>might.AbilityData.Powers.Any(x=>x.Def.defName==name);
        Check.Equal(true,Has("TM_CommanderWorkCrew"),"early shared coordination skill");
        Check.Equal(!alternate,Has("TM_ProvisionerAura"),"chosen foundation");
        Check.Equal(false,Has("TM_HoldTheLine"),"no early mastery");
        int pts=might.MightData.MightAbilityPoints;
        might.MightData.MightAbilityPoints=1;
        Check.Equal(alternate,(bool)Run("TrainSignals",pawn,1)!,"signal training belongs to alternate doctrine");
        Check.Equal(alternate?.54f:.5f,(float)Run("Acceptance",pawn,.5f)!,"trained signal acceptance");
        Check.Equal(alternate?.95f:1f,(float)Run("Acceptance",pawn,1f)!,"refusal remains possible");
        might.MightData.MightAbilityPoints=2;
        Check.Equal(true,(bool)Run("Train",pawn,false,1)!,"one point for working crew");
        Check.Equal(false,(bool)Run("Train",pawn,false,1)!,"stale purchase rejected");
        Reload();
        Check.Equal(true,(bool)Run("Train",pawn,false,2)!,"purchased rank saved");
        Check.Equal(0,might.MightData.MightAbilityPoints,"two points spent once");
        might.MightData.MightAbilityPoints=pts;
        CommanderFieldTests.Verify(f,pawn,might,false);
        for(int i=1;i<=12;i++)
        {
            Check.Equal(false,(bool)Run("Claim",pawn,false)!,"delivery threshold");
            Run("WorkDone",pawn,i,true,false,false);
            if(i==5)Reload();
            Run("WorkDone",pawn,i,true,false,false);
        }
        Check.Equal(true,(bool)Run("Claim",pawn,false)!,"adept reward");
        Check.Equal(true,Has("TM_MoveOut") && Has("TM_StayAlert"),"native movement/alert");
        for(int i=1;i<=(alternate?16:8);i++)
        {
            Check.Equal(false,(bool)Run("Claim",pawn,false)!,"master threshold");
            if(alternate){Run("WorkDone",pawn,i,false,true,true);Run("WorkDone",pawn,i,false,true,true);}
            else{var ally=PawnWithComps(false,false).pawn;ally.thingIDNumber=1000+i;Run("Escape",pawn,ally);Run("Escape",pawn,ally);}
            if(i==4) Reload();
        }
        Check.Equal(true,(bool)Run("Claim",pawn,false)!,"master reward");
        Check.Equal(true,Has("TM_HoldTheLine") && Has("TM_CommanderRally"),"support complements previous abilities");
        might.MightData.MightUserLevel=29;
        for(int i=1;i<=10;i++)
        {
            if(!alternate) Set(Find.TickManager,"ticksGameInt",i*60000);
            Run("WorkDone",pawn,i,true,false,alternate);
            Run("WorkDone",pawn,i,true,false,alternate);
            if(i==5) Reload();
        }
        Check.Equal(false,(bool)Run("Claim",pawn,alternate)!,"level 29");
        might.MightData.MightUserLevel=30;
        Check.Equal(true,(bool)Run("Claim",pawn,alternate)!,"level thirty graduate");
        Check.Equal(false,(bool)Run("Claim",pawn,!alternate)!,"permanent final choice");
        Reload();
        var ultimate=alternate?"TM_CommanderOperationEvac":"TM_CommanderAura";
        might.AbilityData.Powers.RemoveAll(x=>x.Def.defName==ultimate);
        Run("Restore",might,comp.State.Find("rwom.commander")!);
        Check.Equal(true,Has(ultimate),"ultimate restored");
        if(alternate) CommanderFieldTests.Verify(f,pawn,might,true);
        Check.Equal(alternate,Has("TM_CommanderOperationAid"),"two operation modes share a learned choice");
    }
}
