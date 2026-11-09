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
    [HarmonyPatch(typeof(Verb_ShootBeam), "HitCell")]
    public static class MagicalCombatNativeBeamPatch
    {
        public static bool Prefix(Verb_ShootBeam __instance, IntVec3 cell, IntVec3 sourceCell, float damageFactor, HashSet<IntVec3> ___pathCells)
        {
            var caster = __instance.Caster;
            var map = caster?.Map;
            if (map == null || !cell.InBounds(map)) return true;
            var props = __instance.verbProps;
            float damage = props.beamTotalDamage > 0f && ___pathCells.Count > 0
                ? props.beamTotalDamage / ___pathCells.Count : props.beamDamageDef?.defaultDamage ?? 0f;
            // HitCell owns both ApplyDamage and ground fire. Every neighbor cell is a separate hit.
            return !(map.GetComponent<MapComponent_MagicalCombat>()?.TryInterceptIncoming(caster,
                sourceCell.ToVector3Shifted(), cell.ToVector3Shifted(), Math.Max(1f, damage * damageFactor)) ?? false);
        }
    }

}
