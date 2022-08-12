using Verse;

namespace TorannMagic
{
    internal static class MightUserUtility
    {

        public static bool IsMightUser(this Pawn p)
        {
            bool result;
            bool flag4 = p.story.traits.HasTrait(TorannMagicDefOf.Gladiator);
            if (flag4)
            {
                result = true;
                return result;
            }

            result = false;
            return result;
        }

        public static Need_Stamina GetStamina(Pawn pawn)
        {
            CompAbilityUserMight comp;
            bool flag = (comp = pawn.GetCompAbilityUserMight()) != null;
            Need_Stamina result;
            if (flag)
            {
                result = comp.Stamina;
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static CompAbilityUserMight GetMightUser(Pawn pawn)
        {
            CompAbilityUserMight comp;
            bool flag = (comp = pawn.GetCompAbilityUserMight()) != null;
            CompAbilityUserMight result;
            if (flag)
            {
                result = comp;
            }
            else
            {
                result = null;
            }
            return result;
        }
    }
}
