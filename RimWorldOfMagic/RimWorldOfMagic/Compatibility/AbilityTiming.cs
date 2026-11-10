using AbilityUser;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TorannMagic.Compatibility
{
    public static class AbilityTiming
    {
        public static float SafeMultiplier(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp(value, .1f, 5f);
        public static float WarmupMultiplier => SafeMultiplier(ModOptions.Settings.Instance?.abilityWarmupMultiplier ?? 1f);
        public static float WarmupFor(Verb verb) => verb is Verb_UseAbility ability && ability.Ability?.Def is TMAbilityDef ? WarmupMultiplier : 1f;
        public static int Cooldown(int ticks) => Mathf.RoundToInt(ticks * SafeMultiplier(ModOptions.Settings.Instance?.abilityCooldownMultiplier ?? 1f));
    }

    [HarmonyPatch(typeof(PawnAbility), nameof(PawnAbility.MaxCastingTicks), MethodType.Getter)]
    public static class AbilityCooldownSettingPatch
    {
        public static void Postfix(PawnAbility __instance, ref int __result)
        {
            if (__instance.Def is TMAbilityDef) __result = AbilityTiming.Cooldown(__result);
        }
    }

    [HarmonyPatch(typeof(Verb), nameof(Verb.WarmupTime), MethodType.Getter)]
    public static class AbilityWarmupSettingPatch
    {
        public static void Postfix(Verb __instance, ref float __result)
        {
            if (__instance is Verb_UseAbility ability && ability.Ability?.Def is TMAbilityDef)
                __result *= AbilityTiming.WarmupMultiplier;
        }
    }
}
