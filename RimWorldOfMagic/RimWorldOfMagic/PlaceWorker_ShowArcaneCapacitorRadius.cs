using Verse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace TorannMagic
{
    class PlaceWorker_ShowArcaneCapacitorRadius : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map visibleMap = Find.CurrentMap;
            GenDraw.DrawFieldEdges(Building_TMArcaneCapacitor.PortableCellsAround(center, visibleMap));
        }
    }
}
