using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using AbilityUser;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_Sunfire : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int verVal = 0;
        private int pwrVal = 0;
        private int maxAge = 600;
        //private int strikeNum;
        private int age = -1;
        private float arcaneDmg = 1;
        private float lightPotency = .5f;
        Pawn caster;

        List<Vector3> sfBeams = new List<Vector3>();
        List<Vector3> sfBeamsDest = new List<Vector3>();
        List<Vector3> sfBeamsROM = new List<Vector3>();
        List<Vector3> sfBeamsCurve = new List<Vector3>();
        List<int> sfBeamsStep = new List<int>();
        List<int> sfBeamsStartTick = new List<int>();

        ColorInt colorInt = new ColorInt(255, 255, 140);
        private Sustainer sustainer;

        private float angle = 0;
        private float radius = 3;
        private bool ignoreAge = false;
        private float curveFactor = .001f;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Other/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1, false);
            Scribe_Values.Look<float>(ref this.lightPotency, "lightPotency", .5f, false);
            Scribe_Collections.Look<Vector3>(ref this.sfBeams, "sfBeams", LookMode.Value);
            Scribe_Collections.Look<Vector3>(ref this.sfBeamsROM, "sfBeamsROM", LookMode.Value);
            Scribe_Collections.Look<Vector3>(ref this.sfBeams, "sfBeamsCurve", LookMode.Value);
            Scribe_Collections.Look<int>(ref this.sfBeamsStep, "sfBeamsStep", LookMode.Value);
            Scribe_Collections.Look<int>(ref this.sfBeamsStartTick, "sfBeamsStartTick", LookMode.Value);
        }

        private int TicksLeft
        {
            get
            {
                return this.maxAge - this.age;
            }
        }

        private void Initialize()
        {
            caster = this.launcher as Pawn;
            CompAbilityUserMagic comp = caster.GetComp<CompAbilityUserMagic>();
            if (comp != null && comp.MagicData != null)
            {
                //pwrVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_Sunfire, "TM_Sunfire", "_pwr", true);
                //verVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_Sunfire, "TM_Sunfire", "_ver", true);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_Sunfire);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_Sunfire);
            }
            this.arcaneDmg = comp.arcaneDmg;
            if (caster.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD))
            {
                HediffComp_LightCapacitance hd = caster.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_LightCapacitanceHD).TryGetComp<HediffComp_LightCapacitance>();
                this.lightPotency = hd.LightPotency;
                hd.LightEnergy -= 25f;
            }
            int mapTime = GenLocalDate.HourOfDay(caster.Map);
            if (mapTime > 13)
            {
                this.angle = ((float)Mathf.Abs(mapTime - 12f) * -5f);
            }
            else if( mapTime < 12)
            {
                this.angle = ((float)Mathf.Abs(12 - mapTime) * 5f);
            }
            else
            {
                this.angle = 0f;
            }
            this.radius += (.15f * this.def.projectile.explosionRadius);
            this.CheckSpawnSustainer();
            this.InitializeBeams();
        }

        public void InitializeBeams()
        {
            sfBeams.Clear();
            sfBeamsCurve.Clear();
            sfBeamsDest.Clear();
            sfBeamsROM.Clear();
            sfBeamsStartTick.Clear();
            sfBeamsStep.Clear();
            Vector3 centerPos = base.Position.ToVector3Shifted();
            int beamCount = 8 + (Mathf.RoundToInt(this.def.projectile.explosionRadius)*2) + (4*verVal);
            for(int i =0; i < beamCount; i++)
            {                
                Vector3 rndPos = centerPos;
                rndPos.x += Rand.Range(-this.radius, this.radius);
                rndPos.z += Rand.Range(-this.radius, this.radius);
                if(!rndPos.InBounds(this.Map))
                {
                    continue;
                }                
                Vector3 dstPos = rndPos;
                dstPos.x += Rand.Range(-this.radius, this.radius);
                dstPos.z += Rand.Range(-this.radius, this.radius);
                if (!dstPos.InBounds(this.Map))
                {
                    continue;
                }
                int step = Rand.Range(25+(3*verVal), 40);
                sfBeamsStep.Add(step);
                sfBeamsStartTick.Add(i * Rand.Range(15, 20));
                sfBeams.Add(rndPos);
                sfBeamsDest.Add(dstPos);
                sfBeamsROM.Add((dstPos - rndPos) / (float)step);
                sfBeamsCurve.Add(new Vector3(Rand.Range(-this.curveFactor, this.curveFactor), 0f, Rand.Range(-this.curveFactor, this.curveFactor)));
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            ThingDef def = this.def;
            if (!this.initialized)
            {
                Initialize();
                this.initialized = true;
            }

            if (this.sustainer != null)
            {
                this.sustainer.info.volumeFactor = 2f;
                this.sustainer.Maintain();
                if (this.TicksLeft <= 0)
                {
                    this.sustainer.End();
                    this.sustainer = null;
                }
            }

            if(this.sfBeams != null && this.sfBeams.Count > 0)
            {
                DoSunfireActions();
            }
            
            if(sfBeams == null || sfBeams.Count <= 0 || this.ignoreAge)
            { 
                this.ignoreAge = true;
                Destroy(DestroyMode.Vanish);
            }

            if (!this.ignoreAge)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        public void DoSunfireActions()
        {
            List<int> removedIndex = new List<int>();
            removedIndex.Clear();
            try
            {
                for (int i = 0; i < this.sfBeams.Count; i++)
                {
                    if (sfBeamsStep[i] <= 0)
                    {
                        removedIndex.Add(i);
                    }
                    else
                    {
                        if (sfBeamsStartTick[i] <= this.age)
                        {
                            sfBeamsStep[i]--;
                            SunfireDamage(sfBeams[i].ToIntVec3());
                            TM_MoteMaker.ThrowGenericFleck(FleckDefOf.DustPuff, sfBeams[i], this.Map, .4f, .2f, .1f, .2f, Rand.Range(-100, 100), 2f, Rand.Range(0, 360), 0);
                            sfBeams[i] += sfBeamsROM[i];
                            sfBeamsROM[i] += (sfBeamsCurve[i] * (20f - sfBeamsStep[i]));
                        }
                    }
                }
                for (int i = 0; i < removedIndex.Count; i++)
                {
                    this.sfBeamsCurve.Remove(this.sfBeamsCurve[removedIndex[i]]);
                    this.sfBeamsDest.Remove(sfBeamsDest[removedIndex[i]]);
                    this.sfBeamsROM.Remove(sfBeamsROM[removedIndex[i]]);
                    this.sfBeamsStartTick.Remove(sfBeamsStartTick[removedIndex[i]]);
                    this.sfBeamsStep.Remove(sfBeamsStep[removedIndex[i]]);
                    this.sfBeams.Remove(sfBeams[removedIndex[i]]);
                }
            }
            catch
            {
                this.ignoreAge = true;
                Destroy(DestroyMode.Vanish);
            }
        }

        public void SunfireDamage(IntVec3 c)
        {
            List<Thing> tList = c.GetThingList(this.Map);
            float baseDamage = 4f * lightPotency * this.arcaneDmg;
            for(int i = 0; i < tList.Count; i++)
            {
                if(tList[i] is Pawn)
                {
                    Pawn p = tList[i] as Pawn;
                    BodyPartRecord bpr = p.health.hediffSet.GetRandomNotMissingPart(TMDamageDefOf.DamageDefOf.TM_BurningLight, BodyPartHeight.Undefined, BodyPartDepth.Outside);
                    if (bpr != null)
                    {
                        TM_Action.DamageEntities(tList[i], bpr, baseDamage + pwrVal, (.1f * pwrVal), TMDamageDefOf.DamageDefOf.TM_BurningLight, caster);
                    }
                }
                if(tList[i] is Building)
                {
                    TM_Action.DamageEntities(tList[i], null, 4 * (baseDamage + pwrVal), TMDamageDefOf.DamageDefOf.TM_BurningLight, caster);
                }
                if(Rand.Chance(.02f))
                {
                    if (FireUtility.CanEverAttachFire(tList[i]))
                    {
                        FireUtility.TryAttachFire(tList[i], .2f);
                    }
                    else
                    {
                        FireUtility.TryStartFireIn(c, this.Map, .2f);
                    }
                }                
            }
        }

        public override void Draw()
        {
            for (int i = 0; i < this.sfBeams.Count; i++)
            {
                if (sfBeamsStartTick[i] <= this.age && sfBeamsStep[i] > 0)
                {
                    DrawBeams(i);
                }
            }
        }

        public void DrawBeams(int i)
        {
            float lanceWidth = .5f;                                                              
            if (this.age - sfBeamsStartTick[i] <= 10)
            {
                lanceWidth *= (float)(this.age - sfBeamsStartTick[i]) / 10f;
            }
            if (this.sfBeamsStep[i] < 10)
            {
                lanceWidth *= (float)(sfBeamsStep[i]) / 10f;
            }
            float lanceLength = ((float)base.Map.Size.z - sfBeams[i].z) * 1.4f;
            Vector3 a = Vector3Utility.FromAngleFlat(this.angle - 90f);  //angle of beam
            Vector3 lanceVector = sfBeams[i] + a * lanceLength * 0.5f;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(lanceVector, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceLength));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_Sunfire.BeamMat, 0, null, 0, Projectile_Sunfire.MatPropertyBlock);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(sfBeams[i] - (.5f * a * lanceWidth), Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceWidth));  //drawer for beam start
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_Sunfire.BeamEndMat, 0, null, 0, Projectile_Sunfire.MatPropertyBlock);
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.maxAge;
            if (!flag || this.ignoreAge)
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


