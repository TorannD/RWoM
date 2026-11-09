using Multiplayer.API;
using TorannMagic.FleshDevelopment;
namespace TorannMagic.Integrations
{
    internal static class FleshMultiplayerRegistration
    {
        public static void Register()
        {
            MP.RegisterSyncMethod(typeof(FleshActions), nameof(FleshActions.Upgrade));
            MP.RegisterSyncMethod(typeof(FleshActions), nameof(FleshActions.DebugLevelUp)).SetDebugOnly();
            MP.RegisterSyncMethod(typeof(FleshActions), nameof(FleshActions.TryActivate));
            MP.RegisterSyncMethod(typeof(FleshActions), nameof(FleshActions.TryDevour));
            MP.RegisterSyncMethod(typeof(JobDriver_FleshCast), nameof(JobDriver_FleshCast.TryStart));
            MP.RegisterSyncMethod(typeof(FleshMultiplayer), nameof(FleshMultiplayer.StopFeast));
            MP.RegisterSyncMethod(typeof(FleshActions), nameof(FleshActions.StopAll));
        }
    }
}
