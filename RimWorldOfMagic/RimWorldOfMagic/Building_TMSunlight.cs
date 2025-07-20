using Verse;
using UnityEngine;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMSunlight : Building
    {

        private static readonly Material sunlightMat_1 = MaterialPool.MatFrom("Other/sunlight1", false);
        private static readonly Material sunlightMat_2 = MaterialPool.MatFrom("Other/sunlight2", false);
        private static readonly Material sunlightMat_3 = MaterialPool.MatFrom("Other/sunlight3", false);

        private int matRng = 0;
        private float matMagnitude = 1;

        private bool initialized = false;

        protected override void Tick()
        {
            if(!initialized)
            {
                initialized = true;
            }
            if(Find.TickManager.TicksGame % 16 == 0)
            {
                this.matRng++;
                if(this.matRng >= 4)
                {
                    matRng = 0;
                }
            }
            base.Tick();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            Vector3 vector = base.DrawPos;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(matMagnitude, matMagnitude, matMagnitude);
            Matrix4x4 matrix = default(Matrix4x4);
            float angle = 0f;
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            if (matRng == 0)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMSunlight.sunlightMat_1, 0);
            }
            else if (matRng == 1)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMSunlight.sunlightMat_2, 0);
            }
            else if (matRng == 2)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMSunlight.sunlightMat_3, 0);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMSunlight.sunlightMat_2, 0);
            }
        }
    }
}
