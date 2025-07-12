using System;
using UnityEngine;
using Verse;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class TM_MeshBolt : Thing
    {
        public static readonly Material Lightning = MatLoader.LoadMat("Weather/LightningBolt", -1);

        private IntVec3 hitThing;

        private Vector3 origin;

        private Mesh boltMesh;

        private Quaternion direction;

        private Material mat;

        public TM_MeshBolt(IntVec3 hitThing, Vector3 origin, Material _mat)
        {
            this.hitThing = hitThing;
            this.origin = origin;
            this.mat = _mat;
        }

        public void CreateBolt()
        {
            Vector3 vector;
            vector.x = (float)this.hitThing.x;
            vector.y = (float)this.hitThing.y;
            vector.z = (float)this.hitThing.z;
            this.direction = Quaternion.LookRotation((vector - this.origin).normalized);
            float distance = Vector3.Distance(this.origin, vector);
            this.boltMesh = TM_MeshMaker.NewBoltMesh(distance, 6f);
            Graphics.DrawMesh(this.boltMesh, this.origin, this.direction, this.mat, 0);
        }

        public void CreateFadedBolt(int magnitude)
        {
            Vector3 vector;
            vector.x = (float)this.hitThing.x;
            vector.y = (float)this.hitThing.y;
            vector.z = (float)this.hitThing.z;
            this.direction = Quaternion.LookRotation((vector - this.origin).normalized);
            float distance = Vector3.Distance(this.origin, vector);
            this.boltMesh = TM_MeshMaker.NewBoltMesh(distance, 6f);
            //Graphics.DrawMesh(this.boltMesh, this.origin, this.direction, this.mat, 0);
            Graphics.DrawMesh(this.boltMesh, this.origin, this.direction, FadedMaterialPool.FadedVersionOf(this.mat, (float)magnitude), 0);
        }
    }
}
