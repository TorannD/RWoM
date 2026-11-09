using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TorannMagic.Odyssey
{
    [Flags]
    public enum TravelEnvironmentHazard
    {
        None = 0,
        InvalidDestination = 1,
        Vacuum = 2,
        Cold = 4,
        Heat = 8
    }

    public readonly struct TravelEnvironmentResult
    {
        public TravelEnvironmentHazard Hazards { get; }
        public bool ShellExpiring { get; }
        public bool IsSafe => Hazards == TravelEnvironmentHazard.None;
        public TravelEnvironmentResult(TravelEnvironmentHazard hazards, bool shellExpiring = false)
        {
            Hazards = hazards;
            ShellExpiring = shellExpiring;
        }
    }

    // Destination map is explicit: a passenger held by a flight carrier has no Pawn.Map.
    // This evaluator neither moves a pawn nor authorizes a job or displays a dialog.
    public static class TravelEnvironmentSafety
    {
        public const int ShellSafetyMarginTicks = 600;
        [ThreadStatic] private static ShellProjection projection;

        public static bool IsSafe(Pawn pawn, Map map, IntVec3 cell, int horizonTicks = 0)
            => Evaluate(pawn, map, cell, horizonTicks).IsSafe;

        public static TravelEnvironmentResult Evaluate(Pawn pawn, Map map, IntVec3 cell, int horizonTicks = 0)
        {
            if (pawn == null || pawn.Dead || map == null || !cell.IsValid || !cell.InBounds(map))
                return new TravelEnvironmentResult(TravelEnvironmentHazard.InvalidDestination);

            List<HediffStage> expiringStages = null;
            if (ModsConfig.OdysseyActive && pawn.health?.hediffSet != null)
            {
                long deadline = (long)ShellSafetyMarginTicks + Math.Max(0, horizonTicks);
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.def?.defName != EtherShell.HediffName) continue;
                    var timer = hediff.TryGetComp<HediffComp_Disappears>();
                    if (timer == null || timer.EffectiveTicksToDisappear > deadline || hediff.CurStage == null) continue;
                    if (expiringStages == null) expiringStages = new List<HediffStage>();
                    expiringStages.Add(hediff.CurStage);
                }
            }
            bool expiring = expiringStages != null;
            using (expiring ? new ShellProjection(pawn, expiringStages) : null)
            {
                // Native SafeTemperatureRange is the actual pawn's comfort range +/-10 degrees.
                // Uncached worker queries under projection preserve equipment, factors and clamps.
                FloatRange safeTemperature = expiring
                    ? new FloatRange(StatDefOf.ComfyTemperatureMin.Worker.GetValue(StatRequest.For(pawn)) - 10f,
                        StatDefOf.ComfyTemperatureMax.Worker.GetValue(StatRequest.For(pawn)) + 10f)
                    : pawn.SafeTemperatureRange();
                float temperature = GenTemperature.GetTemperatureForCell(cell, map);
                var hazards = TravelEnvironmentHazard.None;
                if (temperature < safeTemperature.min) hazards |= TravelEnvironmentHazard.Cold;
                if (temperature > safeTemperature.max) hazards |= TravelEnvironmentHazard.Heat;

                // GetVacuum intentionally goes through the native patched query, including oxygen fields.
                if (ModsConfig.OdysseyActive && map.Biome.inVacuum && cell.GetVacuum(map) >= .5f)
                {
                    bool harmed = expiring
                        ? !MagicalCreatureVacuum.IsNaturallyVacuumImmune(pawn)
                            && !pawn.RaceProps.IsMechanoid && !(pawn.IsMutant && !pawn.mutant.Def.breathesAir)
                            && StatDefOf.VacuumResistance.Worker.GetValue(StatRequest.For(pawn)) < 1f
                        : pawn.HarmedByVacuum;
                    if (harmed) hazards |= TravelEnvironmentHazard.Vacuum;
                }
                return new TravelEnvironmentResult(hazards, expiring);
            }
        }

        // Stage definitions and health collections remain untouched. The scope is exact-pawn,
        // exact-stage, thread-local and restored on exceptions or nested evaluations.
        private sealed class ShellProjection : IDisposable
        {
            internal readonly Pawn Pawn;
            internal readonly List<HediffStage> Stages;
            private readonly ShellProjection previous;
            internal ShellProjection(Pawn pawn, List<HediffStage> stages)
            {
                Pawn = pawn;
                Stages = stages;
                previous = projection;
                projection = this;
            }
            public void Dispose() { projection = previous; }
        }

        public static bool ProjectShellOffset(StatDef stat, HediffStage stage, Pawn pawn, ref float __result)
        {
            if (projection == null || projection.Pawn != pawn || !projection.Stages.Contains(stage)
                || (stat != StatDefOf.VacuumResistance && stat != StatDefOf.ComfyTemperatureMin)) return true;
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(HediffStatsUtility), nameof(HediffStatsUtility.GetStatOffsetForSeverity))]
    public static class TravelEnvironmentShellProjectionPatch
    {
        public static bool Prefix(StatDef stat, HediffStage stage, Pawn pawn, ref float __result)
            => TravelEnvironmentSafety.ProjectShellOffset(stat, stage, pawn, ref __result);
    }
}
