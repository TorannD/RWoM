using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    public class CompProperties_MagicalCombatInterceptor : CompProperties_ProjectileInterceptor
    {
        public CompProperties_MagicalCombatInterceptor()
        {
            compClass = typeof(CompMagicalCombatInterceptor);
            radius = 5f;
            interceptGroundProjectiles = true;
            interceptAirProjectiles = true;
            hitPoints = -1;
            rechargeHitPointsIntervalTicks = 0;
            chargeIntervalTicks = 0;
            disarmedByEmpForTicks = 0;
            color = new Color(.6f, .25f, .9f);
        }
    }

    // Native discovery and ring rendering only. No per-emitter HP, regeneration, EMP or autonomous state.
    [StaticConstructorOnStartup]
    public class CompMagicalCombatInterceptor : CompProjectileInterceptor
    {
        static Material ringMaterial;
        static readonly MaterialPropertyBlock ringProperties = new MaterialPropertyBlock();
        public Vector3 FieldCenter => parent.Position.ToVector3Shifted();

        public override void CompTick()
        {
        }

        public override void PostDraw()
        {
            var field = parent.Map?.GetComponent<MapComponent_MagicalCombat>();
            if (field == null || !field.ShouldDrawEmitter(parent))
                return;
            if (ringMaterial == null)
                ringMaterial = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);
            var center = FieldCenter;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Color color = Props.color;
            color.a = .28f;
            ringProperties.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = default;
            matrix.SetTRS(center, Quaternion.identity, new Vector3(5f * 2f * 1.1601562f, 1f, 5f * 2f * 1.1601562f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ringMaterial, 0, null, 0, ringProperties);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        public override string CompInspectStringExtra() => null;
    }

    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.Active), MethodType.Getter)]
    public static class MagicalCombatActivePatch
    {
        public static bool Prefix(CompProjectileInterceptor __instance, ref bool __result)
        {
            if (!(__instance is CompMagicalCombatInterceptor))
                return true;
            __result = __instance.parent.Map?.GetComponent<MapComponent_MagicalCombat>()?.CanEmit(__instance.parent) ?? false;
            return false;
        }
    }

    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CheckIntercept))]
    public static class MagicalCombatProjectilePatch
    {
        static readonly FieldInfo originField = AccessTools.Field(typeof(Projectile), "origin");
        public static bool Prefix(CompProjectileInterceptor __instance, Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos, ref bool __result)
        {
            if (!(__instance is CompMagicalCombatInterceptor))
                return true;
            __result = projectile != null && (__instance.parent.Map?.GetComponent<MapComponent_MagicalCombat>()?.TryInterceptProjectile(__instance.parent, projectile, (Vector3)originField.GetValue(projectile), lastExactPos, newExactPos) ?? false);
            return false;
        }
    }

    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.CheckBombardmentIntercept))]
    public static class MagicalCombatBombardmentPatch
    {
        public static bool Prefix(CompProjectileInterceptor __instance, Bombardment bombardment, Bombardment.BombardmentProjectile projectile, ref bool __result)
        {
            if (!(__instance is CompMagicalCombatInterceptor))
                return true;
            // Native Bombardment explodes with Bomb.defaultDamage: charge that same nominal damage once.
            __result = __instance.parent.Map?.GetComponent<MapComponent_MagicalCombat>()?.TryInterceptBombardment(__instance.parent, bombardment.instigator, projectile.targetCell, DamageDefOf.Bomb.defaultDamage, projectile) ?? false;
            return false;
        }
    }

    [HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.BombardmentCanStartFireAt))]
    public static class MagicalCombatBombardmentFirePatch
    {
        public static bool Prefix(CompProjectileInterceptor __instance, ref bool __result)
        {
            if (!(__instance is CompMagicalCombatInterceptor))
                return true;
            // Random orbital fires are separate events, not the intercepted bombardment projectile.
            __result = true;
            return false;
        }
    }
}

