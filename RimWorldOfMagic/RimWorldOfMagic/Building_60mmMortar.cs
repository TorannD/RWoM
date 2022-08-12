using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using AbilityUser;
using Verse;
using Verse.AI;
using UnityEngine;
using Verse.Sound;
using HarmonyLib;



namespace TorannMagic
{   
    [StaticConstructorOnStartup]
    public class Building_60mmMortar : Building
    {
        int mortarMaxRange = 80;
        int mortarMinRange = 20;
        int mortarTicksToFire = 60;
        int mortarCount = 3;
        float mortarAccuracy = 4f;
        ThingDef projectileDef = TorannMagicDefOf.FlyingObject_60mmMortar;

        private int verVal = 0;
        private int pwrVal = 0;
        private int effVal = 0;

        public LocalTargetInfo setTarget = null;
        private TargetingParameters targetingParameters = new TargetingParameters();

        protected CompMannable mannableComp;

        private bool MannedByColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction == Faction.OfPlayer;
        private bool MannedByNonColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction != Faction.OfPlayer;
        private bool PlayerControlled => (base.Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist;
        private bool Manned => MannedByColonist || MannedByNonColonist;
        private bool initialized = false;

        CompAbilityUserMight comp;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.verVal, "verVal", 0, false);
            Scribe_Values.Look<int>(ref this.pwrVal, "pwrVal", 0, false);
            Scribe_Values.Look<int>(ref this.effVal, "effVal", 0, false);
            Scribe_Values.Look<int>(ref this.mortarMaxRange, "mortarMaxRange", 80, false);
            Scribe_Values.Look<int>(ref this.mortarTicksToFire, "mortarTicksToFire", 50, false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mannableComp = GetComp<CompMannable>();
        }

