using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SniperTraining
    {
        internal static int Rank(Pawn pawn, bool breach)
        {
            var path = pawn == null ? null : SniperSurvey.Path(pawn);
            if (!SniperSurvey.HasCombatShot(path) ||
                (!breach && path.ShotChoice != SniperShotChoice.Aimed))
                return 0;
            return 1 + UnityEngine.Mathf.Clamp(breach ? path.BreachUpgrades : path.AimedUpgrades, 0, 2);
        }

        internal static int DamagePercent(int rank) => 110 + 15 * UnityEngine.Mathf.Clamp(rank - 1, 0, 2);
        internal static int BreachPercent(int rank) => 10 + 5 * UnityEngine.Mathf.Clamp(rank - 1, 0, 2);

        internal static bool Upgrade(Pawn pawn, bool breach, int displayedRank)
        {
            var rank = Rank(pawn, breach);
            if (rank != displayedRank || rank < 1 || rank >= 3) return false;
            var data = pawn.GetCompAbilityUserMight()?.ExistingMightData;
            if (data == null || data.MightAbilityPoints < 1) return false;
            var path = SniperSurvey.Path(pawn);
            if (breach) path.BreachUpgrades = rank;
            else path.AimedUpgrades = rank;
            data.MightAbilityPoints--;
            return true;
        }
    }
}
