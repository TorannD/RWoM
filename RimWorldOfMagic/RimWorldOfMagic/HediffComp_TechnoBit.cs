using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    class HediffComp_TechnoBit : HediffComp
    {

        private bool initialized = false;
        private int pwrVal = 0;
        private int effVal = 0;
        private int verVal = 0;

        private int ticksBitWorking = 0;
        private int nextBitEffect = 0;
        private int nextBitGrenade = 0;
        private int nextBitShock = 0;
        private int bitGrenadeCount = 0;
        Vector3 moteLoc = Vector3.zero;

        public int PwrVal
        {
            get
            {
                return this.pwrVal;
            }
            set
            {
                this.pwrVal = value;
            }
        }

        public int EffVal
        {
            get
            {
                return this.effVal;
            }
            set
            {
                this.effVal = value;
            }
        }

        public int VerVal
        {
            get
            {
                return this.verVal;
            }
            set
            {
                this.verVal = value;
            }
        }

        public string LabelCap
        {
            get
            {
                return base.Def.LabelCap;
            }
        }

        public string Label
        {
            get
            {
                return base.Def.label;
            }
        }

        private void Initialize()
        {
            bool spawned = base.Pawn.Spawned;
            
            if (spawned)
            {
                this.parent.Severity = 90f;
                FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 1f);
                DetermineHDRank();
            }
        }

        private void DetermineHDRank()
        {
            CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();
            this.PwrVal = comp.MagicData.MagicPowerSkill_TechnoBit.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoBit_pwr").level;
            this.EffVal = comp.MagicData.MagicPowerSkill_TechnoBit.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoBit_eff").level;
            this.VerVal = comp.MagicData.MagicPowerSkill_TechnoBit.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_TechnoBit_ver").level;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.Pawn.Spawned && !this.Pawn.Dead && !this.Pawn.Downed)
            {
                base.CompPostTick(ref severityAdjustment);
                if (base.Pawn != null & base.parent != null)
                {
                    if (!initialized)
                    {
                        initialized = true;
                        this.Initialize();
                    }
                }

                CompAbilityUserMagic comp = this.Pawn.GetCompAbilityUserMagic();

                if (this.ticksBitWorking > 0 && this.nextBitEffect < Find.TickManager.TicksGame && this.moteLoc != Vector3.zero)
                {
                    Vector3 rndVec = this.moteLoc;
                    rndVec.x += Rand.Range(-.15f, .15f);
                    rndVec.z += Rand.Range(-.15f, .15f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndVec, this.Pawn.Map, Rand.Range(.9f, 1.5f), .05f, 0f, .1f, 0, 0f, 0f, 0f);
                    rndVec = this.moteLoc;
                    rndVec.x += Rand.Range(-.15f, .15f);
                    rndVec.z += Rand.Range(-.15f, .15f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndVec, this.Pawn.Map, Rand.Range(.6f, 1.1f), .05f, 0f, .1f, 0, 0f, 0f, 0f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Enchanting, comp.bitPosition, this.Pawn.Map, Rand.Range(0.35f, 0.43f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
                    this.ticksBitWorking--;
                    this.nextBitEffect = Find.TickManager.TicksGame + Rand.Range(6, 10);
                    if(this.ticksBitWorking == 0)
                    {
                        this.moteLoc = Vector3.zero;
                    }
                }

                if (comp.useTechnoBitToggle)
                {
                    if(Find.TickManager.TicksGame % 60 == 0)
                    {
                        DetermineHDRank();
                    }
                    if (Find.TickManager.TicksGame % 600 == 0 && !this.Pawn.Drafted)
                    {
                        if (comp.Mana.CurLevelPercentage >= .9f && comp.Mana.CurLevel >= (.06f - (.001f * this.VerVal)) && this.Pawn.CurJob.targetA.Thing != null)
                        {                                                       
                            if (this.Pawn.CurJob.targetA.Thing != null)
                            {
                                if((this.Pawn.Position - this.Pawn.CurJob.targetA.Thing.Position).LengthHorizontal < 2 && (this.Pawn.CurJob.bill != null || this.Pawn.CurJob.def.defName == "FinishFrame" || this.Pawn.CurJob.def.defName == "Deconstruct" || this.Pawn.CurJob.def.defName == "Repair" || this.Pawn.CurJob.def.defName == "Mine" || this.Pawn.CurJob.def.defName == "SmoothFloor" || this.Pawn.CurJob.def.defName == "SmoothWall"))
                                {
                                    comp.Mana.CurLevel -= (.05f - (.001f * this.VerVal));
                                    HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_BitAssistHD"), .5f + 1f * this.VerVal);                                                                      
                                    comp.MagicUserXP += Rand.Range(6, 8);
                                    this.ticksBitWorking = 8;
                                    this.moteLoc = this.Pawn.CurJob.targetA.Thing.DrawPos;
                                }
                            }
                        }
                    }

                    if(comp.useTechnoBitRepairToggle && Find.TickManager.TicksGame % (160 - 3 * EffVal) == 0 && this.Pawn.Drafted && comp.Mana.CurLevel >= (.03f - .0006f * EffVal))
                    {
                        Thing damagedThing = TM_Calc.FindNearbyDamagedThing(this.Pawn, Mathf.RoundToInt(5 + .33f * EffVal));
                        Building repairBuilding = damagedThing as Building;
                        if(repairBuilding != null)
                        {
                            repairBuilding.HitPoints = Mathf.Min(Mathf.RoundToInt(repairBuilding.HitPoints + (Rand.Range(8, 12) + (.5f*EffVal))), repairBuilding.MaxHitPoints);
                            comp.Mana.CurLevel -= (.03f - .0006f * EffVal);
                            comp.MagicUserXP += Rand.Range(4, 5);
                            this.ticksBitWorking = 8;
                            this.moteLoc = repairBuilding.DrawPos;
                        }
                        Pawn damagedRobot = damagedThing as Pawn;
                        if(damagedRobot != null)
                        {
                            TM_Action.DoAction_HealPawn(this.Pawn, damagedRobot, 1, (4 + .4f * EffVal));
                            comp.Mana.CurLevel -= (.03f - .0006f * EffVal);
                            comp.MagicUserXP += Rand.Range(4, 6);
                            this.ticksBitWorking = 5;
                            this.moteLoc = damagedRobot.DrawPos;
                        }
                    }

                    if (comp.useTechnoBitRepairToggle && Find.TickManager.TicksGame % (600 - 6 * EffVal) == 0 && !this.Pawn.Drafted && comp.Mana.CurLevel >= .05f)
                    {
                        Thing damagedThing = TM_Calc.FindNearbyDamagedThing(this.Pawn, Mathf.RoundToInt(10 + .5f * EffVal));
                        Building repairBuilding = damagedThing as Building;
                        if (repairBuilding != null)
                        {
                            repairBuilding.HitPoints = Mathf.Min(repairBuilding.HitPoints + (25 + (2*EffVal)), repairBuilding.MaxHitPoints);
                            comp.Mana.CurLevel -= (.05f - .0008f * EffVal);
                            comp.MagicUserXP += Rand.Range(9, 11);
                            this.ticksBitWorking = 8;
                            this.moteLoc = repairBuilding.DrawPos;
                        }
                        Pawn damagedRobot = damagedThing as Pawn;
                        if (damagedRobot != null)
                        {
                            TM_Action.DoAction_HealPawn(this.Pawn, damagedRobot, 2, (8+.4f * EffVal));
                            comp.Mana.CurLevel -= (.05f - .0008f * EffVal);
                            comp.MagicUserXP += Rand.Range(9, 11);
                            this.ticksBitWorking = 5;
                            this.moteLoc = damagedRobot.DrawPos;
                        }
                    }

                    if (comp.Mana.CurLevel >= .1f && (this.Pawn.Drafted || !this.Pawn.IsColonist))
                    {
                        if (this.Pawn.TargetCurrentlyAimingAt != null && (this.Pawn.CurJob.def.defName == "AttackStatic" || this.Pawn.CurJob.def.defName == "Wait_Combat") && this.nextBitGrenade < Find.TickManager.TicksGame) 
                        {
                            float maxRange = 25 + this.PwrVal;
                            Thing targetThing = this.Pawn.TargetCurrentlyAimingAt.Thing;
                            float targetDistance = (this.Pawn.Position - targetThing.Position).LengthHorizontal;
                            float acc = 15f + (PwrVal / 3f);
                            if (TM_Calc.HasLoSFromTo(this.Pawn.Position, this.Pawn.TargetCurrentlyAimingAt.Thing, this.Pawn as Thing, 2f, maxRange) && targetThing.Map != null && this.bitGrenadeCount > 0)
                            {                              
                                IntVec3 rndTargetCell = targetThing.Position;
                                rndTargetCell.x += Mathf.RoundToInt(Rand.Range(-targetDistance / acc, targetDistance / acc)); //grenades were 8
                                rndTargetCell.z += Mathf.RoundToInt(Rand.Range(-targetDistance / acc, targetDistance / acc));
                                LocalTargetInfo ltiTarget = rndTargetCell;
                                //if (this.bitGrenadeCount == 2)
                                //{
                                //    //launch emp grenade
                                //    Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Projectile_TMEMPGrenade"), this.Pawn.Position, this.Pawn.Map, WipeMode.Vanish);
                                //    float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.Pawn.Position, ltiTarget.Cell)).ToAngleFlat();
                                //    for (int m = 0; m < 4; m++)
                                //    {
                                //        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, comp.bitPosition, this.Pawn.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(3f, 5f), launchAngle += Rand.Range(-25, 25), Rand.Range(0, 360));
                                //    }
                                //    SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                                //    info.pitchFactor = 2f;
                                //    info.volumeFactor = .6f;
                                //    SoundDef.Named("Mortar_LaunchA").PlayOneShot(info);
                                //    projectile.def.projectile.speed = 20 + PwrVal;
                                //    projectile.def.projectile.explosionDelay = Rand.Range(80, 120) - (4 * PwrVal);                                    
                                //    projectile.Launch(this.Pawn, comp.bitPosition, ltiTarget, targetThing, ProjectileHitFlags.All, null, null);
                                //}
                                //else
                                //{
                                //    //fire he grenade
                                //    Projectile projectile = (Projectile)GenSpawn.Spawn(ThingDef.Named("Projectile_TMFragGrenade"), this.Pawn.Position, this.Pawn.Map, WipeMode.Vanish);
                                //    float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.Pawn.Position, ltiTarget.Cell)).ToAngleFlat();
                                //    for (int m = 0; m < 4; m++)
                                //    {
                                //        TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, comp.bitPosition, this.Pawn.Map, Rand.Range(.4f, .7f), Rand.Range(.2f, .3f), .05f, Rand.Range(.4f, .6f), Rand.Range(-20, 20), Rand.Range(3f, 5f), launchAngle += Rand.Range(-25, 25), Rand.Range(0, 360));
                                //    }
                                //    SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                                //    info.pitchFactor = 1.4f;
                                //    info.volumeFactor = .5f;
                                //    SoundDef.Named("Mortar_LaunchA").PlayOneShot(info);
                                //    projectile.def.projectile.speed = 20 + PwrVal;
                                //    projectile.def.projectile.explosionDelay = Rand.Range(80, 120) - (4 * PwrVal);
                                //    projectile.Launch(this.Pawn, comp.bitPosition, ltiTarget, targetThing, ProjectileHitFlags.All, null, null);
                                //}
                                Projectile p = (Projectile)(GenSpawn.Spawn(ThingDef.Named("Projectile_TM_BitTechLaser"), this.Pawn.Position, this.Pawn.Map, WipeMode.Vanish));
                                //float launchAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(this.Pawn.Position, ltiTarget.Cell)).ToAngleFlat();
                                
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                                info.pitchFactor = 1.5f;
                                info.volumeFactor = .9f;
                                SoundDef.Named("Shot_ChargeBlaster").PlayOneShot(info);
                                
                                if (rndTargetCell == targetThing.Position)
                                {
                                    p.Launch(this.Pawn, comp.bitPosition, targetThing, targetThing, ProjectileHitFlags.IntendedTarget, false, null, null);
                                }
                                else
                                {
                                    p.Launch(this.Pawn, comp.bitPosition, ltiTarget, targetThing, ProjectileHitFlags.All, false, null, null);
                                }
                                this.nextBitGrenade = 3 + Find.TickManager.TicksGame;
                                this.bitGrenadeCount--;
                                if (this.bitGrenadeCount == 0)
                                {
                                    this.bitGrenadeCount = 3 + (int)((this.PwrVal) / 5);
                                    this.nextBitGrenade = Find.TickManager.TicksGame + (180 - 3*PwrVal);
                                    comp.Mana.CurLevel -= (.06f - (.001f * this.PwrVal));
                                    comp.MagicUserXP += Rand.Range(8, 12);
                                }
                            }
                            else if (this.nextBitGrenade < Find.TickManager.TicksGame && this.bitGrenadeCount <= 0)
                            {
                                this.bitGrenadeCount = 3 + (int)((this.PwrVal) / 5);
                                this.nextBitGrenade = Find.TickManager.TicksGame + (180 - 3 * PwrVal);
                            }
                        }                        
                    }
                }
            }
        }

        public override bool CompShouldRemove
        {
            get
            {
                return base.CompShouldRemove;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
        }       
    }
}
