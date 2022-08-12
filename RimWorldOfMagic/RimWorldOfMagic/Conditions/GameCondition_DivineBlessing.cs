using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TorannMagic.Conditions
{
    public class GameCondition_DivineBlessing : GameCondition
    {
        int age = -1;        
        
        List<Corpse> potentialResurrection = new List<Corpse>();
        List<Pawn> diseasedPawns = new List<Pawn>();
        List<Pawn> injuredPawns = new List<Pawn>();

        public override void Init()
        {
            base.Init();
            if(this.SingleMap != null)
            {
                
                List<Thing> allThings = (from x in this.SingleMap.listerThings.AllThings
                                                where true
                                                select x).ToList<Thing>();

                List<Corpse> potentialCorpses = new List<Corpse>();
                potentialCorpses.Clear();
                potentialResurrection.Clear();
                
                for (int i = 0; i < allThings.Count; i++)
                {
                    Thing t = allThings[i];
                    if(t != null && t is Corpse c)
                    {
                        if(c.InnerPawn.IsColonist && !c.IsDessicated())
                        {
                            potentialResurrection.Add(c);
                        }
                    }
                }
                
                if(Rand.Chance(.15f))
                {
                    Corpse c = this.potentialResurrection.RandomElement();
                    LocalTargetInfo targ = c;
                    TM_CopyAndLaunchProjectile.CopyAndLaunchThing(ThingDef.Named("Projectile_Resurrection"), c, targ, targ, ProjectileHitFlags.All);
                }

                for(int i = 0; i < this.SingleMap.mapPawns.FreeColonistsSpawned.Count; i++)
                {
                    Pawn p = this.SingleMap.mapPawns.FreeColonistsSpawned[i];
                    if(TM_Calc.IsPawnInjured(p, 0))
                    {
                        this.injuredPawns.AddDistinct(p);
                    }
                    List<Hediff> healthConditions = null;
                    healthConditions = TM_Calc.GetPawnAfflictions(p);
                    if(healthConditions != null && healthConditions.Count > 0)
                    {
                        bool treatableCondition = false;
                        for (int j = 0; j < healthConditions.Count; j++)
                        {
                            if (healthConditions[j].def.tendable)
                            {
                                treatableCondition = true;                                
                            }
                        }
                        if(treatableCondition)
                        {
                            diseasedPawns.AddDistinct(p);
                        }
                    }
                }

                for(int i = 0; i < injuredPawns.Count; i++)
                {
                    if(Rand.Chance(.6f))
                    {
                        HealthUtility.AdjustSeverity(injuredPawns[i], TorannMagicDefOf.TM_Regeneration_I, 2f);
                    }
                }

                for (int i = 0; i < injuredPawns.Count; i++)
                {
                    if (Rand.Chance(.7f))
                    {
                        HealthUtility.AdjustSeverity(injuredPawns[i], TorannMagicDefOf.TM_DiseaseImmunityHD, 2f);
                    }
                }
            }
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            age++;

            if(age > 10)
            {
                End();
            }
        }
    }
}
