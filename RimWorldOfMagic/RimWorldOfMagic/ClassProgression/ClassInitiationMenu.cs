using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public static class ClassInitiationMenu
    {
        public static string ProfileForJob(JobDef def)
            => def?.defName == "TM_InitiateWarlock" ? "rwom.warlock"
                : def?.defName == "TM_InitiateSuccubus" ? "rwom.succubus" : null;

        public static IEnumerable<FloatMenuOption> Options(Pawn pawn, Thing book)
        {
            foreach (var profile in ClassInitiation.FoundationCandidates(pawn, book))
            {
                string id = profile.profileId;
                if (id != "rwom.warlock" && id != "rwom.succubus") continue;
                yield return new FloatMenuOption(Label(profile, book), () => Begin(pawn, book, id));
            }
        }
        public static string Label(ClassDisciplineDef profile, Thing book)
            => "TM_ProgressionChooseDemonBranch".Translate(ClassIdentity.DisciplineLabel(profile.profileId), book.LabelCap);

        public static bool Begin(Pawn pawn, Thing book, string profileId)
        {
            if (!ClassInitiation.CanUseFoundation(pawn, book, profileId, out _)) return false;
            if (profileId == "rwom.shadow" || ClassProgressionState.Elemental(profileId))
            {
                var usable = book.TryGetComp<CompUsable>();
                if (usable == null || !usable.CanBeUsedBy(pawn)) return false;
                usable.TryStartUseJob(pawn, LocalTargetInfo.Invalid);
                return true;
            }
            var def = DefDatabase<JobDef>.GetNamedSilentFail(profileId == "rwom.warlock" ? "TM_InitiateWarlock"
                : profileId == "rwom.succubus" ? "TM_InitiateSuccubus" : "");
            if (def == null || pawn.jobs == null || book.IsForbidden(pawn)) return false;
            if (book.Spawned && (book.Map != pawn.Map || !pawn.CanReserveAndReach(book, PathEndMode.Touch, Danger.Deadly))) return false;
            if (!book.Spawned && book.ParentHolder != pawn.carryTracker && book.ParentHolder != pawn.inventory) return false;
            var job = JobMaker.MakeJob(def, book); job.count = 1;
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
