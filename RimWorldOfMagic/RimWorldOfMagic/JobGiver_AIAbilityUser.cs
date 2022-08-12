using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TorannMagic
{
    public class JobGiver_AIAbilityUser : ThinkNode_JobGiver //not implemented, xml disabled
    {
        public override float GetPriority(Pawn pawn)
        {
            CompAbilityUserMagic magicComp = pawn.GetCompAbilityUserMagic();
            if(magicComp != null)// && magicComp.AIAbilityJob != null)
            {
                return 100f;
            }
            CompAbilityUserMight mightComp = pawn.GetCompAbilityUserMight();
            if(mightComp != null && false)
            {

            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            CompAbilityUserMagic magicComp = pawn.GetCompAbilityUserMagic();
            if (magicComp != null)// && magicComp.AIAbilityJob != null)
            {
                Log.Message("giving ai job");
                pawn.jobs.debugLog = true;
                return null;
            }
            CompAbilityUserMight mightComp = pawn.GetCompAbilityUserMight();
            if (mightComp != null && false)
            {

            }
            return null;
        }
    }
}
