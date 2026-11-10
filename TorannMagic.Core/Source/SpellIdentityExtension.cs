using System.Collections.Generic;
using Verse;

namespace TorannMagic.Core
{
    // Data-driven stable identity for spell defs. Lives IN the def XML so the
    // identity survives defName renames and DefDatabase rebuilds. Authored once
    // per spell def; the resolver (AbilityCore) consumes it.
    public class SpellIdentityExtension : DefModExtension
    {
        public string spellId;
        public string domain;   // "Magic" | "Might"
        public int tier;        // 0 = base, 1..N = level variants
    }
}
