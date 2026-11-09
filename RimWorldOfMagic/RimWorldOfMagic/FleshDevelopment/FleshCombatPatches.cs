using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.FleshDevelopment
{
    // The vanilla method returns only the final extra-damage packet. Accumulate every actual
    // target damage result within this melee invocation instead; once per successful attack.
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class FleshCombatPatches
    {
        public sealed class Hit
        {
            public Hit Previous;
            public Pawn Attacker, Victim;
            public float Damage;
            public bool Completed;
            internal ThingDef TrialWeapon;
            internal bool TrialDamageDealt;
        }
        [ThreadStatic] private static Hit current;
        public static void Prefix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target, out Hit __state)
        {
            __state = new Hit { Previous = current, Attacker = __instance.CasterPawn, Victim = target.Pawn };
            __state.TrialWeapon = ClassProgression.BladeRiposteTrial.Weapon(__instance, __state.Attacker, __state.Victim);
            current = __state;
        }
        public static void Record(Thing victim, DamageInfo damage, DamageWorker.DamageResult result)
        {
            FleshBiteAttacks.Record(victim, damage, result);
            if (current != null && victim == current.Victim && damage.Instigator == current.Attacker && result != null)
            {
                current.Damage += Math.Max(0f, result.totalDamageDealt);
                if (current.TrialWeapon != null && damage.Weapon == current.TrialWeapon && damage.Def?.isRanged == false && result.totalDamageDealt > 0f)
                    current.TrialDamageDealt = true;
            }
        }
        public static void Postfix(Verb_MeleeAttackDamage __instance, Hit __state, ref DamageWorker.DamageResult __result)
        {
            if (__state.Completed) return;
            current = null;
            __state.Completed = true;
            try
            {
                if (__state.TrialDamageDealt) ClassProgression.BladeRiposteTrial.Struck(__state.Attacker, __state.Victim);
                FleshBiteAttacks.Finish(__instance, ref __result);
            }
            finally { current = __state.Previous; }
        }
        public static Exception Finalizer(Exception __exception, Hit __state)
        {
            if (__state != null && !__state.Completed) current = __state.Previous;
            return __exception;
        }
        public static bool CanBite(Pawn attacker, Pawn victim)
        {
            var p = FleshRepository.TryGet(attacker);
            return attacker != null && !attacker.Dead && FleshRepository.IsRaisedUndead(attacker) && p != null && (p.FeastUntil > FleshActions.Now || p.FeastRage) && victim != null && !victim.Dead && FleshActions.Organic(victim);
        }
        public static void Bite(Pawn attacker, Pawn victim)
        {
            if (victim == null) return;
            var map = victim.Map;
            var position = victim.Spawned ? victim.DrawPos : UnityEngine.Vector3.zero;
            var result = Sever(attacker, victim);
            if (result != null && result.totalDamageDealt > 0f) FleshEffects.Consumed(attacker, map, position);
        }
        public static DamageWorker.DamageResult Sever(Pawn attacker, Pawn victim)
        {
            if (!CanBite(attacker, victim) || !Rand.Chance(.2f)) return null;
            var parts = victim.health.hediffSet.GetNotMissingParts().Where(part => EdiblePart(victim, part)).ToList();
            if (parts.Count == 0) return null;
            // Include internal and vital organs; vanilla external-only hit selection is intentionally bypassed.
            var part = parts.RandomElement();
            float health = victim.health.hediffSet.GetPartHealth(part);
            float meatBefore = victim.GetStatValue(StatDefOf.MeatAmount);
            float totalCoverage = victim.health.hediffSet.GetCoverageOfNotMissingNaturalParts(victim.RaceProps.body.corePart);
            float partCoverage = victim.health.hediffSet.GetCoverageOfNotMissingNaturalParts(part);
            var bite = new DamageInfo(DamageDefOf.Bite, health + 1f, 999f, instigator: attacker, hitPart: part);
            // Vanilla otherwise preserves outside parts probabilistically and can redirect internal
            // damage to their parents. A bite is confined to its selected part, including vital organs.
            bite.SetAllowDamagePropagation(false);
            bite.SetIgnoreInstantKillProtection(true);
            var result = victim.TakeDamage(bite);
            if (result.totalDamageDealt <= 0f) return result;
            // Normalize the selected part's remaining natural coverage against the remaining body.
            // Partial/mitigated damage consumes only that fraction; zero-meat parts never feed.
            float eaten = FleshActions.BiteMeat(meatBefore, partCoverage, totalCoverage, result.totalDamageDealt, health);
            var p = FleshRepository.TryGet(attacker);
            p.AddThirst(-eaten / 5f);
            FleshActions.UpdateRage(attacker, p);
            return result;
        }
        public static bool EdiblePart(Pawn victim, BodyPartRecord part)
        {
            if (part == null || part == victim.RaceProps.body.corePart || victim.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part)) return false;
            // Consuming a natural parent must not consume an implanted descendant along with it.
            foreach (var h in victim.health.hediffSet.hediffs)
                if (h is Hediff_AddedPart)
                    for (var ancestor = h.Part; ancestor != null; ancestor = ancestor.parent)
                        if (ancestor == part) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class FleshDamageResultPatch
    {
        public static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result) => FleshCombatPatches.Record(__instance, dinfo, __result);
    }

    [HarmonyPatch(typeof(MentalState), nameof(MentalState.MentalStateTick))]
    public static class FleshRageDurationPatch
    {
        public static bool Prefix(MentalState __instance)
        {
            var p = FleshRepository.TryGet(__instance.pawn);
            // Vanilla Berserk behavior/jobs are retained; its random/time recovery is held until satiation.
            return !(p != null && p.FeastRage && __instance.def == MentalStateDefOf.Berserk);
        }
    }
    [HarmonyPatch(typeof(MentalState), nameof(MentalState.RecoverFromState))]
    public static class FleshRageRecoveryPatch
    {
        public static bool Holding(MentalState state)
        {
            Pawn pawn = state?.pawn;
            var p = FleshRepository.TryGet(pawn);
            return pawn != null && !pawn.Dead && FleshRepository.IsRaisedUndead(pawn) && p != null && p.FeastRage && p.Thirst > .2f * p.MaxThirst && pawn.MentalState == state && state.def == MentalStateDefOf.Berserk;
        }
        public static bool Prefix(MentalState __instance) => !Holding(__instance);
    }
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.MentalStateHandlerTickInterval))]
    public static class FleshRageDownedTickPatch
    {
        public static bool Prefix(MentalStateHandler __instance, Pawn ___pawn)
        {
            // Vanilla treats a downed Berserk as erroneous and logs before recovering it. Our
            // saved rage intentionally persists while downed; recovery is checked by Flesh.Tick.
            return !(___pawn.Downed && FleshRageRecoveryPatch.Holding(__instance.CurState));
        }
    }
    [HarmonyPatch(typeof(MentalStateHandler), "ClearMentalStateDirect")]
    public static class FleshRageClearPatch
    {
        public static void Prefix(Pawn ___pawn)
        {
            var p = FleshRepository.TryGet(___pawn);
            if (p != null) p.FeastRage = false;
        }
    }
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public static class FleshRageReplacementPatch
    {
        public static void Prefix(MentalStateHandler __instance, out MentalState __state) => __state = __instance.CurState;
        public static void Postfix(MentalStateHandler __instance, Pawn ___pawn, MentalState __state)
        {
            if (__state == __instance.CurState) return;
            var p = FleshRepository.TryGet(___pawn);
            if (p != null) p.FeastRage = false;
        }
    }
}
