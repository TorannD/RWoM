using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagesInColony_Mood_Disapprove : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsSlave && !p.IsQuestLodger())
            {
                float colonists = PawnsFinder.AllMaps_SpawnedPawnsInFaction(p.Faction).Count;
                float mages = TM_Calc.GetMagesInFactionCount(p.Faction, false);
                float pctOfMages = mages / colonists;
                if (pctOfMages > .5f)
                {
                    return ThoughtState.ActiveAtStage(4);
                }
                else if(pctOfMages > .35f)
                {
                    return ThoughtState.ActiveAtStage(3);
                }
                else if (pctOfMages > .2f)
                {
                    return ThoughtState.ActiveAtStage(2);
                }
                else if (pctOfMages > .1f)
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
