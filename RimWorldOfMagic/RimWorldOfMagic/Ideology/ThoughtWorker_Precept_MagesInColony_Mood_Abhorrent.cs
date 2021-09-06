using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagesInColony_Mood_Abhorrent : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsSlave && !p.IsQuestLodger() && TM_Calc.HasMageInFaction(p.Faction, false))
            {                
                return ThoughtState.ActiveAtStage(1);
                
            }
            else
            {
                return ThoughtState.ActiveAtStage(0);
            }
        }
    }
}
