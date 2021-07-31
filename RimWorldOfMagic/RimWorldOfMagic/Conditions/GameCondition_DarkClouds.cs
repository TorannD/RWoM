using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic.Conditions
{
    public class GameCondition_DarkClouds : GameCondition
    {
        private const int LerpTicks = 500;

        private SkyColorSet EclipseSkyColors = new SkyColorSet(new Color(0.362f, 0.532f, 0.454f), Color.white, new Color(0.4f, 0.4f, 0.4f), 1f);

        public override float SkyTargetLerpFactor(Map map)
        {
            return GameConditionUtility.LerpInOutValue(this, 500f);
        }

        public override SkyTarget? SkyTarget(Map map)
        {
            return new SkyTarget(0f, EclipseSkyColors, 1f, 0f);
        }

        public override void GameConditionTick()
        {
            if(Find.TickManager.TicksGame % 37 == 0)
            {
                Pawn p = this.SingleMap.mapPawns.AllPawnsSpawned.RandomElement();
                if (p.def != TorannMagicDefOf.TM_GiantSkeletonR && p.def != TorannMagicDefOf.TM_SkeletonLichR && p.def != TorannMagicDefOf.TM_SkeletonR && p.def != TorannMagicDefOf.TM_DemonR && p.def != TorannMagicDefOf.TM_LesserDemonR && p.health != null && !p.Downed && p.health.hediffSet != null && !TM_Calc.IsUndead(p))
                {
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_DeathMarkCurse, Rand.Range(6, 10));
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Disease, p.DrawPos, p.Map, 1.4f, .7f, .1f, 1f, Rand.Range(-20, 20), .2f, Rand.Range(-20, 20), Rand.Range(0, 360));
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Disease, p.DrawPos, p.Map, 1f, .5f, .1f, .5f, Rand.Range(-20, 20), .3f, Rand.Range(-30, 30), Rand.Range(0, 360));
                }
            }
            base.GameConditionTick();
        }
    }
}
