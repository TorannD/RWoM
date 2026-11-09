using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    public sealed class JobDriver_WandererSurvey : JobDriver
    {
        private static string Trial(DisciplineProgress path) => path.Stage == InitiationStage.Foundation
            ? "rwom.wanderer.survey" : "rwom.wanderer.expedition";

        internal static bool Eligible(Pawn pawn)
            => pawn?.Spawned == true && !pawn.Dead && !pawn.Downed && pawn.IsColonistPlayerControlled
                && ClassIdentity.TryGetState(pawn, out var state)
                && state.Find("rwom.wanderer") is DisciplineProgress path && (path.Stage == InitiationStage.Foundation || path.Stage == InitiationStage.Adept)
                && !(path.Lessons?.Remaining > 0) && !path.CompletedTrials.Contains(Trial(path));

        private static bool Route(Pawn pawn, IntVec3 origin, IntVec3 goal)
            => origin.IsValid && goal.IsValid && (goal - origin).LengthHorizontalSquared >= (WandererLessons.Path(pawn)?.Stage == InitiationStage.Adept ? 576 : 144);

        internal static void SelectSite(Pawn pawn)
        {
            var mana = pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_Mana);
            var stamina = pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_Stamina);
            var route = ClassIdentity.Component(pawn).State.Vocation;
            if (((route & Vocation.Mage) != 0 && !Ready(mana)) || ((route & Vocation.Warrior) != 0 && !Ready(stamina)))
            {
                Messages.Message("TM_WandererSurveyInfo".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            var map = pawn.Map;
            bool Site(IntVec3 cell) => Eligible(pawn) && pawn.Map == map && Find.CurrentMap == map
                && cell.InBounds(map) && Route(pawn, pawn.Position, cell) && !cell.Fogged(map)
                && cell.Standable(map) && !cell.IsForbidden(pawn);
            Find.Targeter.BeginTargeting(new TargetingParameters {
                canTargetLocations = true, canTargetPawns = false, canTargetItems = false, canTargetBuildings = false,
                validator = target => Site(target.Cell)
            }, target => {
                if (Site(target.Cell) && pawn.CanReserveAndReach(target.Cell, PathEndMode.OnCell, Danger.Some))
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TM_WandererSurvey"),
                        target.Cell, pawn.Position), JobTag.Misc);
            });
        }

        private static bool Ready(Need resource)
            => resource != null && resource.MaxLevel > 0 && resource.CurLevel >= resource.MaxLevel * .1f;

        internal static bool Complete(Pawn pawn, IntVec3 origin, IntVec3 goal)
        {
            if (!Eligible(pawn) || !Route(pawn, origin, goal) || pawn.Position != goal) return false;
            var mana = pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_Mana);
            var stamina = pawn.needs?.TryGetNeed(TorannMagicDefOf.TM_Stamina);
            var state = ClassIdentity.Component(pawn).State;
            bool useMana = (state.Vocation & Vocation.Mage) != 0;
            bool useStamina = (state.Vocation & Vocation.Warrior) != 0;
            if ((useMana && !Ready(mana)) || (useStamina && !Ready(stamina))) return false;
            var path = state.Find("rwom.wanderer");
            if (!path.RecordCompletedTrial(Trial(path))) return false;
            if (useMana) mana.CurLevel -= mana.MaxLevel * .1f;
            if (useStamina) stamina.CurLevel -= stamina.MaxLevel * .1f;
            return true;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
            => Eligible(pawn) && Route(pawn, job.targetB.Cell, job.targetA.Cell)
                && pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Eligible(pawn) || job.targetA.Cell.IsForbidden(pawn));
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            yield return Toils_General.Wait(180, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(() => {
                if (!Complete(pawn, job.targetB.Cell, job.targetA.Cell))
                    EndJobWith(JobCondition.Incompletable);
            });
        }
    }
}
