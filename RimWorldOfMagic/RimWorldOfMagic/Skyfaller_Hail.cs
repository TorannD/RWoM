using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace TorannMagic
{
    public class Skyfaller_Hail : Skyfaller
    {
        protected override void HitRoof()
        {
            if (def.skyfaller.hitRoof)
            {
                CellRect cr = this.OccupiedRect();
                if (cr.Cells.Any((IntVec3 x) => x.Roofed(base.Map)))
                {
                    RoofDef roof = cr.Cells.First((IntVec3 x) => x.Roofed(base.Map)).GetRoof(base.Map);
                    if (!roof.soundPunchThrough.NullOrUndefined())
                    {
                        roof.soundPunchThrough.PlayOneShot(new TargetInfo(base.Position, base.Map));
                    }
                    CellRect cellRect = this.OccupiedRect();
                    for (int i = 0; i < cellRect.Area * def.skyfaller.motesPerCell; i++)
                    {
                        FleckMaker.ThrowDustPuff(cellRect.RandomVector3, base.Map, 2f);
                    }
                    this.Destroy();
                }
            }
        }
    }
}
