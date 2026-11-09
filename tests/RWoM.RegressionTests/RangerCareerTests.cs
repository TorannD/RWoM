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

internal static class RangerCareerTests
{
    internal static void Verify(RuntimeFixture f,Pawn ranger,CompAbilityUserMight might,CompClassProgression comp,bool trails)
    {
        var api = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.RangerCareer",true)!;
        const BindingFlags hidden=BindingFlags.Static|BindingFlags.NonPublic;
        object? Call(string name,params object[] args) => api.GetMethod(name,hidden)!.Invoke(null,args);
        var file=Path.Combine(f.WorkingDirectory,"ranger-bond-trial.xml");
        void Reload()
        {
            var state=comp.State;
            try { Scribe.saver.InitSaving(file,"test"); Scribe_Deep.Look(ref state,"state"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            state=null!;
            try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref state,"state"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Set(comp,"state",state);
        }
        var animal=ranger.Map;
        animal.info=new MapInfo{Size=new IntVec3(40,1,40)};
        animal.cellIndices=new CellIndices(animal);animal.thingGrid=new ThingGrid(animal);
        animal.edificeGrid=new EdificeGrid(animal);
        var grid=new PathGrid(animal,new PathGridDef{defName="AnimalCarePath"});
        var context=new PathingContext(animal,grid);
        animal.pathing=(Pathing)RuntimeHelpers.GetUninitializedObject(typeof(Pathing));
        Set(animal.pathing,"normal",context);Set(animal.pathing,"fenceBlocked",context);
        Set(ranger,"positionInt",new IntVec3(6,0,7));
        var pet=PawnWithComps(false,false).pawn;
        pet.def=(ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pet.def.race=new RaceProperties{intelligence=Intelligence.Animal};
        Set(pet.def.race,"fleshType",new FleshTypeDef{isOrganic=true});
        pet.def.category=ThingCategory.Pawn;pet.def.size=new IntVec2(1,1);
        pet.kindDef=new PawnKindDef{race=pet.def};pet.guest=new Pawn_GuestTracker(pet);
        pet.thingIDNumber=5150;
        Set(pet,"mapIndexOrState",(sbyte)0);Set(pet,"positionInt",ranger.Position+IntVec3.East);
        Set(pet,"factionInt",ranger.Faction);
        var trap=TorannMagicDefOf.TM_PoisonTrap;
        var oldType=trap.abilityClass;
        var oldBond=TorannMagicDefOf.TM_RangerBondHD;
        var original=might.bondedPet;
        try
        {
            trap.abilityClass=typeof(PawnAbility);
            int previousLevel=might.MightData.MightUserLevel, previousPoints=might.MightData.MightAbilityPoints;
            might.MightData.MightUserLevel=11; might.MightData.MightAbilityPoints=7;
            might.AssignAbilities();
            Check.Equal(11,might.MightData.MightUserLevel,"reinitialization preserves enrolled ranger level");
            Check.Equal(7,might.MightData.MightAbilityPoints,"reinitialization preserves ranger points");
            might.MightData.MightUserLevel=previousLevel; might.MightData.MightAbilityPoints=previousPoints;
            TorannMagicDefOf.TM_RangerBondHD=new HediffDef{defName="RegressionRangerBond"};
            pet.health.hediffSet.hediffs.Add(new Hediff{def=TorannMagicDefOf.TM_RangerBondHD,pawn=pet});
            Check.Equal(false,(bool)Call("Eligible",ranger,pet)!,"unrelated animal cannot advance");
            might.bondedPet=pet;
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==TorannMagicDefOf.TM_AnimalFriend),"animal friend unlocked on enrollment");
            Set(Find.TickManager,"ticksGameInt",10);
            Check.Equal(true,(bool)Call("FinishCare",ranger,pet)!,"first physical visit");
            Call("BondChanged",ranger);
            Check.Equal(0,(int)Call("Days",ranger)!,"changing bond resets unfinished trial");
            Check.Equal(false,(bool)Call("FinishCare",ranger,pet)!,"rebonding does not reset daily cooldown");
            for (int i=1;i<=12;i++)
            {
                Set(Find.TickManager,"ticksGameInt",10+60000*i);
                Check.Equal(false,(bool)Call("Claim",ranger)!,"must finish twelve visits first");
                Set(pet,"mapIndexOrState",(sbyte)-1);
                Check.Equal(false,(bool)Call("FinishCare",ranger,pet)!,"off-map animal");
                Set(pet,"mapIndexOrState",(sbyte)0);
                Check.Equal(true,(bool)Call("FinishCare",ranger,pet)!,"actual daily visit accepted");
                if(i==6) Reload();
                Check.Equal(false,(bool)Call("FinishCare",ranger,pet)!,"no second visit credit");
            }
            Reload();
            Check.Equal(true,(bool)Call("Claim",ranger)!,"completed care unlocks trap");
            Check.Equal(false,(bool)Call("Claim",ranger)!,"single reward");
            Reload();
            might.AbilityData.Powers.RemoveAll(a=>a.Def==trap);
            Call("Restore",might);
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==trap),"trap restored");
            Check.Same(pet,might.bondedPet,"training does not replace the companion");
            RangerStagesTests.Verify(f,ranger,pet,might,comp,trails,Reload);
        }
        finally { trap.abilityClass=oldType; TorannMagicDefOf.TM_RangerBondHD=oldBond; might.bondedPet=original; }
    }
}
