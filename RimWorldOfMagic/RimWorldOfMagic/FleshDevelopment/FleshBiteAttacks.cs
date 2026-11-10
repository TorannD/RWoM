using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.FleshDevelopment
{
    // Scope is entered only for an actual melee shot, never during verb previews or selection.
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class FleshBiteAttacks
    {
        public sealed class Attack
        {
            public Attack Previous;
            public Verb_MeleeAttack Verb;
            public Pawn Attacker, Victim;
            public Thing Target;
            public ManeuverDef OriginalManeuver;
            public bool Bite, Converted, Feast, Critical, Finished;
            public DamageWorker.DamageResult BiteResult;
        }
        [ThreadStatic] public static Attack Current;
        public static bool Eligible(Pawn attacker, Pawn victim) => attacker != null && !attacker.Dead && FleshRepository.IsRaisedUndead(attacker)
            && FleshRepository.TryGet(attacker) != null && victim != null && !victim.Dead && FleshActions.Organic(victim);
        public static bool Feasting(Pawn pawn)
        {
            var p = FleshRepository.TryGet(pawn);
            return p != null && (p.FeastUntil > FleshActions.Now || p.FeastRage);
        }
        public static void Prefix(Verb_MeleeAttack __instance, out Attack __state)
        {
            __state = new Attack { Previous = Current, Verb = __instance, Attacker = __instance.CasterPawn, Victim = __instance.CurrentTarget.Pawn, Target = __instance.CurrentTarget.Thing, OriginalManeuver = __instance.maneuver };
            Current = __state;
            if (!(__instance is Verb_MeleeAttackDamage) || __state.Attacker == null || __state.Attacker.Dead || !FleshRepository.IsRaisedUndead(__state.Attacker)
                || FleshRepository.TryGet(__state.Attacker) == null || __state.Target == null || !__state.Attacker.Spawned || __state.Attacker.stances.FullBodyBusy) return;
            var bite = DefDatabase<ManeuverDef>.GetNamedSilentFail("Bite");
            if (bite == null) return;
            __state.Feast = Feasting(__state.Attacker);
            __state.Critical = !__state.Feast && Rand.Chance(.1f);
            __state.Converted = __state.Feast || __state.Critical;
            __state.Bite = __state.Converted || __instance.verbProps.meleeDamageDef == DamageDefOf.Bite;
            if (__state.Bite) __instance.maneuver = bite; // Per-instance reference only; restored even on exceptions.
        }
        public static Exception Finalizer(Exception __exception, Attack __state)
        {
            if (__state != null)
            {
                __state.Verb.maneuver = __state.OriginalManeuver;
                Current = __state.Previous;
            }
            return __exception;
        }
        public static IEnumerable<DamageInfo> ConvertedDamage(IEnumerable<DamageInfo> original, Attack attack)
        {
            if (attack == null || !attack.Converted)
            { foreach (var info in original) yield return info; yield break; }
            // A jaw attack replaces the weapon packet and its bonus/surprise packets, not merely the log label.
            using (var it = original.GetEnumerator())
            {
                if (!it.MoveNext()) yield break;
                var source = it.Current;
                var bite = new DamageInfo(DamageDefOf.Bite, source.Amount * (attack.Critical ? 1.5f : 1f), source.ArmorPenetrationInt,
                    source.Angle, attack.Attacker, source.HitPart, attack.Attacker.def, source.Category, source.IntendedTarget, source.InstigatorGuilty);
                bite.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                yield return bite;
            }
        }
        public static void Record(Thing victim, DamageInfo damage, DamageWorker.DamageResult result)
        {
            var a = Current;
            if (a == null || a.Finished || !a.Bite || victim != a.Victim || damage.Instigator != a.Attacker || result == null || result.totalDamageDealt <= 0f) return;
            // A blocked native bite followed by a successful bonus packet is not a successful bite.
            if (a.BiteResult == null && damage.Def != DamageDefOf.Bite) return;
            if (a.BiteResult == null) a.BiteResult = result;
            else Merge(a.BiteResult, result);
        }
        public static void Merge(DamageWorker.DamageResult target, DamageWorker.DamageResult extra)
        {
            if (target == null || extra == null || ReferenceEquals(target, extra)) return;
            target.totalDamageDealt += extra.totalDamageDealt;
            if (extra.parts != null) foreach (var part in extra.parts) if (target.parts == null || !target.parts.Contains(part)) target.AddPart(extra.hitThing, part);
            if (extra.hediffs != null) foreach (var h in extra.hediffs) if (target.hediffs == null || !target.hediffs.Contains(h)) target.AddHediff(h);
            target.wounded |= extra.wounded;
        }
        public static void Finish(Verb_MeleeAttackDamage verb, ref DamageWorker.DamageResult result)
        {
            var a = Current;
            if (a == null || a.Verb != verb || a.Finished) return;
            a.Finished = true;
            if (!a.Bite || a.BiteResult == null || a.BiteResult.totalDamageDealt <= 0f || a.Victim == null) return;
            var map = a.Victim.Map;
            var position = a.Victim.Spawned ? a.Victim.DrawPos : Vector3.zero;
            if (a.Feast) Merge(a.BiteResult, FleshCombatPatches.Sever(a.Attacker, a.Victim));
            // Vanilla associates this combined result with the bite log after ApplyMeleeDamageToTarget returns.
            result = a.BiteResult;
            ApplyConditions(a.Attacker, a.Victim, result, a.Feast);
            if (FleshActions.Organic(a.Victim)) FleshEffects.Consumed(a.Attacker, map, position);
        }
        public static float ToxicDose(float resistance) => .01f * (1f - Mathf.Clamp01(resistance));
        public static bool Infectable(Pawn victim, BodyPartRecord part)
        {
            if (victim == null || victim.Dead || !FleshActions.Organic(victim) || part == null || victim.health.hediffSet.PartIsMissing(part)
                || victim.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part) || part.def.IsSolid(part, victim.health.hediffSet.hediffs)
                || victim.health.hediffSet.HasRegeneration || victim.health.hediffSet.hediffs.Any(h => h.def == HediffDefOf.WoundInfection && h.Part == part)) return false;
            return victim.health.immunity.DiseaseContractChanceFactor(HediffDefOf.WoundInfection, part) > .001f;
        }
        public static void ApplyConditions(Pawn attacker, Pawn victim, DamageWorker.DamageResult result, bool feast)
        {
            if (!Eligible(attacker, victim) || result == null || result.totalDamageDealt <= 0f) return;
            float dose = ToxicDose(victim.GetStatValue(StatDefOf.ToxicResistance));
            if (dose > 0f) HealthUtility.AdjustSeverity(victim, HediffDefOf.ToxicBuildup, dose);
            if (!feast || result.hediffs == null) return;
            var parts = result.hediffs.OfType<Hediff_Injury>().Where(h => h.Severity > 0 && victim.health.hediffSet.hediffs.Contains(h))
                .Select(h => h.Part).Distinct().Where(part => Infectable(victim, part)).ToList();
            if (parts.Count > 0 && Rand.Chance(.3f)) victim.health.AddHediff(HediffDefOf.WoundInfection, parts.RandomElement());
        }
        [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply")]
        public static class DamagePatch
        {
            public static void Postfix(Verb_MeleeAttackDamage __instance, ref IEnumerable<DamageInfo> __result)
            { if (Current?.Verb == __instance) __result = ConvertedDamage(__result, Current); }
        }
        [HarmonyPatch(typeof(Verb_MeleeAttack), nameof(Verb_MeleeAttack.CreateCombatLog))]
        public static class LogPatch
        {
            public static bool Prefix(Verb_MeleeAttack __instance, Func<ManeuverDef, RulePackDef> rulePackGetter, bool alwaysShow, ref BattleLogEntry_MeleeCombat __result)
            {
                var a = Current;
                if (a == null || a.Verb != __instance || !a.Bite) return true;
                __result = new BattleLogEntry_MeleeCombat(rulePackGetter(__instance.maneuver), alwaysShow, a.Attacker, a.Target,
                    DefDatabase<ImplementOwnerTypeDef>.GetNamed("Bodypart"), "", null, null, __instance.maneuver.logEntryDef);
                Find.BattleLog.Add(__result);
                return false;
            }
        }
    }
}
