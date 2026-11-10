using System.Reflection;
using System.Runtime.CompilerServices;
using AbilityDef = AbilityUser.AbilityDef;
using AutocastPolicy = TorannMagic.AutoCast.AutocastPolicy;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// Ф4 step 6: autocast toggles dual-write. The knowledge store is the source of
// truth for the dispatch gate (step 3), so a player toggle must update the
// store entry — otherwise the gate keeps a stale value and the toggle appears
// dead on imported saves. The knowledge value always mirrors what the legacy
// throttled property actually accepted.
internal static class AutocastToggleTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("AutocastToggle.SyncsKnowledgeStore", SyncsKnowledgeStore);
        yield return new("AutocastToggle.CreatesMissingEntry", CreatesMissingEntry);
        yield return new("AutocastToggle.WithoutStoreStillToggles", WithoutStoreStillToggles);
        yield return new("AutocastToggle.MightMirror", MightMirror);
    }

    private static TMAbilityDef Ability(string name)
    {
        var def = (TMAbilityDef)RuntimeHelpers.GetUninitializedObject(typeof(TMAbilityDef));
        def.defName = name;
        def.label = name;
        def.MainVerb = new AbilityUser.VerbProperties_Ability();
        return def;
    }

    private static Pawn PawnWithKnowledge()
    {
        // The throttled AutoCast property reads Find.TickManager — same headless
        // game shell as AutocastTests.NewPawn.
        if (Current.Game == null)
        {
            Current.Game = (Game)RuntimeHelpers.GetUninitializedObject(typeof(Game));
            SetField(Current.Game, "tickManager", new TickManager());
            SetField(Current.Game, "uniqueIDsManager", new UniqueIDsManager());
        }
        var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawnDef.defName = "Regression_ToggleHuman";
        pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = pawnDef };
        SetField(pawn, "comps", new List<ThingComp>());
        pawn.AllComps.Add(new AbilityKnowledgeComp { parent = pawn });
        return pawn;
    }

    private static void SetField(object target, string field, object value)
    {
        // Private fields live on base types (ThingWithComps.comps); walk up.
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var found = type.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (found == null) continue;
            found.SetValue(target, value);
            return;
        }
        throw new InvalidOperationException("field not found: " + field);
    }

    // RED 1: toggling updates the imported knowledge entry, in both directions.
    private static void SyncsKnowledgeStore(RuntimeFixture _)
    {
        var pawn = PawnWithKnowledge();
        var comp = new CompAbilityUserMagic { parent = pawn };
        var def = Ability("TM_ToggleSyncA");
        var power = new MagicPower(new List<AbilityDef> { def }) { learned = true, autocast = true };
        SetField(power, "interactionTick", -10); // throttled property accepts writes at TicksGame 0
        var knowledge = pawn.TryGetComp<AbilityKnowledgeComp>();
        knowledge.GetOrCreate(SpellIds.Of(def), 0).autocast = true; // imported state

        AutocastPolicy.ToggleAutocast(comp, power);
        Check.Equal(false, power.autocast, "legacy field toggled off");
        Check.Equal(false, knowledge.Find(SpellIds.Of(def)).autocast, "knowledge entry mirrors the toggle");

        SetField(power, "interactionTick", -10); // throttle window passed
        AutocastPolicy.ToggleAutocast(comp, power);
        Check.Equal(true, knowledge.Find(SpellIds.Of(def)).autocast, "second toggle restores both");
    }

    // RED 2: toggling a power with no store entry creates one with the new value.
    private static void CreatesMissingEntry(RuntimeFixture _)
    {
        var pawn = PawnWithKnowledge();
        var comp = new CompAbilityUserMagic { parent = pawn };
        var def = Ability("TM_ToggleSyncB");
        var power = new MagicPower(new List<AbilityDef> { def }) { learned = true };
        SetField(power, "interactionTick", -10);
        var knowledge = pawn.TryGetComp<AbilityKnowledgeComp>();
        Check.Equal(true, knowledge.Find(SpellIds.Of(def)) == null, "entry absent before the toggle");

        AutocastPolicy.ToggleAutocast(comp, power);
        Check.Equal(true, knowledge.Find(SpellIds.Of(def)).autocast, "fresh entry created with the toggled value");
    }

    // RED 3: a pawn without the knowledge comp still toggles (classic path),
    // and a null power never throws.
    private static void WithoutStoreStillToggles(RuntimeFixture _)
    {
        var pawnDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        pawnDef.defName = "Regression_ToggleClassic";
        pawnDef.race = new RaceProperties { intelligence = Intelligence.Humanlike };
        var pawn = new Pawn { def = pawnDef };
        // 1.7 TryGetComp throws on an EMPTY comps list; classic pawns always
        // carry vanilla comps, so keep one dummy here.
        SetField(pawn, "comps", new List<ThingComp> { new DummyComp() });
        var comp = new CompAbilityUserMagic { parent = pawn };
        var def = Ability("TM_ToggleSyncC");
        var power = new MagicPower(new List<AbilityDef> { def }) { learned = true, autocast = true };
        SetField(power, "interactionTick", -10);

        AutocastPolicy.ToggleAutocast(comp, power);
        Check.Equal(false, power.autocast, "classic pawn toggles without a store");
        AutocastPolicy.ToggleAutocast(comp, null);
        Check.Equal(false, power.autocast, "null power is a no-op");
    }

    // RED 4: the might path mirrors the same contract.
    private static void MightMirror(RuntimeFixture _)
    {
        var pawn = PawnWithKnowledge();
        var comp = new CompAbilityUserMight { parent = pawn };
        var def = Ability("TM_ToggleSyncD");
        var power = new MightPower(new List<AbilityDef> { def }) { learned = true, autocast = true };
        SetField(power, "interactionTick", -10);
        var knowledge = pawn.TryGetComp<AbilityKnowledgeComp>();
        knowledge.GetOrCreate(SpellIds.Of(def), 0).autocast = true;

        AutocastPolicy.ToggleAutocast(comp, power);
        Check.Equal(false, power.autocast, "might legacy field toggled off");
        Check.Equal(false, knowledge.Find(SpellIds.Of(def)).autocast, "might knowledge entry mirrors the toggle");
    }


    private sealed class DummyComp : ThingComp { }
}
