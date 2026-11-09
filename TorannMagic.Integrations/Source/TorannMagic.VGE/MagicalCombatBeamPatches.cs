using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    [HarmonyPatch]
    public static class MagicalCombatArtilleryBeamPatch
    {
        public static MethodBase TargetMethod() { var type = AccessTools.TypeByName("VanillaGravshipExpanded.Projectile_ArtilleryBeam"); return type == null ? null : AccessTools.DeclaredMethod(type, "Impact"); }
        public static bool Prepare() => TargetMethod() != null;
        public static bool Prefix(Projectile __instance, bool blockedByShield, Vector3 ___origin)
        {
            if (__instance.Destroyed || __instance.Map == null) return true;
            var map = __instance.Map;
            var field = map.GetComponent<MapComponent_MagicalCombat>();
            if (blockedByShield)
            {
                if (field?.HasInterceptReceipt(__instance) != true) return true;
                __instance.Destroy(DestroyMode.Vanish);
                return false;
            }
            if (!(field?.TryInterceptIncoming(__instance.Launcher,
                ___origin, __instance.ExactPosition, __instance.DamageAmount, __instance) ?? false)) return true;
            // VGE starts fire before calling Bullet.Impact, including its blocked branch.
            __instance.Destroy(DestroyMode.Vanish);
            return false;
        }
    }

    [HarmonyPatch]
    public static class MagicalCombatAnticraftImpactPatch
    {
        public static MethodBase TargetMethod() { var type = AccessTools.TypeByName("VanillaGravshipExpanded2.Projectile_AnticraftBeam"); return type == null ? null : AccessTools.DeclaredMethod(type, "Impact"); }
        public static bool Prepare() => TargetMethod() != null;
        public static bool Prefix(Projectile __instance, bool blockedByShield, Vector3 ___origin)
        {
            // The derived impact creates the lasting strike before calling the artillery base impact.
            return MagicalCombatArtilleryBeamPatch.Prefix(__instance, blockedByShield, ___origin);
        }
    }

    [HarmonyPatch]
    public static class MagicalCombatAnticraftPulsePatch
    {
        // Fixed conservative cost per intercepted cell pulse; native explosion rolls 65..99.
        public const float PulseDamage = 100f;
        public static MethodBase TargetMethod() { var type = AccessTools.TypeByName("VanillaGravshipExpanded2.AnticraftBeamStrike"); return type == null ? null : AccessTools.DeclaredMethod(type, "Tick"); }
        public static bool Prepare() => TargetMethod() != null;
        public static bool AllowPulse(IntVec3 cell, Map map, OrbitalStrike strike)
        {
            if (map == null || !cell.InBounds(map)) return false;
            var field = map.GetComponent<MapComponent_MagicalCombat>();
            var hit = cell.ToVector3Shifted();
            if (field == null || !field.Covers(hit)) return true;
            var launcher = strike.instigator;
            if (launcher == null) return true;
            // Cross-map coordinates have no target-map meaning. Remote orbital pulses arrive from outside.
            if (launcher.Map != map) return !field.TryInterceptOrbital(launcher, hit, PulseDamage);
            var origin = launcher.Position.ToVector3Shifted();
            // No receipt: four real pulses may hit the same cell in one tick and must each pay.
            return !field.TryInterceptIncoming(launcher, origin, hit, PulseDamage);
        }
        private static bool KnownPulseLayout(List<CodeInstruction> code, int boundsIndex)
        {
            if (boundsIndex + 1 >= code.Count || !(code[boundsIndex + 1].operand is Label end)) return false;
            int fire = -1, explosion = -1, terrain = -1, endIndex = -1;
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].labels.Contains(end)) endIndex = i;
                var call = code[i].operand as MethodInfo;
                if (call == null) continue;
                if (call.DeclaringType == typeof(FireUtility) && call.Name == nameof(FireUtility.TryStartFireIn)) { if (fire >= 0) return false; fire = i; }
                if (call.DeclaringType == typeof(GenExplosion) && call.Name == nameof(GenExplosion.DoExplosion)) { if (explosion >= 0) return false; explosion = i; }
                if (call.DeclaringType?.FullName == "VanillaGravshipExpanded.DamageWorker_ExplosionDamageTerrain_Patch" && call.Name == "DamageTerrain") { if (terrain >= 0) return false; terrain = i; }
            }
            return boundsIndex < fire && fire < explosion && explosion < terrain && terrain < endIndex;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var bounds = AccessTools.Method(typeof(GenGrid), nameof(GenGrid.InBounds), new[] { typeof(IntVec3), typeof(Map) });
            var matches = code.Select((instruction, index) => new { instruction, index }).Where(x => x.instruction.Calls(bounds)).ToList();
            // Refuse an unknown mod version rather than partially shielding its damage paths.
            if (matches.Count != 1 || !KnownPulseLayout(code, matches[0].index) || matches[0].index + 1 >= code.Count ||
                (code[matches[0].index + 1].opcode != OpCodes.Brfalse && code[matches[0].index + 1].opcode != OpCodes.Brfalse_S))
            {
                Compatibility.TM_Log.Warning("[RWoM] VGE2 anticraft pulse layout changed; magic beam compatibility skipped.");
                return code;
            }
            int index = matches[0].index;
            var load = new CodeInstruction(OpCodes.Ldarg_0);
            load.labels.AddRange(code[index].labels); code[index].labels.Clear();
            load.blocks.AddRange(code[index].blocks); code[index].blocks.Clear();
            code[index].operand = AccessTools.Method(typeof(MagicalCombatAnticraftPulsePatch), nameof(AllowPulse));
            code.Insert(index, load);
            return code;
        }
    }
}




