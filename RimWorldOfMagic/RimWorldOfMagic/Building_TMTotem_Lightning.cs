using Verse;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMTotem_Lightning : Building
    {

        private int nextSearch = 0;
        private float range = 40;
        private bool initialized = false;
        public int pwrVal = 0;
        public int verVal = 0;
        Pawn target = null;

        public override void Tick()
        {
            if(!initialized)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(120, 180);
                this.range = 40 + (4 * pwrVal);
                initialized = true;
            }
            else if(Find.TickManager.TicksGame >= this.nextSearch)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(120, 180);               

                ScanForTarget();
                if (target != null)
                {
                    if (TM_Calc.HasLoSFromTo(this.Position, target.Position, this, 0, range))
                    {
                        Projectile_Lightning bolt = ThingMaker.MakeThing(TorannMagicDefOf.Projectile_Lightning, null) as Projectile_Lightning;
                        bolt.pwrVal = pwrVal;
                        bolt.verVal = verVal;
                        TM_CopyAndLaunchProjectile.CopyAndLaunchProjectile(bolt, this, target, target, ProjectileHitFlags.All, null);
                    }
                }                               
            }
            base.Tick();
        }

        private void ScanForTarget()
        {            
            //Log.Message("totem has faction " + this.Faction);
            target = TM_Calc.FindNearbyEnemy(this.Position, this.Map, this.Faction, this.range, 5);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            base.ExposeData();
        }
    }
}
