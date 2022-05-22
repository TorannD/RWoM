using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagicUse_Know_Mood_Approve : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsQuestLodger() && !TM_Calc.IsMagicUser(p))
            {
                int eventsInLast60 = Find.HistoryEventsManager.GetRecentCountWithinTicks(TorannMagicDefOf.TM_UsedMagic, 150000);
                if (eventsInLast60 > 12)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else if (eventsInLast60 > 0)
                {
                    return ThoughtState.ActiveAtStage(1);
                }
                else
                {
                    return ThoughtState.ActiveAtStage(2);
                }
            }
            
            return false;
        }
    }
}
