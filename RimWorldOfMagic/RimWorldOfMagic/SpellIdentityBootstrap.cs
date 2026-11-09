using RimWorld;
using Verse;

namespace TorannMagic
{
    // Feeds every ability def to the core registry (when the core mod is
    // present): extension spellIds from XML win, the defName convention id
    // covers every def until XML stamping lands (Ф2 item 2). Defs do not
    // change during play, so a single startup pass is enough.
    [StaticConstructorOnStartup]
    public static class SpellIdentityBootstrap
    {
        static SpellIdentityBootstrap()
        {
            if (!CoreBridge.Available) return;
            foreach (TMAbilityDef def in DefDatabase<TMAbilityDef>.AllDefsListForReading)
                CoreBridge.RegisterSpell(def);
        }
    }
}
