using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using AbilityDef = AbilityUser.AbilityDef;
using TorannMagic;
using TorannMagic.AutoCast;
using TorannMagic.TMDefs;
using Verse;

namespace RWoM.RegressionTests;

// Ф1 sanitizer (card #55): stale spell defs from older mod versions must be
// purged at load — null entries in TMabilityDefs, out-of-range levels, dead
// runtime PawnAbility wrappers — instead of NRE-ing in the healing autocast
// dispatch. Learned flags and points are never touched.
internal static class SpellSanitizerTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SpellSanitizer.PurgesNullsAndClamps", PurgesNullsAndClamps);
        yield return new("SpellSanitizer.EmptyListMarksUnlearned", EmptyListMarksUnlearned);
        yield return new("SpellSanitizer.MightPurgesNullsAndClamps", MightPurgesNullsAndClamps);
        yield return new("SpellSanitizer.DeadRuntimeWrapperDropped", DeadRuntimeWrapperDropped);
        yield return new("SpellSanitizer.MissingSkillLabelDefaults", MissingSkillLabelDefaults);
    }


    private static int id;

    private static TMAbilityDef Ability(string name)
    {
        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = name;
        def.label = name;
        def.manaCost = 10f;
        def.MainVerb = new AbilityUser.VerbProperties_Ability { verbClass = typeof(Verb_Meditate) };
        def.abilityClass = typeof(PawnAbility);
        return def;
    }

    private static MagicPower Power(params TMAbilityDef[] defs)
    {
        var power = new MagicPower(new List<AbilityDef>(defs)) { learned = true, level = defs.Length - 1 };
        return power;
    }

    private static List<AbilityDef> DefsAfter(MagicPower power) => power.TMabilityDefs;

    // RED 1: a def removed by an update leaves a null entry in the saved list;
    // the sanitizer must purge it and clamp the level into range.
    private static void PurgesNullsAndClamps(RuntimeFixture _)
    {
        var third = Ability("SanIII"); var fourth = Ability("SanIV");
        var magic = Power(Ability("SanI"), null, third, fourth);
        magic.level = 2;
        var might = new MightPower(new List<AbilityDef>(magic.TMabilityDefs)) { learned = true, level = 2 };
        SpellSanitizer.Sanitize(magic); SpellSanitizer.Sanitize(might);
        Check.Same(third, magic.abilityDef, "missing II must not promote magic III to IV");
        Check.Same(third, might.abilityDef, "missing II must not promote might III to IV");
        var a = Ability("SpellSanA"); var b = Ability("SpellSanB");
        var power = Power(a, b);
        power.level = 1;
        power.TMabilityDefs[1] = null; // SpellSanB vanished from the game
        SpellSanitizer.Sanitize(power);
        Check.Equal(1, DefsAfter(power).Count, "null entry purged");
        Check.Same(a, DefsAfter(power)[0], "surviving def retained");
        Check.Equal(0, power.level, "level clamped into range");
        Check.Equal(true, power.learned, "learned flag preserved for surviving defs");
    }

    // RED 2: every variant gone — the power is unusable and must stop being
    // learned/autocast (empty abilityDef getter can NRE the grimoire).
    private static void EmptyListMarksUnlearned(RuntimeFixture _)
    {
        var a = Ability("SpellSanC");
        var power = Power(a);
        power.TMabilityDefs.Clear();
        SpellSanitizer.Sanitize(power);
        Check.Equal(false, power.learned, "power with no surviving variants is unlearned");
        Check.Equal(false, power.autocast, "autocast off for an unusable power");
        Check.Equal(0, power.level, "level reset with the list gone");
    }

    // RED 3: might powers share the exact same shape.
    private static void MightPurgesNullsAndClamps(RuntimeFixture _)
    {
        var a = Ability("SpellSanD"); var b = Ability("SpellSanE");
        var power = new MightPower(new List<AbilityDef>(new[] { a, b })) { learned = true, level = 1 };
        power.TMabilityDefs[1] = null;
        SpellSanitizer.Sanitize(power);
        Check.Equal(1, power.TMabilityDefs.Count, "might null entry purged");
        Check.Same(a, power.TMabilityDefs[0], "might surviving def retained");
        Check.Equal(0, power.level, "might level clamped");
    }

    // RED 4: a runtime wrapper whose def died must be dropped from the powers
    // list instead of NRE-ing the dispatcher on .Def derefs.
    private static void DeadRuntimeWrapperDropped(RuntimeFixture _)
    {
        var a = Ability("SpellSanF");
        var live = new PawnAbility { Def = a };
        var dead = new PawnAbility(); // def lost to an update
        var powers = new List<PawnAbility> { live, dead };
        SpellSanitizer.Sanitize(powers);
        Check.Equal(1, powers.Count, "dead runtime wrapper dropped");
        Check.Same(live, powers[0], "live wrapper retained");
    }

    // RED 5: the missing-skill-label case (Purify ver) resolves to a default
    // level instead of NRE-ing the affliction loop.
    private static void MissingSkillLabelDefaults(RuntimeFixture _)
    {
        var list = new List<MagicPowerSkill> { new("TM_Purify_pwr", "x") };
        Check.Equal(0, SpellSanitizer.SkillLevelOrDefault(list, "TM_Purify_ver"), "missing skill label resolves to default level");
        Check.Equal(3, SpellSanitizer.SkillLevelOrDefault(new List<MagicPowerSkill> { new("TM_Purify_ver", "x") { level = 3 } }, "TM_Purify_ver"), "present skill label resolves its level");
    }

    // RED 6: the healing evaluators must skip a lost runtime wrapper instead of
    // NRE-ing on CooldownTicksLeft (the exact "red bugs autocasting healing").
    private static void HealSpellSkipsLostRuntime(RuntimeFixture _)
    {
        var comp = Blank<CompAbilityUserMagic>();
        var mana = Blank<Need_Mana>();
        typeof(CompAbilityUserMagic).GetField("manaInt", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(comp, mana);
        typeof(Need).GetField("curLevelInt", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(mana, 1f);
        var def = Ability("SpellSanG");
        var power = Power(def);
        TorannMagic.AutoCast.HealSpell.Evaluate(comp, def, null, power, out var success);
        Check.Equal(false, success, "a lost runtime wrapper is skipped without throwing");
    }

    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
}
