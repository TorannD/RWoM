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
    public class Projectile_Explosion : Projectile_AbilityBase
    {
        private bool initialized = false;
        private int verVal = 0;
        private int pwrVal = 0;
        private float arcaneDmg = 1f;
        private int beamAge = 0;
        private int age = -1;
        private int duration = 360;
        private int outerRingAngle = 0;
        private int middleRingAngle = 120;
        private int innerRingAngle = 240;
        private int expandingTick = 0;
        private bool phase2Flag = false;
        private bool phase3Flag = false;
        IntVec3 strikePos = default(IntVec3);
        List<IntVec3> outerRing = new List<IntVec3>();
        Pawn caster;
        IEnumerable<IntVec3> oldExplosionCells;
        IEnumerable<IntVec3> newExplosionCells;

        ColorInt colorInt = new ColorInt(200, 50, 0);
        private Sustainer sustainer;

        private float angle = 0;
        private float radius = 12f;
        private float damage = 1f;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Other/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material FireCircle = MaterialPool.MatFrom("Motes/firecircle", true);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.age, "age", -1, false);
            Scribe_Values.Look<int>(ref this.expandingTick, "expandingTick", 0, false);
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", false, false);
            Scribe_Values.Look<bool>(ref this.phase2Flag, "phase2Flag", false, false);
            Scribe_Values.Look<bool>(ref this.phase3Flag, "phase3Flag", false, false);
            Scribe_Values.Look<float>(ref this.radius, "radius", 12f, false);
            Scribe_Values.Look<float>(ref this.damage, "damage", 1f, false);
            Scribe_Values.Look<float>(ref this.arcaneDmg, "arcaneDmg", 1f, false);
            Scribe_Values.Look<int>(ref this.duration, "duration", 360, false);
            Scribe_Values.Look<IntVec3>(ref this.strikePos, "strikePos", default(IntVec3), false);
        }

        private int TicksLeft
        {
            get
            {
                return this.duration - this.age;
            }
        }

        protected override void Impact(Thing hitThing)
        {
            Map map = base.Map;
            ThingDef def = this.def;
            if (!this.initialized)
            {
                caster = this.launcher as Pawn;
                CompAbilityUserMagic comp = caster.GetCompAbilityUserMagic();
                //verVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_Custom, "TM_Explosion", "_ver", true);
                //pwrVal = TM_Calc.GetMagicSkillLevel(caster, comp.MagicData.MagicPowerSkill_Custom, "TM_Explosion", "_pwr", true);
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, TorannMagicDefOf.TM_Explosion);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, TorannMagicDefOf.TM_Explosion);
                this.arcaneDmg = comp.arcaneDmg;
                this.CheckSpawnSustainer();
                this.strikePos = base.Position;
                this.duration = 360 - (verVal*3);
                this.damage = this.DamageAmount * this.arcaneDmg * (1f + (.02f * pwrVal));
                this.radius = this.def.projectile.explosionRadius + (int)(verVal/10);
                this.initialized = true;
                this.outerRing = TM_Calc.GetOuterRing(this.strikePos, radius - 1, radius);
                Color color = colorInt.ToColor;
                Projectile_Explosion.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            }

            if (this.sustainer != null)
            {
                this.sustainer.info.volumeFactor = (this.age) / (this.duration);
                this.sustainer.Maintain();
                if (this.TicksLeft <= 0)
                {
                    this.sustainer.End();
                    this.sustainer = null;
                }
            }

            //there are 6 + 3 phases to explosion (this is no simple matter)
            //phase 1 - warmup and power collection; depicted by wind drawing into a focal point
            //phase 2 - pause (for dramatic effect)
            //phase 3 - initial explosion, ie the "shockwave"
            //phase 4 - ripping winds (the debris launched by the explosion)
            //phase 5 - burning winds (heat and flame - scorched earth)
            //phase 6 - aftershock 
            //training adds 3 phases
            //phase 3a - emp
            //phase 4a - secondary explosions
            //phase 5a - radiation
            
            //warmup 2 seconds
            if (this.age <= (int)(this.duration *.4f) && this.outerRing.Count > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 startPos = outerRing.RandomElement().ToVector3Shifted();
                    Vector3 direction = TM_Calc.GetVector(startPos, strikePos.ToVector3Shifted());
                    TM_MoteMaker.ThrowGenericFleck(FleckDefOf.Smoke, startPos, this.Map, .8f, .3f, .05f, .6f, 0, 12f, (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat(), 0);
                }
            }
            else if(this.age <= (int)(this.duration * .6f))
            {
                //pause                
            }
            else if(this.age > (int)(this.duration * .6f) && !phase3Flag)
            {
                if (!phase2Flag)
                {
                    TargetInfo ti = new TargetInfo(this.strikePos, map, false);
                    TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, map, Vector3.zero, 2, 0f, .1f, .4f, 0, 15f);
                    Effecter igniteED = TorannMagicDefOf.TM_ExplosionED.Spawn();
                    igniteED.Trigger(new TargetInfo(this.strikePos, map, false), new TargetInfo(this.strikePos, map, false));
                    igniteED.Cleanup();
                    SoundInfo info = SoundInfo.InMap(new TargetInfo(this.strikePos, map, false), MaintenanceType.None);
                    info.pitchFactor = .75f;
                    info.volumeFactor = 2.6f;
                    TorannMagicDefOf.TM_FireWooshSD.PlayOneShot(info);
                    this.phase2Flag = true;
                }
                this.expandingTick++;
                if (expandingTick <= this.radius)
                {
                    IntVec3 centerCell = base.Position;
                    if (this.expandingTick <= 1 || oldExplosionCells.Count() <= 0)
                    {
                        oldExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick - 1, true);
                    }
                    else
                    {
                        oldExplosionCells = newExplosionCells;
                    }

                    newExplosionCells = GenRadial.RadialCellsAround(centerCell, expandingTick, true);
                    IEnumerable<IntVec3> explosionCells = newExplosionCells.Except(oldExplosionCells);
                    foreach (IntVec3 cell in explosionCells)
                    {
                        if (cell.InBoundsWithNullCheck(map))
                        {
                            Vector3 heading = (cell - centerCell).ToVector3();
                            float distance = heading.magnitude;
                            Vector3 direction = TM_Calc.GetVector(centerCell, cell);
                            float angle = (Quaternion.AngleAxis(90, Vector3.up) * direction).ToAngleFlat();
                            if (this.expandingTick >= 6 && this.expandingTick < 8)
                            {
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_DirectionalDirt, cell.ToVector3Shifted(), map, .8f, .2f, .15f, .5f, 0, 7f, angle, angle);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ExpandingFlame, cell.ToVector3Shifted(), map, 1.1f, .3f, .02f, .25f, 0, 15f, angle, angle);
                            }
                            List<Thing> hitList = cell.GetThingList(map);
                            Thing burnThing = null;
                            for (int j = 0; j < hitList.Count; j++)
                            {
                                burnThing = hitList[j];
                                DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, Mathf.RoundToInt(Rand.Range(this.damage *.25f, this.damage *.35f)), 1, -1, this.launcher, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
                                burnThing.TakeDamage(dinfo);
                            }                            
                        }
                    }
                }
                else
                {
                    TargetInfo ti = new TargetInfo(this.strikePos, map, false);
                    TM_MoteMaker.MakeOverlay(ti, TorannMagicDefOf.TM_Mote_PsycastAreaEffect, map, Vector3.zero, 4, 0f, .1f, .4f, .5f, 2f);
                    this.expandingTick = 0;
                    this.phase3Flag = true;                    
                }
            }
            else if(phase3Flag)
            {
                this.expandingTick++;
                if(expandingTick < 4)
                {
                    float energy = 80000 * this.arcaneDmg;
                    GenTemperature.PushHeat(this.strikePos, this.Map, energy);
                    GenExplosion.DoExplosion(this.strikePos, this.Map, this.radius/(4-this.expandingTick), DamageDefOf.Bomb, this.launcher, Mathf.RoundToInt(Rand.Range(this.damage *.7f, this.damage*.9f)), 1, DamageDefOf.Bomb.soundExplosion, null, null, null, null, 0, 0, false, null, 0, 0, .4f, true);
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }        

        public override void Draw()
        {
            base.Draw();
            if (initialized)
            {
                if (this.age <= (int)(this.duration * .6f))
                {
                    //DrawSmiteBeams(this.strikePos, this.beamAge);
                }
                else if(this.age > (int)(this.duration *.6f) && this.age <= (int)this.duration * .7f)
                {
                    this.beamAge = this.age - (int)(duration * .6f);
                    DrawSmiteBeams(this.strikePos, this.beamAge);
                }
                if(this.age <= (int)(this.duration * .6f))
                {
                    float sizer = 1f * (float)(this.radius / this.def.projectile.explosionRadius);
                    if(this.age > (int)(this.duration * .4f))
                    {
                        sizer = ((this.duration * .6f) - age) / ((this.duration * .6f) - (this.duration * .4f));
                    }
                    if (this.age > (int)(this.duration * .1f))
                    {
                        outerRingAngle+=3;
                        DrawFlameRing(this.strikePos, 26f*sizer, outerRingAngle);
                    }
                    if (this.age > (int)(this.duration * .18f))
                    {
                        middleRingAngle += 7;
                        DrawFlameRing(this.strikePos, 13f*sizer, middleRingAngle);
                    }
                    if (this.age > (int)(this.duration * .25f))
                    {
                        innerRingAngle += 13;
                        DrawFlameRing(this.strikePos, 7f*sizer, innerRingAngle);
                    }
                }
            }
        }

        public void DrawFlameRing(IntVec3 pos, float size, float angle)
        {
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos.ToVector3Shifted(), Quaternion.Euler(0f, angle, 0f), new Vector3(size, 1f, size));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_Explosion.FireCircle, 0);
        }

        public void DrawSmiteBeams(IntVec3 pos, int wrathAge)
        {
            float lanceWidth = .1f + wrathAge;
            float lanceLength = ((float)base.Map.Size.z * 1.4f);
            Vector3 a = Vector3Utility.FromAngleFlat(this.angle - 90f);  //angle of beam
            Vector3 lanceVector = this.strikePos.ToVector3Shifted() + a * lanceLength * 0.5f;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(lanceVector, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceLength));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_Explosion.BeamMat, 0, null, 0, Projectile_Explosion.MatPropertyBlock);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(lanceVector - (.5f * a * lanceWidth), Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceWidth));  //drawer for beam start
            Graphics.DrawMesh(MeshPool.plane10, matrix2, Projectile_Explosion.BeamEndMat, 0, null, 0, Projectile_Explosion.MatPropertyBlock);
        }

        public override void Tick()
        {
            base.Tick();
            this.age++;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            bool flag = this.age <= this.duration;
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


