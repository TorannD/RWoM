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
    public class GolemWorkstationEffect_ScarecrowGrow : GolemWorkstationEffect
    {
        float growthPerEffectTick = .001f;

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            IEnumerable<Thing> tmpThings = from t in golem_building.Map.listerThings.AllThings
                                           where (t.def.plant != null && t is Plant && (t.Position - golem_building.Position).LengthHorizontal <= 6f)
                                           select t;
            foreach (Thing t in tmpThings)
            {
                Plant p = t as Plant;
                if(p.Growth < 1f)
                {
                    //Log.Message("plant " + p.def.defName + " growth " + p.Growth + " with growth rate of " + p.GrowthRate);
                    p.Growth += (growthPerEffectTick * p.GrowthRate);
                }
            }
        }
    }
}
