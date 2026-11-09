using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TorannMagic.Odyssey
{
    [HarmonyPatch(typeof(VacuumUtility), nameof(VacuumUtility.GetVacuum))]
    public static class MagicalAtmosphereVacuumPatch
    {
        public static void Postfix(IntVec3 cell, Map map, ref float __result)
        {
            if (__result > 0f && map != null)
            {
                __result = map.GetComponent<MapComponent_GrandAtmosphereDome>()?.EffectiveVacuum(cell, __result) ?? __result;
                __result = map.GetComponent<MapComponent_MagicalAtmosphere>()?.EffectiveVacuum(cell, __result) ?? __result;
            }
        }
    }

    // Native wall fires bypass GetVacuum when an adjacent roof is absent.
    [HarmonyPatch(typeof(FireUtility), nameof(FireUtility.GetEffectiveVacuumForFire))]
    public static class MagicalAtmosphereFirePatch
    {
        public static void Postfix(IntVec3 c, Map map, ref float __result)
        {
            if (__result <= 0f || map == null)
                return;
            var edifice = c.GetEdifice(map);
            if (edifice == null || !map.Biome.inVacuum || edifice.def.passability != Traversability.Impassable || edifice.def.Fillage != FillCategory.Full)
                return;
            // Only repair the native direct-vacuum branch. Other branches already use GetVacuum.
            if (c.GetRoof(map) == null || GenAdj.CardinalDirections.Any(d => (c + d).GetRoof(map) == null))
            {
                __result = map.GetComponent<MapComponent_GrandAtmosphereDome>()?.EffectiveVacuum(c, __result) ?? __result;
                __result = map.GetComponent<MapComponent_MagicalAtmosphere>()?.EffectiveVacuum(c, __result) ?? __result;
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_FindOxygen), "TryGiveJob")]
    public static class MagicalAtmosphereFindOxygenPatch
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !ModsConfig.OdysseyActive || !pawn.Spawned || !pawn.ConcernedByVacuum || PawnUtility.PlayerForcedJobNowOrSoon(pawn))
                return;
            if (!pawn.health.hediffSet.TryGetHediff(HediffDefOf.VacuumExposure, out var exposure) || exposure.CurStageIndex < 1 || DestinationVacuum(pawn) < .5f)
                return;
            var field = pawn.Map.GetComponent<MapComponent_MagicalAtmosphere>();
            if (field == null)
                return;
            foreach (var cell in field.CoveredCells.OrderBy(c => c.DistanceToSquared(pawn.Position)))
            {
                if (cell.GetVacuum(pawn.Map) >= .5f || cell.Fogged(pawn.Map) || !cell.InAllowedArea(pawn) || !cell.Standable(pawn.Map))
                    continue;
                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                    continue;
                __result = JobMaker.MakeJob(JobDefOf.GotoOxygenatedArea, cell);
                return;
            }
        }

        public static float DestinationVacuum(Pawn pawn)
        {
            // Preserve the native decision to continue an existing oxygenated destination.
            var job = pawn.CurJob;
            if (job != null)
            {
                var targets = new[]
                {
                    job.targetA,
                    job.targetB,
                    job.targetC
                };
                var destination = IntVec3.Invalid;
                foreach (var target in targets)
                    if (!target.HasThing && target.Cell.IsValid)
                    {
                        destination = target.Cell;
                        break;
                    }

                if (!destination.IsValid)
                    foreach (var target in targets)
                        if (target.HasThing && target.Cell.IsValid)
                        {
                            destination = target.Cell;
                            break;
                        }

                if (destination.IsValid)
                    return destination.GetVacuum(pawn.Map);
            }

            return pawn.Position.GetVacuum(pawn.Map);
        }
    }
}
