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
            if (pawn?.story?.traits == null) return;
            ResolveMagicComp(pawn.GetCompAbilityUserMagic());
            ResolveMightComp(pawn.GetCompAbilityUserMight());
        }

        // Runs right after ThingWithComps.InitializeComps (spawn setup, and PostMake
        // during pawn generation). Clears stale TM_Calc comp-lookup entries and
        // re-derives the tick caches now that the comp list is final. A pawn being
        // generated has no story yet — nothing to resolve then.
        public static void SyncAfterCompInit(Pawn pawn)
        {
            TM_Calc.InvalidateCompCache(pawn);
            if (pawn?.story == null) return;
            ResolveComps(pawn);
        }

        // Pawns made outside the vanilla generator (editor-made, constructed without
        // Awake) reach class grants with no ability comps at all: the trait alone
        // leaves them mana-less and grimoire-less. Run the same injection the spawn
        // path uses — TransformPawn adds every missing ability-user comp and skips
        // existing ones, and unlike vanilla GetComp<T> it is safe on empty comp
        // lists. Invalidate the TM_Calc "no comp" cache afterwards so lookups
        // (grimoire tab, tome comp reference) resolve the freshly added comps.
        public static void EnsureClassComps(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return;
            AbilityUser.AbilityUserUtility.TransformPawn(pawn);
            TM_Calc.InvalidateCompCache(pawn);
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
        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn ___pawn)
        {
            TM_ClassState.CaptureBeforeTraitsChanged(___pawn);
        }

        [HarmonyPriority(Priority.Last)]
        static void Postfix(Pawn ___pawn)
        {
            TM_PawnTracker.ResolveComps(___pawn);
            TM_ClassState.OnTraitsChanged(___pawn);
        }
    }

    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.RemoveTrait))]
    class TM_PawnTracker__RemoveTrait__Postfix
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(Pawn ___pawn)
        {
            TM_ClassState.CaptureBeforeTraitsChanged(___pawn);
        }

        [HarmonyPriority(Priority.Last)]
        static void Postfix(Pawn ___pawn)
        {
            TM_PawnTracker.ResolveComps(___pawn);
            TM_ClassState.OnTraitsChanged(___pawn);
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