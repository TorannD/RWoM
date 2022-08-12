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
    public class Projectile_OrbitalStrike : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int verVal = 0;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;
        private int beamAge = 0;
        private int strikeNum;
        private int age = -1;
        private int beamDuration = 120;
        private int targettingAge = 300;
        IntVec3 strikePos = default(IntVec3);
        Pawn caster;

        ColorInt colorInt = new ColorInt(153, 0, 0);
        private Sustainer sustainer;

        private float angle = 0;
        private float radius = 5;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Other/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.beamAge, "beamAge", 120, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1f, false);
            Scribe_Values.Look<int>(ref this.targettingAge, "targettingAge", 300, false);
            Scribe_Values.Look<IntVec3>(ref this.strikePos, "strikePos", default(IntVec3), false);
        }

        private int TicksLeft
        {
            get
            {
                return (this.beamDuration + this.targettingAge) - this.age;
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            ThingDef def = this.def;
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                MagicPowerSkill pwr = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_OrbitalStrike.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_OrbitalStrike_pwr");
                MagicPowerSkill ver = caster.GetCompAbilityUserMagic().MagicData.MagicPowerSkill_OrbitalStrike.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_OrbitalStrike_ver");
                verVal = ver.level;
                pwrVal = pwr.level;
                this.arcaneDmg = comp.arcaneDmg;
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (settingsRef.AIHardMode && !caster.IsColonist)
                {
                    pwrVal = 3;
                    verVal = 3;
                }
                this.angle = Rand.Range(-3f, 3f);
                this.strikeNum = 1;
                this.CheckSpawnSustainer();
                this.strikePos = base.Position;
                this.targettingAge = 300 - (50 * verVal);
                this.beamDuration = 120 - (10 * verVal);
                this.radius = this.def.projectile.explosionRadius;
                this.initialized = true;
            }

            if (this.sustainer != null)
            {
                this.sustainer.info.volumeFactor = (this.age) / (this.beamDuration + this.targettingAge);
                this.sustainer.Maintain();
                if (this.TicksLeft <= 0)
                {
                    this.sustainer.End();
                    this.sustainer = null;
                }
            }

            if (this.age == (this.targettingAge + this.beamDuration))
            {
                TM_MoteMaker.MakePowerBeamMoteColor(this.strikePos, base.Map, this.radius * 4f, 2f, .5f, .1f, .5f, colorInt.ToColor);                
                GenExplosion.DoExplosion(this.strikePos, map, this.def.projectile.explosionRadius, DamageDefOf.Bomb, this.launcher as Pawn, Mathf.RoundToInt((25 + 5*pwrVal) * this.arcaneDmg), 0, null, def, this.equipmentDef, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                Effecter OSEffect = TorannMagicDefOf.TM_OSExplosion.Spawn();
                OSEffect.Trigger(new TargetInfo(this.strikePos, this.Map, false), new TargetInfo(this.strikePos, this.Map, false));
                OSEffect.Cleanup();
            }
            else
            {
                if (Find.TickManager.TicksGame % 3 == 0)
                {
                    TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_BloodSquirt, this.strikePos.ToVector3Shifted(), this.Map, .3f, .1f, 0, 0, Rand.Range(-100, 100), 0, 0, Rand.Range(0, 360));
                }
            }
            
        }        

        public override void Draw()
        {
            base.Draw();
            if (this.age >= this.targettingAge)
            {
                DrawSmiteBeams(this.strikePos, this.beamAge);
            }            
        }

        public void DrawSmiteBeams(IntVec3 pos, int wrathAge)
        {
            Vector3 drawPos = pos.ToVector3Shifted(); // this.parent.DrawPos;
            drawPos.z = drawPos.z - 1.5f;
            float num = ((float)base.Map.Size.z - drawPos.z) * 1.4f;
            Vector3 a = Vector3Utility.FromAngleFlat(this.angle - 90f);  //angle of beam
            Vector3 a2 = drawPos + a * num * 0.5f;                      //
            a2.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays); //mote depth
            float num2 = Mathf.Min((float)this.beamAge / 10f, 1f);          //
            Vector3 b = a * ((1f - num2) * num);
            float num3 = 0.975f + Mathf.Sin((float)wrathAge * 0.3f) * 0.025f;       //color
            if (this.TicksLeft > (this.beamDuration * .2f))                          //color
            {
                num3 *= (float)(this.age - this.targettingAge) / (this.beamDuration * .8f);
            }
            Color arg_50_0 = colorInt.ToColor;
            Color color = arg_50_0;
            color.a *= num3;
            Projectile_OrbitalStrike.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(a2 + a * this.radius * 0.5f + b, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius, 1f, num));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_OrbitalStrike.BeamMat, 0, null, 0, Projectile_OrbitalStrike.MatPropertyBlock);
            Vector3 vectorPos = drawPos + b;
            vectorPos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(vectorPos, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius, 1f, this.radius));                 //drawer for beam end
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_OrbitalStrike.BeamEndMat, 0, null, 0, Projectile_OrbitalStrike.MatPropertyBlock);
            num = num - (num * ((float)wrathAge/(float)(this.beamDuration/this.strikeNum)));
            Vector3 a3 = a * num;
            Vector3 vectorOrb = drawPos + a3;
            vectorOrb.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
            Matrix4x4 matrix3 = default(Matrix4x4);
            matrix3.SetTRS(vectorOrb, Quaternion.Euler(0f, this.angle, 0f), new Vector3(this.radius*((5*(float)wrathAge)/((float)this.beamDuration/this.strikeNum)), 1f, this.radius * ((5 * (float)wrathAge) / ((float)this.beamDuration/this.strikeNum))));                 //drawer for beam end
            Graphics.DrawMesh(MeshPool.plane10, matrix3, Projectile_OrbitalStrike.BombardMat, 0, null, 0, Projectile_OrbitalStrike.MatPropertyBlock);
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
            if(this.age >= this.targettingAge)
            {
                this.beamAge++;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= (this.targettingAge + this.beamDuration);
            if (!flag)
            {
                base.Destroy(mode);
            }
        }

        private void CheckSpawnSustainer()
        {
            if (this.TicksLeft >= 0)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    this.sustainer = SoundDef.Named("OrbitalBeam").TrySpawnSustainer(SoundInfo.InMap(this.selectedTarget, MaintenanceType.PerTick));
                });
            }
        }
    }
}


