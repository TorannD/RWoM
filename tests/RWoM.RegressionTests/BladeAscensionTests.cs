using System.Reflection;
using System.Xml.Linq;
using AbilityUser;
using RimWorld;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class BladeAscensionTests
{
    internal static void Verify(RuntimeFixture f, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool peaceful, Action reload)
    {
        var api = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.BladeAscension",true)!;
        object? Call(string name, params object[] arguments) =>
            api.GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,arguments);
        var form = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladeSevenSteps") ?? f.AddAbility("TM_BladeSevenSteps");
        var phase = TorannMagicDefOf.TM_PhaseStrike;
        var oldForm = form.abilityClass;
        var oldPhase = phase.abilityClass;
        var points = might.MightData.MightAbilityPoints;
        var position = pawn.Position;
        try
        {
            phase.abilityClass = typeof(PawnAbility);
            var formDef = XDocument.Load(Path.Combine(f.Repository,"v1.7","Defs","AbilityDefs","Abilities_BladeSevenSteps.xml")).Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_BladeSevenSteps");
            form.abilityClass = api.Assembly.GetType((string)formDef.Element("abilityClass")!,true)!;
            might.MightData.MightUserLevel = 30;
            Check.Equal(false,(bool)Call("Choose",pawn,peaceful)!,"level without trial");
            might.MightData.MightUserLevel = 29;
            if (!peaceful) BladePassageTests.Verify(f,pawn,might);
            if (peaceful) BladeMedicalSupplyTests.Verify(f,pawn);
            var first = PawnWithComps(false,false).pawn;
            var second = PawnWithComps(false,false).pawn;
            first.thingIDNumber=5391;
            second.thingIDNumber=5392;
            var origin = new IntVec3(5,0,5);
            Set(pawn,"positionInt",new IntVec3(8,0,5));
            for (int i=0; i<(peaceful?10:6); ++i)
            {
                int now=1000+60000*i;
                Set(Find.TickManager,"ticksGameInt",now);
                if (peaceful)
                {
                    Check.Equal(true,(bool)Call("Supply",pawn)!,"medical delivery");
                    if(i==4) reload();
                    Check.Equal(false,(bool)Call("Supply",pawn)!,"same day not counted twice");
                }
                else
                {
                    Call("StartRoute",pawn,first,origin);
                    Call("FinishRoute",pawn,first);
                    Check.Equal(i,(int)Call("Routes",pawn)!,"different enemy required");
                    Set(Find.TickManager,"ticksGameInt",now+600);
                    Call("FinishRoute",pawn,second);
                    Check.Equal(i,(int)Call("Routes",pawn)!,"expired route rejected");
                    Call("StartRoute",pawn,first,origin);
                    if(i==4) reload();
                    Call("FinishRoute",pawn,second);
                    Call("FinishRoute",pawn,second);
                    Check.Equal(i+1,(int)Call("Routes",pawn)!,"route counts once");
                }
            }
            reload();
            Check.Equal(false,(bool)Call("Choose",pawn,peaceful)!,"level 29 retains completed trial");
            might.MightData.MightUserLevel=30;
            Check.Equal(true,(bool)Call("Choose",pawn,peaceful)!,"final choice");
            Check.Equal(false,(bool)Call("Choose",pawn,!peaceful)!,"cannot switch");
            reload();
            var chosen = peaceful?form:phase;
            var other = peaceful?phase:form;
            might.AbilityData.Powers.RemoveAll(a=>a.Def==chosen);
            var career=api.Assembly.GetType("TorannMagic.ClassProgression.BladeCareer")!;
            career.GetMethod("Restore",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,[might]);
            Check.Equal(InitiationStage.Ascended,comp.State.Find("rwom.bladedancer")!.Stage,"grade saved");
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==chosen),"one restored ultimate");
            Check.Equal(0,might.AbilityData.Powers.Count(a=>a.Def==other),"unselected ultimate remains locked");
            Check.Equal(points,might.MightData.MightAbilityPoints,"no second price");
            if (peaceful) BladeFormTests.Verify(f,pawn,might);
        }
        finally { phase.abilityClass=oldPhase; form.abilityClass=oldForm; Set(pawn,"positionInt",position); }
    }
}
