using System.Reflection;
using RimWorld;
using TorannMagic;
using TorannMagic.TMDefs;
using TorannMagic.ClassProgression;
using Verse;
using static RWoM.RegressionTests.GrimoireProgressionTests;

namespace RWoM.RegressionTests;

// Live report (card 118): a Brightmage reading the Heal scroll kept the
// knowledge entry (rwom.tm_heal learned=True) across saves, but the ability
// vanished from the bar on reload — the custom-class restore gate
// (TM_ClassState.IsMagicPowerAuthorized) only accepted standalone owners and
// the pawn's own class lists, so a scroll spell legitimately learned from
// ANOTHER class's power list (Paladin's Heal, Arcanist's Blink, Heart of
// Frost's Rainmaker) was not re-granted while "already knows" blocked
// re-learning. The fix authorizes a power when its knowledge entry is
// learned: legitimate ownership comes from the knowledge store, unlearned
// off-class powers stay unauthorized.
internal static class KnowledgeRestoreTests
{
    private static readonly BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<TestCase> Cases()
    {
        yield return new("KnowledgeRestore.LearnedEntryAuthorizesOffClassPower", LearnedEntryAuthorizesOffClassPower);
        yield return new("KnowledgeRestore.UnlearnedEntryStaysUnauthorized", UnlearnedEntryStaysUnauthorized);
        yield return new("KnowledgeRestore.NoKnowledgeCompStaysUnauthorized", NoKnowledgeCompStaysUnauthorized);
    }

    private static (Pawn pawn, CompAbilityUserMagic magic, MagicPower healPower) CustomMage(RuntimeFixture f)
    {
        GrimoireProgressionTests.LoadBaseDefinitions(f);
        var (pawn, magic, _) = PawnWithComps(true, false);
        pawn.story.traits.allTraits.Add(new Trait(TorannMagicDefOf.TM_Brightmage));
        var customClass = new TM_CustomClass
        {
            classTrait = TorannMagicDefOf.TM_Brightmage,
            classMageAbilities = new List<TMAbilityDef>(),
            learnableSpells = new List<Verse.ThingDef>()
        };
        Set(magic, "customClass", customClass);
        return (pawn, magic, MagicDataHealOwner(magic));
    }

    private static MagicPower MagicDataHealOwner(CompAbilityUserMagic magic)
    {
        // Heal's canonical owner lives in the Paladin family list.
        var power = magic.MagicData.MagicPowersP.FirstOrDefault(p => p.abilityDef == TorannMagicDefOf.TM_Heal);
        if (power == null) throw new System.InvalidOperationException("Heal power owner missing from MagicData");
        return power;
    }

    private static AbilityKnowledgeComp AttachKnowledge(Pawn pawn)
    {
        var comp = new AbilityKnowledgeComp { parent = pawn };
        pawn.AllComps.Add(comp);
        Set(comp, "legacyImported", true);
        Set(comp, "pendingReconcile", false);
        return comp;
    }

    private static bool Authorized(CompAbilityUserMagic magic, MagicPower power)
    {
        var state = typeof(TM_ClassState).GetMethod("IsMagicPowerAuthorized", Flags)!;
        return (bool)state.Invoke(null, new object?[] { magic, power })!;
    }

    // The reporter's save state: knowledge holds rwom.tm_heal learned=True;
    // the Heal power sits in another class's family list on a custom mage.
    private static void LearnedEntryAuthorizesOffClassPower(RuntimeFixture f)
    {
        var (pawn, magic, healPower) = CustomMage(f);
        var knowledge = AttachKnowledge(pawn);
        var entry = knowledge.GetOrCreate(SpellIds.Of(TorannMagicDefOf.TM_Heal), 0);
        entry.learned = true;

        Check.Equal(true, Authorized(magic, healPower),
            "a learned knowledge entry authorizes the off-class scroll power");
    }

    // An entry that was written unlearned must not grant the ability.
    private static void UnlearnedEntryStaysUnauthorized(RuntimeFixture f)
    {
        var (pawn, magic, healPower) = CustomMage(f);
        var knowledge = AttachKnowledge(pawn);
        var entry = knowledge.GetOrCreate(SpellIds.Of(TorannMagicDefOf.TM_Heal), 0);
        entry.learned = false;

        Check.Equal(false, Authorized(magic, healPower),
            "an unlearned knowledge entry keeps the off-class power unauthorized");
    }

    // Old saves without the knowledge comp must behave exactly as before the
    // fix: the gate stays closed.
    private static void NoKnowledgeCompStaysUnauthorized(RuntimeFixture f)
    {
        var (pawn, magic, healPower) = CustomMage(f);
        AttachKnowledge(pawn);
        pawn.AllComps.Remove(pawn.AllComps.OfType<AbilityKnowledgeComp>().Single());

        Check.Equal(false, Authorized(magic, healPower),
            "without a knowledge comp the off-class power stays unauthorized");
    }
}
