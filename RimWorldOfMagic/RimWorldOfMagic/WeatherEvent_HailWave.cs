using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class WeatherEvent_HailWave : WeatherEvent
    {
        private int duration;
        private int age;
        private int eventFrequency;

        Vector3 waveDirection;
        Vector3 currentVec;
        List<IntVec3> validCells;

        public override bool Expired => age >= duration;

        public WeatherEvent_HailWave(Map map) : base(map)
        {
            duration = Rand.Range(400, 600);
            age = 0;
            eventFrequency = Rand.Range(15, 25);
            validCells = new List<IntVec3>();
            validCells.Clear();            
        }

        public override void FireEvent()
        {            
            MagicMapComponent mmc = map.GetComponent<MagicMapComponent>();
            if (mmc != null && mmc.weatherControlExpiration > Find.TickManager.TicksGame)
            {
                List<Pawn> ePawns = (from obj in map.mapPawns.AllPawnsSpawned
                                     where (obj.Faction.HostileTo(Faction.OfPlayer))
                                     select obj).ToList();
                if (ePawns != null && ePawns.Count > 0)
                {
                    Pawn ep = ePawns.RandomElement();
                    currentVec = TM_Calc.FindValidCellWithinRange(ep.Position, map, Rand.Range(20, 22)).CenterVector3;
                    Vector3 endVec = TM_Calc.FindValidCellWithinRange(ep.Position, map, Rand.Range(5, 7)).CenterVector3;
                    waveDirection = TM_Calc.GetVector(currentVec, endVec);
                }
            }
            else
            {
                currentVec = new IntVec2(Rand.Range(8, map.Size.x - 8), Rand.Range(8, map.Size.z - 8)).ToVector3();
                Vector3 endVec = TM_Calc.FindValidCellWithinRange(currentVec.ToIntVec3(), map, Rand.Range(8, 20)).CenterVector3;
                waveDirection = TM_Calc.GetVector(currentVec, endVec);
            }          
        }

        public override void WeatherEventTick()
        {            
            age++;
            if(Find.TickManager.TicksGame % eventFrequency == 0)
            {
                IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(currentVec.ToIntVec3(), 4, true);                
                validCells.Clear();
                foreach(IntVec3 c in cells)
                {
                    if(c.InBoundsWithNullCheck(map) && c.DistanceToEdge(map) > 2)
                    {
                        if (c.Roofed(map))
                        {
                            if (!c.GetRoof(map).isThickRoof)
                            {
                                validCells.Add(c);
                            }
                        }
                        else
                        {
                            validCells.Add(c);
                        }
                    }
                }                
                currentVec = currentVec + (2 *waveDirection);                
                if(!currentVec.ToIntVec3().InBoundsWithNullCheck(map))
                {                    
                    this.age = this.duration;
                }
            }

            if(Find.TickManager.TicksGame % 3 == 0)
            {
                if (validCells != null && validCells.Count > 0)
                {                    
                    SkyfallerMaker.SpawnSkyfaller(TorannMagicDefOf.TM_Hail, validCells.RandomElement(), map);                    
                }
            }
        }
    }
}
