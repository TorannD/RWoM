using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeCareerTests
{
    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool memory)
    {
        var api = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.BladeCareer", true)!;
        const BindingFlags methods = BindingFlags.Static | BindingFlags.NonPublic;
        object? Call(string name, params object[] arguments) => api.GetMethod(name, methods)!.Invoke(null, arguments);
        void Reload()
        {
            var state = comp.State;
            var file = Path.Combine(f.WorkingDirectory, "bladedancer.xml");
            try { Scribe.saver.InitSaving(file,"test"); Scribe_Deep.Look(ref state,"state"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            state = null!;
            try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref state,"state"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Set(comp,"state",state);
        }

        var spin = TorannMagicDefOf.TM_BladeSpin;
        var passage = DefDatabase<TMAbilityDef>.GetNamed("TM_BladePassage");
        var owner = might.MightData.MightPowersB.Single(p => p.TMabilityDefs.Contains(spin));
        var previous = spin.abilityClass;
        var points = might.MightData.MightAbilityPoints;
        try
        {
            spin.abilityClass = typeof(PawnAbility);
            Check.Equal(false, owner.learned, "no automatic adept reward on enrollment");
            Check.Equal(1, might.AbilityData.Powers.Count(a => a.Def == passage), "passage available before trial");
            if (!memory) BladePassageTests.Verify(f,pawn,might);
            else BladeServiceTests.Verify(f,pawn);
            Check.Equal(6,(int)Call("PassageRange",pawn)!,"starting reach");
            might.MightData.MightAbilityPoints=0;
            Check.Equal(false,(bool)Call("TrainPassage",pawn,1)!,"no free rank");
            might.MightData.MightAbilityPoints=2;
            Check.Equal(true,(bool)Call("TrainPassage",pawn,1)!,"first paid rank");
            Check.Equal(false,(bool)Call("TrainPassage",pawn,1)!,"stale command rejected");
            Check.Equal(8,(int)Call("PassageRange",pawn)!,"trained reach");
            Reload();
            Check.Equal(true,(bool)Call("TrainPassage",pawn,2)!,"last paid rank");
            Check.Equal(false,(bool)Call("TrainPassage",pawn,3)!,"rank capped");
            Check.Equal(10,(int)Call("PassageRange",pawn)!,"mastered reach");
            Check.Equal(0,might.MightData.MightAbilityPoints,"two points spent once");
            might.MightData.MightAbilityPoints=points;
            var blade = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
            blade.category = ThingCategory.Item; blade.thingClass = typeof(ThingWithComps); blade.tools = [new Tool()];
            int threshold = memory ? 20 : 10;
            for (int i = 0; i < threshold; i++)
            {
                Check.Equal(false,(bool)Call("Claim",pawn)!,"trial not yet complete");
                if (!memory) Call("Pass",pawn);
                else
                {
                    var item = new ThingWithComps {def=blade, thingIDNumber=9000+i};
                    Check.Equal(true,(bool)Call("Service",pawn,item)!,"first service counts");
                    if (i==4) Reload();
                    Check.Equal(false,(bool)Call("Service",pawn,item)!,"same weapon remains ineligible");
                }
                if (i==4 && !memory) Reload();
            }
            Reload();
            Check.Equal(true,(bool)Call("Ready",pawn)!,"reward survives pending in save");
            Check.Equal(true,(bool)Call("Claim",pawn)!,"awarded native weapon technique");
            Check.Equal(false,(bool)Call("Claim",pawn)!,"no duplicate claim");
            Reload();
            Check.Equal(InitiationStage.Adept,comp.State.Find("rwom.bladedancer")!.Stage,"adept persists");
            might.AbilityData.Powers.RemoveAll(a=>a.Def==spin || a.Def==passage);
            Call("Restore",might);
            Call("Restore",might);
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==spin),"one restored native reward");
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==passage),"foundation preserved");
            Check.Equal(points,might.MightData.MightAbilityPoints,"trial leaves purchased points intact");
            BladeMasteryTests.Verify(f,pawn,might,comp,memory,Reload);
        }
        finally { spin.abilityClass = previous; }
    }
}
