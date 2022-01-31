using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemWorkstationEffect_LaunchThing : GolemWorkstationEffect
    {
        public ThingDef thing = null;
        public int ticksBetweenBursts;
        public float missRadius;
        public float hitChance;
        public float maxRange;
        public float minRange;        

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksBetweenBursts, "ticksBetweenBursts");
            Scribe_Values.Look<float>(ref this.missRadius, "missRadius");
            Scribe_Values.Look<float>(ref this.hitChance, "hitChance");
            Scribe_Values.Look<float>(ref this.maxRange, "maxRange");
            Scribe_Values.Look<float>(ref this.minRange, "minRange");            
            Scribe_Defs.Look<ThingDef>(ref this.thing, "thing");
        }

        public override void ContinueEffect(Building_TMGolemBase golem_building)
        {
            base.ContinueEffect(golem_building);
            if (Find.TickManager.TicksGame % ticksBetweenBursts == 0)
            {
                if (Rand.Chance(hitChance * LevelModifier))
                {
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(thing, golem_building, target, target, ProjectileHitFlags.IntendedTarget);
                }
                else
                {
                    LocalTargetInfo newCell = TM_Calc.FindValidCellWithinRange(target.Cell, golem_building.Map, missRadius);
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(thing, golem_building, newCell, target, ProjectileHitFlags.All);
                }
            }
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            if (target != null && target.HasThing && !golem_building.holdFire && golem_building.GolemComp.TargetIsValid(golem_building, target.Thing) && (target.Cell - golem_building.Position).LengthHorizontal <= maxRange && (target.Cell - golem_building.Position).LengthHorizontal >= minRange)
            {
                return base.CanDoEffect(golem_building);
            }
            return false;
        }
    }
}
