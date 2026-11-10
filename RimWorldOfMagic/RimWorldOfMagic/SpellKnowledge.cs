using Verse;

namespace TorannMagic
{
    // One known spell: stable spellId (survives def renames), tier within the
    // family, and the three skill ranks by role. This is the persistent state —
    // the AbilityDef lookup is done at use time via AbilityCore.Resolve().
    public class SpellKnowledge : IExposable
    {
        public string spellId;
        public int tier;
        public bool learned = true;
        public bool autocast;
        public int pwr;
        public int eff;
        public int ver;

        public void ExposeData()
        {
            Scribe_Values.Look(ref spellId, "spellId");
            Scribe_Values.Look(ref tier, "tier", 0);
            Scribe_Values.Look(ref learned, "learned", true);
            Scribe_Values.Look(ref autocast, "autocast", false);
            Scribe_Values.Look(ref pwr, "pwr", 0);
            Scribe_Values.Look(ref eff, "eff", 0);
            Scribe_Values.Look(ref ver, "ver", 0);
        }
    }
}
