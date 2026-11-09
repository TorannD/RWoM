using System.Reflection;
using AbilityUser;
using HarmonyLib;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch]
    internal static class FireballMasteryLaunchPatch
    {
        static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(Projectile_AbilityBase), "Launch");
        static void Postfix(Projectile_AbilityBase __instance, Thing launcher, AbilityDef abilityDef, bool __runOriginal)
        {
            if (__runOriginal && __instance is Projectile_Fireball fireball) fireball.RecordMastery(launcher as Pawn, abilityDef);
        }
    }
}
