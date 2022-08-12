using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using System.Diagnostics;
using UnityEngine;
using RimWorld;


namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Building_TM_DMP : Building
    {

        public static float effectRadius = 36f;
        private int rotationRate = 4;
        private float arcaneEnergyCur = 0;
        private int matRot = 0;
        private float matMagnitude = .5f;
        private float matMagnitudeValue = .01f;
        private static List<IntVec3> portableCells = new List<IntVec3>();

        private static readonly Material dmpMat_0 = MaterialPool.MatFrom("Other/dmp0", false);
        private static readonly Material dmpMat_1 = MaterialPool.MatFrom("Other/dmp1", false);
        private static readonly Material dmpMat_2 = MaterialPool.MatFrom("Other/dmp2", false);
        private static readonly Material dmpMat_3 = MaterialPool.MatFrom("Other/dmp3", false);
        private static readonly Material dmpMat_4 = MaterialPool.MatFrom("Other/dmp4", false);
        private static readonly Material dmpMat_5 = MaterialPool.MatFrom("Other/dmp5", false);
        private static readonly Material dmpMat_6 = MaterialPool.MatFrom("Other/dmp6", false);
        private static readonly Material dmpMat_7 = MaterialPool.MatFrom("Other/dmp7", false);

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public IEnumerable<IntVec3> PortableCells
        {
            get
            {
                return Building_TM_DMP.PortableCellsAround(base.InteractionCell, base.Map);
            }
        }

        public bool IsOn
        {
            get
            {
                return this.TryGetComp<CompFlickable>().SwitchIsOn;
            }
        }

        public float ArcaneEnergyCur
        {
            get
            {
                return this.TryGetComp<CompRefuelable>().FuelPercentOfMax;
            }
            set
            {
                this.TryGetComp<CompRefuelable>().Refuel(value);
            }
        }

        public float TargetArcaneEnergyPct
        {
            get
            {
                return this.TryGetComp<CompRefuelable>().TargetFuelLevel / this.TryGetComp<CompRefuelable>().Props.fuelCapacity;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            CompAbilityUserMagic comp = myPawn.GetCompAbilityUserMagic();
            if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some, false, false, TraverseMode.ByPawn))
            {
                list.Add(new FloatMenuOption("CannotUseNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
            }
            else
            {                
                if (comp.IsMagicUser && this.IsOn)
                {
                    list.Add(new FloatMenuOption("TM_ChargeManaStorage".Translate(
                            Mathf.RoundToInt(this.arcaneEnergyCur * 100)
                        ), delegate
                    {
                        Job job = new Job(TorannMagicDefOf.ChargePortal, this);
                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
            }
            return list;
        }

        public static List<IntVec3> PortableCellsAround(IntVec3 pos, Map map)
        {
            Building_TM_DMP.portableCells.Clear();
            if (!pos.InBoundsWithNullCheck(map))
            {
                return Building_TM_DMP.portableCells;

            }
            Region region = pos.GetRegion(map, RegionType.Set_All);
            if (region == null)
            {
                return Building_TM_DMP.portableCells;
            }
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 current in r.Cells)
                {
                    if (current.InHorDistOf(pos, Building_TM_DMP.effectRadius))
                    {
                        Building_TM_DMP.portableCells.Add(current);
                    }
                }
                return false;
            }, 54, RegionType.Set_Passable);
            return Building_TM_DMP.portableCells;
        }

        public override void Tick()
        {
            base.Tick();
            if(Find.TickManager.TicksGame % this.rotationRate == 0)
            {
                this.matRot++;
                if(this.matRot >= 8)
                {
                    this.matRot = 0;
                }
                this.matMagnitude += this.matMagnitudeValue;
                if(this.matMagnitude >= .5f)
                {
                    this.matMagnitudeValue = -.005f;
                }
                if(this.matMagnitude <= .2f)
                {
                    this.matMagnitudeValue = .005f;
                }
            }

            if (Find.TickManager.TicksGame % 240 == 0 && this.IsOn)
            {
                List<Pawn> mapPawns = this.Map.mapPawns.AllPawnsSpawned;
                Pawn pawn = null;
                for(int i = 0; i < mapPawns.Count; i++)
                {
                    pawn = mapPawns[i];
                    if(!pawn.DestroyedOrNull() && pawn.Spawned && !pawn.Dead && !pawn.Downed && pawn.RaceProps != null && !pawn.AnimalOrWildMan() && pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction == this.Faction)
                    {
                        CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                        float rangeToTarget = (pawn.Position - this.Position).LengthHorizontal;
                        if (pawn.drafter != null && TM_Calc.IsMagicUser(pawn) && rangeToTarget <= effectRadius && comp != null && comp.Mana != null)
                        {
                            if(pawn.Drafted && comp.Mana.CurLevelPercentage <= .9f && this.ArcaneEnergyCur >= .01f)
                            {
                                TransferMana(comp);
                                break;
                            }

                            if (!pawn.Drafted && comp.Mana.CurInstantLevelPercentage < .4f && this.ArcaneEnergyCur >= .01f)
                            {
                                TransferMana(comp);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void TransferMana(CompAbilityUserMagic comp)
        {
            this.ArcaneEnergyCur -= 20;
            comp.Mana.CurLevel += .16f;
            for (int i = 0; i < 4; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, this.DrawPos, this.Map,  .4f + .5f * i, 0.2f, .02f + (.15f * i), .4f - (.06f * i), Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360));                           
            }
            for (int i = 0; i < 4; i++)
            {
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Casting, comp.Pawn.DrawPos, this.Map, 1.5f - (.4f * i), 0.2f, .02f + (.15f * i), .4f + (.06f * i), Rand.Range(-300, 300), 0, 0, Rand.Range(0, 360)); ;
            }
            TM_MoteMaker.ThrowManaPuff(comp.Pawn.DrawPos, comp.Pawn.Map, 1f);
        }

        public override void Draw()
        {
            base.Draw();
            Vector3 vector = base.DrawPos;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 s = new Vector3(matMagnitude, matMagnitude, matMagnitude);
            Matrix4x4 matrix = default(Matrix4x4);
            float angle = 0;
            matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
            if (matRot == 0)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_0, 0);
            }
            else if (matRot == 1)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_1, 0);
            }
            else if (matRot == 2)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_2, 0);
            }
            else if (matRot == 3)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_3, 0);
            }
            else if (matRot == 4)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_4, 0);
            }
            else if (matRot == 5)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_5, 0);
            }
            else if (matRot == 6)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_6, 0);
            }
            else
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TM_DMP.dmpMat_7, 0);
            }
            
        }
    }
}
