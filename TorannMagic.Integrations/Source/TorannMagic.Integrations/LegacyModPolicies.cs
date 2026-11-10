using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TorannMagic.Compatibility;
using Verse;

namespace TorannMagic.Integrations
{
    [HarmonyPatch(typeof(OptionalCompatibility), nameof(OptionalCompatibility.IsEnabled))]
    internal static class LegacyModPolicies
    {
        // Preserve the legacy gates. Unused detectors remain callable for old
        // extensions; they do not advertise full support for those mods on 1.7.
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "PrisonLabor", "Prison Labor" },
            { "PrisonLaborOutdated", "Prison Labor 0.9 (Outdated)" },
            { "RimOfMadness_Vampires", "Rim of Madness - Vampires" },
            { "ChildrenSchoolLearning", "Children, school and learning" },
            { "AlienHumanoidRaces", "Humanoid Alien Races 2.0" },
            { "GiddyUpBattleMounts", "Giddy-up! Battle Mounts" },
            { "SRTS_Expanded", "SRTS Expanded" },
            { "DualWield", "Dual Wield" },
            { "SOS2", "Save Our Ship 2" },
            { "NoJobAuthors", "No Job Authors" }
        };
        public static bool Enabled(string feature)
        {
            if (feature == "ShipOxygenGuidance") return ModsConfig.IsActive("vanillaexpanded.gravship");
            return feature != null && Names.TryGetValue(feature, out string name)
                && LoadedModManager.RunningMods.Any(mod => mod.Name == name);
        }
        static void Postfix(string feature, ref bool __result) => __result |= Enabled(feature);
    }

    [HarmonyPatch(typeof(OptionalCompatibility), nameof(OptionalCompatibility.NotifyLegacyBirth))]
    internal static class LegacyBirthBridgePatch
    {
        static void Postfix(Pawn pawn) => LegacyBirthAdapter.Apply(pawn);
    }

    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
    internal static class LegacyChildrenBirthPatch
    {
        static bool Prepare() => LegacyModPolicies.Enabled("ChildrenSchoolLearning");
        [HarmonyPriority(10)]
        static void Postfix(Pawn pawn) => LegacyBirthAdapter.Apply(pawn);
    }
}
