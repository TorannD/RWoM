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
    public class GameCondition_TargetedPredatorInsanity : GameCondition
    {
        int age = -1;        
        
        List<Pawn> enragedPredators = new List<Pawn>();
        List<Pawn> potentialHostiles = new List<Pawn>();

        public override void Init()
        {
            base.Init();
            if(this.SingleMap != null)
            {                
                List<Pawn> potentialAnimals = new List<Pawn>();
                potentialHostiles.Clear();                
                potentialAnimals.Clear();
                for(int i = 0; i < this.SingleMap.mapPawns.AllPawnsSpawned.Count; i++)
                {
                    Pawn animal = this.SingleMap.mapPawns.AllPawnsSpawned[i];
                    if(animal.Faction == null)
                    {
                        if(animal.RaceProps != null && animal.RaceProps.predator)
                        {
                            potentialAnimals.Add(animal);
                        }
                    }
                    else if(animal.Faction.HostileTo(Faction.OfPlayerSilentFail))
                    {
                        potentialHostiles.AddDistinct(animal);
                    }
                }
                if(potentialAnimals != null && potentialAnimals.Count > 0)
                {
                    for (int i = 0; i < potentialAnimals.Count; i++)
                    {
                        Pawn a = potentialAnimals[i];                 
                        a.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter,null, true, false, null, true);
                        if(a.InMentalState)
                        {
                            this.enragedPredators.AddDistinct(a);
                        }
                    }
                }
            }
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();
            age++;
            if(age > 5)
            {
                for(int i = 0; i < enragedPredators.Count; i++)
                {
                    Job job = new Job(JobDefOf.AttackMelee, potentialHostiles.RandomElement());
                    enragedPredators[i].jobs.TryTakeOrderedJob(job, JobTag.InMentalState);
                    HealthUtility.AdjustSeverity(enragedPredators[i], TorannMagicDefOf.TM_Movement, 2);
                    HealthUtility.AdjustSeverity(enragedPredators[i], TorannMagicDefOf.TM_Manipulation, 2);
                }
            }

            if(age > 10)
            {
                End();
            }
        }
    }
}
