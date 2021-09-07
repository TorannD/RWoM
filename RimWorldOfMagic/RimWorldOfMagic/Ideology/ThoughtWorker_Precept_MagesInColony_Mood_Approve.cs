using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagesInColony_Mood_Approve : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsSlave && !p.IsQuestLodger())
            {
                //float colonists = PawnsFinder.AllMaps_SpawnedPawnsInFaction(p.Faction).Count;
                int mageCount = TM_Calc.GetMagesInFactionCount(p.Faction, false);
                if (mageCount > 15)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else if (mageCount > 7)
                {
                    return ThoughtState.ActiveAtStage(1);
                }
                else if(mageCount > 3)
                {
                    return ThoughtState.ActiveAtStage(2);
                }
                else if (mageCount > 0)
                {
                    return ThoughtState.ActiveAtStage(3);
                }
                else
                {
                    return ThoughtState.ActiveAtStage(4);
                }
            }
            return false;
        }
    }
}
