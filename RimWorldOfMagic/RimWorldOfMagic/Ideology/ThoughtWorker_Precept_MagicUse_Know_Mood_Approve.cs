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
                int eventsInLast24 = Find.HistoryEventsManager.GetRecentCountWithinTicks(TorannMagicDefOf.TM_UsedMagic, 60000);
                if (eventsInLast24 > 12)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else if (eventsInLast24 > 0)
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
