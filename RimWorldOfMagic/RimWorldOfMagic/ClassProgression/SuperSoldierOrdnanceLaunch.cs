using System.Reflection;
using AbilityUser;
using HarmonyLib;
using Verse;

namespace TorannMagic.ClassProgression
{
    [HarmonyPatch]
    internal static class SuperSoldierOrdnanceLaunch
    {
        private static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(Projectile_AbilityBase),"Launch");
        private static void Postfix(Projectile_AbilityBase __instance,Thing launcher,bool __runOriginal)
        {
            if(!__runOriginal || launcher is not Pawn body ||
                (__instance is not Projectile_Mk203GL && __instance is not Projectile_BreachingCharge)) return;
            var source=SuperSoldierMartialPath.Owner(body);
            // Native projectiles keep their original impact-time rules. A borrowed
            // launch must retain its training even after the soul leaves the body.
            if(source==null || source==body) return;
            if(__instance is Projectile_Mk203GL grenade) grenade.CaptureTraining(source);
            else ((Projectile_BreachingCharge)__instance).CaptureTraining(source);
        }
    }
}
