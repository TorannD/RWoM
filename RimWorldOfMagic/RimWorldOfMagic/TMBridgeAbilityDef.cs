using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TorannMagic
{
    // Ф5 spike: a vanilla AbilityDef that stands in for an RWoM spell. The
    // bridge carries the stable spellId; the original TMAbilityDef (all the
    // content: verbs, costs, configs) resolves through the core registry at
    // use time. Bridge defs are data-only shims — no spell content is
    // duplicated.
    public class TMBridgeAbilityDef : AbilityDef
    {
        public string spellId;

        private TMAbilityDef original;

        public TMAbilityDef Original
        {
            get
            {
                if (original == null && !string.IsNullOrEmpty(spellId)
                    && CoreBridge.TryResolve(spellId, out Def resolved))
                {
                    original = resolved as TMAbilityDef;
                }
                return original;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (string.IsNullOrEmpty(spellId))
            {
                yield return GetType().Name + " " + defName + " has no spellId.";
            }
        }
    }
}
