using Verse;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using Verse.Sound;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMTotem_Earth : Building
    {

        private int nextSearch = 0;
        private float range = 5;
        private bool initialized = false;
        public int pwrVal = 0;
        public int verVal = 0;
        Pawn target = null;

        public override void Tick()
        {
            if(!initialized)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(160, 220);
                initialized = true;
            }
            else if(Find.TickManager.TicksGame >= this.nextSearch)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(160, 220);               

                ScanForTarget();
                if (target != null)
                {
                    List<IntVec3> targetCells = GenRadial.RadialCellsAround(this.Position, range, false).ToList();
                    IntVec3 curCell = default(IntVec3);
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Position, this.Map, false), MaintenanceType.None);
                    info.pitchFactor = .5f;
                    info.volumeFactor = 1.2f;
                    SoundDefOf.Crunch.PlayOneShot(info);
                    for (int i = 0; i < targetCells.Count(); i++)
                    {
                        curCell = targetCells[i];
                        if (curCell.IsValid && curCell.InBoundsWithNullCheck(this.Map))
                        {
                            List<Thing> thingList = curCell.GetThingList(this.Map);
                            for (int j = 0; j < thingList.Count(); j++)
                            {
                                if (thingList[j] is Pawn)
                                {
                                    Pawn p = thingList[j] as Pawn;
                                    TM_Action.DamageEntities(p, null, Rand.Range(2f,5f), DamageDefOf.Crush, this);
                                    p.stances.StaggerFor(Rand.Range(60, 90) + (3 *pwrVal));                                    
                                }
                            }                            
                        }
                        if(Rand.Chance(.35f))
                        {
                            Find.CameraDriver.shaker.DoShake(4);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ThickDust, curCell.ToVector3(), this.Map, Rand.Range(.4f, 1.2f), Rand.Range(.2f, 1f), .3f, 1f, Rand.Range(-30, 30), 1f, 25f, Rand.Range(0, 360));
                        }
                    }

                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_EarthCrack, this.Position.ToVector3Shifted(), this.Map, range+1, .25f, .15f, .75f, 0, 0, 0, Rand.Range(0, 360));
                    
                }                               
            }
            base.Tick();
        }

        private void ScanForTarget()
        {            
            //Log.Message("totem has faction " + this.Faction);
            target = TM_Calc.FindNearbyEnemy(this.Position, this.Map, this.Faction, this.range, 0);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            base.ExposeData();
        }
    }
}
