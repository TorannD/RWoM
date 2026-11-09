using System.Reflection;
using AbilityUser;
using RimWorld;
using TorannMagic;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

internal static class ChaosProgressionTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("ChaosProgression.PacifistCanRerollWhileUndrafted", f =>
        {
            var root = Path.Combine(f.Repository, "v1.7", "Defs", "AbilityDefs");
            var bridge = System.Xml.Linq.XDocument.Load(Path.Combine(root, "Bridge", "Bridge_Abilities_TMagic_ChaosMage.xml"))
                .Root!.Elements("AbilityDef").Single(e => (string?)e.Element("spellId") == "rwom.tm_chaostradition");
            Check.Equal(false, (bool?)bridge.Element("disableGizmoWhileUndrafted") ?? true, "idle pawn can reroll");
            Check.Equal(false, (bool?)bridge.Element("casterMustBeCapableOfViolence") ?? true, "pacifist can reroll");
            var original = System.Xml.Linq.XDocument.Load(Path.Combine(root, "Abilities_TMagic_ChaosMage.xml"))
                .Root!.Elements().Single(e => (string?)e.Element("defName") == "TM_ChaosTradition");
            Check.Equal(false, (bool?)original.Element("MainVerb")?.Element("isViolent") ?? true, "live cast owner permits a pacifist");
        });
        yield return new("ChaosProgression.RealTraditionCastRefundsWithoutSubtractingPermanentInvestment", f =>
        {
            var comp = Prepare(f); var data = comp.MagicData;
            var spell = data.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt));
            spell.learned = true; spell.level = 2;
            data.MagicPowerSkill_ChaosTradition[0].level = 2;
            data.MagicPowerSkill_global_spirit[0].level = 4;
            data.MagicAbilityPoints = 7;
            var previous = Current.Game;
            Current.Game = (Game)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Game));
            Set(Current.Game, "tickManager", new TickManager());
            Rand.PushState(473);
            try
            {
                var verb = new Verb_ChaosTradition { caster = comp.Pawn };
                var success = (bool)typeof(Verb_ChaosTradition).GetMethod("TryCastShot", Flags)!.Invoke(verb, null)!;
                Check.Equal(true, success, "real reroll reports successful cast");
                Check.Equal(7 + 2 * spell.costToLevel, data.MagicAbilityPoints, "verb preserves unspent points and refunds only borrowed investment");
                Check.Equal(2, data.MagicPowerSkill_ChaosTradition[0].level, "tradition rank preserved through cast");
                Check.Equal(4, data.MagicPowerSkill_global_spirit[0].level, "global rank preserved through cast");
            }
            finally { Rand.PopState(); Current.Game = previous; }
        });
        yield return new("ChaosProgression.SavedBorrowedInvestmentSurvivesAndRefundsOnce", f =>
        {
            var comp = Prepare(f); var data = comp.MagicData;
            var spell = data.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt));
            spell.learned = true;
            comp.LevelUpPower(spell); comp.LevelUpPower(spell);
            data.MagicPowerSkill_ShadowBolt[0].level = 3;
            data.MagicAbilityPoints = 11;
            // This fixture serializes the progression payload, not the owning world.
            Set(data, "magicPawn", null!);
            var loaded = f.Load(f.Save(data)); Set(comp, "magicData", loaded);
            var owner = loaded.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt));
            Check.Equal(true, MagicRows(Build(comp.Pawn)).Contains(owner), "serialized borrowed owner visible without transient chaos preview list");
            Check.Equal(2, owner.level, "saved rank");
            Check.Equal(3, loaded.MagicPowerSkill_ShadowBolt[0].level, "saved skill");
            TM_ChaosProgression.RestorePower(comp, owner);
            var runtime = comp.AbilityData.Powers.Where(a => owner.TMabilityDefs.Contains(a.Def)).ToList();
            Check.Equal(1, runtime.Count, "loading restores exactly one runtime rank");
            Check.Same(owner.abilityDef, runtime[0].Def, "runtime uses the saved upgraded rank");
            int expected = 11 + 2 * owner.costToLevel + 3 * loaded.MagicPowerSkill_ShadowBolt[0].costToLevel;
            TM_ChaosProgression.ReleasePowers(comp);
            Check.Equal(expected, loaded.MagicAbilityPoints, "saved investment refunded at actual costs");
            Check.Equal(false, comp.AbilityData.Powers.Any(a => owner.TMabilityDefs.Contains(a.Def)), "all runtime ranks removed");
            TM_ChaosProgression.ReleasePowers(comp);
            Check.Equal(expected, loaded.MagicAbilityPoints, "repeated release cannot mint points");
        });
        yield return new("ChaosProgression.ReleaseRemovesChildrenAndPreservesStandalone", f =>
        {
            var comp = Prepare(f); var data = comp.MagicData;
            var spell = data.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_SoulBond));
            spell.learned = true;
            comp.spell_ShadowCall = true; comp.spell_ShadowStep = true;
            comp.AddPawnAbility(TorannMagicDefOf.TM_ShadowCall);
            comp.AddPawnAbility(TorannMagicDefOf.TM_ShadowStep);
            comp.AddPawnAbility(TorannMagicDefOf.TM_ChaosTradition);
            TM_ChaosProgression.ReleasePowers(comp);
            Check.Equal(false, comp.AbilityData.Powers.Any(a => a.Def == TorannMagicDefOf.TM_ShadowCall || a.Def == TorannMagicDefOf.TM_ShadowStep), "borrowed dependent actions removed");
            Check.Equal(false, comp.spell_ShadowCall || comp.spell_ShadowStep, "dependent flags cannot restore outgoing actions");
            Check.Equal(true, comp.AbilityData.Powers.Any(a => a.Def == TorannMagicDefOf.TM_ChaosTradition), "permanent ability retained");
        });
        yield return new("ChaosProgression.ActiveBorrowedOwnersAppearAndInactiveDoNot", f =>
        {
            var comp = Prepare(f);
            var borrowed = comp.MagicData.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt));
            borrowed.learned = true; borrowed.level = 2;
            var rows = MagicRows(Build(comp.Pawn));
            Check.Equal(true, rows.Contains(borrowed), "active borrowed rank uses its actual saved WD owner");
            Check.Equal(2, rows.Count, "Chaos Tradition plus current borrowed spell only");
            Check.Equal(false, rows.Contains(comp.MagicData.MagicPowersSD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt))), "no unlearned shared SD shadow row");
        });
        yield return new("ChaosProgression.OpenGrimoireRefreshesAfterReroll", f =>
        {
            var comp = Prepare(f);
            var old = comp.MagicData.MagicPowersIF.First(); old.learned = true;
            var window = Build(comp.Pawn);
            old.learned = false;
            var next = comp.MagicData.MagicPowersHoF.First(); next.learned = true;
            typeof(Window_TMGrimoire).GetMethod("EnsureFilters", Flags)!.Invoke(window, null);
            Check.Equal(false, MagicRows(window).Contains(old), "removed borrowed spell leaves an already open grimoire");
            Check.Equal(true, MagicRows(window).Contains(next), "new borrowed spell enters an already open grimoire");
        });
        yield return new("ChaosProgression.RerollRefundsExactInvestmentAndPreservesPermanentSkills", f =>
        {
            var comp = Prepare(f); var data = comp.MagicData;
            var borrowed = data.MagicPowersWD.Single(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_ShadowBolt)); borrowed.learned = true; borrowed.level = 2; borrowed.costToLevel = 3;
            var skill = data.GetSkill_Power((TMAbilityDef)borrowed.TMabilityDefs[0]); skill.level = 3; skill.costToLevel = 2;
            data.MagicPowerSkill_ChaosTradition[0].level = 2;
            data.MagicPowerSkill_global_spirit[0].level = 4;
            data.MagicAbilityPoints = 7; data.MagicUserLevel = 40; data.MagicUserXP = 123;
            Rand.PushState(471);
            try { TM_Calc.AssignChaosMagicPowers(comp); }
            finally { Rand.PopState(); }
            Check.Equal(19, data.MagicAbilityPoints, "7 unspent + 2 ranks at 3 + 3 skill levels at 2; no refund for free initial grant");
            Check.Equal(0, borrowed.level, "old spell ranks reset even if drawn again");
            Check.Equal(0, skill.level, "old spell skill reset");
            Check.Equal(2, data.MagicPowerSkill_ChaosTradition[0].level, "Chaos Tradition investment remains");
            Check.Equal(4, data.MagicPowerSkill_global_spirit[0].level, "global investment remains");
            Check.Equal(40, data.MagicUserLevel, "level unchanged"); Check.Equal(123, data.MagicUserXP, "XP unchanged");
            Rand.PushState(472);
            try { TM_Calc.AssignChaosMagicPowers(comp); }
            finally { Rand.PopState(); }
            Check.Equal(19, data.MagicAbilityPoints, "second reroll without investment cannot refund twice");
        });
    }

    static CompAbilityUserMagic Prepare(RuntimeFixture f)
    {
        LoadBaseDefinitions(f);
        foreach (var ability in DefDatabase<TMAbilityDef>.AllDefsListForReading)
        {
            ability.abilityClass = typeof(PawnAbility); ability.shouldInitialize = true;
        }
        var p = PawnWithComps(true, false);
        p.pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.ChaosMage));
        p.magic.magicPowersInitialized = true;
        p.magic.MagicData.MagicPowersCM[0].learned = true;
        return p.magic;
    }
}
