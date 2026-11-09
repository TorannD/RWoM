using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class BladeCareer
    {
        internal const string Id = "rwom.bladedancer";
        internal static Pawn TrialOwner(Pawn body)
        {
            ActivePath(body,out var owner);
            return owner;
        }

        internal static DisciplineProgress ActivePath(Pawn body,out Pawn owner)
        {
            owner=body;
            var local=Path(body);
            if (local!=null || body==null || !SpiritRebirthState.TryGetPossessingSpirit(body,out var soul) ||
                !SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending) return local;
            var retained=Path(soul);
            if (retained!=null) owner=soul;
            return retained;
        }
        internal const int RequiredPasses = 10;
        internal const int RequiredWeapons = 20;
        private const string Trial = "rwom.bladedancer.foundation";
        internal static DisciplineProgress Path(Pawn pawn) =>
            ClassIdentity.TryGetState(pawn, out var state) ? state.Find(Id) : null;
        internal static int PassageRank(Pawn pawn)
        {
            var path = Path(pawn);
            return path == null ? 0 : 1 + UnityEngine.Mathf.Clamp(path.BladePassageUpgrades, 0, 2);
        }
        internal static int PassageRange(Pawn pawn)
        {
            int rank = PassageRank(pawn);
            return rank == 0 ? 0 : 4 + 2 * rank;
        }
        internal static bool TrainPassage(Pawn pawn, int displayedRank)
        {
            var rank = PassageRank(pawn);
            var data = pawn.GetCompAbilityUserMight()?.ExistingMightData;
            if (rank < 1 || rank >= 3 || rank != displayedRank || data == null || data.MightAbilityPoints < 1) return false;
            Path(pawn).BladePassageUpgrades = rank;
            data.MightAbilityPoints--;
            return true;
        }
        internal static int Passes(Pawn pawn) => Path(pawn)?.BladePassages ?? 0;
        internal static int Serviced(Pawn pawn) => Path(pawn)?.BladeWeapons?.Count ?? 0;
        private static bool Training(DisciplineProgress path) =>
            path?.Stage == InitiationStage.Foundation && !path.CompletedTrials.Contains(Trial);

        internal static void Pass(Pawn pawn)
        {
            var path = Path(pawn);
            if (Training(path) && ++path.BladePassages == RequiredPasses) path.RecordCompletedTrial(Trial);
        }

        internal static bool CanService(Pawn pawn, Thing weapon)
        {
            var path = Path(pawn);
            return Training(path) && weapon?.thingIDNumber > 0 && weapon.def.IsWeapon &&
                path.BladeWeapons?.Contains(weapon.thingIDNumber) != true;
        }

        internal static bool Service(Pawn pawn, Thing weapon)
        {
            if (!CanService(pawn, weapon)) return false;
            var path = Path(pawn);
            path.BladeWeapons ??= new List<int>();
            path.BladeWeapons.Add(weapon.thingIDNumber);
            if (path.BladeWeapons.Count == RequiredWeapons) path.RecordCompletedTrial(Trial);
            return true;
        }

        internal static bool Ready(Pawn pawn)
        {
            var path = Path(pawn);
            return path?.Stage == InitiationStage.Foundation && path.CompletedTrials.Contains(Trial);
        }

        private static MightPower Spin(CompAbilityUserMight might) => TorannMagicDefOf.TM_BladeSpin == null ? null :
            might?.ExistingMightData?.MightPowersB.FirstOrDefault(p => p.TMabilityDefs.Contains(TorannMagicDefOf.TM_BladeSpin));

        internal static bool Claim(Pawn pawn)
        {
            if (!Ready(pawn)) return false;
            var might = pawn.GetCompAbilityUserMight();
            if (Spin(might)?.abilityDef == null ||
                !ClassIdentity.Component(pawn).State.TryAdvanceAfterTrial(Id, InitiationStage.Foundation, Trial)) return false;
            Restore(might);
            return true;
        }

        internal static void Restore(CompAbilityUserMight might)
        {
            var path = Path(might.Pawn);
            if (path == null) return;
            TM_ClassState.AddRuntimeAbility(might, DefDatabase<TMAbilityDef>.GetNamedSilentFail("TM_BladePassage"));
            BladeMastery.Restore(might, path);
            BladeAscension.Restore(might, path);
            if (path.Stage >= InitiationStage.Adept && Spin(might) is MightPower owner)
            {
                owner.learned = true;
                TM_ClassState.AddRuntimeAbility(might, owner.abilityDef);
            }
        }
    }
}
