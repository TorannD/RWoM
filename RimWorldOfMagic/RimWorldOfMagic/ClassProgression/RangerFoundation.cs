using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.ClassProgression
{
    internal static class RangerFoundation
    {
        private static int Training(Pawn pawn) =>
            pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightPowerSkill_RangerTraining
                .FirstOrDefault(s => s.label == "TM_RangerTraining_pwr")?.level ?? 0;

        internal static float Offset(Pawn pawn, StatDef stat)
        {
            if (stat==null) return 0f;
            var path = RangerCareer.ActivePath(pawn,out var owner);
            if (path == null || path.RangerTrailLore || pawn.story?.traits?.HasTrait(TorannMagicDefOf.Ranger) == true) return 0f;
            int rank=UnityEngine.Mathf.Clamp(Training(owner),0,3);
            var degree = TorannMagicDefOf.Ranger?.degreeDatas?.Find(d => d.degree == rank);
            return degree?.statOffsets?.FirstOrDefault(s => s.stat == stat)?.value ?? 0f;
        }

        internal static float Factor(Pawn pawn, StatDef stat)
        {
            if (stat == null || stat != StatDefOf.MoveSpeed || pawn?.Spawned!=true || pawn.Dead || pawn.Downed ||
                pawn.Drafted || pawn.carryTracker?.CarriedThing is not Thing cargo || cargo is Pawn ||
                !(pawn.jobs?.curDriver is JobDriver_HaulToCell || pawn.jobs?.curDriver is JobDriver_HaulToContainer) ||
                RangerCareer.ActivePath(pawn,out var owner)?.RangerTrailLore!=true ||
                pawn.story?.traits?.HasTrait(TorannMagicDefOf.Ranger)==true || pawn.Position.Roofed(pawn.Map)) return 1f;
            return 1f + .02f * UnityEngine.Mathf.Clamp(Training(owner) + 1, 1, 5);
        }
    }
}
