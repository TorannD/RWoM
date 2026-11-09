using System.Runtime.CompilerServices;
using Verse;

namespace TorannMagic
{
    public static class SpellIds
    {
        private static readonly ConditionalWeakTable<Def, string> Ids = new();

        public static string Of(Def def)
            => def?.defName == null ? null : Ids.GetValue(def, Resolve);

        private static string Resolve(Def def)
            => CoreBridge.DeclaredId(def) ?? Convention(def);

        // These are two distinct shipped abilities: RWoM's TM_ShadowStep is
        // a mana bond, Kure's TM_Shadowstep is a stamina-based teleport.
        internal const string NativeShadowStepId = "rwom.tm_shadowstep";
        internal const string KureShadowStepId = "kure.tm_shadowstep";
        internal static string Convention(Def def) => def.defName == "TM_Shadowstep"
            ? KureShadowStepId : "rwom." + def.defName.ToLowerInvariant();
    }
}
