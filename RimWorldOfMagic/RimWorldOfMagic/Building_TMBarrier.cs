using Verse;
using UnityEngine;

namespace TorannMagic
{
    public class Building_TMBarrier : Building
    {

        private bool initialized = false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        }
                
        //public override void Tick()
        //{
        //    if(!initialized)
        //    {
        //        initialized = true;
        //    }
        //    if(Find.TickManager.TicksGame % 4 == 0)
        //    {
        //        TM_MoteMaker.ThrowBarrierMote(this.DrawPos, this.Map, .7f);
        //    }
        //    base.Tick();
        //}

        public override void Draw()
        {
            base.Draw();
            Vector3 vector = base.DrawPos;
            float size = Rand.Range(1.60f, 1.70f);
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(size,size, size);
            Matrix4x4 matrix = default(Matrix4x4);
            float angle = Rand.Range(0, 360);
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TM_MatPool.barrier_Mote_Mat, 0);            
        }
    }
}
