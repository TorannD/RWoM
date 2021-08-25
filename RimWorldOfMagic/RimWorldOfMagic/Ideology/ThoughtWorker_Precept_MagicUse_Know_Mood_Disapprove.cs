using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagicUse_Know_Mood_Disapprove : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsQuestLodger())
            {
                int eventsInLast24 = Find.HistoryEventsManager.GetRecentCountWithinTicks(TorannMagicDefOf.TM_UsedMagic, 60000);
                if (eventsInLast24 > 15)
                {
                    return ThoughtState.ActiveAtStage(3);
                }
                else if(eventsInLast24 > 10)
                {
                    return ThoughtState.ActiveAtStage(2);
                }
                else if (eventsInLast24 > 5)
                {
                    return ThoughtState.ActiveAtStage(1);
                }
                else
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return false;
        }
    }
}
