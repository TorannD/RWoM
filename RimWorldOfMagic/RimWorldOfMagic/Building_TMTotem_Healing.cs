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
    public class Building_TMTotem_Healing : Building
    {

        private int nextSearch = 0;
        private float range = 40;
        private bool initialized = false;
        public int pwrVal = 0;
        public int verVal = 0;
        public float arcanePwr = 1f;
        Pawn target = null;

        public override void Tick()
        {
            if(!initialized)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(30, 40);
                this.range = 20 + pwrVal;
                initialized = true;
            }
            else if(Find.TickManager.TicksGame >= this.nextSearch)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(60, 70);

                target = TM_Calc.FindNearbyInjuredPawn(this.Position, this.Map, this.Faction, (int)range, 0f, true);
                if (target != null)
                {
                    TM_Action.DoAction_HealPawn(null, target, 1, Rand.Range(1f, 3f) * arcanePwr * (1f + (.04f * pwrVal)));
                    Vector3 totemPos = this.DrawPos;
                    totemPos.z += 1.3f;
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small, totemPos, this.Map, 1.5f, .6f, .1f, 1f, 0, 0, 0, 0);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector3 pos = target.DrawPos;
                        pos.x += Rand.Range(-.3f, .3f);
                        pos.z += Rand.Range(-.25f, .5f);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Healing_Small, pos, this.Map, (.2f * (1f + (i*.5f))), Rand.Range(.05f, .15f), Rand.Range(.05f, .25f), Rand.Range(.1f, .3f), 0, 0, 0, 0);
                    }                    
                }                               
            }
            base.Tick();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<float>(ref this.arcanePwr, "arcanePwr", 1f, false);
            base.ExposeData();
        }
    }
}
