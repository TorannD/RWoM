using Verse;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic.Thoughts
{
    public class ThoughtWorker_TM_WearingDemonhide : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            string text = null;
            int num = 0;
            List<Apparel> wornApparel = p.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (wornApparel[i].Stuff == TorannMagicDefOf.TM_Demonhide)
                {
                    if (text == null)
                    {
                        text = wornApparel[i].def.label;
                    }
                    num++;
                }
            }
            if (num == 0)
            {
                return ThoughtState.Inactive;
            }
            if (num >= 3)
            {
                return ThoughtState.ActiveAtStage(2, text);
            }
            return ThoughtState.ActiveAtStage(num - 1, text);
        }
    }
}
