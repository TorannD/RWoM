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
        Pawn threat = null;
        private Pawn strikeBody;
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

        public override string CompLabelInBracketsExtra => parent.Severity.ToString("0.00")+"%";

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
            this.parent.Severity = 90f;
            if (body.Spawned) FleckMaker.ThrowLightningGlow(body.TrueCenter(),body.Map,1f);
            DeterminePsionicHD();            
        }

        private void DeterminePsionicHD()
        {
            this.comp = this.Pawn.GetCompAbilityUserMight();
            if (comp != null && comp.MightData != null)
            {
                this.PwrVal = this.Pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault(x=>x.label=="TM_PsionicAugmentation_pwr")?.level ?? 0;
                this.EffVal = this.Pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault(x=>x.label=="TM_PsionicAugmentation_eff")?.level ?? 0;
                this.VerVal = this.Pawn.GetCompAbilityUserMight().MightData.MightPowerSkill_PsionicAugmentation.FirstOrDefault(x=>x.label=="TM_PsionicAugmentation_ver")?.level ?? 0;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Pawn==null || Pawn.Dead || ClassProgression.SpiritRebirthEnergy.Marked(Pawn)) return;
            RunAugmentation(Pawn,ref severityAdjustment);
        }

        internal static bool CanProject(Pawn body,Pawn soul) => body?.Spawned==true && body.IsColonistPlayerControlled &&
            !body.Dead && !body.Downed && ClassProgression.PsionicMartialPath.Energy(body)==null &&
            ClassProgression.PsionicMartialPath.Path(body)==null && ClassProgression.PsionicMartialPath.Energy(soul)!=null &&
            ClassProgression.SpiritRebirthState.TryGetPossessingSpirit(body,out var current) && current==soul &&
            ClassProgression.SpiritRebirthState.TryGet(soul,out var state) && !state.DepletionPending;

        internal static void TickHosted(Pawn soul)
        {
            var energy=ClassProgression.PsionicMartialPath.Energy(soul);
            var tracker=energy?.TryGetComp<HediffComp_Psionic>();
            if (tracker==null) return;
            if (!ClassProgression.SpiritRebirthState.TryGet(soul,out var state) || state.DepletionPending ||
                state.Host is not Pawn body || !state.HasValidHost(soul) || (ClassProgression.PsionicMartialPath.Energy(body)!=null || ClassProgression.PsionicMartialPath.Path(body)!=null))
            {
                tracker.CancelStrike();
                return;
            }
            if (!body.Spawned || body.Dead || body.Downed || !body.IsColonistPlayerControlled) return;
            float change=0;
            tracker.RunAugmentation(body,ref change);
            if (change!=0) energy.Severity+=change;
        }

        private void CancelStrike()
        {
            doPsionicAttack=false;
            strikeBody=null;
            threat=null;
            ticksTillPsionicStrike=0;
        }

        private void RunAugmentation(Pawn body, ref float severityAdjustment)
        {
            if (parent!=null)
            {
                if (!initialized) { initialized=true; Initialize(body); }
                comp ??= Pawn.GetCompAbilityUserMight();
                var career=ClassProgression.PsionicMartialPath.Path(Pawn);
                if(career!=null && (body.jobs?.curDriver is ClassProgression.JobDriver_PsionicCrucible || ClassProgression.PsionicMartialPath.Read(career)?.DisruptedUntil>Find.TickManager.TicksGame))
                {CancelStrike();return;}

                if (Find.TickManager.TicksGame % 60 == 0 && initialized)
                {
                    DeterminePsionicHD();
                    if(career?.MartialAlternate!=true || body.jobs?.curDriver is not JobDriver_PsionicBarrier)
                        severityAdjustment += (body.GetStatValue(StatDefOf.PsychicSensitivity, false) * Rand.Range(.04f, .12f));
                    Hediff hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_Artifact_PsionicBoostHD);
                    float maxSev = 100;
                    if (hediff != null)
                    {
                        maxSev += hediff.Severity; 
                    }
                    this.parent.Severity = Mathf.Clamp(this.parent.Severity, 0, maxSev);

                }

                if (body.Spawned && !body.Downed && body.Map!=null && comp!=null)
                {                    
                    if (doPsionicAttack && (strikeBody ?? Pawn)!=body) CancelStrike();
                    if (this.doPsionicAttack)
                    {
                        this.ticksTillPsionicStrike--;
                        if (this.ticksTillPsionicStrike <= 0)
                        {
                            this.doPsionicAttack = false;
                            if (threat?.Spawned==true && threat.Map==body.Map && !threat.Dead && !threat.Downed && threat.HostileTo(body) && !threat.IsPrisoner)
                            {
                                TM_MoteMaker.MakePowerBeamMotePsionic(threat.DrawPos.ToIntVec3(), threat.Map, 2f, 2f, .7f, .1f, .6f);
                                DamageInfo dinfo2 = new DamageInfo(TMDamageDefOf.DamageDefOf.TM_PsionicInjury, Rand.Range(6, 12) * body.GetStatValue(StatDefOf.PsychicSensitivity, false) + (2 * VerVal), 0, -1, body, null, null, DamageInfo.SourceCategory.ThingOrUnknown, this.threat);
                                this.threat.TakeDamage(dinfo2);
                            }
                        }
                    }                    

                    if (comp.usePsionicAugmentationToggle && body.drafter != null && body.CurJob != null)
                    {
                        if (Find.TickManager.TicksGame % 600 == 0 && !body.Drafted)
                        {
                            if (this.parent.Severity >= 95 && body.CurJob.targetA != null && body.CurJob.targetA.Thing != null)
                            {
                                if ((body.Position - body.CurJob.targetA.Thing.Position).LengthHorizontal > 20 && (body.Position - body.CurJob.targetA.Thing.Position).LengthHorizontal < 300 && body.CurJob.locomotionUrgency >= LocomotionUrgency.Jog && body.CurJob.bill == null)
                                {
                                    if (!WorkBoost(body,"TM_PsionicSpeedHD",EffVal,10f)) return;
                                    for (int i = 0; i < 12; i++)
                                    {
                                        float direction = Rand.Range(0, 360);
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, body.DrawPos, body.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                    }
                                    comp.MightUserXP += Rand.Range(10, 15);
                                }
                                if ((body.Position - body.CurJob.targetA.Thing.Position).LengthHorizontal < 2 && (body.CurJob.bill != null || body.CurJob.def.defName == "Sow" || body.CurJob.def.defName == "FinishFrame" || body.CurJob.def.defName == "Deconstruct" || body.CurJob.def.defName == "Repair" || body.CurJob.def.defName == "Clean" || body.CurJob.def.defName == "Mine" || body.CurJob.def.defName == "SmoothFloor" || body.CurJob.def.defName == "SmoothWall" || body.CurJob.def.defName == "Harvest" || body.CurJob.def.defName == "HarvestDesignated" || body.CurJob.def.defName == "CutPlant" || body.CurJob.def.defName == "CutPlantDesignated"))
                                {
                                    if (!WorkBoost(body,"TM_PsionicManipulationHD",PwrVal,12f)) return;
                                    for (int i = 0; i < 12; i++)
                                    {
                                        float direction = Rand.Range(0, 360);
                                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, body.DrawPos, body.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                    }
                                    comp.MightUserXP += Rand.Range(10, 15);
                                }                                
                            }
                        }

                        if (this.parent.Severity >= 20)
                        {
                            if (Find.TickManager.TicksGame % 180 == 0 && (body.Drafted || !body.IsColonist) && ((body.equipment.Primary != null && !body.equipment.Primary.def.IsRangedWeapon) || body.equipment.Primary == null))
                            {
                                if (body.CurJob.targetA != null && body.CurJob.targetA.Thing != null && body.CurJob.targetA.Thing is Pawn && body.CurJobDef == JobDefOf.AttackMelee)
                                {
                                    //Log.Message("performing psionic dash - curjob " + body.CurJob);
                                    //Log.Message("curjob def " + body.CurJob.def.defName);
                                    //Log.Message("target " + body.CurJob.targetA.Thing);
                                    //Log.Message("target range " + (body.CurJob.targetA.Thing.Position - body.Position).LengthHorizontal);
                                    Pawn targetPawn = body.CurJob.targetA.Thing as Pawn;
                                    float targetDistance = (body.Position - targetPawn.Position).LengthHorizontal;
                                    if (targetDistance > 3 && targetDistance < (12 + EffVal) && targetPawn.Map==body.Map && !targetPawn.Downed && targetPawn.HostileTo(body) &&
                                        Odyssey.TravelEnvironmentSafety.IsSafe(body,body.Map,targetPawn.Position,Mathf.CeilToInt(targetDistance/.75f)+1))
                                    {
                                        for (int i = 0; i < 12; i++)
                                        {
                                            float direction = Rand.Range(0, 360);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, body.DrawPos, body.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                        }
                                        FlyingObject_PsionicLeap flyingObject = (FlyingObject_PsionicLeap)GenSpawn.Spawn(ThingDef.Named("FlyingObject_PsionicLeap"), body.Position, body.Map);
                                        flyingObject.SetSkillOwner(Pawn);
                                        flyingObject.Launch(body, body.CurJob.targetA.Thing, body);
                                        if(flyingObject.GetDirectlyHeldThings().Contains(body))
                                        {
                                            parent.Severity-=3f;
                                            comp.Stamina.CurLevel -= .03f;
                                            comp.MightUserXP += Rand.Range(20, 30);
                                            return; // Takeoff clears the body's map/job; do not run another augmentation this tick.
                                        }
                                    }
                                }
                            }

                            if (this.nextPsionicAttack < Find.TickManager.TicksGame && body.Drafted && comp.usePsionicMindAttackToggle)
                            {
                                if (body.CurJob.def != TorannMagicDefOf.JobDriver_PsionicBarrier && VerVal > 0)
                                {
                                    this.threat = TM_Calc.FindNearbyEnemy(body, 20 + (2 * verVal)); // GetNearbyTarget(20 + (2 * VerVal));
                                    if (threat != null)
                                    {
                                        //start psionic attack; ends after delay
                                        SoundInfo info = SoundInfo.InMap(new TargetInfo(body.Position, body.Map, false), MaintenanceType.None);
                                        TorannMagicDefOf.TM_Implosion.PlayOneShot(info);
                                        Effecter psionicAttack = TorannMagicDefOf.TM_GiantExplosion.Spawn();
                                        psionicAttack.Trigger(new TargetInfo(threat.Position, threat.Map, false), new TargetInfo(threat.Position, threat.Map, false));
                                        psionicAttack.Cleanup();
                                        for (int i = 0; i < 12; i++)
                                        {
                                            float direction = Rand.Range(0, 360);
                                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi, body.DrawPos, body.Map, Rand.Range(.1f, .4f), 0.2f, .02f, .1f, 0, Rand.Range(8, 10), direction, direction);
                                        }
                                        float weaponModifier = 1;
                                        if (body.equipment.Primary != null)
                                        {
                                            if (body.equipment.Primary.def.IsRangedWeapon)
                                            {
                                                StatModifier wpnMass = body.equipment.Primary.def.statBases?.FirstOrDefault((StatModifier x) => x.stat.defName == "Mass");
                                                weaponModifier = Mathf.Clamp(wpnMass?.value ?? 1f, .8f, 6);
                                            }
                                            else //assume melee weapon
                                            {
                                                StatModifier wpnMass = body.equipment.Primary.def.statBases?.FirstOrDefault((StatModifier x) => x.stat.defName == "Mass");
                                                weaponModifier = Mathf.Clamp(wpnMass?.value ?? 1f, .6f, 4);
                                            }
                                        }
                                        else //unarmed
                                        {
                                            weaponModifier = .4f;
                                        }
                                        this.nextPsionicAttack = Find.TickManager.TicksGame + (int)(Mathf.Clamp((600 - (60 * verVal)) * weaponModifier, 120, 900));
                                        float energyCost = Mathf.Clamp((10f - VerVal) * weaponModifier, 2f, 12f);
                                        parent.Severity-=energyCost;
                                        comp.MightUserXP += Rand.Range(8, 12);
                                        strikeBody=body;
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

        private bool WorkBoost(Pawn body,string family,int rank,float cost)
        {
            string suffix=rank<=0?"":rank==1?"_I":rank==2?"_II":"_III";
            var def=HediffDef.Named(family+suffix);
            float severity=1f+.02f*rank;
            if (body==Pawn) HealthUtility.AdjustSeverity(body,def,severity);
            else
            {
                var existing=body.health.hediffSet.GetFirstHediffOfDef(def);
                if (existing!=null)
                {
                    var link=existing.TryGetComp<ClassProgression.HediffComp_SpiritCastOrigin>();
                    if (link?.Projected!=true || !link.Valid) return false;
                    existing.Severity+=severity;
                }
                else
                {
                    var effect=HediffMaker.MakeHediff(def,body);
                    var link=effect.TryGetComp<ClassProgression.HediffComp_SpiritCastOrigin>();
                    if (link==null) return false;
                    link.Bind(Pawn);effect.Severity=severity;body.health.AddHediff(effect);
                    if (!body.health.hediffSet.hediffs.Contains(effect)) return false;
                }
            }
            parent.Severity-=cost;
            return true;
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
            Scribe_Values.Look(ref doPsionicAttack,"psionicStrikePending");
            Scribe_Values.Look(ref ticksTillPsionicStrike,"psionicStrikeDelay");
            Scribe_Values.Look(ref nextPsionicAttack,"psionicStrikeReadyAt");
            Scribe_References.Look(ref threat,"psionicStrikeTarget");
            Scribe_References.Look(ref strikeBody,"psionicStrikeBody");
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
        }

    }
}
