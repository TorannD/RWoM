using System.Reflection;
using System.Runtime.CompilerServices;
using RimWorld;
using TorannMagic;
using Verse;

namespace RWoM.RegressionTests;

// The SkillRecord.Learn prefix guards magic XP handling; robots and androids
// (Vehicle Framework engineers, VRE machines) have no pawn.story at all, and a
// story-less pawn crashed every maintenance XP tick (workshop report #50).
internal static class SkillRecordPatchTests
{
    public static IEnumerable<TestCase> Cases()
    {
        yield return new("SkillRecordPatch.StorylessPawnSkipsMagic", Storyless);
        yield return new("SkillRecordPatch.NonHumanlikeSkipsMagic", NonHumanlike);
        yield return new("SkillRecordPatch.UndeadHumanBlocksLearning", Undead);
        yield return new("SkillRecordPatch.OrdinaryHumanPassesThrough", Ordinary);
    }

    static MethodInfo Prefix() => typeof(TorannMagic.TorannMagicMod)
        .GetNestedType("SkillRecord_Patch", BindingFlags.NonPublic | BindingFlags.Public)!
        .GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static)!;

    static Pawn Pawn(bool humanlike, bool withStory, bool withTraits, Trait? trait = null)
    {
        var def = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        def.defName = "SkillRecordPatchPawn"; def.category = ThingCategory.Pawn;
        def.race = new RaceProperties { intelligence = humanlike ? Intelligence.Humanlike : Intelligence.ToolUser };
        var pawn = new Pawn { def = def };
        pawn.health = new Pawn_HealthTracker(pawn);
        if (withStory)
        {
            pawn.story = new Pawn_StoryTracker(pawn) { traits = withTraits ? new TraitSet(pawn) : null! };
            if (trait != null) pawn.story.traits.allTraits.Add(trait);
        }
        return pawn;
    }

    static bool Invoke(Pawn pawn) => (bool)Prefix().Invoke(null, new object?[] { null, pawn })!;

    static void Storyless(RuntimeFixture f)
    {
        Check.Equal(true, Invoke(Pawn(humanlike: true, withStory: false, withTraits: false)),
            "a story-less pawn (robot frame) lets vanilla learning run instead of crashing");
        Check.Equal(true, Invoke(Pawn(humanlike: true, withStory: true, withTraits: false)),
            "a story with no trait set also skips magic handling");
    }

    static void NonHumanlike(RuntimeFixture f)
    {
        Check.Equal(true, Invoke(Pawn(humanlike: false, withStory: true, withTraits: true)),
            "non-humanlike races skip magic XP handling");
    }

    static void Undead(RuntimeFixture f)
    {
        var old = TorannMagicDefOf.Undead;
        var undead = new TraitDef { defName = "SkillRecordPatchUndead" };
        TorannMagicDefOf.Undead = undead;
        try
        {
            Check.Equal(false, Invoke(Pawn(humanlike: true, withStory: true, withTraits: true, new Trait(undead))),
                "undead humans keep vanilla learning blocked");
        }
        finally { TorannMagicDefOf.Undead = old; }
    }

    static void Ordinary(RuntimeFixture f)
    {
        var old = TorannMagicDefOf.Undead;
        var undead = new TraitDef { defName = "SkillRecordPatchUndeadOrdinary" };
        TorannMagicDefOf.Undead = undead;
        try
        {
            Check.Equal(true, Invoke(Pawn(humanlike: true, withStory: true, withTraits: true)),
                "ordinary humans keep vanilla learning running");
        }
        finally { TorannMagicDefOf.Undead = old; }
    }
}
