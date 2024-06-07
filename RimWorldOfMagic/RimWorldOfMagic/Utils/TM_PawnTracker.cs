using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Utils
{
    /*
     * This class stores information about which Pawns are which classes, whether they are spawned, etc. It uses
     * the harmony patches below the class to achieve this state tracking.
     */
    public static class TM_PawnTracker
    {
        // For simplicity, use this not fully optimized function to prevent bugs that would be hard to find
        public static void ResolveComps(Pawn pawn)
        {
            ResolveMagicComp(pawn.TryGetComp<CompAbilityUserMagic>());
            ResolveMightComp(pawn.TryGetComp<CompAbilityUserMight>());
        }
        public static void ResolveMagicComp(CompAbilityUserMagic magicComp)
        {
            if (magicComp == null) return;

            magicComp.IsFaceless = magicComp.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless);
            magicComp.TickConditionsMet =
                magicComp.SetIsMagicUser() && !magicComp.Pawn.IsWildMan() && !magicComp.IsFaceless;
        }

        public static void ResolveMightComp(CompAbilityUserMight mightComp)
        {
            if (mightComp == null) return;

            mightComp.IsFaceless = mightComp.Pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless);
            mightComp.TickConditionsMet = mightComp.SetIsMightUser() && !mightComp.Pawn.NonHumanlikeOrWildMan();
        }
    }


    // ====== Harmony Patches ======================================================================================
    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.GainTrait))]
    class TM_PawnTracker__GainTrait__Postfix
    {
        static void Postfix(Pawn ___pawn)
        {
            TM_PawnTracker.ResolveComps(___pawn);
        }
    }

    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.RemoveTrait))]
    class TM_PawnTracker__RemoveTrait__Postfix
    {
        static void Postfix(Pawn ___pawn)
        {
            TM_PawnTracker.ResolveComps(___pawn);
        }
    }

    // This is how pawns gain/lose WildMan status
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ChangeKind))]
    class TM_PawnTracker__ChangeKind__Postfix
    {
        static void Postfix(Pawn __instance)
        {
            TM_PawnTracker.ResolveComps(__instance);
        }
    }
}