using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Compatibility
{
    internal static class TM_PridestalkerCompatibility
    {
        // Kure's historical degree 0 duplicated degree 4's label and stats but
        // never registered a class. Retain the same Trait and all saved owners;
        // only repair that known degree when the canonical definition exists.
        internal static void Normalize(TraitDef def, ref int degree)
        {
            if (degree == 0 && def?.defName == "TM_Pridestalker"
                && def.degreeDatas?.Any(data => data.degree == 4) == true)
                degree = 4;
        }
    }

    [HarmonyPatch(typeof(Trait), MethodType.Constructor, new Type[] { typeof(TraitDef), typeof(int), typeof(bool) })]
    internal static class PridestalkerTraitConstructorPatch
    {
        private static void Prefix(TraitDef def, ref int degree) => TM_PridestalkerCompatibility.Normalize(def, ref degree);
    }

    [HarmonyPatch(typeof(Trait), nameof(Trait.ExposeData))]
    internal static class PridestalkerTraitLoadPatch
    {
        private static void Postfix(Trait __instance, ref int ___degree)
        {
            // Scribe_Defs resolves the def in this pass. Finish before the pawn's
            // PostLoadInit queries class identity; never reset progression data.
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
                TM_PridestalkerCompatibility.Normalize(__instance.def, ref ___degree);
        }
    }

    [HarmonyPatch(typeof(ClassDefinitionCompatibility), nameof(ClassDefinitionCompatibility.CanonicalTraitDegree))]
    internal static class PridestalkerRosterDegreePatch
    {
        private static void Postfix(TraitDef trait, ref int __result) => TM_PridestalkerCompatibility.Normalize(trait, ref __result);
    }
}
