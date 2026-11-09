using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using Verse.AI;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class RangerStagesTests
{
    internal static void Verify(RuntimeFixture f, Pawn ranger, Pawn pet, CompAbilityUserMight might, CompClassProgression comp, bool peaceful, Action reload)
    {
        var asm = typeof(SniperSurvey).Assembly;
        var mastery=asm.GetType("TorannMagic.ClassProgression.RangerMastery",true)!;
        var ascension=asm.GetType("TorannMagic.ClassProgression.RangerAscension",true)!;
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        object? Master(string method,params object[] args)=>mastery.GetMethod(method,flags)!.Invoke(null,args);
        object? Ascend(string method,params object[] args)=>ascension.GetMethod(method,flags)!.Invoke(null,args);

        var trail=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerSafeTrail") ?? f.AddAbility("TM_RangerSafeTrail");
        var hunt=DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_RangerBreakthrough") ?? f.AddAbility("TM_RangerBreakthrough");
        var storm=TorannMagicDefOf.TM_ArrowStorm;
        var oldTrail=trail.abilityClass;var oldHunt=hunt.abilityClass;var oldStorm=storm.abilityClass;
        var oldRelations=typeof(Faction).GetField("relations",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(ranger.Faction);
        var foe=new Faction{def=new FactionDef()};
        Set(foe,"relations",new List<FactionRelation>{new(){other=ranger.Faction,kind=FactionRelationKind.Hostile}});
        Set(ranger.Faction,"relations",new List<FactionRelation>{new(){other=foe,kind=FactionRelationKind.Hostile}});
        var victim=PawnWithComps(false,false).pawn;
        victim.kindDef=new PawnKindDef{race=victim.def};victim.mindState=new Pawn_MindState(victim);victim.guest=new Pawn_GuestTracker(victim);
        Set(victim,"mapIndexOrState",(sbyte)0);Set(victim,"factionInt",foe);
        var originalPoints=might.MightData.MightAbilityPoints;
        try
        {
            trail.abilityClass=storm.abilityClass=typeof(PawnAbility);
            hunt.abilityClass=typeof(RangerHuntAbility);
            if (peaceful) RangerFireTrialTests.Verify(f,ranger);
            else RangerTrailTests.Trap(ranger,victim,might);
            for(int i=0;i<(peaceful?12:8);i++)
            {
                Set(Find.TickManager,"ticksGameInt",800000+60000*i);
                Check.Equal(false,(bool)Master("Claim",ranger)!,"no early mastery");
                if(peaceful)
                {
                    var cell=new IntVec3(12,0,12);
                    Master("Cleared",ranger,cell);
                    if(i==3) reload();
                    Master("Cleared",ranger,cell+IntVec3.East);
                    Check.Equal(i+1,(int)Master("Clearances",ranger)!,"same sector cooldown survives save");
                }
                else
                {
                    victim.thingIDNumber=9540+i;
                    Master("Trapped",ranger,victim);
                    if(i==3) reload();
                    Master("Trapped",ranger,victim);
                    Check.Equal(i+1,(int)Master("Traps",ranger)!,"trap requires distinct victims");
                }
            }
            reload();
            Check.Equal(true,(bool)Master("Claim",ranger)!,"earned safe trail");
            Check.Equal(false,(bool)Master("Claim",ranger)!,"no repeat mastery");
            Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==trail),"safe trail added");
            if (peaceful) RangerAnimalCareTrialTests.Verify(ranger);
            else RangerTrailTests.Trail(f,ranger,pet,victim);
            might.MightData.MightUserLevel=30;
            Check.Equal(false,(bool)Ascend("Choose",ranger,peaceful)!,"level alone insufficient");
            might.MightData.MightUserLevel=29;
            for(int i=0;i<(peaceful?10:8);i++)
            {
                Set(Find.TickManager,"ticksGameInt",2000000+60000*i);
                string method=peaceful?"Care":"Retreat";
                Ascend(method,ranger,pet);
                if(i==3) reload();
                Ascend(method,ranger,pet);
                Check.Equal(i+1,(int)Ascend(peaceful?"Cares":"Retreats",ranger)!,"no duplicate daily credit");
            }
            reload();
            Check.Equal(false,(bool)Ascend("Ready",ranger)!,"level 29 retains completed final trial");
            might.MightData.MightUserLevel=30;
            Check.Equal(true,(bool)Ascend("Choose",ranger,peaceful)!,"final choice");
            Check.Equal(false,(bool)Ascend("Choose",ranger,!peaceful)!,"choice is permanent");
            reload();
            var selected=peaceful?hunt:storm;
            might.AbilityData.Powers.RemoveAll(p=>p.Def==selected || p.Def==trail);
            asm.GetType("TorannMagic.ClassProgression.RangerCareer")!.GetMethod("Restore",flags)!.Invoke(null,[might]);
            Check.Equal(InitiationStage.Ascended,comp.State.Find("rwom.ranger")!.Stage,"ascension saved");
            Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==selected),"selected ultimate restored");
            Check.Equal(0,might.AbilityData.Powers.Count(p=>p.Def==(peaceful?storm:hunt)),"other ultimate remains locked");
            Check.Equal(1,might.AbilityData.Powers.Count(p=>p.Def==trail),"master's skill retained");
            Check.Equal(originalPoints,might.MightData.MightAbilityPoints,"no unexpected point debit");
            Check.Same(pet,might.bondedPet,"original animal retained");
            if (peaceful) RangerHuntTests.Verify(f,ranger,might);
        }
        finally
        {
            trail.abilityClass=oldTrail;hunt.abilityClass=oldHunt;storm.abilityClass=oldStorm;
            Set(ranger.Faction,"relations",oldRelations!);
        }
    }
}
