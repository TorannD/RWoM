using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritRebirthRecoveryJob
    {
        internal const string DefinitionName = "TM_SpiritRebirthRecovery";
        internal static Job Create(Pawn creator, Pawn original)
        {
            var def = DefDatabase<JobDef>.GetNamedSilentFail(DefinitionName);
            var receipt = ClassIdentity.Component(original)?.SpiritRitual;
            var circle = receipt?.RecoveryCircle;
            if (def?.driverClass != typeof(JobDriver_SpiritRebirthRecovery) || receipt == null
                || !receipt.CanOrderPaidRecovery(creator, original, circle)
                || !creator.CanReach(circle, PathEndMode.InteractionCell, Danger.Deadly)
                || (original.Spawned && !creator.CanReach(original, PathEndMode.Touch, Danger.Deadly))) return null;
            foreach (var cell in GenRadial.RadialCellsAround(circle.Position, 3.9f, true))
                if (cell.InHorDistOf(circle.Position, 3.9f) && !circle.OccupiedRect().Contains(cell) && cell != circle.InteractionCell
                    && receipt.RecoveryDestinationValid(creator, cell) && creator.CanReserve(cell, 1, -1, null, false))
                    return JobMaker.MakeJob(def, circle, original, cell);
            return null;
        }
    }
}
