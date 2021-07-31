using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace TorannMagic.WorldTransport
{
    [StaticConstructorOnStartup]
    public class TM_DropPodLeaving : FlyShipLeaving
    {
        public IntVec3 arrivalCell = default(IntVec3);
        public bool draftFlag = false;

        private bool alreadyLeft;
        private static List<Thing> tmpActiveDropPods = new List<Thing>();
        private int maxTicks = 40;
        private Vector3 startingPos = default(Vector3);

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BombardMat = MaterialPool.MatFrom("Other/Bombardment", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

        private const int beamLength = 200;

        protected override void LeaveMap()
        {
            ModOptions.Constants.SetPawnInFlight(true);
            if (alreadyLeft)
            {
                base.LeaveMap();
            }
            else if (groupID < 0)
            {
                Log.Error("Drop pod left the map, but its group ID is " + groupID);
                Destroy();
            }
            else if (destinationTile < 0)
            {
                Log.Error("Drop pod left the map, but its destination tile is " + destinationTile);
                Destroy();
            }
            else
            {
                Lord lord = TransporterUtility.FindLord(groupID, base.Map);
                if (lord != null)
                {
                    base.Map.lordManager.RemoveLord(lord);
                }
                WorldTransport.TM_TravelingTransportPods travelingTransportPods = (WorldTransport.TM_TravelingTransportPods)WorldObjectMaker.MakeWorldObject(TorannMagicDefOf.TM_TravelingTransportLightBeam);
                travelingTransportPods.Tile = base.Map.Tile;
                travelingTransportPods.SetFaction(Faction.OfPlayer);
                travelingTransportPods.destinationTile = destinationTile;
                travelingTransportPods.arrivalAction = arrivalAction;
                travelingTransportPods.destinationCell = arrivalCell;
                if (this.def == TorannMagicDefOf.TM_LightPodLeaving)
                {
                    travelingTransportPods.TravelSpeed = .025f;
                    travelingTransportPods.draftFlag = this.draftFlag;
                }
                Find.WorldObjects.Add(travelingTransportPods);
                tmpActiveDropPods.Clear();
                tmpActiveDropPods.AddRange(base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod));
                for (int i = 0; i < tmpActiveDropPods.Count; i++)
                {
                    WorldTransport.TM_DropPodLeaving dropPodLeaving = tmpActiveDropPods[i] as WorldTransport.TM_DropPodLeaving;
                    if (dropPodLeaving != null && dropPodLeaving.groupID == groupID)
                    {
                        dropPodLeaving.alreadyLeft = true;
                        travelingTransportPods.AddPod(dropPodLeaving.Contents, justLeftTheMap: true);
                        dropPodLeaving.Contents = null;
                        dropPodLeaving.Destroy();
                    }
                }
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            startingPos = this.DrawPos;
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void Tick()
        {
            if (ticksToImpact == maxTicks)
            {
                LeaveMap();
            }
            base.Tick();
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
            Vector3 drawPos = this.startingPos + angleVector;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, beamLength));   //drawer for beam
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_DropPodLeaving.BeamMat, 0, null, 0, TM_DropPodLeaving.MatPropertyBlock);
            Matrix4x4 matrix2 = default(Matrix4x4);
            matrix2.SetTRS(this.startingPos + Vector3Utility.FromAngleFlat(this.angle + 90) * .5f * lanceWidth, Quaternion.Euler(0f, this.angle, 0f), new Vector3(lanceWidth, 1f, lanceWidth));                 //drawer for beam start
            Graphics.DrawMesh(MeshPool.plane10, matrix2, TM_DropPodLeaving.BeamEndMat, 0, null, 0, TM_DropPodLeaving.MatPropertyBlock);
        }
    }
}
