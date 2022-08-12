using Verse;

namespace TorannMagic
{
    internal static class MagicUserUtility
    {
        public static Need_Mana GetMana(Pawn pawn)
        {
            CompAbilityUserMagic comp;
            bool flag = (comp = pawn.GetCompAbilityUserMagic()) != null;
            Need_Mana result;
            if (flag)
            {
                result = comp.Mana;
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static CompAbilityUserMagic GetMagicUser(Pawn pawn)
        {
            CompAbilityUserMagic comp;
            bool flag = (comp = pawn.GetCompAbilityUserMagic()) != null;
            CompAbilityUserMagic result;
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
