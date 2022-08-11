using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TorannMagic
{
    public class CompLeaper : ThingComp
    {
        private bool initialized = true;
        public float explosionRadius = 2f;
        private int nextLeap = 0;

        private Pawn Pawn
        {
            get
            {
                Pawn pawn = this.parent as Pawn;
                bool flag = pawn == null;
                if (flag)
                {
                    Log.Error("pawn is null");
                }
                return pawn;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (this.Pawn.Spawned)
            {                
                if (Find.TickManager.TicksGame % nextLeap == 0 && !Pawn.Downed && !Pawn.Dead)
                {
                    LocalTargetInfo lti = null;
                    if (this.Pawn.CurJob != null && this.Pawn.CurJob.targetA != null)
                    {
                        lti = this.Pawn.jobs.curJob.targetA.Thing;
                    }
                    if (lti != null && lti.Thing != null)
                    {
                        Thing target = lti.Thing;
                        if (target is Pawn && target.Spawned)
                        {
                            float targetRange = (target.Position - this.Pawn.Position).LengthHorizontal;
                            if (targetRange <= this.Props.leapRangeMax && targetRange > this.Props.leapRangeMin)
                            {
                                if (Rand.Chance(this.Props.GetLeapChance))
                                {
                                    if (CanHitTargetFrom(this.Pawn.Position, target))
                                    {
                                        LeapAttack(target);
                                    }
                                }
                                else
                                {
                                    if (this.Props.textMotes)
                                    {
                                        if (Rand.Chance(.5f))
                                        {
                                            MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, "grrr", -1);
                                        }
                                        else
                                        {
                                            MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, "hsss", -1);
                                        }
                                    }
                                }
                            }
                            else if (this.Props.bouncingLeaper)
                            {
                                Faction targetFaction = null;
                                if (target != null && target.Faction != null)
                                {
                                    targetFaction = target.Faction;
                                }

                                List<Pawn> list = new List<Pawn>();
                                list.Clear();
                                list = (from x in this.Pawn.Map.mapPawns.AllPawnsSpawned
                                        where x.Position.InHorDistOf(this.Pawn.Position, (float)this.Props.leapRangeMax) && x.Faction == targetFaction && !x.DestroyedOrNull() && !x.Downed
                                        select x).ToList<Pawn>();

                                if (list.Count > 0)
                                {
                                    Pawn bounceTarget = list.RandomElement();
                                    if (Rand.Chance(1 - this.Props.leapChance))
                                    {
                                        if (CanHitTargetFrom(this.Pawn.Position, target))
                                        {
                                            LeapAttack(bounceTarget);
                                        }
                                    }
                                }
                                //IEnumerable<IntVec3> targets = GenRadial.RadialCellsAround(this.Pawn.Position, this.Props.leapRangeMax, false);
                                //for (int i = 0; i < targets.Count(); i++)
                                //{
                                //    Pawn bounceTarget = null;

                                //    curCell = targets.ToArray<IntVec3>()[i];
                                //    if (curCell.InBoundsWithNullCheck(this.Pawn.Map) && curCell.IsValid)
                                //    {
                                //        bounceTarget = curCell.GetFirstPawn(this.Pawn.Map);
                                //        if (bounceTarget != null && bounceTarget != target && !bounceTarget.Downed && !bounceTarget.Dead && bounceTarget.RaceProps != null)
                                //        {
                                //            if (bounceTarget.Faction != null && bounceTarget.Faction == targetFaction)
                                //            {
                                //                if (Rand.Chance(1 - this.Props.leapChance))
                                //                {
                                //                    i = targets.Count();
                                //                }
                                //                else
                                //                {
                                //                    bounceTarget = null;
                                //                }
                                //            }
                                //            else
                                //            {
                                //                bounceTarget = null;
                                //            }
                                //        }
                                //        else
                                //        {
                                //            bounceTarget = null;
                                //        }
                                //    }

                                //    if (bounceTarget != null)
                                //    {

                                //        if (CanHitTargetFrom(this.Pawn.Position, target))
                                //        {
                                //            if (!bounceTarget.Downed && !bounceTarget.Dead)
                                //            {
                                //                LeapAttack(bounceTarget);
                                //            }
                                //            LeapAttack(bounceTarget);
                                //            break;
                                //        }
                                //    }
                                //    targets.GetEnumerator().MoveNext();
                                //}
                            }
                        }
                    }
                }
                if (Find.TickManager.TicksGame % 10 == 0)
                {
                    if (this.Pawn.Downed && !this.Pawn.Dead)
                    {
                        GenExplosion.DoExplosion(this.Pawn.Position, this.Pawn.Map, Rand.Range(this.explosionRadius * .5f, this.explosionRadius * 1.5f), DamageDefOf.Burn, this.Pawn, Rand.Range(6, 10), 0, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                        this.Pawn.Kill(null, null);
                    }
                }
            }
        }

        public void LeapAttack(LocalTargetInfo target)
        {                
            bool flag = target != null && target.Cell != default(IntVec3);
            if (flag)
            {
                if (this.Pawn != null && this.Pawn.Position.IsValid && this.Pawn.Spawned && this.Pawn.Map != null && !this.Pawn.Downed && !this.Pawn.Dead && !target.Thing.DestroyedOrNull())
                {
                    this.Pawn.jobs.StopAll();
                    FlyingObject_Leap flyingObject = (FlyingObject_Leap)GenSpawn.Spawn(ThingDef.Named("FlyingObject_Leap"), this.Pawn.Position, this.Pawn.Map);
                    flyingObject.Launch(this.Pawn, target.Cell, this.Pawn);
                }
            }            
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.initialized = true;
            Pawn pawn = this.parent as Pawn;
            this.nextLeap = Mathf.RoundToInt(Rand.Range(Props.ticksBetweenLeapChance * .75f, 1.25f * Props.ticksBetweenLeapChance));
            this.explosionRadius = this.Props.explodingLeaperRadius * Rand.Range(.8f, 1.25f);
        }

        public CompProperties_Leaper Props
        {
            get
            {
                return (CompProperties_Leaper)this.props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.initialized, "initialized", true, false);
        }

        private bool CanHitTargetFrom(IntVec3 pawn, LocalTargetInfo target)
        {
            bool result = false;
            if (target.IsValid && target.CenterVector3.InBoundsWithNullCheck(this.Pawn.Map) && !target.Cell.Fogged(this.Pawn.Map) && target.Cell.Walkable(this.Pawn.Map))
            {
                ShootLine shootLine;
                result = this.TryFindShootLineFromTo(pawn, target, out shootLine);                
            }
            else
            {
                result = false;
            }
            
            return result;
        }

        public bool TryFindShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            if (targ.HasThing && targ.Thing.Map != this.Pawn.Map)
            {
                resultingLine = default(ShootLine);
                return false;
            }
            resultingLine = new ShootLine(root, targ.Cell);
            if (!GenSight.LineOfSightToEdges(root, targ.Cell, this.Pawn.Map, true, null))
            {
                return false;
            }
            return true;
        }
    }
}
