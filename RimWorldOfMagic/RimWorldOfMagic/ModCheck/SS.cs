using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TorannMagic.ModCheck
{
    public static class SS
    {
        private static bool warnedAboutUnsupportedApi;

        public static void ClearWeaponMemory(Pawn pawn)
        {
            if (pawn?.equipment == null)
                return;

            // Sidearms is optional. Inspect the component supplied by the running mod,
            // rather than linking the obsolete Sidearms assembly shipped with old builds.
            var memory = pawn.AllComps?.FirstOrDefault(comp =>
                comp.GetType().FullName == "SimpleSidearms.rimworld.CompSidearmMemory");
            if (memory == null)
                return;

            var type = memory.GetType();
            var remembered = AccessTools.Property(type, "RememberedWeapons");
            var forget = AccessTools.GetDeclaredMethods(type).FirstOrDefault(method =>
                method.Name == "ForgetSidearmMemory" && method.GetParameters().Length == 1);
            var regenerate = AccessTools.Method(type, "GenerateRememberedWeaponsFromEquipped", Type.EmptyTypes);
            if (remembered == null || forget == null || regenerate == null)
            {
                WarnAboutUnsupportedApi();
                return;
            }

            try
            {
                var weapons = (remembered.GetValue(memory) as IEnumerable)?.Cast<object>().ToArray();
                if (weapons == null || weapons.Length == 0)
                    return;

                var argumentType = forget.GetParameters()[0].ParameterType;
                if (weapons.Any(weapon => weapon != null && !argumentType.IsInstanceOfType(weapon)))
                {
                    WarnAboutUnsupportedApi();
                    return;
                }

                // Snapshot the list: forgetting a weapon mutates it.
                foreach (var weapon in weapons)
                    forget.Invoke(memory, new[] { weapon });
                regenerate.Invoke(memory, null);
            }
            catch (TargetInvocationException exception)
            {
                Log.Warning("[A RimWorld of Magic] Simple Sidearms weapon memory update failed: " + exception.InnerException);
            }
        }

        private static void WarnAboutUnsupportedApi()
        {
            if (warnedAboutUnsupportedApi)
                return;
            warnedAboutUnsupportedApi = true;
            Log.Warning("[A RimWorld of Magic] The loaded Simple Sidearms weapon memory API is unsupported; skipping memory updates.");
        }
    }
}
