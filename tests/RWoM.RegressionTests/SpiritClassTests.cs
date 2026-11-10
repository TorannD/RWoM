using System.Reflection;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

internal static class SpiritClassTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpiritClass.PossessorUsesOwnSkillTree", OwnSkills);
    }

    private static void OwnSkills(RuntimeFixture fixture)
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var race = (ThingDef)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        race.defName = "RegressionSpirit";
        race.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = race };
        pawn.health = new Pawn_HealthTracker(pawn);
        var comp = new CompAbilityUserMagic { parent = pawn };
        typeof(ThingWithComps).GetField("comps", fields)!.SetValue(pawn, new List<ThingComp> { comp });
        TM_Calc.InvalidateCompCache(pawn);
        var data = new MagicData();
        typeof(CompAbilityUserMagic).GetField("magicData", fields)!.SetValue(comp, data);
        var spirit = fixture.Ability("TM_SpiritPossession");
        data.GetSkill_Power(spirit).level = 2;
        data.GetSkill_Versatility(spirit).level = 3;
        data.GetSkill_Efficiency(spirit).level = 1;
        var previousSpirit = TorannMagicDefOf.TM_SpiritPossession;
        var previousPossess = TorannMagicDefOf.TM_Possess;
        var previousSettings = TorannMagic.ModOptions.Settings.Instance;
        TorannMagicDefOf.TM_SpiritPossession = spirit;
        TorannMagicDefOf.TM_Possess = fixture.AddAbility("TM_Possess");
        TorannMagic.ModOptions.Settings.Instance = new TorannMagic.ModOptions.Settings { AIHardMode = false };
        try
        {
            var hediff = new Hediff_Possessor { pawn = pawn, def = new HediffDef { defName = "TM_SpiritPossessorHD", maxSeverity = 10f } };
            hediff.UpdateSkills();
            Check.Equal(45f, hediff.MaxLevelBonus, "Spirit versatility increases its energy capacity");
            Check.Equal(2.5f, hediff.Severity, "Spirit power increases its own hediff severity");
            Check.Equal(1, (int)typeof(Hediff_Possessor).GetField("effVal", fields)!.GetValue(hediff)!, "Spirit efficiency comes from its own tree");
        }
        finally
        {
            TorannMagicDefOf.TM_SpiritPossession = previousSpirit;
            TorannMagicDefOf.TM_Possess = previousPossess;
            TorannMagic.ModOptions.Settings.Instance = previousSettings;
        }
    }
}
