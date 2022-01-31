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
    public class GolemWorkstationEffect_RegulateTemperature : GolemWorkstationEffect
    {

        public float maxHeat = 100;
        public float minHeat = -100;
        public float maxHeatAbsPerEffect = 200;
        public bool canHeat = true;
        public bool canCool = true;

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {            
            golem_building.canRegulateTemp = true;
            if (CanPushHeat(golem_building))
            {
                base.StartEffect(golem_building, upgrade, effectLevel);
                GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, maxHeatAbsPerEffect * HeatMultiplier(golem_building));
            }
        }
        
        private float HeatMultiplier(Building_TMGolemBase golem_building)
        {
            float val = 1f;
            val = (golem_building.tempGoal - golem_building.AmbientTemperature)/20f;  
            return val;            
        }

        private bool CanPushHeat(Building_TMGolemBase golem_building)
        {
            if(golem_building.AmbientTemperature <= golem_building.tempGoal && !canHeat)
            {
                return false;
            }
            if(golem_building.AmbientTemperature >= golem_building.tempGoal && !canCool)
            {
                return false;
            }
            if(golem_building.AmbientTemperature >= maxHeat)
            {
                return false;
            }
            else if(golem_building.AmbientTemperature <= minHeat)
            {
                return false;
            }
            return true;
        }
    }
}
