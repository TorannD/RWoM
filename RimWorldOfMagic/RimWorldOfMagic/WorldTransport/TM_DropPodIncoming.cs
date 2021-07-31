using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TorannMagic.WorldTransport
{
    [StaticConstructorOnStartup]
    public class TM_DropPodIncoming : Skyfaller, IActiveDropPod, IThingHolder
    {
        private int maxTicks = 50;
        private const int beamLength = 200;

        public bool draftFlag = false;

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Other/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        public ActiveDropPodInfo Contents
        {
            get
            {
                return ((TM_ActiveDropPod)innerContainer[0]).Contents;
            }
            set
            {
                ((TM_ActiveDropPod)innerContainer[0]).Contents = value;
            }
        }

        protected override void SpawnThings()
        {
            if (!Contents.spawnWipeMode.HasValue)
            {
                base.SpawnThings();
            }
            else
            {
                for (int num = innerContainer.Count - 1; num >= 0; num--)
                {
                    GenSpawn.Spawn(innerContainer[num], base.Position, base.Map, Contents.spawnWipeMode.Value);
                }
            }
        }

        protected override void Impact()
        {
            for (int i = 0; i < 6; i++)
            {
                FleckMaker.ThrowDustPuff(base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f), base.Map, 1.2f);
            }
            FleckMaker.ThrowLightningGlow(base.Position.ToVector3Shifted(), base.Map, 2f);
            GenClamor.DoClamor(this, 15f, ClamorDefOf.Impact);
            ModOptions.Constants.SetPawnInFlight(false);
            base.Impact();
        }


        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            DrawLightBeam();
        }

        public void DrawLightBeam()
        {
            float lanceWidth = 4f;
            if (this.ticksToImpact < (this.maxTicks * .5f))
            {
                lanceWidth *= (float)this.ticksToImpact / this.maxTicks;
            }
            if (this.ticksToImpact > (this.maxTicks * .5f))
            {
                lanceWidth *= (float)(this.maxTicks - this.ticksToImpact) / this.maxTicks;
            }
            lanceWidth *= Rand.Range(.9f, 1.1f);
            Vector3 angleVector = Vector3Utility.FromAngleFlat(this.angle - 90) * .5f * beamLength;
            Vector3 drawPos = base.Position.ToVector3Shifted() + angleVector;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, beamLength));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_DropPodIncoming.BeamMat, 0, null, 0, TM_DropPodIncoming.MatPropertyBlock);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(base.Position.ToVector3Shifted() + Vector3Utility.FromAngleFlat(this.angle + 90) * .5f * lanceWidth, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceWidth));                 //drawer for beam start
            Graphics.DrawMesh(MeshPool.plane10, matrix2, TM_DropPodIncoming.BeamEndMat, 0, null, 0, TM_DropPodIncoming.MatPropertyBlock);
        }
    }
}
