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
    public class GolemWorkstationEffect_DestroyFilth : GolemWorkstationEffect
    {        

        public float maxRange;
        public int filthPerIteration;

        public override void StartEffect(Building_TMGolemBase golem_building, TM_GolemUpgrade upgrade, float effectLevel = 1)
        {
            base.StartEffect(golem_building, upgrade, effectLevel);

            List<Thing> allThings = golem_building.Map.listerFilthInHomeArea.FilthInHomeArea;
            List<Thing> allFilth = new List<Thing>();
            allFilth.Clear();            
            if(allThings != null && allThings.Count > 0)
            { 
                for(int i = 0; i < allThings.Count; i++)
                {
                    if( TM_Calc.HasLoSFromTo(golem_building.Position, allThings[i].Position, golem_building, 0, maxRange))
                    {
                        allFilth.Add(allThings[i]);
                    }
                }
            }
            int count = filthPerIteration > allFilth.Count ? allFilth.Count : filthPerIteration;
            for (int i = 0; i < count; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlueSwirl, allFilth[i].DrawPos, golem_building.Map, Rand.Range(.3f, .35f), .4f, .1f, .25f, Rand.Range(-400, -200), 0f, 0, Rand.Range(0, 360));
                
                allFilth[i].Destroy(DestroyMode.Vanish);
            }
            if (count > 0)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlueSwirl, golem_building.DrawPos, golem_building.Map, 1f, .5f, .1f, .5f, -250, 0, 0, Rand.Range(0, 360));
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BlueSwirl, golem_building.DrawPos, golem_building.Map, .6f, .3f, .3f, .6f, 150, 0, 0, Rand.Range(0, 360));
            }
        }
        
    }
}
