using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TorannMagic.Odyssey
{
    [StaticConstructorOnStartup]
    public static class ManaNetworkOverlay
    {
        public static readonly Color FillColor = new Color(.65f, .25f, .95f, .18f);
        private static Material fillMaterial;

        public static int DistanceSquared(CellRect footprint, IntVec3 cell)
        {
            int dx = Math.Max(footprint.minX - cell.x, Math.Max(0, cell.x - footprint.maxX));
            int dz = Math.Max(footprint.minZ - cell.z, Math.Max(0, cell.z - footprint.maxZ));
            return dx * dx + dz * dz;
        }

        // Same occupied rectangle and distance as network attachment, including rotations.
        public static List<IntVec3> Cells(ThingDef def, IntVec3 center, Rot4 rotation, IntVec3 mapSize)
        {
            var footprint = GenAdj.OccupiedRect(center, rotation, def.size);
            var cells = new List<IntVec3>();
            int radius = (int)MapComponent_ManaNetwork.Range;
            for (int x = Math.Max(0, footprint.minX - radius); x <= Math.Min(mapSize.x - 1, footprint.maxX + radius); x++)
                for (int z = Math.Max(0, footprint.minZ - radius); z <= Math.Min(mapSize.z - 1, footprint.maxZ + radius); z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (DistanceSquared(footprint, cell) <= radius * radius) cells.Add(cell);
                }
            return cells;
        }

        public static void Draw(ThingDef def, IntVec3 center, Rot4 rotation, Map map)
        {
            if (map == null || map != Find.CurrentMap) return;
            var cells = Cells(def, center, rotation, map.Size);
            if (fillMaterial == null) fillMaterial = SolidColorMaterials.SimpleSolidColorMaterial(FillColor);
            foreach (var cell in cells)
                GenDraw.DrawTargetHighlightWithLayer(cell, AltitudeLayer.MetaOverlays, fillMaterial);
            GenDraw.DrawFieldEdges(cells, new Color(FillColor.r, FillColor.g, FillColor.b, .7f));
        }
    }

    public class PlaceWorker_ManaNetwork : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            ManaNetworkOverlay.Draw(def, center, rot, Find.CurrentMap);
        }
    }
}
