using HarmonyLib;
using Verse;

namespace TorannMagic.Compatibility
{
    public static class TM_DefReload
    {
        public static void RefreshDefinitions()
        {
            TM_Data.ResetDefinitionCaches();
            TM_ClassUtility.RefreshBuiltinClassIndexes();
            if (DefDatabase<TM_CustomClassDef>.GetNamedSilentFail("TM_CustomClasses") != null)
                TM_ClassUtility.LoadCustomClasses();
        }
    }

    [HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.LoadAllPlayData))]
    public static class TM_DefReloadAfterLoad
    {
        public static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(TM_DefReload.RefreshDefinitions);
        }
    }
}
