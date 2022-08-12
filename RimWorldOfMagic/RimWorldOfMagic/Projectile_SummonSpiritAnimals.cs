using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Projectile_SummonSpiritAnimals : Projectile_Ability
    {

        private int age = -1;
        private bool initialized = false;
        private bool destroyed = false;
        private int duration = 1;
        TMPawnSummoned newPawn = new TMPawnSummoned();        
        private int verVal;
        private int pwrVal;
        private float arcaneDmg = 1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 1, false);
            Scribe_Values.Look<bool>(ref this.destroyed, "destroyed", false, false);
        }        

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {            
            if (!(this.age < duration))
            {
                base.Destroy(mode);
            }
        }        

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            //base.Impact(hitThing);
            GenClamor.DoClamor(this, this.def.projectile.explosionRadius, ClamorDefOf.Impact);
            SpawnThings spawnThing = new SpawnThings();
            Pawn caster = this.launcher as Pawn;
            arcaneDmg = caster.GetCompAbilityUserMagic().arcaneDmg;
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_SummonSpiritAnimalMass, false);
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_SummonSpiritAnimalMass, false);

            CellRect cellRect = CellRect.CenteredOn(this.Position, Mathf.RoundToInt(this.def.projectile.explosionRadius));
            cellRect.ClipInsideMap(map);

            for (int i = 0; i < 3; i++)
            {
                AbilityUser.SpawnThings tempPod = new SpawnThings();
                IntVec3 shiftPos = cellRect.RandomCell;
                    
                if (i == 0)
                {
                    tempPod.def = TorannMagicDefOf.TM_SpiritBearR;
                    tempPod.kindDef = PawnKindDef.Named("TM_SpiritBear");
                }
                else if (i == 1)
                {
                    tempPod.def = TorannMagicDefOf.TM_SpiritMongooseR;
                    tempPod.kindDef = PawnKindDef.Named("TM_SpiritMongoose");
                }
                else
                {
                    tempPod.def = TorannMagicDefOf.TM_SpiritCrowR;
                    tempPod.kindDef = PawnKindDef.Named("TM_SpiritCrow");
                }
                tempPod.spawnCount = 1;
                tempPod.temporary = true;
                int spawnDuration = Mathf.RoundToInt(Rand.Range(1200, 1240) + (300 * verVal));
                if (shiftPos != default(IntVec3))
                {
                    try
                    {                            
                        Thing spirit = TM_Action.SingleSpawnLoop(caster, tempPod, shiftPos, map, spawnDuration, true, false, caster.Faction, false);
                        Pawn animal = spirit as Pawn;
                        Pawn enemy = TM_Calc.FindNearbyEnemy(animal, 30);
                        animal.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                        animal.playerSettings.followDrafted = true;
                        
                        //TM_Action.TrainAnimalFull(animal, caster);

                        foreach (Pawn item in PawnUtility.SpawnedMasteredPawns(caster))
                        {
                            if (item.caller != null)
                            {
                                item.caller.Notify_Released();
                            }
                            item.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                        }

                        if (enemy != null)
                        {
                            Job defendJob = new Job(JobDefOf.AttackMelee, enemy);
                            animal.jobs.TryTakeOrderedJob(defendJob);
                        }

                        HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_Movement, (1 + (3*pwrVal)) * arcaneDmg);
                        HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_Manipulation, (1 + (3 * pwrVal)) * arcaneDmg);
                        HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_Sight, (1 + (3 * pwrVal)) * arcaneDmg);
                        
                        if (animal.def == TorannMagicDefOf.TM_SpiritCrowR)
                        {
                            HealthUtility.AdjustSeverity(animal, TorannMagicDefOf.TM_BirdflightHD, .5f);
                        }

                        CompAnimalController animalComp = animal.TryGetComp<CompAnimalController>();
                        if (animalComp != null)
                        {
                            animalComp.summonerPawn = caster;
                        }

                        for (int j = 0; j < 3; j++)
                        {
                            Vector3 rndPos = spirit.DrawPos;
                            rndPos.x += Rand.Range(-.5f, .5f);
                            rndPos.z += Rand.Range(-.5f, .5f);
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ghost, rndPos, map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, Rand.Range(1, 2), 0, 0);
                            FleckMaker.ThrowSmoke(rndPos, map, Rand.Range(.8f, 1.2f));
                        }
                    }
                    catch
                    {
                        Log.Message("TM_Exception".Translate(
                                caster.LabelShort,
                                "Spirit Animal"
                            ));
                    }
                }                    
            }

            this.age = this.duration;   
            
            Destroy();
        }
    }
}