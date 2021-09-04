using RimWorld;
using System.Collections.Generic;
using Verse;
namespace TorannMagic.Ideology
{
    public class TM_RitualBehaviorWorker_SeverMagic : RitualBehaviorWorker
    {
        public TM_RitualBehaviorWorker_SeverMagic()
        {
        }

        public TM_RitualBehaviorWorker_SeverMagic(RitualBehaviorDef def)
            : base(def)
        {
        }

        public override void PostCleanup(LordJob_Ritual ritual)
        {
            if (ritual != null)
            {
                Pawn warden = ritual.PawnWithRole("voidseekerId");
                Pawn pawn = ritual.PawnWithRole("mageId");
                if (pawn != null && pawn.IsPrisonerOfColony)
                {
                    WorkGiver_Warden_TakeToBed.TryTakePrisonerToBed(pawn, warden);
                    pawn.guest.WaitInsteadOfEscapingFor(1250);
                }
            }
        }
    }
}
