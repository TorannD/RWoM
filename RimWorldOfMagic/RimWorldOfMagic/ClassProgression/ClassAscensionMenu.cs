using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public static class ClassAscensionMenu
    {
        public static IEnumerable<FloatMenuOption> Options(Pawn pawn, Thing circle)
        {
            if (!ClassAscension.IsCandidate(pawn)) yield break;
            foreach (var id in ClassAscension.Candidates(pawn))
                foreach (var option in ForClass(pawn, circle, id)) yield return option;
        }

        private static IEnumerable<FloatMenuOption> ForClass(Pawn pawn, Thing circle, string classId)
        {
            string keyPrefix = classId == "rwom.chaos" ? "TM_ChaosAscension" : classId == "rwom.empath" ? "TM_ProgressionEmpathAscension" : classId == "rwom.golemancer" ? "TM_ProgressionGolemancerAscension" : classId == "rwom.shaman" ? "TM_ProgressionShamanAscension" : classId == "rwom.brightmage" ? "TM_ProgressionBrightmageAscension" : classId == "rwom.chronomancer" ? "TM_ProgressionChronomancerAscension" : classId == "rwom.enchanter" ? "TM_ProgressionEnchanterAscension" : classId == "rwom.bloodmage" ? "TM_ProgressionBloodMageAscension" : classId == "rwom.technomancer" ? "TM_ProgressionTechnomancerAscension" : classId == "rwom.geomancer" ? "TM_ProgressionGeomancerAscension" : classId == "rwom.warlock" ? "TM_ProgressionWarlockAscension" : classId == "rwom.succubus" ? "TM_ProgressionSuccubusAscension" : classId == "rwom.druid" ? "TM_ProgressionDruidAscension" : classId == "rwom.bard" ? "TM_ProgressionBardAscension" : classId == "rwom.paladin" ? "TM_ProgressionPaladinAscension" : classId == "rwom.summoner" ? "TM_ProgressionSummonerAscension" : classId == "rwom.arcanist" ? "TM_ProgressionArcanistAscension" : classId == "rwom.lightning" ? "TM_ProgressionLightningAscension" : classId == "rwom.ice" ? "TM_ProgressionIceAscension" : classId == "rwom.fire" ? "TM_ProgressionFireAscension"
                : classId == "rwom.necromancer" ? "TM_ProgressionNecromancerAscension" : "TM_ProgressionAscension";
            string label = (keyPrefix + "Action").Translate();
            if (circle == null || !circle.Spawned || circle.Map != pawn.Map
                || circle is Building_TMMagicCircle busy && (busy.IsActive || busy.IsPending || busy.MageList.Count > 0)
                || !pawn.CanReserve(circle) || !pawn.CanReach(circle, PathEndMode.InteractionCell, Danger.Some))
            {
                yield return new FloatMenuOption(label + " (" + "TM_ProgressionAscensionUnavailable".Translate() + ")", null);
                yield break;
            }
            var offering = ClassAscension.FindClassOffering(pawn, circle, classId);
            if (offering == null)
            {
                yield return new FloatMenuOption(label + " (" + (keyPrefix + "NeedsOffering").Translate() + ")", null);
                yield break;
            }
            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("TM_ClassAscension");
            if (jobDef == null || !ClassAscension.CanBegin(pawn, circle, offering))
            {
                yield return new FloatMenuOption(label + " (" + "TM_ProgressionAscensionUnavailable".Translate() + ")", null);
                yield break;
            }
            yield return new FloatMenuOption(label, () =>
            {
                // Recheck the selected pawn and exact offering after the menu was opened.
                if (!ClassAscension.CanBegin(pawn, circle, offering)) return;
                var job = JobMaker.MakeJob(jobDef, circle, offering);
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}
