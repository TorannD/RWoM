using AbilityUser;
using Verse;

namespace TorannMagic.Extensions
{
    public static class ThingWithCompsExtensions
    {
        // Non-generic GetComp<CompAbilityUserMagic> for performance since isInst against generic T is slow
        public static CompAbilityUserMagic GetCompAbilityUserMagic(this ThingWithComps thingWithComps)
        {
            for (int i = 0; i < thingWithComps.AllComps.Count; i++)
            {
                if (thingWithComps.AllComps[i] is CompAbilityUserMagic comp)
                    return comp;
            }

            return default;
        }

        // Non-generic GetComp<CompAbilityUserMight> for performance since isInst against generic T is slow
        public static CompAbilityUserMight GetCompAbilityUserMight(this ThingWithComps thingWithComps)
        {
            for (int i = 0; i < thingWithComps.AllComps.Count; i++)
            {
                if (thingWithComps.AllComps[i] is CompAbilityUserMight comp)
                    return comp;
            }

            return default;
        }
    }
}
