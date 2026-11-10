using AbilityAI;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Integrations.AbilityAICompat
{
    // The classic gizmo overlay toggles autocast with raw Unity input
    // (TM_Action.DrawAutoCastForGizmo), which an IMGUI event consumption
    // cannot suppress. While Ability AI owns a bridged spell, that toggle
    // writes a preference no automatic path reads, so this patch skips the
    // write; the right-click opens Ability AI's own autocast menu instead.
    // With Ability AI's master toggle off the classic toggle resumes, and
    // spells without a rule stay classic as before. The two overloads need
    // explicit signatures: Harmony cannot resolve them by name alone.
    [HarmonyPatch(typeof(AutoCast.AutocastPolicy), "ToggleAutocast",
        new[] { typeof(CompAbilityUserMagic), typeof(MagicPower) })]
    internal static class Patch_AutocastToggle_Handover_Magic
    {
        static bool Prefix(MagicPower power)
        {
            return !Patch_AutocastToggle_Handover.ShouldHandOver(power?.abilityDef as TMAbilityDef);
        }
    }

    [HarmonyPatch(typeof(AutoCast.AutocastPolicy), "ToggleAutocast",
        new[] { typeof(CompAbilityUserMight), typeof(MightPower) })]
    internal static class Patch_AutocastToggle_Handover_Might
    {
        static bool Prefix(MightPower power)
        {
            return !Patch_AutocastToggle_Handover.ShouldHandOver(power?.abilityDef as TMAbilityDef);
        }
    }

    internal static class Patch_AutocastToggle_Handover
    {
        internal static bool ShouldHandOver(TMAbilityDef def)
        {
            if (!AbilityAISettings.Enabled || !AbilityAISettings.PlayerAutocast) return false;
            TMBridgeAbilityDef bridge = VanillaBridgeSync.BridgeDefFor(def);
            return bridge != null && bridge.HasModExtension<AbilityAIExtension>();
        }
    }
}
