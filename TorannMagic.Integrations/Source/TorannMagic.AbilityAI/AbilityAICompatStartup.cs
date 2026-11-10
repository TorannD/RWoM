using AbilityAI;
using HarmonyLib;
using TorannMagic.Core;
using Verse;

namespace TorannMagic.Integrations.AbilityAICompat
{
    // Loaded only when Ability AI is active (LoadFolders IfModActive), after
    // Ability AI's own startup ([StaticConstructorOnStartup] ordering by load
    // order + loadAfter). Registers the ownership handover, the claim hook
    // and the gizmo menu patch, each once. If the provider id is already
    // taken (an assembly loaded twice), the whole handover stays with the
    // first registration.
    [StaticConstructorOnStartup]
    internal static class AbilityAICompatStartup
    {
        static AbilityAICompatStartup()
        {
            if (!CastOwnership.Register(TmAbilityAIOwnership.ProviderId, TmAbilityAIOwnership.IsOwned))
            {
                Log.Error("[RWoM Integrations] Ability AI handover already registered; skipping the duplicate.");
                return;
            }
            AbilityAIHooks.Register(TmAbilityAIHook.Instance);
            new Harmony("huginn.rwom.integrations.abilityai").PatchAll(typeof(AbilityAICompatStartup).Assembly);
            Log.Message("[RWoM Integrations] Ability AI handover active: RWoM autocast defers to Ability AI for bridged spells.");
        }
    }
}
