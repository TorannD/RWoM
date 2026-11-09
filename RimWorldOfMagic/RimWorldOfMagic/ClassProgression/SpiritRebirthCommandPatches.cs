using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityUser;
using HarmonyLib;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(PawnAbility), nameof(PawnAbility.ShouldShowGizmo))]
    internal static class SpiritRebirthAbilityVisibilityPatch
    {
        static bool Prefix(PawnAbility __instance, ref bool __result)
        {
            var owner = __instance.AbilityUser;
            if (!(owner is CompAbilityUserMagic) && !(owner is CompAbilityUserMight)) return true;
            if (!SpiritRebirthActionPolicy.Marked(owner.Pawn) || SpiritRebirthCasting.IsSpiritAbility(__instance.Def)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class SpiritRebirthCommandCachePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(CompAbilityUserMagic), nameof(CompAbilityUserMagic.GetGizmoCommands), new[] { typeof(string) });
            yield return AccessTools.DeclaredMethod(typeof(CompAbilityUserMight), nameof(CompAbilityUserMight.GetGizmoCommands), new[] { typeof(string) });
        }

        static bool Prefix(CompAbilityUserTMBase __instance, ref Command __result)
        {
            // Every current named cache entry belongs to a former class. Keep
            // cached objects/settings intact; native Spirit ability gizmos use
            // their own producer and normal cast-time validation.
            if (!SpiritRebirthActionPolicy.Marked(__instance.Pawn)) return true;
            __result = null;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class SpiritRebirthToggleExecutionPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string name in new[] { nameof(CompAbilityUserMagic.ToggleTechnoBit), nameof(CompAbilityUserMagic.ToggleTechnoBitRepair), nameof(CompAbilityUserMagic.ToggleElementalShot) })
                yield return AccessTools.DeclaredMethod(typeof(CompAbilityUserMagic), name, Type.EmptyTypes);
            foreach (string name in new[] { nameof(CompAbilityUserMight.ToggleCleave), nameof(CompAbilityUserMight.ToggleCQC), nameof(CompAbilityUserMight.TogglePsionicAugmentation), nameof(CompAbilityUserMight.TogglePsionicMindAttack) })
                yield return AccessTools.DeclaredMethod(typeof(CompAbilityUserMight), name, Type.EmptyTypes);
        }

        // Cached delegates and multiplayer commands must recheck at execution.
        static bool Prefix(CompAbilityUserTMBase __instance)
            => !SpiritRebirthActionPolicy.Marked(__instance.Pawn);
    }

    [HarmonyPatch]
    internal static class SpiritRebirthDirectActionPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string name in new[] { nameof(TM_Action.PromoteWanderer), nameof(TM_Action.PromoteWayfarer), nameof(TM_Action.RemoveSymbiosisCommand) })
                yield return AccessTools.DeclaredMethod(typeof(TM_Action), name, new[] { typeof(Pawn) });
        }

        static bool Prefix(Pawn __0) => !SpiritRebirthActionPolicy.Marked(__0);
    }
}
