using System;
using System.Collections.Generic;
using AbilityUser;
using HarmonyLib;
using Verse;

namespace TorannMagic.Compatibility
{
    // Flight admission is required by classic Scorn as well as beta progression.
    // Keep it outside ClassProgression so its startup switch cannot disable it.
    [HarmonyPatch(typeof(Projectile_AbilityBase), nameof(Projectile_AbilityBase.Launch), new Type[] {
        typeof(Thing), typeof(AbilityDef), typeof(UnityEngine.Vector3), typeof(LocalTargetInfo),
        typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(List<ApplyHediffs>),
        typeof(List<ApplyMentalStates>), typeof(List<SpawnThings>) })]
    public static class ScornLaunchPatch
    {
        public static void Postfix(Projectile_AbilityBase __instance, Thing launcher)
        {
            if (__instance is Projectile_Scorn scorn && launcher is Pawn body)
                scorn.BindLaunch(body);
        }
    }
}
