using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class ThoughtWorker_Precept_MagicUse_Mood_Approve : ThoughtWorker_Precept
    {
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (p.IsColonist && !p.IsPrisoner && !p.IsQuestLodger() && TM_Calc.IsMagicUser(p))
            {
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                float totalPower = 0f;
                foreach(TM_EventRecords er in comp.MagicUsed)
                {
                    totalPower += er.eventPower;
                }
                if (totalPower > 2f)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else if(totalPower > 0f)
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
