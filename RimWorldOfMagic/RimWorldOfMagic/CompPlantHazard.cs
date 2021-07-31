using Verse;
using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    public class CompPlantHazard : ThingComp
    {
        private int lifeSpan = 150;
        private int searchTick = 10;
        private int growthTick = 1;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.lifeSpan, "lifeSpan", 150, false);
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if(Find.TickManager.TicksGame % growthTick == 0)
            {
                Plant plant = this.parent as Plant;
                if (plant != null && plant.Growth < 1f)
                {
                    this.growthTick = Rand.Range(30, 60);
                    plant.Growth += .0735f;
                    parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);
                }
            }
            
            if (Find.TickManager.TicksGame % searchTick == 0)
            {
                Pawn touchingPawn = this.parent.Position.GetFirstPawn(this.parent.Map);
                
                this.searchTick = Rand.Range(200, 300);
                if (touchingPawn != null)
                {
                    if (touchingPawn.jobs != null && touchingPawn.CurJob.targetA != null && touchingPawn.CurJob.targetA.Cell != this.parent.Position)
                    {
                        List<BodyPartRecord> bpr = touchingPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.MovingLimbCore).ToList();
                        bpr.AddRange(touchingPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.MovingLimbDigit).ToList());
                        bpr.AddRange(touchingPawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.MovingLimbSegment).ToList());
                        if (bpr != null && bpr.Count > 0)
                        {
                            TM_Action.DamageEntities(touchingPawn, bpr.RandomElement(), Rand.Range(2, 3), DamageDefOf.Scratch, this.parent);
                        }
                        else
                        {
                            TM_Action.DamageEntities(touchingPawn, null, Rand.Range(2, 3), DamageDefOf.Scratch, this.parent);
                        }
                    }
                }
                lifeSpan--;
            }
            if(lifeSpan <= 0)
            {
                this.parent.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
