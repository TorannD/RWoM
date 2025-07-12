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
    class HediffComp_Psionic : HediffComp
    {

        private bool initialized = false;
        private int pwrVal = 0;
        private int effVal = 0;
        private int verVal = 0;

        private bool doPsionicAttack = false;
        private int ticksTillPsionicStrike = 0;
        private int nextPsionicAttack = 0;
        Pawn threat =  null;
        CompAbilityUserMight comp;

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
            this.parent.Severity = 90f;
            FleckMaker.ThrowLightningGlow(base.Pawn.TrueCenter(), base.Pawn.Map, 1f);
            DeterminePsionicHD();            
        }

        private void DeterminePsionicHD()
        {
            this.comp = this.Pawn.GetCompAbilityUserMight();
            if (comp != null && comp.MightData != null)
            {
                this.PwrVal = this.Pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicAugmentation_pwr").level;
                this.EffVal = this.Pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicAugmentation_eff").level;
                this.VerVal = this.Pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault((MightPowerSkill x) => x.label == "TM_PsionicAugmentation_ver").level;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (base.Pawn != null & base.parent != null && !this.Pawn.Dead)
            {
                if (!initialized)
                {
                    initialized = true;
                    this.Initialize();
                }
                base.CompPostTick(ref severityAdjustment);

                if (Find.TickManager.TicksGame % 60 == 0 && initialized)
                {
                    DeterminePsionicHD();
                    severityAdjustment += (this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) * Rand.Range(.04f, .12f));
                    if (Find.Selector.FirstSelectedObject == this.Pawn)
                    {
                        HediffStage hediffStage = this.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TM_PsionicHD"), false).CurStage;
                        hediffStage.label = this.parent.Severity.ToString("0.00") + "%";
                    }

                    Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_PsionicBoostHD);
                    float maxSev = 100;
                    if (hediff != null)
                    {
                        maxSev += hediff.Severity; 
                    }
                    this.parent.Severity = Mathf.Clamp(this.parent.Severity, 0, maxSev);

                }

                if (base.Pawn.Spawned && !this.Pawn.Downed && base.Pawn.Map != null && comp != null)
                {                    
                    if (this.doPsionicAttack)
                    {
                        this.ticksTillPsionicStrike--;
                        if (this.ticksTillPsionicStrike <= 0)
                        {
                            this.doPsionicAttack = false;
                            if (threat != null && !threat.Destroyed && !threat.Dead && !threat.Downed)
                            {
                                TM_MoteMaker.MakePowerBeamMotePsionic(threat.DrawPos.ToIntVec3(), threat.Map, 2f, 2f, .7f, .1f, .6f);
                                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_PsionicInjury, Rand.Range(6, 12) * this.Pawn.GetStatValue(StatDefOf.PsychicSensitivity, false) + (2 * VerVal), 0, -1, this.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown, this.threat);
                                this.threat.TakeDamage(dinfo2);
                            }
                        }
                    }                    

                    if (comp.usePsionicAugmentationToggle && this.Pawn.drafter != null && this.Pawn.CurJob != null)
                    {
                        if (Find.TickManager.TicksGame % 600 == 0 && !this.Pawn.Drafted)
                        {
                            if (this.parent.Severity >= 95 && this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null)
                            {
                                if ((this.Pawn.Position - this.Pawn.CurJob.targetA.Thing.Position).LengthHorizontal > 20 && (this.Pawn.Position - this.Pawn.CurJob.targetA.Thing.Position).LengthHorizontal < 300 && this.Pawn.CurJob.locomotionUrgency >= LocomotionUrgency.Jog && this.Pawn.CurJob.bill == null)
                                {
                                    this.parent.Severity -= 10f;
                                    if (this.EffVal == 0)
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicSpeedHD"), 1f + .02f * this.EffVal);
                                    }
                                    else if (this.EffVal == 1)
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicSpeedHD_I"), 1f + .02f * this.EffVal);
                                    }
                                    else if (this.EffVal == 2)
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicSpeedHD_II"), 1f + .02f * this.EffVal);
                                    }
                                    else
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicSpeedHD_III"), 1f + .02f * this.EffVal);
                                    }
                                    for (int i = 0; i < 12; i++)
                                    {
                                        float direction = Rand.Range(0, 360);
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                    }
                                    comp.MightUserXP += Rand.Range(10, 15);
                                }
                                if ((this.Pawn.Position - this.Pawn.CurJob.targetA.Thing.Position).LengthHorizontal < 2 && (this.Pawn.CurJob.bill != null || this.Pawn.CurJob.def.defName == "Sow" || this.Pawn.CurJob.def.defName == "FinishFrame" || this.Pawn.CurJob.def.defName == "Deconstruct" || this.Pawn.CurJob.def.defName == "Repair" || this.Pawn.CurJob.def.defName == "Clean" || this.Pawn.CurJob.def.defName == "Mine" || this.Pawn.CurJob.def.defName == "SmoothFloor" || this.Pawn.CurJob.def.defName == "SmoothWall" || this.Pawn.CurJob.def.defName == "Harvest" || this.Pawn.CurJob.def.defName == "HarvestDesignated" || this.Pawn.CurJob.def.defName == "CutPlant" || this.Pawn.CurJob.def.defName == "CutPlantDesignated"))
                                {
                                    this.parent.Severity -= 12f;
                                    if (this.PwrVal == 0)
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicManipulationHD"), 1f + .02f * this.PwrVal);
                                    }
                                    else if (this.PwrVal == 1)
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicManipulationHD_I"), 1f + .02f * this.PwrVal);
                                    }
                                    else if (this.PwrVal == 2)
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicManipulationHD_II"), 1f + .02f * this.PwrVal);
                                    }
                                    else
                                    {
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicManipulationHD_III"), 1f + .02f * this.PwrVal);
                                    }
                                    for (int i = 0; i < 12; i++)
                                    {
                                        float direction = Rand.Range(0, 360);
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                    }
                                    comp.MightUserXP += Rand.Range(10, 15);
                                }                                
                            }
                        }

                        if (this.parent.Severity >= 20)
                        {
                            if (Find.TickManager.TicksGame % 180 == 0 && (this.Pawn.Drafted || !this.Pawn.IsColonist) && ((this.Pawn.equipment.Primary != null && !this.Pawn.equipment.Primary.def.IsRangedWeapon) || this.Pawn.equipment.Primary == null))
                            {
                                if (this.Pawn.CurJob.targetA != null && this.Pawn.CurJob.targetA.Thing != null && this.Pawn.CurJob.targetA.Thing is Pawn && this.Pawn.CurJobDef == JobDefOf.AttackMelee)
                                {
                                    //Log.Message("performing psionic dash - curjob " + this.Pawn.CurJob);
                                    //Log.Message("curjob def " + this.Pawn.CurJob.def.defName);
                                    //Log.Message("target " + this.Pawn.CurJob.targetA.Thing);
                                    //Log.Message("target range " + (this.Pawn.CurJob.targetA.Thing.Position - this.Pawn.Position).LengthHorizontal);
                                    Pawn targetPawn = this.Pawn.CurJob.targetA.Thing as Pawn;
                                    float targetDistance = (this.Pawn.Position - targetPawn.Position).LengthHorizontal;
                                    if (targetDistance > 3 && targetDistance < (12 + EffVal) && targetPawn.Map != null && !targetPawn.Downed)
                                    {
                                        for (int i = 0; i < 12; i++)
                                        {
                                            float direction = Rand.Range(0, 360);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                        }
                                        FlyingObject_PsionicLeap flyingObject = (FlyingObject_PsionicLeap)GenSpawn.Spawn(ThingDef.Named("FlyingObject_PsionicLeap"), this.Pawn.Position, this.Pawn.Map);
                                        flyingObject.Launch(this.Pawn, this.Pawn.CurJob.targetA.Thing, this.Pawn);
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicHD"), -3f);
                                        comp.Stamina.CurLevel -= .03f;
                                        comp.MightUserXP += Rand.Range(20, 30);
                                    }
                                }
                            }

                            if (this.nextPsionicAttack < Find.TickManager.TicksGame && this.Pawn.Drafted && comp.usePsionicMindAttackToggle)
                            {
                                if (this.Pawn.CurJob.def != TorannMagicDefOf.JobDriver_PsionicBarrier && VerVal > 0)
                                {
                                    this.threat = TM_Calc.FindNearbyEnemy(this.Pawn, 20 + (2 * verVal)); // GetNearbyTarget(20 + (2 * VerVal));
                                    if (threat != null)
                                    {
                                        //start psionic attack; ends after delay
                                        SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false), MaintenanceType.None);
                                        TorannMagicDefOf.TM_Implosion.PlayOneShot(info);
                                        Effecter psionicAttack = TorannMagicDefOf.TM_GiantExplosion.Spawn();
                                        psionicAttack.Trigger(new TargetInfo(threat.Position, threat.Map, false), new TargetInfo(threat.Position, threat.Map, false));
                                        psionicAttack.Cleanup();
                                        for (int i = 0; i < 12; i++)
                                        {
                                            float direction = Rand.Range(0, 360);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, this.Pawn.DrawPos, this.Pawn.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                        }
                                        float weaponModifier = 1;
                                        if (this.Pawn.equipment.Primary != null)
                                        {
                                            if (this.Pawn.equipment.Primary.def.IsRangedWeapon)
                                            {
                                                StatModifier wpnMass = this.Pawn.equipment.Primary.def.statBases.FirstOrDefault((StatModifier x) => x.stat.defName == "Mass");
                                                weaponModifier = Mathf.Clamp(wpnMass.value, .8f, 6);
                                            }
                                            else //assume melee weapon
                                            {
                                                StatModifier wpnMass = this.Pawn.equipment.Primary.def.statBases.FirstOrDefault((StatModifier x) => x.stat.defName == "Mass");
                                                weaponModifier = Mathf.Clamp(wpnMass.value, .6f, 4);
                                            }
                                        }
                                        else //unarmed
                                        {
                                            weaponModifier = .4f;
                                        }
                                        this.nextPsionicAttack = Find.TickManager.TicksGame + (int)(Mathf.Clamp((600 - (60 * verVal)) * weaponModifier, 120, 900));
                                        float energyCost = Mathf.Clamp((10f - VerVal) * weaponModifier, 2f, 12f);
                                        HealthUtility.AdjustSeverity(this.Pawn, HediffDef.Named("TM_PsionicHD"), -energyCost);
                                        comp.MightUserXP += Rand.Range(8, 12);
                                        this.doPsionicAttack = true;
                                        this.ticksTillPsionicStrike = 24;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private float GetAngleFromTo(Vector3 from, Vector3 to)
        {
            Vector3 heading = (to - from);
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;
            float directionAngle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
            return directionAngle;
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
