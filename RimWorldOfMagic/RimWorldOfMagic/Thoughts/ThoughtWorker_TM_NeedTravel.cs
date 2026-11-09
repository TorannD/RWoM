using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_NeedTravel : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.needs != null)
            {
                Need_Travel tn = p.needs.TryGetNeed(TorannMagicDefOf.TM_Travel) as Need_Travel;
                if (tn != null)
                {
                    switch (tn.CurCategory)
                    {
                        case TravelCategory.Wanderlust:
                            return ThoughtState.ActiveAtStage(0);
                        case TravelCategory.Adventuring:
                            return ThoughtState.ActiveAtStage(1);
                        default:
                            // Complacent: the satisfied middle band has no mood effect.
                            // This default used to throw, spamming red errors for every
                            // traveller in the neutral range.
                            return ThoughtState.Inactive;
                    }
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
