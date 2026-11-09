using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace TorannMagic.Thoughts
{
    public class InteractionWorker_Bard : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (initiator?.IsColonist != true || recipient?.IsColonist != true || initiator.jobs?.curDriver?.asleep == true ||
                recipient.jobs?.curDriver?.asleep == true) return 0f;
            var comp = ClassProgression.BardTrials.SocialOwner(initiator, false);
            return comp?.IsMagicUser == true && comp.MagicData.MagicPowersB.Exists(p => p.learned &&
                p.TMabilityDefs.Contains(TorannMagicDefOf.TM_Entertain)) ? 1f : 0f;
        }
    }
}
