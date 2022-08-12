using AbilityUser;
using Verse;

namespace TorannMagic
{
    public class Verb_DismissFertileLands : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            Pawn caster = base.CasterPawn;
            Pawn pawn = this.currentTarget.Thing as Pawn;

            CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
            if(comp.IsMagicUser)
            {
                if(comp.fertileLands.Count > 0)
                {
                    for(int i = 0; i < comp.fertileLands.Count; i++)
                    {
                        ModOptions.Constants.RemoveGrowthCell(comp.fertileLands[i]);
                    }
                    comp.fertileLands.Clear();
                    comp.RemovePawnAbility(TorannMagicDefOf.TM_DismissFertileLands);
                    comp.AddPawnAbility(TorannMagicDefOf.TM_FertileLands);
                }
                else
                {
                    Log.Message("found no cells to remove");
                }
            }
            return true;
        }
    }
}
