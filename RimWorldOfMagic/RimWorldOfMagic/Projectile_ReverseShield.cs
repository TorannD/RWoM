using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Projectile_ReverseShield : Projectile_AbilityBase
    {

        public int age = -1;
        public int duration = 1200;
        private bool initialized = false;
        private int tickFrequency = 3;
        public float radius = 5;
        public int lastInterceptTick = 0;
        List<IntVec3> shieldBlockCells = new List<IntVec3>();
        List<IntVec3> shieldInternalCells = new List<IntVec3>();
        Color shieldColor = new Color(.15f, 0, .4f);
        bool impacted = false;

        private static readonly Material ForceFieldMat = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public float LastIntercept => lastInterceptTick <= 50 ? lastInterceptTick : 50f;

        public float GetCurrentAlpha => 1f - (.01f * LastIntercept);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.age, "age");
            Scribe_Collections.Look<IntVec3>(ref this.shieldBlockCells, "shieldBlockCells", LookMode.Value);
            Scribe_Collections.Look<IntVec3>(ref this.shieldInternalCells, "shieldInternalCells", LookMode.Value);
        }


        private void Initialize()
        {
            shieldInternalCells = GenRadial.RadialCellsAround(this.Position, radius - 1, true).ToList();
            shieldBlockCells = GenRadial.RadialCellsAround(this.Position, radius + 1, true).Except(shieldInternalCells).ToList();
            this.radius = this.def.projectile.explosionRadius;
            impacted = true;
        }

        protected override void Impact(Thing hitThing)
        {            
            if(!initialized)
            {
                this.initialized = true;
                Initialize();
            }
            if(Find.TickManager.TicksGame % tickFrequency == 0 && shieldInternalCells != null && shieldBlockCells != null && this.Map != null)
            {
                List<Thing> tmpProjs = this.Map.listerThings.AllThings.Where(t => t is Projectile).ToList();
                if (tmpProjs != null)
                {
                    foreach (Thing t in tmpProjs)
                    {
                        Projectile p = t as Projectile;
                        if (p != null && !p.Launcher.DestroyedOrNull() && shieldInternalCells.Contains(p.Launcher.Position) && shieldBlockCells.Contains(p.Position))
                        {
                            lastInterceptTick = 0;
                            float dirAngle = (Quaternion.AngleAxis(90, Vector3.up) * TM_Calc.GetVector(p.Launcher.DrawPos, p.DrawPos)).ToAngleFlat();
                            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Shadow, p.DrawPos, this.Map, Rand.Range(.6f, 1f), .1f, 0f, .2f, Rand.Range(-100, 100), p.def.projectile.speed / 4f, dirAngle, Rand.Range(0, 360));
                            p.Destroy();
                        }
                    }
                }
            }
            base.Impact(hitThing);
            lastInterceptTick++;
            age++;
        }

        public override void Draw()
        {
            if (impacted)
            {
                Vector3 pos = this.Position.ToVector3Shifted();
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                float currentAlpha = GetCurrentAlpha;
                if (currentAlpha > 0f)
                {
                    Color value = shieldColor;
                    value.a *= currentAlpha;
                    MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
                    Matrix4x4 matrixs = default(Matrix4x4);
                    matrixs.SetTRS(pos, Quaternion.identity, new Vector3(radius * 2f, 1f, radius * 2f));
                    Matrix4x4 matrix = default(Matrix4x4);                    
                    matrix.SetTRS(pos, Quaternion.identity, new Vector3(radius * 2f * 1.1f, 1f, radius * 2f * 1.1f));
                    Graphics.DrawMesh(MeshPool.plane10, matrixs, ForceFieldMat, 0, null, 0, MatPropertyBlock);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, ForceFieldMat, 0, null, 0, MatPropertyBlock);
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.age > this.duration)
            {
                base.Destroy(mode);
            }
        }

    }    
}


