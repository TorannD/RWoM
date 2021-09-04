using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TorannMagic.Ideology
{
    public class CompProperties_AbilitySpawnDetailed : CompProperties_AbilityEffect
    {
        public ThingDef spawnThingDef;
        public ThingDef stuff = null;
        public int spawnCount = 1;
        public bool assumeCasterFaction = true;
        public bool requiresLineOfSight;
        public bool allowOnBuildings;
        public bool temporary;
        public int durationTicks;

        public CompProperties_AbilitySpawnDetailed()
        {
            compClass = typeof(CompAbilityEffect_SpawnDetailed);
        }
    }
}
