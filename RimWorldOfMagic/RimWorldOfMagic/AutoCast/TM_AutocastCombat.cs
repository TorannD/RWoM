using System.Runtime.CompilerServices;
using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.AutoCast
{
    /// <summary>
    /// NPC automatic casts need a live engagement, not merely an attack-shaped job.
    /// Only current combat references are examined; a short-lived weak reference
    /// bridges the combat job being replaced by a support cast. No map scans.
    /// </summary>
    public static class TM_AutocastCombat
    {
        private sealed class Engagement
        {
            public Thing opponent;
            public int tick;
        }
        private static readonly ConditionalWeakTable<Pawn, Engagement> engagements = new ConditionalWeakTable<Pawn, Engagement>();

        private static bool Remember(Pawn caster, Thing candidate, out Thing opponent)
        {
            opponent = candidate;
            Engagement engagement = engagements.GetValue(caster, _ => new Engagement());
            engagement.opponent = candidate;
            engagement.tick = Find.TickManager.TicksGame;
            return true;
        }

        public static bool RequiresCombat(Pawn pawn)
        {
            // A secure slave can satisfy IsColonist. Do not let that route bypass
            // the NPC admission policy, or let prisoners inherit player privileges.
            return pawn != null && (!pawn.IsColonist || pawn.IsPrisoner || pawn.IsSlave);
        }

        public static bool IsAbilityJob(Job job)
        {
            return job?.def != null && (job.def == TorannMagicDefOf.TMCastAbilityVerb
                || job.def == TorannMagicDefOf.TMCastAbilitySelf
                || job.def == AbilityUser.AbilityDefOf.CastAbilityVerb
                || job.def == AbilityUser.AbilityDefOf.CastAbilitySelf);
        }

        public static bool IsActiveOpponent(Pawn caster, Thing opponent)
        {
            if (caster == null || !caster.Spawned || caster.Dead || caster.Downed || caster.Destroyed
                || opponent == null || opponent == caster || !opponent.Spawned || opponent.Destroyed
                || opponent.Map != caster.Map || !(opponent is IAttackTarget attackTarget)) return false;
            if (opponent is Pawn pawn && (pawn.Dead || pawn.Downed || pawn.IsPrisoner)) return false;
            // Thing.HostileTo respects mental states, prisoners, rebellion and
            // dormant pawns; faction-only hostility does not.
            return opponent.HostileTo(caster) && !opponent.Fogged()
                && GenHostility.IsActiveThreatTo(attackTarget, caster.Faction, canBeFogged: false);
        }

        public static bool TryGetCombatOpponent(Pawn caster, out Thing opponent)
        {
            opponent = null;
            if (caster == null || !caster.Spawned || caster.Dead || caster.Downed || caster.Destroyed
                || caster.InMentalState || caster.jobs == null) return false;

            // TargetCurrentlyAimingAt assumes a stance tracker even though a pawn
            // in generation/teardown can already have its ability comp ticking.
            Thing aimed = caster.stances != null ? caster.TargetCurrentlyAimingAt.Thing : null;
            if (IsActiveOpponent(caster, aimed)) return Remember(caster, aimed, out opponent);
            Job job = caster.CurJob;
            if (job == null) return false;
            bool combatJob = caster.IsFighting() || (IsAbilityJob(job) && job.count == 0);
            // enemyTarget can survive a transition back to caravan/idle work.
            // It is evidence only while the pawn is still executing combat.
            Thing enemy = caster.mindState?.enemyTarget;
            if (combatJob && IsActiveOpponent(caster, enemy)) return Remember(caster, enemy, out opponent);
            if (combatJob && IsActiveOpponent(caster, job.targetA.Thing))
                return Remember(caster, job.targetA.Thing, out opponent);
            // Starting an allied/self support spell replaces the attack job. Keep
            // its separately admitted opponent only for that executing AI spell,
            // and revalidate threat/hostility/map on every final check. Never let a
            // cached engagement authorize a later idle/caravan job.
            if (IsAbilityJob(job) && job.count == 0 && engagements.TryGetValue(caster, out Engagement prior)
                && Find.TickManager.TicksGame - prior.tick <= 600 && IsActiveOpponent(caster, prior.opponent))
            {
                opponent = prior.opponent;
                return true;
            }
            return false;
        }

        public static bool AllowsAutomaticCast(Pawn caster, TMAbilityDef ability, LocalTargetInfo target)
        {
            if (!ClassProgression.SpiritRebirthCasting.Allows(caster, ability)) return false;
            if (RequiresCombat(caster) && !TryGetCombatOpponent(caster, out _)) return false;
            return UsefulTarget(caster, ability, target);
        }

        public static bool UsefulTarget(Pawn caster, TMAbilityDef ability, LocalTargetInfo target)
        {
            if (!TM_Calc.AutocastTargetAcceptable(caster, ability, target)) return false;
            Pawn pawn = target.Pawn;
            string name = TM_Calc.BaseAbilityDefName(ability.defName);
            if (name == "TM_Heal" || name == "TM_AdvancedHeal" || name == "TM_Regenerate")
            {
                if (pawn?.health?.hediffSet == null) return false;
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff is Hediff_Injury injury && injury.Severity > 0f && injury.CanHealNaturally()) return true;
                    if (name == "TM_AdvancedHeal" && hediff is Hediff_MissingPart missing && missing.IsFresh) return true;
                }
                return false;
            }
            if (name == "TM_FirstAid" || name == "TM_CauterizeWound")
                return pawn?.health != null && pawn.health.HasHediffsNeedingTend(false);
            // Disease/mechanite usefulness is already skill-aware in TM_Calc;
            // enforce it here as well for jobs created by external AI profiles.
            return true;
        }

        public static bool HasTravelProgress(IntVec3 origin, IntVec3 landing, IntVec3 destination)
        {
            return origin.IsValid && landing.IsValid && destination.IsValid && landing != origin
                && (landing - destination).LengthHorizontalSquared < (origin - destination).LengthHorizontalSquared;
        }

        public static bool TryGetTravelDestination(Pawn caster, out LocalTargetInfo destination)
        {
            destination = LocalTargetInfo.Invalid;
            if (caster == null || !caster.Spawned || caster.CurJob == null || caster.pather == null
                || !caster.pather.Moving || caster.pather.curPath == null || !caster.pather.curPath.Found
                || caster.pather.curPath.NodesLeftCount <= 0) return false;
            LocalTargetInfo target = caster.pather.Destination;
            if (!target.IsValid || !target.Cell.InBounds(caster.Map) || target.Cell == caster.Position
                || (target.HasThing && (!target.Thing.Spawned || target.Thing.Map != caster.Map))) return false;
            destination = target;
            return true;
        }

        public static bool LandingAllowed(Pawn caster, IntVec3 landing)
        {
            return caster != null && caster.Spawned && landing.IsValid
                && landing.InBounds(caster.Map) && landing != caster.Position;
        }
    }
}
