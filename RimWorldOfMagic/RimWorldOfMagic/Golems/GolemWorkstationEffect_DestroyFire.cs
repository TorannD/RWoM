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
    public class GolemWorkstationEffect_DestroyFire : GolemWorkstationEffect
    {        

        public float maxRange;
        public int firesPerIteration;

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            base.StartEffect(golem_building, upgrade, effectLevel);

            List<Thing> allThings = new List<Thing>();
            List<Thing> allFilth = new List<Thing>();
            allThings.Clear();
            allFilth.Clear();
            List<IntVec3> cellsAround = GenRadial.RadialCellsAround(golem_building.Position, maxRange, true).ToList();
            for (int i = 0; i < cellsAround.Count; i++)
            {
                allThings = cellsAround[i].GetThingList(golem_building.Map);
                for (int j = 0; j < allThings.Count; j++)
                {
                    if (allThings[j].def == ThingDefOf.Fire)
                    {
                        allFilth.Add(allThings[j]);
                    }
                }
            }
            int count = firesPerIteration > allFilth.Count ? allFilth.Count : firesPerIteration;
            for (int i = 0; i < count; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_RedSwirl, allFilth[i].DrawPos, golem_building.Map, Rand.Range(.3f, .35f), .4f, .1f, .25f, Rand.Range(-400, -200), 0f, 0, Rand.Range(0, 360));
                
                allFilth[i].Destroy(DestroyMode.Vanish);
            }
            if (count > 0)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_RedSwirl, golem_building.DrawPos, golem_building.Map, 1f, .5f, .1f, .5f, -250, 0, 0, Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_RedSwirl, golem_building.DrawPos, golem_building.Map, .6f, .3f, .3f, .6f, 150, 0, 0, Rand.Range(0, 360));
            }
        }
        
    }
}
