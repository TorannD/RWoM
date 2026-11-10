#if RW17
using AbilityUser;
using HarmonyLib;
using TorannMagic.AutoCast;
using Verse;
using Verse.AI;

namespace TorannMagic.Compatibility
{
    internal static class AbilityCastSafety
    {
        public static bool AllowsCaster(Verb verb)
        {
            if (!(verb is Verb_UseAbility abilityVerb) || !(abilityVerb.Ability?.Def is TMAbilityDef)) return true;
            // Caravans and other off-map holders cannot execute map abilities.
            return verb.caster != null && verb.caster.Spawned && verb.caster.Map != null
                && ClassProgression.SpiritRebirthCasting.Allows(verb.CasterPawn, abilityVerb.Ability.Def);
        }

        public static bool AllowsSelfTarget(Thing caster, TMAbilityDef ability, LocalTargetInfo target)
        {
            if (!target.HasThing || target.Thing != caster) return true;
            // Older explicit self abilities sometimes retain canTargetSelf=false.
            // Their self category is intentional; isViolent is not a targeting rule.
            return ability.MainVerb.AbilityTargetCategory == AbilityTargetCategory.TargetSelf
                || ability.MainVerb.targetParams?.canTargetSelf == true;
        }

        public static bool IsAutomaticCast(Verb verb)
        {
            var job = verb.CasterPawn?.CurJob;
            // Jecs marks AI jobs playerForced too. Use its actual context marker
            // and executing verb, not colonist/faction status or playerForced.
            return job != null && job.verbToUse == verb && job.count == 0
                && TM_AutocastCombat.IsAbilityJob(job);
        }

        private static bool ValidMapTarget(Verb verb, LocalTargetInfo target)
        {
            Thing caster = verb.caster;
            return caster != null && caster.Spawned && target.IsValid && target.Cell.InBounds(caster.Map)
                && (!target.HasThing || (target.Thing.Spawned && !target.Thing.Destroyed && target.Thing.Map == caster.Map));
        }

        public static bool CanAutocast(Verb verb, LocalTargetInfo target)
        {
            if (!(verb is Verb_UseAbility abilityVerb) || !(abilityVerb.Ability?.Def is TMAbilityDef ability)) return true;
            if (TM_ExternalCastOwnership.IsCasting(verb.CasterPawn) || !TM_AutocastCombat.AllowsAutomaticCast(verb.CasterPawn, ability, target)
                || !AllowsSelfTarget(verb.caster, ability, target) || !ValidMapTarget(verb, target)
                || !verb.CanHitTarget(target)) return false;
            // A custom CanHitTargetFrom override may omit LoS. Use the actual
            // engine shoot line (including leaning), only when this spell needs it.
            return !verb.verbProps.requireLineOfSight
                || verb.TryFindShootLineFromTo(verb.caster.Position, target, out _);
        }

        public static bool AllowsStart(Verb verb, LocalTargetInfo target)
        {
            if (!(verb is Verb_UseAbility abilityVerb) || !(abilityVerb.Ability?.Def is TMAbilityDef ability)) return true;
            return AllowsCaster(verb) && AllowsSelfTarget(verb.caster, ability, target)
                && (!IsAutomaticCast(verb) || CanAutocast(verb, target));
        }

        public static bool CanBeginJobCast(Verb_UseAbility verb, LocalTargetInfo target)
        {
            // The job deducts some special resources before TryStartCastOn. Check
            // its native start prerequisites first, preserving deduction timing
            // for zero-warmup effects that read the remaining resource.
            return AllowsStart(verb, target) && verb.caster != null && verb.caster.Spawned
                && verb.state != VerbState.Bursting && verb.CanHitTarget(target)
                && (!verb.CasterIsPawn || verb.WarmupTime <= 0f
                    || verb.TryFindShootLineFromTo(verb.caster.Position, target, out _));
        }

        public static bool AllowsProjectileLaunch(Verb_UseAbility verb, LocalTargetInfo target)
        {
            if (!(verb.Ability?.Def is TMAbilityDef ability)) return true;
            if (!AllowsCaster(verb)) return false;
            if (!AllowsSelfTarget(verb.caster, ability, target)) return false;
            if (!IsAutomaticCast(verb)) return true;
            if (!ValidMapTarget(verb, target)
                || !TM_AutocastCombat.AllowsAutomaticCast(verb.CasterPawn, ability, target)) return false;
            // Preserve the original continuation policy for bursts that explicitly
            // allow losing sight. Reject before JecsLite's player-facing message.
            return !verb.verbProps.requireLineOfSight || !verb.verbProps.stopBurstWithoutLos
                || verb.TryFindShootLineFromTo(verb.caster.Position, target, out _);
        }
    }

    // Framework think nodes can create ability jobs without either comp scheduler.
    // Reject off-map orders in every context; apply combat policy only to AI jobs.
    [HarmonyPatch(typeof(PawnAbility), nameof(PawnAbility.UseAbility))]
    public static class AbilityAutomaticJobAdmissionPatch
    {
        public static bool Prefix(PawnAbility __instance, AbilityContext context, LocalTargetInfo target, ref Job __result)
        {
            if (!(__instance.Def is TMAbilityDef)) return true;
            if (AbilityCastSafety.AllowsCaster(__instance.Verb)
                && (context != AbilityContext.AI || AbilityCastSafety.CanAutocast(__instance.Verb, target))) return true;
            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnAbility), nameof(PawnAbility.TryCastAbility))]
    public static class AbilityOffMapCastPatch
    {
        public static bool Prefix(PawnAbility __instance, ref bool __result)
        {
            if (!(__instance.Def is TMAbilityDef) || AbilityCastSafety.AllowsCaster(__instance.Verb)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class AbilityOffMapBurstPatch
    {
        public static bool Prefix(Verb __instance)
        {
            if (AbilityCastSafety.AllowsCaster(__instance)) return true;
            __instance.state = VerbState.Idle;
            return false;
        }
    }

    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new[] {
        typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class AbilityCastStartPatch
    {
        public static bool Prefix(Verb __instance, LocalTargetInfo castTarg, ref bool __result)
        {
            if (AbilityCastSafety.AllowsStart(__instance, castTarg)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Verb_UseAbility), nameof(Verb_UseAbility.PreCastShotCheck))]
    public static class AbilityPreCastStartPatch
    {
        public static bool Prefix(Verb_UseAbility __instance, LocalTargetInfo castTarg, ref bool __result)
        {
            if (AbilityCastSafety.AllowsStart(__instance, castTarg)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Verb_UseAbility), nameof(Verb_UseAbility.TryLaunchProjectileCheck))]
    public static class AbilityProjectileLaunchSafetyPatch
    {
        public static bool Prefix(Verb_UseAbility __instance, LocalTargetInfo launchTarget, ref bool __result)
        {
            if (AbilityCastSafety.AllowsProjectileLaunch(__instance, launchTarget)) return true;
            __result = false;
            return false;
        }
    }
}
#endif
