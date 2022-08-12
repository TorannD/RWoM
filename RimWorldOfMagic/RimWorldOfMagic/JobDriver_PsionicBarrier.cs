using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;


namespace TorannMagic
{
    internal class JobDriver_PsionicBarrier : JobDriver
    {
        private const TargetIndex building = TargetIndex.A;

        int age = -1;
        int barrierSearchFrequency = 1;
        int duration = 900;
        bool psiFlag;
        float psiEnergy = 0;
        List<IntVec3> barrierCells = new List<IntVec3>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompAbilityUserMight comp = this.pawn.GetCompAbilityUserMight();
            float radius = 2.5f;
            //radius += (.75f * TM_Calc.GetMightSkillLevel(pawn, comp.MightData.MightPowerSkill_PsionicBarrier, "TM_PsionicBarrier", "_ver", true));
            radius += (.75f * TM_Calc.GetSkillVersatilityLevel(pawn, TorannMagicDefOf.TM_PsionicBarrier));
            //if (this.pawn.story.traits.HasTrait(TorannMagicDefOf.TM_Psionic))
            //{
            //    radius = 2 + (.5f * comp.MightData.MightPowerSkill_PsionicBarrier.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBarrier_ver").level);
            //}
            //else if(this.pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
            //{
            //    radius = 2 + (.5f * comp.MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver").level);
            //}
            this.psiFlag = this.pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"), false);
            Toil psionicBarrier = new Toil();
            psionicBarrier.initAction = delegate
            {
                if (age > duration)
                {
                    this.EndJobWith(JobCondition.Succeeded);
                }                
                Map map = this.pawn.Map;
                this.barrierCells = new List<IntVec3>();
                this.barrierCells.Clear();
                this.GetCellList(radius);
                ticksLeftThisToil = 10;
                comp.shouldDrawPsionicShield = true;                
            };
            psionicBarrier.tickAction = delegate
            {
                //DrawBarrier(radius);
                if (Find.TickManager.TicksGame % this.barrierSearchFrequency == 0)
                {
                    if (psiFlag)
                    {
                        if(Rand.Chance(.15f * comp.MightData.MightPowerSkill_PsionicBarrier.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBarrier_pwr").level))
                        {
                            RepelProjectiles(false, radius);
                        }
                        else
                        {
                            RepelProjectiles(psiFlag, radius);
                        }
                        if (this.pawn.IsColonist)
                        {
                            HealthUtility.AdjustSeverity(this.pawn, HediffDef.Named("TM_PsionicHD"), -.004f);
                        }
                        else
                        {
                            HealthUtility.AdjustSeverity(this.pawn, HediffDef.Named("TM_PsionicHD"), -.04f);
                        }
                        
                        psiEnergy = this.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).Severity;
                        if (psiEnergy < 1)
                        {
                            this.EndJobWith(JobCondition.Succeeded);
                        }
                        if (this.psiFlag)
                        {
                            ticksLeftThisToil = (int)psiEnergy;
                        }
                    }
                    else
                    {
                        RepelProjectiles(false, radius);
                        comp.Stamina.CurLevel -= .00035f;
                    }
                }                
                age++;                
                if(!psiFlag)
                {
                    ticksLeftThisToil = Mathf.RoundToInt(((float)(duration - age) / (float)duration)*100f);
                    if (age > duration)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                    if(comp.Stamina.CurLevel < .01f)
                    {
                        this.EndJobWith(JobCondition.Succeeded);
                    }
                }               
            };
            psionicBarrier.defaultCompleteMode = ToilCompleteMode.Delay;
            psionicBarrier.defaultDuration = this.duration;
            psionicBarrier.WithProgressBar(TargetIndex.A, delegate
            {
                if (this.pawn.DestroyedOrNull() || this.pawn.Dead || this.pawn.Downed)
                {
                    return 1f;
                }
                return 1f - (float)psionicBarrier.actor.jobs.curDriver.ticksLeftThisToil/100;

            }, false, 0f);
            psionicBarrier.AddFinishAction(delegate
            {
                if(this.pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                {                    
                    CompAbilityUserMight mightComp = this.pawn.GetCompAbilityUserMight();
                    if (mightComp.mimicAbility != null)
                    {
                        mightComp.RemovePawnAbility(mightComp.mimicAbility);
                    }                  
                }
                comp.shouldDrawPsionicShield = false;
                //do soemthing?
            });
            yield return psionicBarrier;
        }

        private void RepelProjectiles(bool usePsionicEnergy, float radius)
        {
            for (int i = 0; i < this.barrierCells.Count(); i++)
            {
                List<Thing> cellList = this.barrierCells[i].GetThingList(this.pawn.Map);
                for (int j = 0; j < cellList.Count; j++)
                {
                    if (cellList[j] is Projectile)
                    {
                        Projectile p = cellList[j] as Projectile;
                        if (p.Launcher != null && p.Launcher.Position.DistanceTo(TargetLocA) > (radius +.5f))
                        {
                            Vector3 displayEffect = this.barrierCells[i].ToVector3Shifted();
                            displayEffect.x += Rand.Range(-.3f, .3f);
                            displayEffect.y += Rand.Range(-.3f, .3f);
                            displayEffect.z += Rand.Range(-.3f, .3f);
                            float projectileDamage = cellList[j].def.projectile.GetDamageAmount(1, null);
                            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.LightningGlow, displayEffect, this.Map, projectileDamage / 8f, .2f, .1f, .3f, 0, 0, 0, Rand.Range(0, 360));
                            if(usePsionicEnergy)
                            {
                                int eff = this.pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicBarrier.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicBarrier_eff").level;
                                float sevReduct = (projectileDamage / (12 + eff));
                                HealthUtility.AdjustSeverity(this.pawn, HediffDef.Named("TM_PsionicHD"), -sevReduct);
                            }
                            else
                            {
                                this.pawn.GetCompAbilityUserMight().Stamina.CurLevel -= (projectileDamage / 600);
                            }
                            if(cellList[j].def.projectile.explosionRadius > 0 && cellList[j].def != TorannMagicDefOf.Projectile_FogOfTorment)
                            {
                                GenExplosion.DoExplosion(barrierCells[i], this.pawn.Map, cellList[j].def.projectile.explosionRadius, cellList[j].def.projectile.damageDef, this.pawn, (int)projectileDamage, cellList[j].def.projectile.GetArmorPenetration(1, null), cellList[j].def.projectile.soundExplode,
                                    null, cellList[j].def, null, cellList[j].def.projectile.postExplosionSpawnThingDef, cellList[j].def.projectile.postExplosionSpawnChance, cellList[j].def.projectile.postExplosionSpawnThingCount, cellList[j].def.projectile.applyDamageToExplosionCellsNeighbors,
                                    cellList[j].def.projectile.preExplosionSpawnThingDef, cellList[j].def.projectile.preExplosionSpawnChance, cellList[j].def.projectile.preExplosionSpawnThingCount, cellList[j].def.projectile.explosionChanceToStartFire, cellList[j].def.projectile.explosionDamageFalloff);
                            }
                            cellList[j].Destroy(DestroyMode.Vanish);
                        }
                    }
                }
            }
        }        

        private void DrawBarrier(float radius)
        {
            float drawRadius = radius * .23f;
            float num = Mathf.Lerp(drawRadius, 9.5f, drawRadius);
            Vector3 vector = this.TargetLocA.ToVector3Shifted();
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.VisEffects);
            Vector3 s = new Vector3(num, 9.5f, num);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(Rand.Range(0,360), Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_MatPool.PsionicBarrier, 0);
        }

        private void GetCellList(float radius)
        {
            IEnumerable<IntVec3> outerCells = GenRadial.RadialCellsAround(TargetLocA, radius, false);
            IEnumerable<IntVec3> innerCells = GenRadial.RadialCellsAround(TargetLocA, radius - 2, false);
            this.barrierCells = outerCells.Except(innerCells).ToList<IntVec3>();
            //for (int i = 0; i < this.barrierCells.Count(); i++)
            //{
            //    IntVec3 intVec = this.barrierCells[i];
            //    if (intVec.InBoundsWithNullCheck(this.pawn.Map) && intVec.IsValid)
            //    {
            //        FleckMaker.ThrowHeatGlow(intVec, this.pawn.Map, 1f);
            //    }
            //}
        }
    }
}
