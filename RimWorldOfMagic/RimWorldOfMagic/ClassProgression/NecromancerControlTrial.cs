using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    public static class NecromancerControlTrial
    {
        public const string TrialId = "rwom.necromancer.sustain-servant";
        private static DisciplineProgress Path(Pawn owner)
        {
            if (!ClassIdentity.TryGetState(owner, out var state)) return null;
            var path = state.Find("rwom.necromancer");
            return path?.Domain == DisciplineDomain.Magic && path.Stage == InitiationStage.Foundation
                && !path.CompletedTrials.Contains(TrialId) ? path : null;
        }
        public static bool IsOwnedServant(Pawn owner, Pawn servant)
            => owner != null && servant != null && !servant.Destroyed && !servant.Dead
                && servant.Faction == owner.Faction && servant.health?.hediffSet?.hediffs != null
                && servant.health.hediffSet.hediffs.Any(h => h.TryGetComp<HediffComp_Undead>()?.linkedPawn == owner);

        public static void Observe(CompAbilityUserMagic magic)
        {
            var owner = magic?.Pawn;
            var path = Path(owner);
            if (path == null || Current.Game == null || Find.TickManager == null) return;
            var progress = path.EnsureServantControl();
            Pawn selected = null;
            if (magic.supportedUndead != null)
                foreach (var thing in magic.supportedUndead)
                {
                    var servant = thing as Pawn;
                    if (!IsOwnedServant(owner, servant)) continue;
                    if (selected == null) selected = servant;
                    if (servant.ThingID == progress.SubjectId) { selected = servant; break; }
                }
            bool active = owner.Spawned && !owner.Dead && selected?.Spawned == true && owner.Map == selected.Map;
            progress.Observe(selected?.ThingID, active, Find.TickManager.TicksGame);
            if (progress.ControlledTicks >= ServantControlProgress.RequiredTicks)
                path.RecordCompletedTrial(TrialId);
        }
        public static void PauseOwner(Pawn owner)
        {
            var progress = Path(owner)?.ServantControl;
            if (progress != null) progress.Observe(progress.SubjectId, false, Current.Game == null ? 0 : Find.TickManager?.TicksGame ?? 0);
        }
        public static void ServantLifecycle(Pawn owner, Pawn servant, bool lost)
        {
            var progress = Path(owner)?.ServantControl;
            if (progress == null || servant == null || progress.SubjectId != servant.ThingID) return;
            progress.Observe(lost ? null : progress.SubjectId, false, Current.Game == null ? 0 : Find.TickManager?.TicksGame ?? 0);
        }
    }
}
