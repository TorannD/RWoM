using Verse;
using RimWorld;
using AbilityUser;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace TorannMagic
{
    public class Projectile_FertileLands : Projectile_AbilityBase
    {
        bool initialized = false;
        Pawn caster;

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            this.caster = this.launcher as Pawn;

            if(!this.initialized)
            {
                this.initialized = true;
            }

            CompAbilityUserMagic comp = this.caster.GetCompAbilityUserMagic();
            comp.fertileLands = new List<IntVec3>();
            comp.fertileLands.Clear();
            List<IntVec3> affectedCells = new List<IntVec3>();
            affectedCells.Clear();
            affectedCells = ModOptions.Constants.GetGrowthCells();
            List<IntVec3> targetCells = GenRadial.RadialCellsAround(base.Position, 6, true).ToList();            
            for (int i = 0; i < targetCells.Count(); i++)
            {
                bool uniqueCell = true;
                for(int j =0; j < affectedCells.Count; j++)
                {
                    if(affectedCells[j] == targetCells[i])
                    {
                        uniqueCell = false;
                    }
                }
                if(uniqueCell)
                {
                    comp.fertileLands.Add(targetCells.ToArray<IntVec3>()[i]);
                }                
            }
            TM_MoteMaker.ThrowTwinkle(base.Position.ToVector3Shifted(), map, 1f);
            
            ModOptions.Constants.SetGrowthCells(comp.fertileLands);
            comp.RemovePawnAbility(TorannMagicDefOf.TM_FertileLands);
            comp.AddPawnAbility(TorannMagicDefOf.TM_DismissFertileLands);
        }       
    }
}


