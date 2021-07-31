using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TorannMagic.Conditions
{
    public class GameCondition_DarkThunderstorm : GameCondition
    {
        private const int LerpTicks = 500;
        private int nextStrike = 10;
        public List<Pawn> enemyPawns = null;
        public Faction faction = null;

        private SkyColorSet EclipseSkyColors = new SkyColorSet(new Color(0.2f, 0.43f, 0.723f), Color.white, new Color(0.45f, 0.45f, 0.45f), 1f);

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
            if(Find.TickManager.TicksGame % nextStrike == 0)
            {
                this.nextStrike = 1000;
                List<Pawn> allPawns = this.SingleMap.mapPawns.AllPawnsSpawned;
                if (allPawns != null && allPawns.Count > 0)
                {
                    enemyPawns = new List<Pawn>();
                    enemyPawns.Clear();
                    for (int i = 0; i < allPawns.Count; i++)
                    {
                        Pawn p = allPawns[i];
                        if (p != null && !p.Downed && faction != null && p.Faction.HostileTo(faction) && !p.IsPrisoner)
                        {
                            enemyPawns.Add(p);
                        }
                    }
                }
            }
            base.GameConditionTick();
        }

        public override void ExposeData()
        {
            string facID = (faction == null) ? "null" : faction.GetUniqueLoadID();
            Scribe_Values.Look(ref facID, "faction", "null");
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (facID == "null")
                {
                    faction = null;
                }
                else if (Find.World != null && Find.FactionManager != null)
                {
                    faction = Find.FactionManager.AllFactions.FirstOrDefault((Faction fa) => fa.GetUniqueLoadID() == facID);
                }
            }
            base.ExposeData();
        }
    }
}
