using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMMageLight : Building
    {

        private static readonly Material sunlightMat_1 = MaterialPool.MatFrom("Other/sunlight1", false);
        private static readonly Material sunlightMat_2 = MaterialPool.MatFrom("Other/sunlight2", false);
        private static readonly Material sunlightMat_3 = MaterialPool.MatFrom("Other/sunlight3", false);

        private int matRng = 0;
        private float matMagnitude = 1;
        private bool objectFloatingDown = false;
        private Vector3 objectPosition = default(Vector3);
        private float objectOffset = .5f;

        private bool initialized = false;
                
        public override void Tick()
        {
            if(!initialized)
            {
                objectPosition = this.DrawPos;
                initialized = true;
            }
            base.Tick();
        }        

        public override void Draw()
        {
            base.Draw();

            if (this.objectFloatingDown)
            {
                if (this.objectOffset < .35f)
                {
                    this.objectFloatingDown = false;
                }
                this.objectOffset -= .001f;
            }
            else
            {
                if (this.objectOffset > .6f)
                {
                    this.objectFloatingDown = true;
                }
                this.objectOffset += .001f;
            }

            this.objectPosition = this.DrawPos;
            this.objectPosition.z += this.objectOffset;
            this.objectPosition.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = Rand.Range(0, 360);
            Vector3 s = new Vector3(.35f, 1f, .35f);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(this.objectPosition, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_RenderQueue.mageLightMat, 0);
        }
    }
}
