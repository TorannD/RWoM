using System.Collections.Generic;

namespace TorannMagic
{
    public class Verb_Firestorm : Verb_RankedStormField
    {
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Firestorm;
    }
}
