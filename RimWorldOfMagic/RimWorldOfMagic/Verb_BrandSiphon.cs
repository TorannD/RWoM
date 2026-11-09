using Verse;

namespace TorannMagic
{
    public class Verb_BrandSiphon : Verb_Brand
    {
        protected override HediffDef Effect => TorannMagicDefOf.TM_SiphonBrandHD;
        internal override bool Accepts(Pawn body, Pawn target)
        {
            var magic = target?.GetCompAbilityUserMagic();
            return base.Accepts(body, target) && target.RaceProps.Humanlike && !TM_Calc.IsUndead(target)
                && magic?.Mana != null && magic.IsMagicUser;
        }
    }
}
