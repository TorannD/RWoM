using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    // Optional VGE2 adapter. Ownership stays in VGE's saved innerContainer throughout landing/retry.
    public static class MagicalCombatWarpod
    {
        static bool PositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        const string TypeName = "VanillaGravshipExpanded2.Projectile_Warpod";
        const int RetryInterval = 60;
        static readonly ConditionalWeakTable<Projectile, Landing> landings = new ConditionalWeakTable<Projectile, Landing>();
        static readonly FieldInfo landedField = AccessTools.Field(typeof(Projectile), "landed");
        static readonly FieldInfo destinationField = AccessTools.Field(typeof(Projectile), "destination");
        static readonly FieldInfo originField = AccessTools.Field(typeof(Projectile), "origin");
        static readonly FieldInfo ticksField = AccessTools.Field(typeof(Projectile), "ticksToImpact");
        public sealed class Landing
        {
            public IntVec3 Cell;
            public List<Vector3> Centers = new List<Vector3>();
            public int Rotation;
            public int NextRetry;
            internal bool Busy;
        }
        public static bool IsWarpod(Projectile projectile) => projectile?.GetType().FullName == TypeName;
        public static bool IsLanded(Projectile projectile) => projectile != null && landings.TryGetValue(projectile, out _);
        static ThingOwner Contents(Projectile projectile) => (projectile as IThingHolder)?.GetDirectlyHeldThings();
        public static MethodBase Method(string name)
        {
            var type = AccessTools.TypeByName(TypeName);
            return type == null ? null : AccessTools.DeclaredMethod(type, name);
        }
        public static bool TryPlan(Projectile projectile, MapComponent_MagicalCombat field, Vector3 crossing,
            List<Vector3> centers, out Landing landing, out float damage)
        {
            landing = null;
            damage = 0f;
            if (!IsWarpod(projectile) || Contents(projectile) == null || projectile.Map == null) return false;
            var payload = AccessTools.Field(projectile.GetType(), "warpodDef")?.GetValue(projectile) as ThingDef;
            var explosive = payload?.GetCompProperties<CompProperties_Explosive>();
            damage = explosive?.damageAmountBase ?? projectile.DamageAmount;
            if (damage < 0f) damage = explosive?.explosiveDamageType?.defaultDamage ?? projectile.DamageAmount;
            if (!PositiveFinite(damage)) damage = projectile.DamageAmount;
            if (!PositiveFinite(damage) || centers == null || centers.Count == 0) return false;
            var plan = new Landing { Centers = new List<Vector3>(centers), Rotation = projectile.Rotation.AsInt };
            var root = crossing.ToIntVec3();
            var candidates = GenRadial.RadialCellsAround(root, 10f, true)
                .OrderBy(c => (c.ToVector3Shifted() - crossing).sqrMagnitude).ThenBy(c => c.x).ThenBy(c => c.z);
            foreach (var cell in candidates)
                if (SafeCell(projectile, field, plan, cell))
                {
                    plan.Cell = cell;
                    landing = plan;
                    return true;
                }
            return false;
        }
        static bool SafeCell(Projectile projectile, MapComponent_MagicalCombat field, Landing landing, IntVec3 cell, bool moved = false)
        {
            var map = projectile.Map;
            if (map == null || map.Disposed || !cell.InBounds(map) || landing.Centers == null || landing.Centers.Count == 0) return false;
            var terrain = cell.GetTerrain(map);
            if (terrain == null || terrain.exposesToVacuum || !cell.Standable(map)) return false;
            var point = cell.ToVector3Shifted();
            foreach (var center in landing.Centers)
            {
                var actualCenter = moved ? (center - landing.Cell.ToVector3Shifted()).RotatedBy(projectile.Rotation.AsAngle - new Rot4(landing.Rotation).AsAngle) + projectile.Position.ToVector3Shifted() : center;
                float x = actualCenter.x - point.x, z = actualCenter.z - point.z;
                if (x * x + z * z <= MapComponent_MagicalCombat.Radius * MapComponent_MagicalCombat.Radius) return false;
            }
            return field?.Covers(point) != true;
        }
        public static bool CanCommit(Projectile projectile, MapComponent_MagicalCombat field, Landing landing)
            => landing != null && SafeCell(projectile, field, landing, landing.Cell);
        public static void Commit(Projectile projectile, Landing landing)
        {
            // Mark before returning to native CheckForFreeInterceptBetween -> virtual Impact.
            landings.Add(projectile, landing);
            projectile.Position = landing.Cell;
            SyncPosition(projectile);
            ticksField.SetValue(projectile, 0);
            landedField.SetValue(projectile, true);
        }
        static void SyncPosition(Projectile projectile)
        {
            var direction = ((Vector3)destinationField.GetValue(projectile) - (Vector3)originField.GetValue(projectile)).Yto0();
            if (direction.sqrMagnitude < .0001f) direction = Vector3.forward;
            var destination = projectile.Position.ToVector3Shifted();
            originField.SetValue(projectile, destination - direction.normalized);
            destinationField.SetValue(projectile, destination);
        }
        public static bool Impact(Projectile projectile)
        {
            if (!landings.TryGetValue(projectile, out var landing)) return true;
            Unload(projectile, landing, true, true);
            return false;
        }
        public static bool Tick(Projectile projectile)
        {
            if (!landings.TryGetValue(projectile, out var landing)) return true;
            Unload(projectile, landing, false, true);
            return false;
        }
        static void Unload(Projectile projectile, Landing landing, bool force, bool destroyEmpty)
        {
            if (landing.Busy || projectile.Destroyed || !projectile.Spawned || projectile.Map.Disposed) return;
            SyncPosition(projectile);
            int now = Find.TickManager.TicksGame;
            if (!force && now < landing.NextRetry) return;
            landing.NextRetry = (int)Math.Min(int.MaxValue, (long)now + RetryInterval);
            var owner = Contents(projectile);
            if (owner == null) return;
            landing.Busy = true;
            try
            {
                var map = projectile.Map;
                var field = map.GetComponent<MapComponent_MagicalCombat>();
                owner.TryDropAll(projectile.Position, map, ThingPlaceMode.Near, nearPlaceValidator: c => SafeCell(projectile, field, landing, c, true), playDropSound: false);
                if (destroyEmpty && owner.Count == 0) projectile.Destroy(DestroyMode.Vanish);
            }
            finally { landing.Busy = false; }
        }
        public static bool MayDestroy(ThingWithComps thing)
        {
            if (!(thing is Projectile projectile) || !landings.TryGetValue(projectile, out var landing)) return true;
            var owner = Contents(projectile);
            if (owner == null || owner.Count == 0) return true;
            var map = projectile.Map;
            // Ordinary map removal keeps native held-pawn discovery; no global teardown veto.
            if (map == null || map.Disposed || !Find.Maps.Contains(map)) return true;
            Unload(projectile, landing, true, false);
            return owner.Count == 0;
        }
        public static void Expose(Projectile projectile)
        {
            bool marked = landings.TryGetValue(projectile, out var landing);
            Scribe_Values.Look(ref marked, "TM_magicWarpodLanded", false);
            if (!marked) return;
            if (landing == null) { landing = new Landing(); landings.Add(projectile, landing); }
            Scribe_Values.Look(ref landing.Cell, "TM_magicWarpodCell");
            Scribe_Values.Look(ref landing.NextRetry, "TM_magicWarpodRetry", 0);
            Scribe_Values.Look(ref landing.Rotation, "TM_magicWarpodRotation", 0);
            Scribe_Collections.Look(ref landing.Centers, "TM_magicWarpodBoundary", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                landing.Busy = false;
                landedField.SetValue(projectile, true);
                int now = Find.TickManager.TicksGame;
                if ((long)landing.NextRetry - now > RetryInterval) landing.NextRetry = now;
            }
        }
    }
    [HarmonyPatch]
    public static class MagicalCombatWarpodImpactPatch
    {
        public static MethodBase TargetMethod() => MagicalCombatWarpod.Method("Impact");
        public static bool Prepare() => TargetMethod() != null;
        public static bool Prefix(Projectile __instance) => MagicalCombatWarpod.Impact(__instance);
    }
    [HarmonyPatch]
    public static class MagicalCombatWarpodTickPatch
    {
        public static MethodBase TargetMethod() => MagicalCombatWarpod.Method("Tick");
        public static bool Prepare() => TargetMethod() != null;
        public static bool Prefix(Projectile __instance) => MagicalCombatWarpod.Tick(__instance);
    }
    [HarmonyPatch]
    public static class MagicalCombatWarpodSavePatch
    {
        public static MethodBase TargetMethod() => MagicalCombatWarpod.Method("ExposeData");
        public static bool Prepare() => TargetMethod() != null;
        public static void Postfix(Projectile __instance) => MagicalCombatWarpod.Expose(__instance);
    }
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.Destroy))]
    public static class MagicalCombatWarpodDestroyPatch
    {
        public static bool Prefix(ThingWithComps __instance) => MagicalCombatWarpod.MayDestroy(__instance);
    }
}
