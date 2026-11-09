using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthActionPolicy
    {
        internal static bool Marked(Pawn pawn) => ClassIdentity.Component(pawn)?.SpiritRebirth != null;

        internal static bool Allows(Pawn pawn, Job job)
        {
            if (!Marked(pawn)) return true;
            if (job?.def == null) return false;
            // Idle is also safe for a dormant/unsupported saved marker. Movement
            // and abilities require a validated active original, never its host.
            if (job.def == JobDefOf.Wait || job.def == JobDefOf.Wait_MaintainPosture) return true;
            if (!SpiritRebirthFormPolicy.IsEthereal(pawn)) return false;
            if (job.def == JobDefOf.Goto) return true;
            if (job.def != TorannMagicDefOf.TMCastAbilityVerb && job.def != TorannMagicDefOf.TMCastAbilitySelf) return false;
            var verb = job.verbToUse as Verb_UseAbility;
            var ability = verb?.Ability;
            var owner = ability?.AbilityUser;
            return verb?.CasterPawn == pawn && owner?.Pawn == pawn
                && pawn.AllComps.Any(c => ReferenceEquals(c, owner))
                && owner.AbilityData.Powers.Any(a => ReferenceEquals(a, ability))
                && ReferenceEquals(ability.Verb, verb)
                && SpiritRebirthCasting.Allows(pawn, ability.Def);
        }

        // Explicit commit/load integration point only. No activation or load hook
        // calls this yet; native cleanup owns reservation and queued-job release.
        internal static void ClearExistingJobs(Pawn pawn)
        {
            if (Marked(pawn)) pawn.jobs?.StopAll(ifLayingKeepLaying: false);
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
    internal static class SpiritRebirthOrderedJobPatch
    {
        static bool Prefix(Pawn ___pawn, Job job, ref bool __result)
        {
            if (SpiritRebirthActionPolicy.Allows(___pawn, job)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    internal static class SpiritRebirthStartJobPatch
    {
        static bool Prefix(Pawn_JobTracker __instance, Pawn ___pawn, ref Job newJob,
            ref JobCondition lastJobEndCondition, ref ThinkNode jobGiver,
            ref bool resumeCurJobAfterwards, ref ThinkTreeDef thinkTree, ref JobTag? tag)
        {
            if (SpiritRebirthActionPolicy.Allows(___pawn, newJob)) return true;
            // A proposal can already hold reservations after queue/think-tree
            // selection. Its caller may still reference it, so do not pool it.
            if (newJob != null && !ReferenceEquals(newJob, __instance.curJob))
                ___pawn.ClearReservationsForJob(newJob);
            if (__instance.curJob != null && SpiritRebirthActionPolicy.Allows(___pawn, __instance.curJob)) return false;
            // Replace the proposal in this invocation, avoiding recursive job
            // selection and its ten-jobs-per-tick recovery path.
            newJob = JobMaker.MakeJob(JobDefOf.Wait, 60);
            lastJobEndCondition = JobCondition.InterruptForced;
            resumeCurJobAfterwards = false;
            jobGiver = null;
            thinkTree = null;
            tag = null;
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "CanPawnTakeOpportunisticJob")]
    internal static class SpiritRebirthOpportunisticJobPatch
    {
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (!SpiritRebirthActionPolicy.Marked(pawn)) return true;
            __result = false;
            return false;
        }
    }

    internal static class SpiritRebirthRaceDispatch
    {
        internal static void Interval(HediffGiver giver, Pawn pawn, Hediff cause)
        {
            if (!SpiritRebirthActionPolicy.Marked(pawn)) giver.OnIntervalPassed(pawn, cause);
        }

        internal static bool Added(HediffGiver giver, Pawn pawn, Hediff hediff)
            => !SpiritRebirthActionPolicy.Marked(pawn) && giver.OnHediffAdded(pawn, hediff);

        internal static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> instructions, string native, string wrapper)
        {
            var code = instructions.ToList();
            var target = AccessTools.Method(typeof(HediffGiver), native, new[] { typeof(Pawn), typeof(Hediff) });
            var replacement = AccessTools.Method(typeof(SpiritRebirthRaceDispatch), wrapper);
            var sites = code.Where(i => i.Calls(target)).ToList();
            if (sites.Count != 1)
                throw new InvalidOperationException("Spirit race dispatch expected exactly one native " + native + " call, found " + sites.Count);
            sites[0].opcode = OpCodes.Call;
            sites[0].operand = replacement;
            return code;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.HealthTickInterval))]
    internal static class SpiritRebirthRaceIntervalPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => SpiritRebirthRaceDispatch.Replace(instructions, nameof(HediffGiver.OnIntervalPassed), nameof(SpiritRebirthRaceDispatch.Interval));
    }

    [HarmonyPatch]
    internal static class SpiritRebirthRaceAddedPatch
    {
        static MethodBase TargetMethod() => AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
            new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            => SpiritRebirthRaceDispatch.Replace(instructions, nameof(HediffGiver.OnHediffAdded), nameof(SpiritRebirthRaceDispatch.Added));
    }
}
