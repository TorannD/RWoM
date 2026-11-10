using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using TorannMagic.Compatibility;

namespace TorannMagic.Integrations
{
    // Preserve the existing RWoM restriction policy. HAR's newer CanGetTrait
    // additionally applies degree/chance rules; adopting those is a separate change.
    [HarmonyPatch(typeof(ClassDefinitionCompatibility), nameof(ClassDefinitionCompatibility.AllowsTrait))]
    public static class AlienRaceTraitRestrictions
    {
        public static bool Allows(ThingDef race, Pawn pawn, TraitDef trait)
        {
            Type type = race?.GetType();
            while (type != null && type.FullName != "AlienRace.ThingDef_AlienRace") type = type.BaseType;
            if (type == null) return true;
            object settings = AccessTools.Field(type, "alienRace")?.GetValue(race);
            object restrictions = settings == null ? null : AccessTools.Field(settings.GetType(), "raceRestriction")?.GetValue(settings);
            var traits = restrictions == null ? null : AccessTools.Field(restrictions.GetType(), "traitList")?.GetValue(restrictions) as List<TraitDef>;
            if (traits == null) return true;
            if (traits.Contains(trait)) return false;
            if (pawn?.story?.AllBackstories != null)
                foreach (BackstoryDef story in pawn.story.AllBackstories)
                    if (story?.disallowedTraits != null)
                        foreach (BackstoryTrait forbidden in story.disallowedTraits)
                            if (forbidden.def == trait) return false;
            return true;
        }

        private static void Postfix(ThingDef race, Pawn pawn, TraitDef trait, ref bool __result)
        {
            if (__result) __result = Allows(race, pawn, trait);
        }
    }
}
