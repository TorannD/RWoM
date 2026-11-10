using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SpiritHostedGladiator
    {
        private static bool LocalFortitude(Pawn pawn, CompAbilityUserMight might)
        {
            return pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_HediffFortitude) ||
                JobDriver_GladiatorDefense.Path(pawn)?.Defense == GladiatorDefense.Fortitude ||
                pawn.story?.traits?.HasTrait(TorannMagicDefOf.Gladiator) == true ||
                might?.customClass?.classFighterAbilities?.Contains(TorannMagicDefOf.TM_Fortitude) == true;
        }

        internal static CompAbilityUserMight FortitudeOwner(Pawn body, Hediff receipt)
        {
            if (receipt == null || body.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_SpiritPossessionHD) != receipt ||
                !SpiritRebirthState.TryGetPossessingSpirit(body, out var soul) ||
                !SpiritRebirthState.TryGet(soul, out var state) || state.DepletionPending ||
                LocalFortitude(body, body.GetCompAbilityUserMight()) ||
                body.GetCompAbilityUserMight()?.AbilityData.AllPowers.Any(a => a?.Def == TorannMagicDefOf.TM_Fortitude) == true ||
                body.GetCompAbilityUserMagic()?.AbilityData.AllPowers.Any(a => a?.Def == TorannMagicDefOf.TM_Fortitude) == true) return null;
            var original = soul.GetCompAbilityUserMight();
            if (original?.ExistingMightData == null) return null;
            return LocalFortitude(soul, original) || original.ExistingMightData.mightPowerG?.Any(p => p?.learned == true &&
                p.TMabilityDefs?.Contains(TorannMagicDefOf.TM_Fortitude) == true) == true ? original : null;
        }
    }
}
