using Verse;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TMPowerNode : Building
    {

        private float arcaneEnergyCur = 0;
        private float arcaneEnergyMax = 1;

        private static readonly Material powernodeMat_1 = MaterialPool.MatFrom("Other/energynode_1", false);
        private static readonly Material powernodeMat_2 = MaterialPool.MatFrom("Other/energynode_2", false);
        private static readonly Material powernodeMat_3 = MaterialPool.MatFrom("Other/energynode_3", false);
        private static readonly Material powernodeMat_4 = MaterialPool.MatFrom("Other/energynode_4", false);

        private int matRng = 0;
        private float matMagnitude = 1;

        private bool initialized = false;
        private int nextSearch = 10;
        public bool defensive = false;
        public bool buffJolt = false;
        public bool buffShock = false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.defensive, "defensive", false, false);
            Scribe_Values.Look<bool>(ref this.buffShock, "buffShock", false, false);
            Scribe_Values.Look<bool>(ref this.buffJolt, "buffJolt", false, false);
            base.ExposeData();
        }

        public override void Tick()
        {
            if(!initialized)
            {
                initialized = true;
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(180, 220);
            }
            if(Find.TickManager.TicksGame % 8 == 0)
            {
                this.matRng++;
                if(this.matRng >= 4)
                {
                    matRng = 0;
                }
            }
            if (Find.TickManager.TicksGame >= this.nextSearch)
            {
                this.nextSearch = Find.TickManager.TicksGame + Rand.Range(180, 220);
                if (defensive)
                {
                    Pawn e = TM_Calc.FindNearbyEnemy(this.Position, this.Map, this.factionInt, 20, 0);
                    if (e != null && TM_Calc.HasLoSFromTo(this.Position, e, this, 0, 20))
                    {
                        GenExplosion.DoExplosion(e.Position, this.Map, .4f, DamageDefOf.Stun, this, 4, 1f);
                        if (e.RaceProps.IsMechanoid || TM_Calc.IsRobotPawn(e))
                        {
                            GenExplosion.DoExplosion(e.Position, this.Map, .4f, TMDamageDefOf.DamageDefOf.TM_ElectricalBurn, this, 8, 1f);
                        }
                    }
                }
                if (buffShock)
                {
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 6, this.Faction, true);
                    if (pList != null && pList.Count > 0)
                    {
                        for (int i = 0; i < pList.Count; i++)
                        {
                            Pawn p = pList[i];
                            if (p.health != null && p.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_ShockTherapyHD, 0.12f);
                            }
                        }
                    }
                }
                if (buffJolt)
                {
                    List<Pawn> pList = TM_Calc.FindAllPawnsAround(this.Map, this.Position, 6, this.Faction, true);
                    if (pList != null && pList.Count > 0)
                    {
                        for (int i = 0; i < pList.Count; i++)
                        {
                            Pawn p = pList[i];
                            if (p.health != null && p.health.hediffSet != null)
                            {
                                HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_JoltHD, 0.1f);
                            }
                        }
                    }
                }

            }
            base.Tick();
        }        

        public override void Draw()
        {
            Vector3 vector = base.DrawPos;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(matMagnitude, matMagnitude, matMagnitude);
            Matrix4x4 matrix = default(Matrix4x4);
            float angle = 0f;
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            if (matRng == 0)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPowerNode.powernodeMat_1, 0);
            }
            else if (matRng == 1)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPowerNode.powernodeMat_2, 0);
            }
            else if (matRng == 2)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPowerNode.powernodeMat_3, 0);
            }
            else 
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPowerNode.powernodeMat_4, 0);
            }            
        }
    }
}
