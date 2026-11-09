using TorannMagic;
using TorannMagic.ClassProgression;
using Verse;

namespace RWoM.RegressionTests;

internal static class ClassSpellRestoreTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ClassSpellRestore.StandaloneAndFlagOwnership", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            foreach (var ability in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { ability.abilityClass = typeof(AbilityUser.PawnAbility); ability.shouldInitialize = true; }
            var p = GrimoireProgressionTests.PawnWithComps(true, false);
            var progression = new CompClassProgression { parent = p.pawn };
            p.pawn.AllComps.Add(progression);
            progression.BeginNewProgression(Vocation.Mage);
            progression.State.TryAddFoundation("rwom.priest", DisciplineDomain.Magic);
            var heater = p.magic.MagicData.ReturnMatchingMagicPower(TorannMagicDefOf.TM_Heater);
            heater.learned = true;
            p.magic.spell_Extinguish = true;
            p.magic.spell_Teach = true;
            p.magic.MagicData.MagicAbilityPoints = 7;
            p.magic.InitializeSpell();
            var runtime = p.magic.AbilityData.Powers.Single(a => a.Def == TorannMagicDefOf.TM_Heater);
            runtime.CooldownTicksLeft = 123;
            p.magic.InitializeSpell();
            p.magic.ResolveFactionChange();
            Check.Same(runtime, p.magic.AbilityData.Powers.Single(a => a.Def == TorannMagicDefOf.TM_Heater), "standalone runtime retained");
            Check.Equal(123, runtime.CooldownTicksLeft, "standalone cooldown retained");
            Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Extinguish), "flag-only shared spell restored once");
            Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_TeachMagic), "nonmatching flag name uses explicit mapping");
            Check.Equal(false, p.magic.AbilityData.Powers.Any(a => a.Def == TorannMagicDefOf.TM_Resurrection), "closed priest ultimate is not fabricated");
            Check.Equal(7, p.magic.MagicData.MagicAbilityPoints, "restoration does not spend or grant points");
        });
        yield return new("ClassSpellRestore.PreservedRankAndClassicNoOp", f =>
        {
            GrimoireProgressionTests.LoadBaseDefinitions(f);
            foreach (var ability in DefDatabase<TMAbilityDef>.AllDefsListForReading)
            { ability.abilityClass = typeof(AbilityUser.PawnAbility); ability.shouldInitialize = true; }
            var p = GrimoireProgressionTests.PawnWithComps(true, false);
            p.magic.spell_Blink = true;
            Check.Equal(false, ClassSpellAccess.RestoreLearned(p.magic), "classic path remains owned by existing initializer");
            Check.Equal(0, p.magic.AbilityData.Powers.Count, "classic no-op grants nothing");
            var progression = new CompClassProgression { parent = p.pawn };
            p.pawn.AllComps.Add(progression);
            progression.BeginNewProgression(Vocation.Mage);
            var blink = p.magic.MagicData.MagicPowersA.Single(x => x.TMabilityDefs.Contains(TorannMagicDefOf.TM_Blink));
            blink.learned = true;
            blink.level = 2;
            ((TMAbilityDef)blink.abilityDef).shouldInitialize = false;
            p.magic.spell_Resurrection = true;
            p.magic.InitializeSpell();
            Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == blink.abilityDef), "saved rank restored once");
            Check.Equal(false, p.magic.AbilityData.Powers.Any(a => a.Def == TorannMagicDefOf.TM_Blink), "flag does not duplicate lower rank");
            Check.Equal(1, p.magic.AbilityData.Powers.Count(a => a.Def == TorannMagicDefOf.TM_Resurrection), "explicit prior ultimate ownership preserved");
            Check.Equal(2, blink.level, "rank not rewritten");
        });
    }
}
