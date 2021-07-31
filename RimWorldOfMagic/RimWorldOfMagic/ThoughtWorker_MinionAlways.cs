using RimWorld;
using System;
using Verse;

namespace TorannMagic
{
    class ThoughtWorker_MinionAlways : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            ThoughtState result = ThoughtState.Inactive;
            if (p != null && p.kindDef != null)
            {
                bool flag = p.kindDef.defName == "TM_Minion";

                if (flag)
                {
                    result = ThoughtState.ActiveAtStage(0);
                }
            }            
            return result;
        }
    }
}
