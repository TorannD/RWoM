using Verse;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMHeater : Building_WorkTable
    {

        private int nextSearch = 0;
        private bool initialized = false;
        public bool defensive = false;
        public bool buffWarm = false;
        public bool boostJoy = false;

                
        public override void Tick()
        {
            if(!initialized)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(280, 320);
                initialized = true;
            }
            if(Find.TickManager.TicksGame >= this.nextSearch)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(280, 320);
                if (defensive)
                {
                    Pawn e = TM_Calc.FindNearbyEnemy(this.Position, this.Map, this.Faction, 20, 0);
                    if (e != null && TM_Calc.HasLoSFromTo(this.Position, e, this, 0, 20))
                    {
                        Projectile firebolt = ThingMaker.MakeThing(ThingDef.Named("Projectile_Firebolt"), null) as Projectile;
                        TM_CopyAndLaunchProjectile.CopyAndLaunchProjectile(firebolt, this, e, e, ProjectileHitFlags.All, null);
                    }
                }
                if (buffWarm)
                {
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 7, this.Faction, true);
                    if (pList != null && pList.Count > 0)
                    {
                        for (int i = 0; i < pList.Count; i++)
                        {
                            Pawn p = pList[i];
                            if (p.health != null && p.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_WarmHD, 0.18f);
                            }
                        }
                    }
                }
                if (boostJoy)
                {
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 7, this.Faction, true);
                    if (pList != null && pList.Count > 0)
                    {
                        for (int i = 0; i < pList.Count; i++)
                        {
                            Pawn p = pList[i];
                            if (p.needs != null && p.needs.joy != null)
                            {
                                Need joy = p.needs.TryGetNeed(NeedDefOf.Joy);
                                if(joy != null)
                                {
                                    joy.CurLevel += Rand.Range(.01f, .02f);
                                }
                            }
                        }
                    }
                }
            }
            base.Tick();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.defensive, "defensive", false, false);
            Scribe_Values.Look<bool>(ref this.boostJoy, "boostJoy", false, false);
            Scribe_Values.Look<bool>(ref this.buffWarm, "buffWarm", false, false);
            base.ExposeData();
        }
    }
}
