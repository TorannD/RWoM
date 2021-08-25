using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagicUse_Know_Mood_Abhorrent : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsQuestLodger() && !TM_Calc.IsMagicUser(p))
            {
                int eventsInLast72 = Find.HistoryEventsManager.GetRecentCountWithinTicks(TorannMagicDefOf.TM_UsedMagic, 180000);
                if (eventsInLast72 > 0)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return false;
        }
    }
}
