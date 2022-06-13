using AbilityUser;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class Projectile_WetGround : Projectile_AbilityBase
    {

        protected override void Impact(Thing hitThing)
        {

            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            CellRect cellRect = CellRect.CenteredOn(base.Position, 1);
            cellRect.ClipInsideMap(map);

            IntVec3 c = cellRect.CenterCell;
            TerrainDef terrain;
            float radius = this.def.projectile.explosionRadius;

            IntVec3 curCell;
            IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(c, radius, true);
            for (int i = 0; i < cells.Count(); i++)
            {
                curCell = cells.ToArray<IntVec3>()[i];                
                if (curCell.InBoundsWithNullCheck(map) && curCell.IsValid)
                {
                    terrain = curCell.GetTerrain(map);
                    if (terrain.defName == "Sand" || terrain.defName == "Gravel")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("Soil"));
                        FleckMaker.ThrowDustPuff(curCell, map, .75f);
                    }
                    else if (terrain.defName == "SoftSand")
                    {
                        map.terrainGrid.SetTerrain(curCell, TerrainDef.Named("Sand"));
                        FleckMaker.ThrowDustPuff(curCell, map, .75f);
                    }
                    else
                    {
                        //Messages.Message("TerraformNotSandOrGravel".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
            }
        }
    }
}
