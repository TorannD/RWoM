using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_IsMage_Social_Approve : ThoughtWorker_Precept_Social
    {
        protected override ThoughtState ShouldHaveThought(Pawn pawn, Pawn otherPawn)
        {
            if(TM_Calc.IsMagicUser(otherPawn))
            {
                if (TM_Calc.IsMagicUser(pawn))
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            else if(TM_Calc.IsMagicUser(pawn))
            {
                return ThoughtState.ActiveAtStage(2);
            }
            return false;
        }
    }
}
