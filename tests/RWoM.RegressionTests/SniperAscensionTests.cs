using System.Reflection;
using System.Xml.Linq;
using AbilityUser;
using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class SniperAscensionTests
{
    internal static void Definitions(RuntimeFixture fixture)
    {
        var defs = Path.Combine(fixture.Repository, "v1.7", "Defs");
        var original = XDocument.Load(Path.Combine(defs, "AbilityDefs", "Abilities_SniperSingleOpening.xml")).Root!;
        var shot = original.Elements().Single(e => (string?)e.Element("defName") == "TM_SniperSingleOpening");
        var bridge = XDocument.Load(Path.Combine(defs, "AbilityDefs", "Bridge", "Bridge_Abilities_SniperSingleOpening.xml")).Root!.Elements().Single();
        var verb = shot.Element("MainVerb")!;
        Check.Equal(typeof(SniperOpeningAbility).FullName, (string?)shot.Element("abilityClass"), "native ability dispatches its own job");
        Check.Equal(typeof(Verb_SniperSingleOpening).FullName, (string?)verb.Element("verbClass"), "native ballistic verb");
        Check.Equal((string?)verb.Element("verbClass"), (string?)bridge.Element("verbProperties")!.Element("verbClass"), "bridge targets same verb");
        Check.Equal(.60m,(decimal)shot.Element("staminaCost")!,"base stamina");
        Check.Equal(2000,(int)verb.Element("SecondsToRecharge")!,"two-day cooldown");
        var round = original.Elements().Single(e => (string?)e.Element("defName") == (string?)verb.Element("defaultProjectile"));
        Check.Equal(typeof(SniperOpeningBullet).FullName, (string?)round.Element("thingClass"), "ordinary ballistic launch with boosted projectile");
        var job = XDocument.Load(Path.Combine(defs, "JobDefs", "TM_SniperSingleOpening.xml")).Root!.Elements().Single();
        Check.Equal(typeof(JobDriver_SniperSingleOpening).FullName,(string?)job.Element("driverClass"),"registered saved job");
    }
    internal static void Verify(RuntimeFixture fixture, Pawn pawn, CompAbilityUserMight might, CompClassProgression comp, bool peaceful, Action reload)
    {
        var api = typeof(SniperSurvey).Assembly.GetType("TorannMagic.ClassProgression.SniperAscension",true)!;
        object? Call(string name, params object[] args) => api.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,args);
        var shadow = TorannMagicDefOf.TM_ShadowSlayer;
        var opening = DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_SniperSingleOpening") ?? fixture.AddAbility("TM_SniperSingleOpening");
        var oldShadow = shadow.abilityClass;
        var oldOpening = opening.abilityClass;
        int points = might.MightData.MightAbilityPoints;
        try
        {
            shadow.abilityClass = typeof(PawnAbility);
            opening.abilityClass = typeof(SniperOpeningAbility);
            might.MightData.MightUserLevel = 30;
            Check.Equal(false,(bool)Call("Choose",pawn,peaceful)!,"level alone cannot ascend");
            might.MightData.MightUserLevel = 29;
            if (peaceful) SniperEvacuationDrillTests.Verify(pawn);
            else SniperRescueTrialTests.Verify(pawn);
            for (int i=0;i<(peaceful?8:6);i++)
            {
                if (peaceful)
                {
                    Set(Find.TickManager,"ticksGameInt",200 + i*60000);
                    Check.Equal(true,(bool)Call("Drill",pawn)!,"eligible drill credited");
                    Check.Equal(false,(bool)Call("Drill",pawn)!,"no second drill today");
                }
                else
                {
                    var saved = PawnWithComps(false,false).pawn;
                    saved.thingIDNumber = 6300 + i;
                    Call("Rescue",pawn,saved);
                    Call("Rescue",pawn,saved);
                    Check.Equal(i+1,(int)Call("Rescues",pawn)!,"distinct rescued ally");
                }
                if(i==2) reload();
            }
            Check.Equal(false,(bool)Call("Ready",pawn)!,"level 29 waits with completed trial");
            reload();
            might.MightData.MightUserLevel=30;
            Check.Equal(true,(bool)Call("Choose",pawn,peaceful)!,"permanent graduation");
            Check.Equal(false,(bool)Call("Choose",pawn,!peaceful)!,"cannot switch ultimate");
            reload();
            var chosen=peaceful?opening:shadow;
            var other=peaceful?shadow:opening;
            might.AbilityData.Powers.RemoveAll(a=>a.Def==chosen);
            typeof(SniperSurvey).GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[might]);
            Check.Equal(InitiationStage.Ascended,comp.State.Find("rwom.sniper")!.Stage,"ascension survives save");
            Check.Equal(1,might.AbilityData.Powers.Count(a=>a.Def==chosen),"chosen ultimate restored");
            Check.Equal(0,might.AbilityData.Powers.Count(a=>a.Def==other),"other ultimate not awarded");
            Check.Equal(points,might.MightData.MightAbilityPoints,"no unexpected point expenditure");
            if (peaceful) SniperOpeningTests.Verify(fixture, pawn, might, opening);
        }
        finally {shadow.abilityClass=oldShadow;opening.abilityClass=oldOpening;}
    }
}
