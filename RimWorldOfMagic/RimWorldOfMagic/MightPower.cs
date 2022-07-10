using AbilityUser;
using System.Collections.Generic;

namespace TorannMagic 
{
    public class MightPower : Power
    {
        public MightPower()
        {
        }

        public MightPower(List<AbilityDef> newAbilityDefs)
        {
            this.level = 0;
            this.TMabilityDefs = newAbilityDefs;
            this.maxLevel = newAbilityDefs.Count - 1;            

            if (this.abilityDef == TorannMagicDefOf.TM_PsionicBarrier || this.abilityDef == TorannMagicDefOf.TM_PsionicBarrier_Projected)
            {
                this.learnCost = 2;
                this.costToLevel = 2;
                this.maxLevel = 1;
            }

            if (this.abilityDef == TorannMagicDefOf.TM_PistolSpec || this.abilityDef == TorannMagicDefOf.TM_RifleSpec || this.abilityDef == TorannMagicDefOf.TM_ShotgunSpec)
            {
                this.learnCost = 0;
            }
        }
    }
}
