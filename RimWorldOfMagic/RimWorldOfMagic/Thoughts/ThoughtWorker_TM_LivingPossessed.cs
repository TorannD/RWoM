using Verse;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_LivingPossessed : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.needs == null) return false;
            if (!p.RaceProps.Humanlike) return false;
            Need_Spirit nd = p.needs.TryGetNeed(TorannMagicDefOf.TM_SpiritND) as Need_Spirit;
            if(nd != null && !nd.wasDead)
            {
                return true;
            }
            return false;
        }
    }
}
