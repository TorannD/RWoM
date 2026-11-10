using Verse;
using Verse.Sound;
using RimWorld;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_LightLance : ClassProgression.Projectile_SpiritBolt
    {
        private bool initialized = false;
        private bool geometryReady;
        private bool soundRequested;
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_LightLance;
        protected override string SkillStem => "TM_LightLance";
        public int burnTime = 200;
        private int age = -1;
        private float lightPotency = .5f;
        IntVec3 launchPosition;

        ColorInt colorInt = new ColorInt(255, 255, 140);
        private Sustainer sustainer;

        private float angle = 0;
        private float radius = 1;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        Vector3 lanceAngle;
        Vector3 lanceAngleInv;
        Vector3 drawPosStart;
        Vector3 drawPosEnd;
        float lanceLength;
        Vector3 lanceVector;
        Vector3 lanceVectorInv;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.burnTime, "burntime", 200, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1, false);
            Scribe_Values.Look<float>(ref this.lightPotency, "lightPotency", .5f, false);
            Scribe_Values.Look(ref initialized, "TM_lanceImpacted", age >= 0 && ticksToImpact <= 0, true);
            Scribe_Values.Look(ref launchPosition, "TM_lanceOrigin", origin.ToIntVec3(), true);
        }

        internal override void PrepareProjection(MagicAbility owner)
        {
            base.PrepareProjection(owner);
            var light = owner.Pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD)?.TryGetComp<HediffComp_LightCapacitance>();
            if (light != null) lightPotency = light.LightPotency;
        }

        private int TicksLeft
        {
            get
            {
                return this.burnTime - this.age;
            }
        }

        private void Initialize()
        {
            var caster = launcher as Pawn;
            launchPosition = !projected && caster?.Spawned == true && caster.Map == Map ? caster.Position : origin.ToIntVec3();
            var comp = SourcePawn?.GetCompAbilityUserMagic();
            if (!projected && comp != null)
            {
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_LightLance);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_LightLance);
                arcaneDmg = comp.arcaneDmg;
            }
            if (!projected && SourcePawn?.health?.hediffSet?.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD) == true)
            {
                HediffComp_LightCapacitance hd = SourcePawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD).TryGetComp<HediffComp_LightCapacitance>();
                this.lightPotency = hd.LightPotency;
            }
            this.burnTime += (pwrVal * 22);
        }

        private void RestoreGeometry()
        {
            if (geometryReady) return;
            geometryReady = true;
            this.radius = Mathf.Clamp(1.8f + (.25f * verVal) * lightPotency, 1f, 3f);
            this.angle = launchPosition == Position ? 0f : (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(launchPosition, Position)).ToAngleFlat();
            lanceAngle = Vector3Utility.FromAngleFlat(this.angle - 90);                 //angle of beam
            lanceAngleInv = Vector3Utility.FromAngleFlat(this.angle + 90);              //opposite angle of beam
            drawPosStart = this.launchPosition.ToVector3Shifted() + lanceAngle;         //this.parent.DrawPos;
            drawPosEnd = base.Position.ToVector3Shifted() + lanceAngleInv;
            lanceLength = (drawPosEnd - drawPosStart).magnitude;
            lanceVector = drawPosStart + (lanceAngle * lanceLength * 0.5f);
            lanceVectorInv = drawPosEnd + (lanceAngleInv * lanceLength * .5f);          //draw for double beam
            lanceVector.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);          //graphic depth
        }

        private void CheckSpawnSustainer()
        {
            if (!soundRequested && Spawned && TicksLeft > 0)
            {
                soundRequested = true;
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (Spawned && TicksLeft > 0 && sustainer == null)
                        this.sustainer = SoundDef.Named("OrbitalBeam").TrySpawnSustainer(SoundInfo.InMap(new TargetInfo(Position, Map), MaintenanceType.PerTick));
                });
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            this.Destroy(DestroyMode.Vanish);
            if (Destroyed) return;

            if (!this.initialized)
            {
                Initialize();
                this.initialized = true;
            }
            RestoreGeometry();
            CheckSpawnSustainer();

            if (this.sustainer != null)
            {
                this.sustainer.info.volumeFactor = 1;
                this.sustainer.Maintain();
                if (this.TicksLeft <= 0)
                {
                    this.sustainer.End();
                    this.sustainer = null;
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (initialized)
            {
                RestoreGeometry();
                DrawLance(launchPosition);
            }
        }

        public void DrawLance(IntVec3 launcherPos)
        {           
            float lanceWidth = this.radius;                                                              //
            if(this.age < (this.burnTime * .165f))
            {
                lanceWidth *= (float)this.age / 40f;
            }
            if(this.age > (this.burnTime * .835f))
            {
                lanceWidth *= (float)(this.burnTime - this.age) / 40f;
            }
            lanceWidth *= Rand.Range(.9f, 1.1f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(lanceVector, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceLength));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_LightLance.BeamMat, 0, null, 0, Projectile_LightLance.MatPropertyBlock);

            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(drawPosStart - (.5f*lanceAngle*lanceWidth), Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceWidth));                 //drawer for beam start
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_LightLance.BeamEndMat, 0, null, 0, Projectile_LightLance.MatPropertyBlock);
            drawPosEnd.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix4 = default(Matrix4x4);
            matrix4.SetTRS(drawPosEnd - (.5f*lanceAngleInv*lanceWidth), Quaternion.Euler(0f, this.angle - 180, 0f), new Vector3(lanceWidth, 1f, lanceWidth));                 //drawer for beam end
            Graphics.DrawMesh(MeshPool.plane10, matrix4, Projectile_LightLance.BeamEndMat, 0, null, 0, Projectile_LightLance.MatPropertyBlock);            
        }

        protected override void Tick()
        {
            base.Tick();
            this.age++;
            if (Spawned && initialized && this.age < (this.burnTime * .9f))
            {
                if (Find.TickManager.TicksGame % 5 == 0)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Heat, this.launchPosition.ToVector3Shifted(), this.Map, Rand.Range(.6f, 1.1f), .4f, .1f, .3f, Rand.Range(-200, 200), Rand.Range(5f, 9f), this.angle + Rand.Range(-15f, 15f), Rand.Range(0, 360));
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.burnTime;
            if (!flag)
            {
                sustainer?.End();
                sustainer = null;
                base.Destroy(mode);
            }
        }        
    }
}


