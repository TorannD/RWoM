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
    public class Building_TMArcaneCapacitor : Building
    {

        public static float effectRadius = 12f;
        private float arcaneEnergyCur = 0;
        private static List<IntVec3> portableCells = new List<IntVec3>();

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public IEnumerable<IntVec3> PortableCells
        {
            get
            {
                return Building_TMArcaneCapacitor.PortableCellsAround(base.InteractionCell, base.Map);
            }
        }

        public bool CapacitorIsOn
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
                if (comp.IsMagicUser && this.CapacitorIsOn)
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
            Building_TMArcaneCapacitor.portableCells.Clear();
            if (!pos.InBoundsWithNullCheck(map))
            {
                return Building_TMArcaneCapacitor.portableCells;

            }
            Region region = pos.GetRegion(map, RegionType.Set_All);
            if (region == null)
            {
                return Building_TMArcaneCapacitor.portableCells;
            }
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 current in r.Cells)
                {
                    if (current.InHorDistOf(pos, Building_TMArcaneCapacitor.effectRadius))
                    {
                        Building_TMArcaneCapacitor.portableCells.Add(current);
                    }
                }
                return false;
            }, 13, RegionType.Set_Passable);
            return Building_TMArcaneCapacitor.portableCells;
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 120 == 0 && this.CapacitorIsOn)
            {
                List<Pawn> mapPawns = this.Map.mapPawns.AllPawnsSpawned;
                Pawn pawn = null;
                for(int i = 0; i < mapPawns.Count; i++)
                {
                    pawn = mapPawns[i];
                    if(!pawn.DestroyedOrNull() && !pawn.Dead && !pawn.Downed && pawn.RaceProps.Humanlike && pawn.Faction != null && pawn.Faction == this.Faction)
                    {
                        CompAbilityUserMagic comp = pawn.GetCompAbilityUserMagic();
                        float rangeToTarget = (pawn.Position - this.Position).LengthHorizontal;
                        if (pawn.drafter != null && TM_Calc.IsMagicUser(pawn) &&  rangeToTarget <= effectRadius && comp != null && comp.Mana != null)
                        {
                            if( pawn.Drafted && comp.Mana.CurLevelPercentage <= .9f && this.ArcaneEnergyCur > .05f)
                            {
                                TransferMana(comp, rangeToTarget * .4f);
                            }

                            if(!pawn.Drafted && (2.5f*comp.Mana.CurLevelPercentage < this.ArcaneEnergyCur))
                            {
                                TransferMana(comp, rangeToTarget * .4f);
                            }
                        }
                    }
                }
            }
        }

        private void TransferMana(CompAbilityUserMagic comp, float magnitude)
        {
            this.ArcaneEnergyCur -= 7;
            comp.Mana.CurLevel += .05f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 moteDirection = TM_Calc.GetVector(this.Position, comp.Pawn.Position);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ArcaneStream, this.DrawPos, this.Map, Rand.Range(.4f, .8f), 0.15f, .02f + (.08f * i), .3f - (.04f * i), Rand.Range(-10, 10), magnitude + magnitude*i, (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat(), Rand.Chance(.5f) ? (Quaternion.AngleAxis(90, Vector3.up) * moteDirection).ToAngleFlat() : (Quaternion.AngleAxis(-90, Vector3.up) * moteDirection).ToAngleFlat());                           
            }
            for (int i = 0; i < 10; i++)
            {
                float direction = Rand.Range(0, 360);
                TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Psi_Arcane, this.DrawPos, this.Map, Rand.Range(.3f, .5f), 0.1f, .02f, .1f, 0, Rand.Range(5, 7), direction, direction);
            }
            TM_MoteMaker.ThrowManaPuff(comp.Pawn.DrawPos, comp.Pawn.Map, 1f);
        }
    }
}
