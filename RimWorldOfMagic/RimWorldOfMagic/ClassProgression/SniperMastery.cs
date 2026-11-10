using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SniperMastery
    {
        internal const int RequiredHits = 8;
        internal const int ShiftTicks = 2500;
        internal const int RequiredShifts = 12;
        internal const int RequiredTicks = RequiredShifts * ShiftTicks;
        private const string Trial = "rwom.sniper.mastery";

        internal static int Hits(Pawn pawn) => SniperSurvey.Path(pawn)?.FractureHits ?? 0;
        internal static int ResearchTicks(Pawn pawn) => SniperSurvey.Path(pawn)?.ResearchWorkTicks ?? 0;
        private static bool Adept(DisciplineProgress path) =>
            path?.Stage == InitiationStage.Adept && SniperSurvey.HasCombatShot(path);

        internal static bool Ready(Pawn pawn)
        {
            var path = SniperSurvey.Path(pawn);
            return Adept(path) && path.CompletedTrials.Contains(Trial);
        }

        internal static void Hit(Pawn shooter)
        {
            var path = SniperSurvey.Path(shooter);
            if (!Adept(path) || path.FractureHits >= RequiredHits || path.ResearchWorkTicks >= RequiredTicks) return;
            if (++path.FractureHits == RequiredHits) path.RecordCompletedTrial(Trial);
        }

        internal static void Research(Pawn pawn, int workTicks)
        {
            if (workTicks <= 0) return;
            var path = SniperSurvey.Path(pawn);
            if (!Adept(path) || path.FractureHits >= RequiredHits || path.ResearchWorkTicks >= RequiredTicks) return;
            path.ResearchWorkTicks += System.Math.Min(workTicks, RequiredTicks - path.ResearchWorkTicks);
            if (path.ResearchWorkTicks == RequiredTicks) path.RecordCompletedTrial(Trial);
        }

        private static MightPower Owner(CompAbilityUserMight comp) =>
            comp?.ExistingMightData?.MightPowersS.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_DisablingShot));

        internal static bool Claim(Pawn pawn)
        {
            if (!Ready(pawn)) return false;
            var comp = pawn.GetCompAbilityUserMight();
            var owner = Owner(comp);
            if (TorannMagicDefOf.TM_DisablingShot == null || owner?.abilityDef == null ||
                !ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(SniperFoundation.Id, InitiationStage.Adept, Trial))
                return false;
            Restore(comp, SniperSurvey.Path(pawn));
            return true;
        }

        internal static void Restore(CompAbilityUserMight comp, DisciplineProgress path)
        {
            if (path?.Stage < InitiationStage.Master || !SniperSurvey.HasCombatShot(path) ||
                !path.CompletedTrials.Contains(Trial) || TorannMagicDefOf.TM_DisablingShot == null) return;
            var owner = Owner(comp);
            if (owner?.abilityDef == null) return;
            owner.learned = true;
            TM_ClassState.AddRuntimeAbility(comp, owner.abilityDef);
        }
    }
}
