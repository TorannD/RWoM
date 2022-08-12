using System;
using Verse;
using RimWorld;

namespace TorannMagic.Thoughts
{
    public class Inspiration_MagicUser : InspirationWorker
    {
        public override bool InspirationCanOccur(Pawn pawn)
        {
            bool baseInspiration = base.InspirationCanOccur(pawn);
            bool magicInspiration = false;
            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if (!pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {
                    magicInspiration = true;
                }
            }
            return baseInspiration && magicInspiration;
        }
    }
}
