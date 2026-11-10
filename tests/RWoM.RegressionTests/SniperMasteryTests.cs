using System.Reflection;
using AbilityUser;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperMasteryTests
{
    internal static void Verify(RuntimeFixture fixture, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool peaceful)
    {
        var api = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.SniperMastery", true)!;
        object? Call(string name, params object[] args) =>
            api.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);
        void Reload()
        {
            var state = comp.State;
            var file = Path.Combine(fixture.WorkingDirectory, "sniper-mastery.xml");
            try { Scribe.saver.InitSaving(file,"test"); Scribe_Deep.Look(ref state,"state"); Scribe.saver.FinalizeSaving(); }
            finally { Scribe.ForceStop(); }
            state = null!;
            try { Scribe.loader.InitLoading(file); Scribe_Deep.Look(ref state,"state"); Scribe.loader.FinalizeLoading(); }
            finally { Scribe.ForceStop(); }
            Set(comp,"state",state);
        }
        var ability = TorannMagicDefOf.TM_DisablingShot;
        var owner = might.MightData.MightPowersS.Single(p => p.TMabilityDefs.Contains(ability));
        var oldType = ability.abilityClass;
        var points = might.MightData.MightAbilityPoints;
        try
        {
            ability.abilityClass = typeof(PawnAbility);
            Check.Equal(false,(bool)Call("Claim",pawn)!,"adept cannot claim at start");
            if (peaceful)
            {
                SniperResearchTrialTests.Verify(pawn);
                Call("Research",pawn,0);
                Call("Research",pawn,1455);
                Reload();
                Check.Equal(1455,(int)Call("ResearchTicks",pawn)!,"partial shift survives save");
                Call("Research",pawn,28544);
            }
            else
            {
                for (int i = 0; i < 7; i++) Call("Hit",pawn);
                Reload();
                Check.Equal(7,(int)Call("Hits",pawn)!,"seventh hit survives save");
            }
            Check.Equal(false,(bool)Call("Claim",pawn)!,"one tick or hit short");
            if (peaceful) Call("Research",pawn,int.MaxValue); else Call("Hit",pawn);
            Reload();
            Check.Equal(true,(bool)Call("Ready",pawn)!,"unclaimed reward survives save");
            Check.Equal(true,(bool)Call("Claim",pawn)!,"claim existing disabling family");
            Check.Equal(true,owner.learned,"native power learned");
            Check.Equal(false,(bool)Call("Claim",pawn)!,"cannot claim twice");
            Check.Equal(InitiationStage.Master,comp.State.Find("rwom.sniper")!.Stage,"master stage");
            Reload();
            might.AbilityData.Powers.RemoveAll(a=>a.Def==ability);
            typeof(SniperSurvey).GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[might]);
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==ability),"native power restored");
            Check.Equal(points,might.MightData.MightAbilityPoints,"claim costs no training points");
            SniperAscensionTests.Verify(fixture, pawn, might, comp, peaceful, Reload);
        }
        finally { ability.abilityClass = oldType; }
    }
}
