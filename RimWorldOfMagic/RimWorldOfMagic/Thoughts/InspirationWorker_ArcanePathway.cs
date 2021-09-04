using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class InspirationWorker_ArcanePathway : InspirationWorker
    {
        public override bool InspirationCanOccur(Pawn pawn)
        {
            bool flag = ModsConfig.IdeologyActive;
            if(flag && pawn.story != null && pawn.story.traits != null && pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Gifted))
            {
                return base.InspirationCanOccur(pawn);
            }
            return false;
        }
    }
}
