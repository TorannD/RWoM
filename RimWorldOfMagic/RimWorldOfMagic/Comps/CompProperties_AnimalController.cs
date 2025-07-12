using System;
using Verse;
using System.Collections.Generic;

namespace TorannMagic
{
    public class CompProperties_AnimalController : CompProperties
    {
        public int abilityAttemptFrequency = 300;
        public int maxRangeForCloseThreat = 5;
        public int maxRangeForFarThreat = 40;
        public List<TMAbilityDef> abilities = new List<TMAbilityDef>();

        public CompProperties_AnimalController()
        {
            this.compClass = typeof(CompAnimalController);
        }
    }
}
