using System.Runtime.CompilerServices;
using AbilityDef = AbilityUser.AbilityDef;
using AutocastPolicy = TorannMagic.AutoCast.AutocastPolicy;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// Ф4 step 5: the AI cast dispatch gates through the knowledge store. Only the
// learned flag applies to AI (the player autocast toggle does not), and the
// AI source no longer prefilters by the legacy learned flag, so a store entry
// overrides it in both directions. No entry — legacy decides.
internal static class AICastKnowledgeTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("AICastKnowledge.GateOverridesLegacyLearned", GateOverridesLegacyLearned);
        yield return new("AICastKnowledge.GateWithoutEntryFallsBackLegacy", GateWithoutEntryFallsBackLegacy);
        yield return new("AICastKnowledge.GateNullSafe", GateNullSafe);
        yield return new("AICastKnowledge.SourceGateExcludesAndIncludes", SourceGateExcludesAndIncludes);
    }

    private static TMAbilityDef Ability(string name)
    {
        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = name;
        def.label = name;
        def.MainVerb = new AbilityUser.VerbProperties_Ability();
        return def;
    }

    private static MagicPower Power(TMAbilityDef def, bool learned)
        => new MagicPower(new List<AbilityDef> { def }) { learned = learned };

    // RED 1: a knowledge entry overrides the legacy learned flag both ways.
    private static void GateOverridesLegacyLearned(RuntimeFixture _)
    {
        var def = Ability("TM_AIKnowledgeA");
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        var off = knowledge.GetOrCreate(SpellIds.Of(def), 0);
        off.learned = false;
        Check.Equal(false, AutocastPolicy.EnabledForAICast(Power(def, true), knowledge),
            "knowledge learned=false suppresses AI casting");

        var defB = Ability("TM_AIKnowledgeB");
        var on = knowledge.GetOrCreate(SpellIds.Of(defB), 0);
        on.learned = true;
        Check.Equal(true, AutocastPolicy.EnabledForAICast(Power(defB, false), knowledge),
            "knowledge learned=true enables AI casting for a legacy-unlearned power");
    }

    // RED 2: no store or no entry — the legacy learned flag decides.
    private static void GateWithoutEntryFallsBackLegacy(RuntimeFixture _)
    {
        var defOn = Ability("TM_AIKnowledgeC");
        var defOff = Ability("TM_AIKnowledgeD");
        Check.Equal(true, AutocastPolicy.EnabledForAICast(Power(defOn, true), null), "null store falls back to legacy");
        Check.Equal(false, AutocastPolicy.EnabledForAICast(Power(defOff, false), null), "null store keeps legacy off");

        var emptyStore = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        Check.Equal(true, AutocastPolicy.EnabledForAICast(Power(defOn, true), emptyStore), "empty store falls back to legacy");
        Check.Equal(false, AutocastPolicy.EnabledForAICast(Power(defOff, false), emptyStore), "empty store keeps legacy off");
    }

    // RED 3: missing power/def never throws.
    private static void GateNullSafe(RuntimeFixture _)
    {
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        Check.Equal(false, AutocastPolicy.EnabledForAICast((MagicPower)null, knowledge), "null power is gated off");
        Check.Equal(false, AutocastPolicy.EnabledForAICast(
            new MagicPower { learned = true, TMabilityDefs = new List<AbilityDef>() }, knowledge),
            "power with an empty def list is gated off");
    }

    // RED 4: the AI source gate filters the raw power list (the widened source
    // without the legacy learned prefilter).
    private static void SourceGateExcludesAndIncludes(RuntimeFixture _)
    {
        var onDef = Ability("TM_AIKnowledgeE");
        var offDef = Ability("TM_AIKnowledgeF");
        var onPower = Power(onDef, false); // legacy-unlearned, store says learned
        var offPower = Power(offDef, true); // legacy-learned, store says forgotten
        var powers = new List<MagicPower> { onPower, offPower };
        var knowledge = new AbilityKnowledgeComp { parent = new ThingWithComps() };
        knowledge.GetOrCreate(SpellIds.Of(onDef), 0).learned = true;
        knowledge.GetOrCreate(SpellIds.Of(offDef), 0).learned = false;

        var allowed = AutocastPolicy.AIPowers(powers, knowledge).ToList();
        Check.Equal(1, allowed.Count, "knowledge-forgotten power excluded from AI dispatch");
        Check.Same(onPower, allowed[0], "knowledge-learned power retained for AI dispatch");
    }
}
