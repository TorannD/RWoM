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

        private void Initialize(Pawn body)
        {
            bool spawned = body.Spawned;
            
            if (spawned)
            {
                this.parent.Severity = 90f;
                FleckMaker.ThrowLightningGlow(body.TrueCenter(), body.Map, 1f);
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
            base.CompPostTick(ref severityAdjustment);
            if (!ClassProgression.SpiritRebirthEnergy.Marked(Pawn)) TickFor(Pawn);
        }

        internal void TickFor(Pawn body)
        {
            if (body.Spawned && !body.Dead && !body.Downed)
            {
                var comp = Pawn.GetCompAbilityUserMagic();
                if (comp?.Mana == null) return;
                if (!initialized)
                {
                    initialized = true;
                    Initialize(body);
                }

                if (this.ticksBitWorking > 0 && this.nextBitEffect < Find.TickManager.TicksGame && this.moteLoc != Vector3.zero)
                {
                    Vector3 rndVec = this.moteLoc;
                    rndVec.x += Rand.Range(-.15f, .15f);
                    rndVec.z += Rand.Range(-.15f, .15f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndVec, body.Map, Rand.Range(.9f, 1.5f), .05f, 0f, .1f, 0, 0f, 0f, 0f);
                    rndVec = this.moteLoc;
                    rndVec.x += Rand.Range(-.15f, .15f);
                    rndVec.z += Rand.Range(-.15f, .15f);
                    TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.SparkFlash, rndVec, body.Map, Rand.Range(.6f, 1.1f), .05f, 0f, .1f, 0, 0f, 0f, 0f);
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Enchanting, comp.BitOrigin(body), body.Map, Rand.Range(0.35f, 0.43f), .2f, .05f, Rand.Range(.4f, .6f), Rand.Range(-200, 200), 0, 0, 0);
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
                    if (Find.TickManager.TicksGame % 600 == 0 && !body.Drafted)
                    {
                        var job = body.CurJob;
                        var target = job?.targetA.Thing;
                        if (comp.Mana.CurLevelPercentage >= .9f && comp.Mana.CurLevel >= .06f - .001f * VerVal &&
                            target != null && (body.Position - target.Position).LengthHorizontal < 2 &&
                            (job.bill != null || job.def.defName == "FinishFrame" || job.def.defName == "Deconstruct" ||
                            job.def.defName == "Repair" || job.def.defName == "Mine" || job.def.defName == "SmoothFloor" || job.def.defName == "SmoothWall"))
                        {
                            comp.Mana.CurLevel -= Math.Max(0f,.05f - .001f * VerVal);
                            HealthUtility.AdjustSeverity(body, HediffDef.Named("TM_BitAssistHD"), .5f + VerVal);
                            comp.MagicUserXP += Rand.Range(6,8);
                            ticksBitWorking = 8;
                            moteLoc = target.DrawPos;
                        }
                    }

                    if(comp.useTechnoBitRepairToggle && Find.TickManager.TicksGame % Math.Max(1,160 - 3 * EffVal) == 0 && body.Drafted && comp.Mana.CurLevel >= (.03f - .0006f * EffVal))
                    {
                        Thing damagedThing = TM_Calc.FindNearbyDamagedThing(body, Mathf.RoundToInt(5 + .33f * EffVal));
                        if (damagedThing != null)
                        {
                            Building repairBuilding = damagedThing as Building;
                            if (repairBuilding != null)
                            {
                                var trial = ClassProgression.TechnomancerTrials.CaptureRepair(this, repairBuilding, body);
                                repairBuilding.HitPoints = Mathf.Min(Mathf.RoundToInt(repairBuilding.HitPoints + (Rand.Range(8, 12) + (.5f * EffVal))), repairBuilding.MaxHitPoints);
                                repairBuilding.Map.listerBuildingsRepairable.Notify_BuildingRepaired(repairBuilding);
                                ClassProgression.TechnomancerTrials.CompleteRepair(trial);
                                comp.Mana.CurLevel -= Math.Max(0f,.03f - .0006f * EffVal);
                                comp.MagicUserXP += Rand.Range(4, 5);
                                this.ticksBitWorking = 8;
                                this.moteLoc = repairBuilding.DrawPos;
                            }
                            Pawn damagedRobot = damagedThing as Pawn;
                            if (damagedRobot != null)
                            {
                                TM_Action.DoAction_HealPawn(body, damagedRobot, 1, (4 + .4f * EffVal));
                                comp.Mana.CurLevel -= Math.Max(0f,.03f - .0006f * EffVal);
                                comp.MagicUserXP += Rand.Range(4, 6);
                                this.ticksBitWorking = 5;
                                this.moteLoc = damagedRobot.DrawPos;
                            }
                        }
                    }

                    if (comp.useTechnoBitRepairToggle && Find.TickManager.TicksGame % Math.Max(1,600 - 6 * EffVal) == 0 && !body.Drafted && comp.Mana.CurLevel >= .05f)
                    {
                        Thing damagedThing = TM_Calc.FindNearbyDamagedThing(body, Mathf.RoundToInt(10 + .5f * EffVal));
                        Building repairBuilding = damagedThing as Building;
                        if (repairBuilding != null)
                        {
                            var trial = ClassProgression.TechnomancerTrials.CaptureRepair(this, repairBuilding, body);
                            repairBuilding.HitPoints = Mathf.Min(repairBuilding.HitPoints + (25 + (2*EffVal)), repairBuilding.MaxHitPoints);
                            repairBuilding.Map.listerBuildingsRepairable.Notify_BuildingRepaired(repairBuilding);
                            ClassProgression.TechnomancerTrials.CompleteRepair(trial);
                            comp.Mana.CurLevel -= Math.Max(0f,.05f - .0008f * EffVal);
                            comp.MagicUserXP += Rand.Range(9, 11);
                            this.ticksBitWorking = 8;
                            this.moteLoc = repairBuilding.DrawPos;
                        }
                        Pawn damagedRobot = damagedThing as Pawn;
                        if (damagedRobot != null)
                        {
                            TM_Action.DoAction_HealPawn(body, damagedRobot, 2, (8+.4f * EffVal));
                            comp.Mana.CurLevel -= Math.Max(0f,.05f - .0008f * EffVal);
                            comp.MagicUserXP += Rand.Range(9, 11);
                            this.ticksBitWorking = 5;
                            this.moteLoc = damagedRobot.DrawPos;
                        }
                    }

                    if (comp.Mana.CurLevel >= .1f && (body.Drafted || !body.IsColonist) && (body==Pawn || !body.WorkTagIsDisabled(WorkTags.Violent)))
                    {
                        if (body.TargetCurrentlyAimingAt.Thing != null && (body.CurJob?.def.defName == "AttackStatic" || body.CurJob?.def.defName == "Wait_Combat") && this.nextBitGrenade < Find.TickManager.TicksGame)
                        {
                            float maxRange = 25 + this.PwrVal;
                            Thing targetThing = body.TargetCurrentlyAimingAt.Thing;
                            float targetDistance = (body.Position - targetThing.Position).LengthHorizontal;
                            float acc = 15f + (PwrVal / 3f);
                            if (TM_Calc.HasLoSFromTo(body.Position, body.TargetCurrentlyAimingAt.Thing, body, 2f, maxRange) && targetThing.Map != null && this.bitGrenadeCount > 0)
                            {                              
                                IntVec3 rndTargetCell = targetThing.Position;
                                rndTargetCell.x += Mathf.RoundToInt(Rand.Range(-targetDistance / acc, targetDistance / acc)); //grenades were 8
                                rndTargetCell.z += Mathf.RoundToInt(Rand.Range(-targetDistance / acc, targetDistance / acc));
                                LocalTargetInfo ltiTarget = rndTargetCell;
                                Projectile p = (Projectile)(GenSpawn.Spawn(ThingDef.Named("Projectile_TM_BitTechLaser"), body.Position, body.Map, WipeMode.Vanish));

                                
                                if (body!=Pawn && p is ClassProgression.Bullet_TechnoElemental shot) shot.SetSource(Pawn);
                                SoundInfo info = SoundInfo.InMap(new TargetInfo(body.Position, body.Map, false), MaintenanceType.None);
                                info.pitchFactor = 1.5f;
                                info.volumeFactor = .9f;
                                SoundDef.Named("Shot_ChargeBlaster").PlayOneShot(info);
                                
                                if (rndTargetCell == targetThing.Position)
                                {
                                    p.Launch(body, comp.BitOrigin(body), targetThing, targetThing, ProjectileHitFlags.IntendedTarget, false, null, null);
                                }
                                else
                                {
                                    p.Launch(body, comp.BitOrigin(body), ltiTarget, targetThing, ProjectileHitFlags.All, false, null, null);
                                }
                                this.nextBitGrenade = 3 + Find.TickManager.TicksGame;
                                this.bitGrenadeCount--;
                                if (this.bitGrenadeCount == 0)
                                {
                                    this.bitGrenadeCount = 3 + (int)((this.PwrVal) / 5);
                                    this.nextBitGrenade = Find.TickManager.TicksGame + (180 - 3*PwrVal);
                                    comp.Mana.CurLevel -= Math.Max(0f,.06f - (.001f * this.PwrVal));
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
            Scribe_Values.Look(ref nextBitGrenade,"TM_bitNextShot");
            Scribe_Values.Look(ref bitGrenadeCount,"TM_bitBurstRemaining");
        }       
    }
}
