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
    public class Building_TMPortal : Building
    {
        private bool isPaired = false;
        private Map portalDestinationMap = null;
        private IntVec3 portalDestinationPosition = IntVec3.Invalid;
        private float arcaneEnergyCur = 0;
        private float arcaneEnergyMax = 1;
        private float launchDistance;
        private int maxLaunchDistance = 999;
        private float portalEnergyCost = .02f;

        private static readonly Vector2 BarSize = new Vector2(1.2f, 0.2f);
        private static readonly Material EnergyBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.0f, 0.0f, 1f), false);
        private static readonly Material EnergyBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.4f, 0.4f, 0.4f), false);
        private static List<IntVec3> portableCells = new List<IntVec3>();

        private static readonly Material portalMat_1 = MaterialPool.MatFrom("Motes/portal_swirl1", false);
        private static readonly Material portalMat_2 = MaterialPool.MatFrom("Motes/portal_swirl2", false);
        private static readonly Material portalMat_3 = MaterialPool.MatFrom("Motes/portal_swirl3", false);
        private static readonly Material portalMat_4 = MaterialPool.MatFrom("Motes/portal_swirl4", false);
        private static readonly Material portalMat_5 = MaterialPool.MatFrom("Motes/portal_swirl5", false);
        private int matRng = 0;
        private float matMagnitude = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.isPaired, "isPaired", false, false);
            Scribe_Values.Look<float>(ref this.arcaneEnergyCur, "arcaneEnergyCur", 0f, false);
            Scribe_Values.Look<float>(ref this.arcaneEnergyMax, "arcaneEnergyMax", 0f, false);
            Scribe_References.Look<Map>(ref this.portalDestinationMap, "portalDestinationMap", false);
            //Scribe_Values.Look<Map>(ref this.portalDestinationMap, "portalDestinationMap", null, false);
            Scribe_Values.Look<IntVec3>(ref this.portalDestinationPosition, "portalDestinationPosition", IntVec3.Invalid, false);
        }

        //public Map Map
        //{
        //    get
        //    {
        //        if ((int)this.mapIndexOrState >= 0)
        //        {
        //            return Find.Maps[(int)this.mapIndexOrState];
        //        }
        //        return null;
        //    }
        //}
   //     Scribe_Values.Look<sbyte>(ref this.mapIndexOrState, "map", -1, false);
			//if (Scribe.mode == LoadSaveMode.LoadingVars && (int) this.mapIndexOrState >= 0)

   //         {
   //         this.mapIndexOrState = -1;
   //     }

        public IEnumerable<IntVec3> PortableCells
        {
            get
            {
                return Building_TMPortal.PortableCellsAround(base.InteractionCell, base.Map);
            }
        }

        public bool IsPaired
        {
            get
            {
                return isPaired;
            }
            set
            {
                isPaired = value;
            }
        }
        public Map PortalDestinationMap
        {
            get
            {
                return portalDestinationMap;
                //if((int)this.portalDestinationMap >=0)
                //{
                //    return Find.Maps[(int)this.portalDestinationMap];
                //}
                //return null;
            }
            set
            {
                portalDestinationMap = value;
            }
        }
        
        public IntVec3 PortalDestinationPosition
        {
            get
            {
                return portalDestinationPosition;
            }
            set
            {
                portalDestinationPosition = value;
            }
        }

        public float ArcaneEnergyCur
        {
            get
            {
                return arcaneEnergyCur;
            }
            set
            {
                arcaneEnergyCur = value;
            }
        }

        public int LaunchDistance
        {
            get
            {
                return (int)(this.arcaneEnergyCur * 100 * this.arcaneEnergyMax);
            }
        }

        public int MaxLaunchDistance
        {
            get
            {
                return this.maxLaunchDistance;
            }
        }

        public float PortalEnergyCost
        {
            get
            {
                return this.portalEnergyCost;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            LessonAutoActivator.TeachOpportunity(ConceptDef.Named("TM_Portals"), OpportunityType.GoodToKnow);
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>() != null)
            {
                yield return new Command_Action
                {
                    action = new Action(this.MakeMatchingStockpile),
                    hotKey = KeyBindingDefOf.Misc3,
                    defaultDesc = "TM_CommandMakePortalStockpileDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true),
                    defaultLabel = "TM_CommandMakePortalStockpileLabel".Translate()
                };
            }
        }

        private void MakeMatchingStockpile()
        {
            Designator des = DesignatorUtility.FindAllowedDesignator<Designator_ZoneAddStockpile_Resources>();
            des.DesignateMultiCell(from c in this.PortableCells
                                   where des.CanDesignateCell(c).Accepted
                                   select c);
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

                if (!isPaired)
                {
                    if (comp.spell_FoldReality || comp.MagicData.MagicPowersA.FirstOrDefault((MagicPower x) => x.abilityDef == TorannMagicDefOf.TM_FoldReality).learned)
                    {
                        if (comp.Mana.CurLevel >= .7f)
                        {
                            list.Add(new FloatMenuOption("TM_SelectPortalDestination".Translate(), delegate
                            {
                                Job job = new Job(TorannMagicDefOf.PortalDestination, this);
                                myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            }, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else
                        {
                            list.Add(new FloatMenuOption("TM_NeedManaToActivatePortal".Translate(
                                comp.Mana.CurInstantLevel.ToString("0.000")
                            ), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                    }
                    else
                    {
                        list.Add(new FloatMenuOption("TM_PortalNoGatewayLearned".Translate(
                            myPawn.LabelShort
                        ), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                }
                if (isPaired && this.arcaneEnergyCur >= .05f)
                {
                    list.Add(new FloatMenuOption("TM_UsePortal".Translate(), delegate
                    {
                        Job job = new Job(TorannMagicDefOf.UsePortal, this);
                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                if (isPaired && comp.IsMagicUser)
                {
                    list.Add(new FloatMenuOption("TM_ChargePortal".Translate(
                            Mathf.RoundToInt(this.arcaneEnergyCur * 100)
                        ), delegate
                    {
                        Job job = new Job(TorannMagicDefOf.ChargePortal, this);
                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                if (isPaired && this.arcaneEnergyCur >= .1f)
                {
                    list.Add(new FloatMenuOption("TM_PortalStockpile".Translate(), delegate
                    {
                        Job job = new Job(TorannMagicDefOf.PortalStockpile, this);
                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                if (isPaired && comp.IsMagicUser)
                {
                    list.Add(new FloatMenuOption("TM_DeactivatePortal".Translate(), delegate
                    {
                        Job job = new Job(TorannMagicDefOf.DeactivatePortal, this);
                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
            }
            return list;
        }

        public static List<IntVec3> PortableCellsAround(IntVec3 pos, Map map)
        {
            Building_TMPortal.portableCells.Clear();
            if (!pos.InBoundsWithNullCheck(map))
            {
                return Building_TMPortal.portableCells;

            }
            Region region = pos.GetRegion(map, RegionType.Set_Passable);
            if (region == null)
            {
                return Building_TMPortal.portableCells;
            }
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 current in r.Cells)
                {
                    if (current.InHorDistOf(pos, 2.4f))
                    {
                        Building_TMPortal.portableCells.Add(current);
                    }
                }
                return false;
            }, 13, RegionType.Set_Passable);
            return Building_TMPortal.portableCells;
        }

        public override void Tick()
        {
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                this.matRng = Rand.RangeInclusive(0, 4);
                this.matMagnitude = Math.Min(matMagnitude, 1f);
                this.matMagnitude = Math.Max(matMagnitude, 0f);
                this.matMagnitude = 3 * this.arcaneEnergyCur;

                IntVec3 curCell;
                IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(this.InteractionCell, 3, true);
                for (int i = 0; i < targets.Count(); i++)
                {
                    curCell = targets.ToArray<IntVec3>()[i];
                    if (curCell.InBoundsWithNullCheck(this.Map) && curCell.IsValid)
                    {
                        Pawn interactingPawn = curCell.GetFirstPawn(this.Map);
                        if (interactingPawn != null)
                        {
                            if (interactingPawn.jobs.curJob.def == TorannMagicDefOf.UsePortal)
                            {
                                try
                                {
                                    PortalPawn(interactingPawn);
                                    this.arcaneEnergyCur -= .05f;
                                }
                                catch
                                {
                                    if (!interactingPawn.Spawned)
                                    {
                                        GenSpawn.Spawn(interactingPawn, this.InteractionCell, this.Map);
                                        Messages.Message("TM_PortalFailed".Translate(
                                                interactingPawn.LabelShort
                                            ), MessageTypeDefOf.RejectInput);

                                    }
                                    this.IsPaired = false;                                    
                                }

                            }
                        }
                    }
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (this.isPaired)
            {
                Vector3 vector = base.DrawPos;
                vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                Vector3 s = new Vector3(matMagnitude, matMagnitude, matMagnitude);
                Matrix4x4 matrix = default(Matrix4x4);
                float angle = 0f;
                matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);
                if (matRng == 0)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPortal.portalMat_1, 0);
                }
                else if (matRng == 1)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPortal.portalMat_2, 0);
                }
                else if (matRng == 2)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPortal.portalMat_3, 0);
                }
                else if (matRng == 3)
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPortal.portalMat_4, 0);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Building_TMPortal.portalMat_5, 0);
                }
            }
        }

        private void PortalPawn(Pawn pawn)
        {
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1f);
            TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));
            pawn.DeSpawn();
            GenSpawn.Spawn(pawn, this.PortalDestinationPosition, this.PortalDestinationMap);
            TM_MoteMaker.ThrowCastingMote(pawn.DrawPos, pawn.Map, Rand.Range(1.2f, 2f));
            FleckMaker.ThrowHeatGlow(pawn.Position, pawn.Map, 1.6f);
        }
    }
}