        public override void Tick()
        {
            //base.Tick();

            if (Manned)
            {

                if (!initialized)
                {
                    comp = mannableComp.ManningPawn.GetCompAbilityUserMight();
                    this.verVal = mannableComp.ManningPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_60mmMortar.FirstOrDefault((MightPowerSkill x) => x.label == "TM_60mmMortar_ver").level;
                    this.pwrVal = mannableComp.ManningPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_60mmMortar.FirstOrDefault((MightPowerSkill x) => x.label == "TM_60mmMortar_pwr").level;
                    this.effVal = mannableComp.ManningPawn.GetCompAbilityUserMight().MightData.MightPowerSkill_60mmMortar.FirstOrDefault((MightPowerSkill x) => x.label == "TM_60mmMortar_eff").level;
                    this.mortarTicksToFire = Find.TickManager.TicksGame + (300);
                    this.mortarMaxRange += (verVal * 10);
                    if(verVal >= 3)
                    {
                        this.mortarCount++;
                    }
                    this.mortarAccuracy = mortarAccuracy - (.7f * effVal);
                    this.setTarget = null;
                    this.targetingParameters.canTargetBuildings = true;
                    this.targetingParameters.canTargetPawns = true;
                    this.targetingParameters.canTargetLocations = true;
                    this.initialized = true;
                }

                if (!mannableComp.ManningPawn.DestroyedOrNull() && !mannableComp.ManningPawn.Dead && !mannableComp.ManningPawn.Downed)
                {
                    if (this.mortarTicksToFire < Find.TickManager.TicksGame && this.mortarCount > 0)
                    {
                        this.mortarTicksToFire = Find.TickManager.TicksGame + (60 - (6 * verVal));
                        LocalTargetInfo target = null;
                        if (this.setTarget != null)
                        {
                            target = setTarget;
                        }
                        else
                        {
                            target = TM_Calc.FindNearbyEnemy(this.Position, this.Map, this.Faction, this.mortarMaxRange, this.mortarMinRange);
                        }
                        if (target != null && target.Cell.IsValid && target.Cell.DistanceToEdge(this.Map) > 5)
                        {
                            bool flag = target.Cell != default(IntVec3);
                            if (flag)
                            {
                                IntVec3 rndTarget = target.Cell;
                                rndTarget.x += Mathf.RoundToInt(Rand.Range(-mortarAccuracy, mortarAccuracy));
                                rndTarget.z += Mathf.RoundToInt(Rand.Range(-mortarAccuracy, mortarAccuracy));
                                Thing launchedThing = new Thing()
                                {
                                    def = projectileDef
                                };
                                int arc = 1;
                                if(target.Cell.x >= this.Position.x)
                                {
                                    arc = -1;
                                }
                                FlyingObject_Advanced flyingObject = (FlyingObject_Advanced)GenSpawn.Spawn(this.projectileDef, this.Position, this.Map);
                                flyingObject.AdvancedLaunch(this, null, 0, Mathf.Clamp(Rand.Range(50, 60),0,this.Position.DistanceToEdge(this.Map)), false, this.DrawPos, rndTarget, launchedThing, Rand.Range(35, 40), true, Rand.Range(14 + pwrVal, 20 + (2*pwrVal)), (3f + (.35f * pwrVal)), DamageDefOf.Bomb, null, arc, true);
                                this.mortarCount--;
                            }                            
                            SoundInfo info = SoundInfo.InMap(new TargetInfo(this.Position, this.Map, false), MaintenanceType.None);
                            info.pitchFactor = 1.6f;
                            info.volumeFactor = .7f;
                            SoundDef.Named("Mortar_LaunchA").PlayOneShot(info);
                        }
                    }
                }

                if(this.mortarCount <= 0)
                {
                    this.mannableComp.ManningPawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                    this.Destroy(DestroyMode.Vanish);
                }
            }
            else
            {                
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(this.Position, this.mortarMinRange, Color.red);
            GenDraw.DrawFieldEdges(Building_60mmMortar.PortableCellsAround(Position, Map, this.mortarMaxRange));
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (true)
            {
                TM_Command_Target command_Target = new TM_Command_Target();
                command_Target.defaultLabel = "CommandSetForceAttackTarget".Translate();
                command_Target.defaultDesc = "CommandSetForceAttackTargetDesc".Translate();
                command_Target.targetingParams = this.targetingParameters;
                command_Target.hotKey = KeyBindingDefOf.Misc4;
                command_Target.icon = TexCommand.Attack;
                command_Target.action = delegate (LocalTargetInfo target)
                {
                    float distance = (Position - target.Cell).LengthHorizontal;
                    if (distance < this.mortarMinRange)
                    {
                        Messages.Message("TooClose".Translate(), MessageTypeDefOf.RejectInput);
                    }
                    else if (distance > this.mortarMaxRange)
                    {
                        Messages.Message("OutOfRange".Translate(), MessageTypeDefOf.RejectInput);                        
                    }
                    else
                    {
                        setTarget = target;
                    }
                };
                yield return command_Target;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 rndPos = this.DrawPos;
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                TM_MoteMaker.ThrowGenericFleck(FleckDefOf.ExplosionFlash, rndPos, this.Map, Rand.Range(.6f, .8f), .1f, .05f, .05f, 0, 0, 0, Rand.Range(0, 360));
                FleckMaker.ThrowSmoke(rndPos, this.Map, Rand.Range(.8f, 1.2f));
                rndPos = this.DrawPos;
                rndPos.x += Rand.Range(-.5f, .5f);
                rndPos.z += Rand.Range(-.5f, .5f);
                TM_MoteMaker.ThrowGenericFleck(TorannMagicDefOf.ElectricalSpark, rndPos, this.Map, Rand.Range(.4f, .7f), .2f, .05f, .1f, 0, 0, 0, Rand.Range(0, 360));
            }
            base.Destroy(mode);
        }

        public static List<IntVec3> PortableCellsAround(IntVec3 pos, Map map, float cellRadius)
        {
            List<IntVec3> cellRange = new List<IntVec3>();
            cellRange.Clear();
            if (!pos.InBoundsWithNullCheck(map))
            {
                return null;
            }
            Region region = pos.GetRegion(map, RegionType.Set_All);
            if (region == null)
            {
                return null;
            }
            int drawRad = (int)(cellRadius * 4);
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 current in r.Cells)
                {
                    if (current.InHorDistOf(pos, cellRadius))
                    {
                        cellRange.Add(current);
                    }
                }
                return false;
            }, drawRad, RegionType.Set_All);
            return cellRange;
        }
    }
}
