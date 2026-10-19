using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TorannMagic.ModCheck
{
    public static class GiddyUp
    {
        // Resolve the optional 1.7 API without requiring Giddy-Up to be installed.
        private static readonly Type storageType = AccessTools.TypeByName("GiddyUp.ExtendedDataStorage");
        private static readonly Type dataType = AccessTools.TypeByName("GiddyUp.ExtendedPawnData");
        private static readonly Type utilityType = AccessTools.TypeByName("GiddyUp.MountUtility");
        private static readonly PropertyInfo singleton = storageType == null ? null : AccessTools.Property(storageType, "Singleton");
        private static readonly MethodInfo getData = storageType == null ? null : AccessTools.Method(storageType, "GetExtendedPawnData", new[] { typeof(Pawn) });
        private static readonly PropertyInfo mountProperty = dataType == null ? null : AccessTools.Property(dataType, "Mount");
        private static readonly MethodInfo dismount = utilityType == null || dataType == null ? null : AccessTools.Method(utilityType, "InvoluntaryDismount", new[] { typeof(Pawn), typeof(Pawn), dataType });

        public static void ForceDismount(Pawn pawn)
        {
            var data = GetData(pawn);
            var mount = data == null ? null : mountProperty?.GetValue(data) as Pawn;
            if (mount != null)
                dismount?.Invoke(null, new object[] { pawn, mount, data });
        }

        public static bool IsMount(Pawn pawn)
        {
            return pawn?.CurJobDef?.defName == "Mounted";
        }

        public static Pawn GetMount(Pawn rider)
        {
            var data = GetData(rider);
            var mount = data == null ? null : mountProperty?.GetValue(data) as Pawn;
            return IsMount(mount) ? mount : null;
        }

        private static object GetData(Pawn pawn)
        {
            if (pawn == null)
                return null;
            var storage = singleton?.GetValue(null);
            return storage == null ? null : getData?.Invoke(storage, new object[] { pawn });
        }
    }
}
