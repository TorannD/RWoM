using System.Linq;
using RimWorld;
using Verse;

namespace TorannMagic.ClassProgression
{
    internal static class SniperFoundation
    {
        internal const string Id = "rwom.sniper";

        internal static float Offset(Pawn pawn, StatDef stat)
        {
            if (stat == null || !ClassIdentity.TryGetState(pawn, out var state) ||
                state.Find(Id) is not DisciplineProgress path || path.Domain != DisciplineDomain.Might ||
                pawn.story?.traits?.HasTrait(TorannMagicDefOf.TM_Sniper) == true) return 0f;
            if (path.SniperPatientAim && stat != StatDefOf.AimingDelayFactor) return 0f;
            int rank = pawn.GetCompAbilityUserMight()?.ExistingMightData?.MightPowerSkill_SniperFocus
                .FirstOrDefault(skill => skill.label == "TM_SniperFocus_pwr")?.level ?? 0;
            if (path.SniperPatientAim)
            {
                if (!pawn.Spawned || pawn.Downed || pawn.pather == null ||
                    pawn.pather.Moving || Find.TickManager.TicksGame - pawn.pather.LastMovedTick < 180) return 0f;
                return -.02f * UnityEngine.Mathf.Clamp(rank + 1, 1, 5);
            }
            var degrees = TorannMagicDefOf.TM_Sniper?.degreeDatas;
            var degree = degrees?.Find(d => d.degree == UnityEngine.Mathf.Clamp(rank, 0, 3));
            return degree?.statOffsets?.FirstOrDefault(offset => offset.stat == stat)?.value ?? 0f;
        }
    }
}
