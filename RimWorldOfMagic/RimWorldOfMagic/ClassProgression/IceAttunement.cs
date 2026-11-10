using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    // Shared Ice/Lightning/Arcanist solo alternative to field trials. Keep the Ice name for saved jobs.
    // Books still own stage advancement.
    public static class IceAttunement
    {
        public const int RitualTicks = 2500;
        public const int OfferingCount = 100;
        private static readonly HashSet<Pawn> inFlight = new HashSet<Pawn>();

        public static string CandidateClassId(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || !pawn.IsColonistPlayerControlled
                || !ClassIdentity.TryGetState(pawn, out var state) || state.Vocation != Vocation.Mage
                || (state.Disciplines.Count != 1 && !state.ElementalPair) || ClassInitiation.HasLegacyClass(pawn)) return null;
            var path = state.ElementalPair ? state.Find("rwom.ice") : state.Disciplines[0];
            return path?.Domain == DisciplineDomain.Magic
                && (path.ClassId == "rwom.ice" || path.ClassId == "rwom.lightning" || path.ClassId == "rwom.arcanist" || path.ClassId == "rwom.paladin" || path.ClassId == "rwom.summoner" || path.ClassId == "rwom.druid" || path.ClassId == "rwom.bard" || path.ClassId == "rwom.warlock" || path.ClassId == "rwom.succubus" || path.ClassId == "rwom.geomancer" || path.ClassId == "rwom.technomancer" || path.ClassId == "rwom.bloodmage" || path.ClassId == "rwom.enchanter" || path.ClassId == "rwom.chronomancer" || path.ClassId == "rwom.brightmage" || path.ClassId == "rwom.shaman" || path.ClassId == "rwom.golemancer" || path.ClassId == "rwom.empath") ? path.ClassId : null;
        }

        public static string CandidateTrialId(Pawn pawn)
        {
            var classId = CandidateClassId(pawn);
            if (classId == null) return null;
            var path = ClassIdentity.Component(pawn).State.Find(classId);
            var trial = path.Stage == InitiationStage.Foundation ? classId + ".foundation"
                : path.Stage == InitiationStage.Adept ? classId + ".mastery" : null;
            return trial != null && !path.CompletedTrials.Contains(trial) ? trial : null;
        }

        public static bool CanBegin(Pawn pawn, Thing circle, Thing silver)
        {
            if (CandidateTrialId(pawn) == null || circle == null || circle.Destroyed || !circle.Spawned
                || circle.Map != pawn.Map || circle.def?.thingClass != typeof(Building_TMMagicCircle)
                || circle.Faction != pawn.Faction || circle.IsForbidden(pawn)
                || silver == null || silver.Destroyed || silver.stackCount < OfferingCount
                || silver.def?.defName != "Silver" || !silver.def.destroyable || silver.IsForbidden(pawn)) return false;
            if (circle is Building_TMMagicCircle busy && (busy.IsActive || busy.IsPending || busy.MageList.Count > 0)) return false;
            return silver.Spawned ? silver.Map == pawn.Map : silver.ParentHolder == pawn.carryTracker;
        }

        public static Thing FindOffering(Pawn pawn, Thing circle)
        {
            if (CandidateTrialId(pawn) == null || circle?.Map != pawn.Map) return null;
            var carried = pawn.carryTracker?.CarriedThing;
            if (CanBegin(pawn, circle, carried)) return carried;
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("Silver");
            if (def == null) return null;
            return pawn.Map.listerThings.ThingsOfDef(def)
                .Where(t => CanBegin(pawn, circle, t) && pawn.CanReserve(t, 1, OfferingCount)
                    && pawn.CanReach(t, PathEndMode.Touch, Danger.Some))
                .OrderBy(t => pawn.Position.DistanceToSquared(t.Position)).FirstOrDefault();
        }

        public static IEnumerable<FloatMenuOption> Options(Pawn pawn, Thing circle)
        {
            if (CandidateTrialId(pawn) == null) yield break;
            string keyPrefix = CandidateClassId(pawn) == "rwom.empath" ? "TM_ProgressionEmpathAttunement" : CandidateClassId(pawn) == "rwom.golemancer" ? "TM_ProgressionGolemancerAttunement" : CandidateClassId(pawn) == "rwom.shaman" ? "TM_ProgressionShamanAttunement" : CandidateClassId(pawn) == "rwom.brightmage" ? "TM_ProgressionBrightmageAttunement" : CandidateClassId(pawn) == "rwom.chronomancer" ? "TM_ProgressionChronomancerAttunement" : CandidateClassId(pawn) == "rwom.enchanter" ? "TM_ProgressionEnchanterAttunement" : CandidateClassId(pawn) == "rwom.bloodmage" ? "TM_ProgressionBloodMageAttunement" : CandidateClassId(pawn) == "rwom.technomancer" ? "TM_ProgressionTechnomancerAttunement" : CandidateClassId(pawn) == "rwom.geomancer" ? "TM_ProgressionGeomancerAttunement" : CandidateClassId(pawn) == "rwom.warlock" ? "TM_ProgressionWarlockAttunement" : CandidateClassId(pawn) == "rwom.succubus" ? "TM_ProgressionSuccubusAttunement" : CandidateClassId(pawn) == "rwom.druid" ? "TM_ProgressionDruidAttunement" : CandidateClassId(pawn) == "rwom.bard" ? "TM_ProgressionBardAttunement" : CandidateClassId(pawn) == "rwom.paladin" ? "TM_ProgressionPaladinAttunement" : CandidateClassId(pawn) == "rwom.summoner" ? "TM_ProgressionSummonerAttunement" : CandidateClassId(pawn) == "rwom.arcanist" ? "TM_ProgressionArcanistAttunement" : CandidateClassId(pawn) == "rwom.lightning"
                ? "TM_ProgressionLightningAttunement" : "TM_ProgressionIceAttunement";
            string label = (keyPrefix + "Action").Translate();
            if (circle == null || !circle.Spawned || circle.Map != pawn.Map
                || circle is Building_TMMagicCircle busy && (busy.IsActive || busy.IsPending || busy.MageList.Count > 0)
                || !pawn.CanReserve(circle) || !pawn.CanReach(circle, PathEndMode.InteractionCell, Danger.Some))
            {
                yield return new FloatMenuOption(label + " (" + "TM_ProgressionAscensionUnavailable".Translate() + ")", null);
                yield break;
            }
            var offering = FindOffering(pawn, circle);
            if (offering == null)
            {
                yield return new FloatMenuOption(label + " (" + (keyPrefix + "NeedsOffering").Translate() + ")", null);
                yield break;
            }
            var jobDef = DefDatabase<JobDef>.GetNamedSilentFail("TM_IceAttunement");
            if (jobDef == null || !CanBegin(pawn, circle, offering))
            {
                yield return new FloatMenuOption(label + " (" + "TM_ProgressionAscensionUnavailable".Translate() + ")", null);
                yield break;
            }
            yield return new FloatMenuOption(label, () =>
            {
                if (!CanBegin(pawn, circle, offering)) return;
                var job = JobMaker.MakeJob(jobDef, circle, offering);
                job.count = OfferingCount;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }

        private static void RestoreUnpaidSilver(Pawn pawn, Thing payment, ThingOwner holder)
        {
            if (payment.Destroyed || payment.Spawned || payment.holdingOwner != null) return;
            // Avoid stack absorption, which would call the same failing Destroy callback.
            if (holder != null)
            {
                try { holder.TryAdd(payment, false); }
                catch { if (payment.holdingOwner != null || payment.Spawned) return; }
                if (payment.holdingOwner != null || payment.Spawned) return;
            }
            if (pawn.inventory == null) pawn.inventory = new Pawn_InventoryTracker(pawn);
            pawn.inventory.innerContainer.TryAdd(payment, false);
            if (payment.holdingOwner == null)
                throw new System.InvalidOperationException("Could not retain unpaid ice attunement silver.");
        }
        public static bool Complete(Pawn pawn, Thing circle, Thing silver)
        {
            if (pawn == null || !inFlight.Add(pawn)) return false;
            try
            {
                if (!CanBegin(pawn, circle, silver) || pawn.Position != circle.InteractionCell
                    || silver.ParentHolder != pawn.carryTracker || silver.Spawned) return false;
                var path = ClassIdentity.Component(pawn).State.Find(CandidateClassId(pawn));
                var trial = CandidateTrialId(pawn);
                var holder = silver.holdingOwner;
                var payment = silver.SplitOff(OfferingCount);
                if (payment == null) return false;
                try { payment.Destroy(DestroyMode.Vanish); }
                catch
                {
                    if (!payment.Destroyed)
                    {
                        RestoreUnpaidSilver(pawn, payment, holder);
                        return false;
                    }
                }
                if (!payment.Destroyed)
                {
                    RestoreUnpaidSilver(pawn, payment, holder);
                    return false;
                }
                return path.RecordCompletedTrial(trial);
            }
            finally { inFlight.Remove(pawn); }
        }
    }
}


