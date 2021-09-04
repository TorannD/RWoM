using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_PerformedSeverMagic_Social : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn pawn, Pawn otherPawn)
        {
            Thought_Memory tm = pawn.needs?.mood?.thoughts?.memories.GetFirstMemoryOfDef(TorannMagicDefOf.TM_SeverMagic_ForVeneratedTD);
            if (tm != null && otherPawn.Ideo.GetRole(otherPawn).def == TorannMagicDefOf.TM_IdeoRole_VoidSeeker)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            tm = pawn.needs?.mood?.thoughts?.memories.GetFirstMemoryOfDef(TorannMagicDefOf.TM_SeverMagic_ForApproveTD);
            if (tm != null && otherPawn.Ideo.GetRole(otherPawn).def == TorannMagicDefOf.TM_IdeoRole_VoidSeeker)
            {
                return ThoughtState.ActiveAtStage(0);
            }            
            return false;
        }
    }
}
