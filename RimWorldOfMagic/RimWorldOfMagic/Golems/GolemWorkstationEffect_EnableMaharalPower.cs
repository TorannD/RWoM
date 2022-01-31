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
    public class GolemWorkstationEffect_EnableMaharalPower : GolemWorkstationEffect
    {
        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            golem_building.Energy.canDrawPower = true;
            parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(golem_building.Energy);
            Effecter effectExit = EffecterDefOf.Skip_EntryNoDelay.Spawn();
            effectExit.Trigger(new TargetInfo(golem_building), new TargetInfo(golem_building));
            effectExit.Cleanup();
        }

        public override bool CanDoEffect(Building_TMGolemBase golem_building)
        {
            if (golem_building != null && golem_building.Map != null && golem_building.Energy != null && golem_building.Energy.StoredEnergyPct > .02f)
            {
                return !golem_building.Energy.canDrawPower;
            }
            return false;
        }
    }
}
