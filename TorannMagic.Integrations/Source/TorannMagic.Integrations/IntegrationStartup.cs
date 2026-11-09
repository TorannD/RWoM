using HarmonyLib;
using TorannMagic.Core;
using Verse;

namespace TorannMagic.Integrations
{
    [StaticConstructorOnStartup]
    public static class IntegrationStartup
    {
        static IntegrationStartup()
        {
            // The probe only inspects actual pawn job/verb types, so no optional
            // VEF/VPE assembly resolution is needed even with those mods absent.
            CastOwnership.Register("rwom.integrations.vef", VefCastOwnership.IsCasting);
            new Harmony("huginn.rwom.integrations").PatchAll(typeof(IntegrationStartup).Assembly);
        }
    }
}
