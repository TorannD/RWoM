using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace TorannMagic.Ideology
{
    public class TM_JobGiver_SeverMagic : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            //LordJob_Ritual_Mutilation lordJob_Ritual_Mutilation;
            //if (lord == null || (lordJob_Ritual_Mutilation = (lord.LordJob as LordJob_Ritual_Mutilation)) == null)
            //{
            //    return null;
            //}
            Pawn pawn2 = pawn.mindState.duty.focusSecond.Pawn;
            if (!pawn.CanReserveAndReach(pawn2, PathEndMode.ClosestTouch, Danger.None))
            {
                return null;
            }
            return JobMaker.MakeJob(TorannMagicDefOf.TM_SeverMagic, pawn2, pawn.mindState.duty.focus);
        }
    }
}
