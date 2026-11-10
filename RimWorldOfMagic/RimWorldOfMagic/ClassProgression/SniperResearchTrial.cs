using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch(typeof(JobDriver_Research), "MakeNewToils")]
    internal static class SniperResearchTrial
    {
        private static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_Research __instance)
        {
            return ClassProgressionFeature.Enabled ? TrackResearch(__result, __instance) : __result;
        }

        private static IEnumerable<Toil> TrackResearch(IEnumerable<Toil> toils, JobDriver_Research driver)
        {
            foreach (var toil in toils)
            {
                var work = toil.tickIntervalAction;
                if (work != null && toil.activeSkill?.Invoke() == SkillDefOf.Intellectual)
                {
                    toil.tickIntervalAction = elapsed =>
                    {
                        var pawn = driver.pawn;
                        var bench = driver.job.targetA.Thing as Building_ResearchBench;
                        var project = elapsed > 0 && pawn?.Spawned == true && !pawn.Dead && !pawn.Downed &&
                            bench?.Spawned == true && bench.Map == pawn.Map &&
                            SniperSurvey.Path(pawn)?.Stage == InitiationStage.Adept
                            ? Find.ResearchManager.GetProject() : null;
                        float before = project == null ? 0f : Find.ResearchManager.GetProgress(project);
                        work(elapsed);
                        // The last productive interval still counts if it finishes the project.
                        if (project != null && Find.ResearchManager.GetProgress(project) > before)
                            SniperMastery.Research(pawn, elapsed);
                    };
                }
                yield return toil;
            }
        }
    }
}
