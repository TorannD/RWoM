using RimWorld;
using Verse;

namespace TorannMagic.SihvRMagicScrollScribe
{
    public class CompUseEffect_WriteSpell_Rain : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            CompAbilityUserMagic comp = user.GetCompAbilityUserMagic();

            if (parent.def != null && comp.spell_Rain == true)
            {}
            else
            {
                Messages.Message("NotSpellKnown".Translate(), MessageTypeDefOf.RejectInput);
            }
        }
    }        
}
