using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class GolemWorkstationEffect_RepairBuilding : GolemWorkstationEffect
    {
        public int healthRestored = 8;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.healthRestored, "healthRestored");
        }

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            FleckMaker.ThrowDustPuff(golem_building.Position, golem_building.Map, 1f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 rndPos = golem_building.DrawPos;
                rndPos.x += Rand.Range(-.3f, .3f);
                rndPos.z += Rand.Range(-.3f, .3f);
                TM_MoteMaker.ThrowSparkFlashMote(rndPos, golem_building.Map, Rand.Range(.8f, 1.2f));
            }
            golem_building.HitPoints = Mathf.RoundToInt(Mathf.Clamp(golem_building.HitPoints + (healthRestored * LevelModifier), 0, golem_building.MaxHitPoints));
            base.StartEffect(golem_building, upgrade);
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            if(golem_building.HitPoints < golem_building.MaxHitPoints)
            {
                return base.CanDoEffect(golem_building);
            }
            return false;
        }
    }
}
