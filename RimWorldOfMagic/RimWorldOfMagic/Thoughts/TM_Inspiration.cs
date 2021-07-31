using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class TM_Inspiration : Inspiration
    {
        public override void PostStart(bool sendLetter = true)
        {
            base.PostStart(sendLetter);
            if(this.def == TorannMagicDefOf.ID_Enlightened && base.pawn != null && base.pawn.health != null && base.pawn.health.hediffSet != null)
            {
                HealthUtility.AdjustSeverity(base.pawn, TorannMagicDefOf.TM_EnlightenedHD, 1f);
            }
        }

        public override void PostEnd()
        {
            base.PostEnd();
            if (this.def == TorannMagicDefOf.ID_Enlightened && base.pawn != null && base.pawn.health != null && base.pawn.health.hediffSet != null)
            {
                Hediff hd = base.pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_EnlightenedHD, false);
                if (hd != null)
                {
                    base.pawn.health.RemoveHediff(hd);
                }
            }
        }
    }
}
