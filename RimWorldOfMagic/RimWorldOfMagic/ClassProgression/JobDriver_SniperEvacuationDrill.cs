using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class JobDriver_SniperEvacuationDrill : JobDriver
    {
        private IntVec3 firstStation = IntVec3.Invalid;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstStation, "sniperDrillFirstStation", IntVec3.Invalid);
        }

        private static bool Station(Pawn actor, Building_Bed bed) =>
            actor?.Spawned == true && !actor.Downed && actor.IsColonistPlayerControlled &&
            bed?.Spawned == true && bed.Map == actor.Map && bed.Faction == actor.Faction && bed.def.building?.bed_humanlike == true &&
            !bed.ForPrisoners && !bed.IsForbidden(actor) && !bed.IsBurning();

        private static bool Pair(Pawn actor, Building_Bed start, Building_Bed end) =>
            Station(actor, start) && Station(actor, end) && start != end &&
            (start.Position - end.Position).LengthHorizontalSquared >= 144;

        internal static void Target(Pawn actor)
        {
            if (!SniperAscension.DrillAllowed(actor)) return;
            var map = actor.Map;
            bool Selectable(Building_Bed bed) => actor.Map == map && Find.CurrentMap == map && Station(actor, bed) &&
                actor.CanReach(bed, PathEndMode.Touch, Danger.Some);
            TargetingParameters Beds(Building_Bed start) => new TargetingParameters
            {
                canTargetBuildings = true, canTargetPawns = false, canTargetItems = false,
                validator = t => t.Thing is Building_Bed bed && Selectable(bed) && (start == null || Pair(actor, start, bed))
            };

            Messages.Message("TM_SniperDrillFirst".Translate(), MessageTypeDefOf.NeutralEvent, false);
            Find.Targeter.BeginTargeting(Beds(null), first =>
            {
                var start = first.Thing as Building_Bed;
                if (!Selectable(start)) return;
                Messages.Message("TM_SniperDrillSecond".Translate(), MessageTypeDefOf.NeutralEvent, false);
                Find.Targeter.BeginTargeting(Beds(start), second =>
                {
                    if (SniperAscension.DrillAllowed(actor) && second.Thing is Building_Bed end &&
                        Selectable(start) && Selectable(end) && Pair(actor, start, end))
                        actor.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_SniperEvacuationDrill"), start, end), JobTag.Misc);
                }, actor, requiresCastedSelected: false);
            }, actor, requiresCastedSelected: false);
        }

        private bool Valid() => SniperAscension.DrillAllowed(pawn) && Pair(pawn, job.targetA.Thing as Building_Bed, job.targetB.Thing as Building_Bed);
        // Use the approach cells. Reserving or occupying a patient’s bed would interfere with rescue.
        public override bool TryMakePreToilReservations(bool errorOnFailed) => Valid();
        private bool At(TargetIndex station) => pawn.CanReachImmediate(job.GetTarget(station), PathEndMode.Touch);
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Valid());
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(300, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A).FailOn(() => !At(TargetIndex.A));
            yield return Toils_General.Do(() =>
            {
                if (At(TargetIndex.A)) firstStation = pawn.Position;
                else EndJobWith(JobCondition.Incompletable);
            });
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.Wait(300, TargetIndex.B).WithProgressBarToilDelay(TargetIndex.B).FailOn(() => !At(TargetIndex.B));
            yield return Toils_General.Do(() =>
            {
                if (Valid() && firstStation.IsValid && (firstStation - pawn.Position).LengthHorizontalSquared >= 64 && At(TargetIndex.B))
                    SniperAscension.Drill(pawn);
                else EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
