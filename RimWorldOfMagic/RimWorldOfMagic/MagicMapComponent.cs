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
    public class MagicMapComponent: MapComponent
    {
        public float windSpeed = 0f;
        public int windSpeedEndTick = 0;
        public bool allowAllIncidents = false;
        public int weatherControlExpiration;

        public MagicMapComponent(Map map): base(map)
        {

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.windSpeed, "windSpeed", 0f, false);
            Scribe_Values.Look<int>(ref this.windSpeedEndTick, "windSpeedEndTick", 0, false);
            Scribe_Values.Look<bool>(ref this.allowAllIncidents, "allowAllIncidents", false, false);
            Scribe_Values.Look<int>(ref this.weatherControlExpiration, "weatherControlExpiration", 0, false);
        }

        public void ApplyComponentConditions(string condition, float value = 0f)
        {
            if(condition == "NameOfTheWind")
            {
                windSpeed = 2f;
                windSpeedEndTick = Find.TickManager.TicksGame + Rand.Range(160000, 240000);
            }
            if(condition == "ArcaneInspiration")
            {
                List<Pawn> colonists = this.map.mapPawns.FreeColonistsSpawned.InRandomOrder().ToList();
                int count = Mathf.Clamp(Rand.RangeInclusive(1, 3), 1, colonists.Count);
                for(int i =0; i < count; i++)
                {
                    InspirationDef id = TM_Calc.GetRandomAvailableInspirationDef(colonists[i]);
                    colonists[i].mindState.inspirationHandler.TryStartInspiration(id);
                }
            }
            if(condition == "AllowAllIncidents")
            {
                this.allowAllIncidents = true;
            }
        }
    }
}
