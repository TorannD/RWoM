using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagesSlave_Mood_Venerated : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsSlave && !p.IsQuestLodger() && TM_Calc.HasMageInFaction(p.Faction, false, true))
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return false;
        }
    }
}
