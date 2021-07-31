using Verse;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMCooler : Building
    {

        private float arcaneEnergyCur = 0;
        private float arcaneEnergyMax = 1;

        private static readonly Material coolerMat_1 = MaterialPool.MatFrom("Other/cooler", false);
        private static readonly Material coolerMat_2 = MaterialPool.MatFrom("Other/coolerB", false);
        private static readonly Material coolerMat_3 = MaterialPool.MatFrom("Other/coolerC", false);

        private int matRng = 0;
        private float matMagnitude = 1;
        private int nextSearch = 0;
        public bool defensive = false;
        public bool buffCool = false;
        public bool buffFresh = false;

        private bool initialized = false;

        //public override void SpawnSetup(Map map, bool respawningAfterLoad)
        //{
        //    base.SpawnSetup(map, respawningAfterLoad);
        //    //LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        //}

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.defensive, "defensive", false, false);
            Scribe_Values.Look<bool>(ref this.buffCool, "buffCool", false, false);
            Scribe_Values.Look<bool>(ref this.buffFresh, "buffFresh", false, false);
            base.ExposeData();
        }

        public override void Tick()
        {
            if(!initialized)
            {
                initialized = true;
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(400, 500);
            }
            if(Find.TickManager.TicksGame % 8 == 0)
            {
                this.matRng++;
                if(this.matRng >= 3)
                {
                    matRng = 0;
                }
            }
            if (Find.TickManager.TicksGame >= this.nextSearch)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(400, 500);
                if (defensive)
                {
                    List<Pawn> ePawns = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 10, this.factionInt, false);
                    if (ePawns != null && ePawns.Count > 0)
                    {
                        for (int i = 0; i < ePawns.Count; i++)
                        {
                            if (ePawns[i].Faction.HostileTo(this.Faction))
                            {
                                HealthUtility.AdjustSeverity(ePawns[i], TorannMagicDefOf.TM_FrostSlowHD, .4f);
                                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Ice, ePawns[i].DrawPos, this.Map, 1f, .3f, .1f, .8f, Rand.Range(-100, 100), .4f, Rand.Range(0, 35), Rand.Range(0, 360));
                            }
                        }
                    }
                }
                if(buffCool)
                {
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 7, this.Faction, true);
                    if(pList != null && pList.Count > 0)
                    {
                        for(int i =0; i < pList.Count; i++)
                        {
                            Pawn p = pList[i];
                            if (p.health != null && p.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_CoolHD, 0.25f);
                            }
                        }
                    }
                }
                if(buffFresh)
                {
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 7, this.Faction, true);
                    if (pList != null && pList.Count > 0)
                    {
                        for (int i = 0; i < pList.Count; i++)
                        {
                            Pawn p = pList[i];
                            if (p.health != null && p.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_RefreshedHD, 0.2f);
                            }
                        }
                    }
                }
            }
            base.Tick();

        }        

        public override void Draw()
        {
            base.Draw();

            Vector3 vector = base.DrawPos;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(matMagnitude, matMagnitude, matMagnitude);
            Matrix4x4 matrix = default(Matrix4x4);
            float angle = 0f;
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            if (matRng == 0)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMCooler.coolerMat_1, 0);
            }
            else if (matRng == 1)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMCooler.coolerMat_2, 0);
            }
            else 
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMCooler.coolerMat_3, 0);
            }            
        }
    }
}
