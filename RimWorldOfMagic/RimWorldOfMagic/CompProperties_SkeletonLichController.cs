using System;
using Verse;

namespace TorannMagic
{
    public class CompProperties_SkeletonLichController : CompProperties
    {
        public bool alwaysManhunter = false;
        public int maxRangeForCloseThreat = 5;
        public int maxRangeForFarThreat = 40;

        public int rangedCooldownTicks = 0;
        public int rangedBurstCount = 1;
        public int rangedTicksBetweenBursts = 0;
        public int rangedAttackDelay = 0;
        public int aoeCooldownTicks = 0;
        public int aoeAttackDelay = 0;
        public int knockbackCooldownTicks = 0;
        public int knockbackAttackDelay = 0;
        public int chargeCooldownTicks = 0;
        public int chargeAttackDelay = 0;
        public int tauntCooldownTicks = 0;
        public int tauntAttackDelay = 0;
        public float tauntChance = 1f;

        //public int rangedWarmupTicks = 0;
        //public int aoeWarmupTicks = 0;
        //public int knockbackWarmupTicks = 0;
        //public int chargeWarmupTicks = 0;

        public ThingDef rangedThingDef = TorannMagicDefOf.FlyingObject_ShadowBolt;

        public CompProperties_SkeletonLichController()
        {
            this.compClass = typeof(CompSkeletonLichController);
        }
    }
}
