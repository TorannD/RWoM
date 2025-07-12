using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
    class Verb_Summon : Verb_UseAbility
    {
        Vector3 pVect;
        
        public override bool CanHitTargetFrom(IntVec3 casterPos, LocalTargetInfo targ)
        {
            bool flag = base.UseAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetThing;
            bool result;
            if (flag)
            {
                result = base.CanHitTargetFrom(casterPos, targ);
            }
            else
            {
                if( targ.Cell.IsValid && !targ.Cell.Fogged(base.caster.Map))
                {
                    if ((casterPos - targ.Cell).LengthHorizontal > this.verbProps.range)
                    {
                        result = false;
                    }
                    else
                    {
                        ShootLine shootLine;
                        result = base.TryFindShootLineFromTo(casterPos, targ, out shootLine);
                    }
                }
                else
                {
                    result = false;
                }
                             
            }
            return result;
        }

        protected override bool TryCastShot()
        {
            CellRect cellRect = CellRect.CenteredOn(this.currentTarget.Cell, 1);
            Map map = base.CasterPawn.Map;
            cellRect.ClipInsideMap(map);

            IntVec3 centerCell = cellRect.CenterCell;
            Thing summonableThing = new Thing();
            //FlyingObject flyingPawn = new FlyingObject();
            Pawn summonablePawn = new Pawn();

            bool pflag = true;

            summonableThing = centerCell.GetFirstPawn(map);
            if (summonableThing == null)
            {
                pflag = false;
                summonableThing = centerCell.GetFirstItem(map);
            }
            else
            {
                pVect = summonableThing.TrueCenter();
                pVect.x = base.caster.TrueCenter().x;
                pVect.z = base.caster.TrueCenter().z;
                pVect.y = 0f;
                summonablePawn = summonableThing as Pawn;
                if (summonablePawn != base.CasterPawn)
                {
                    //flyingPawn = (FlyingObject)GenSpawn.Spawn(ThingDef.Named("TM_SummonedPawn"), summonableThing.Position, summonableThing.Map);
                }
                else
                {
                    //flyingPawn = null;
                    summonableThing = null;
                    Messages.Message("TM_CantSummonSelf".Translate(), MessageTypeDefOf.NegativeEvent);
                }                
            }

            bool result = false;
            bool arg_40_0;
            if (this.currentTarget != null && base.CasterPawn != null)
            {
                IntVec3 arg_29_0 = this.currentTarget.Cell;
                arg_40_0 = this.currentTarget.Cell.IsValid;
            }
            else
            {
                arg_40_0 = false;
            }
            bool flag = arg_40_0;
            if (flag)
            {
                if (summonableThing != null)
                {
                    if (pflag)// && flyingPawn != null)
                    {
                        //Thing p = summonablePawn;
                        if(!summonablePawn.RaceProps.Humanlike || summonablePawn.Faction == this.CasterPawn.Faction)
                        {
                            summonablePawn.DeSpawn();
                            GenSpawn.Spawn(summonablePawn, base.CasterPawn.Position, map);
                        }
                        else if (summonablePawn.RaceProps.Humanlike && summonablePawn.Faction != this.CasterPawn.Faction && Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, summonablePawn, true)))
                        {
                            //summonablePawn.DeSpawn();
                            //GenSpawn.Spawn(p, base.caster.Position, base.CasterPawn.Map, Rot4.North, false);

                            //Pawn p = summonablePawn;
                            summonablePawn.DeSpawn();
                            //p.SetPositionDirect(this.currentTarget.Cell);
                            GenSpawn.Spawn(summonablePawn, base.CasterPawn.Position, map);
                            //flyingPawn = null;
                            if(summonablePawn.IsColonist && !base.CasterPawn.IsColonist)
                            {
                                TM_Action.SpellAffectedPlayerWarning(summonablePawn);
                            }
                            //summonablePawn.Position = base.CasterPawn.Position;
                            //p.jobs.StopAll(false);
                        }
                        else
                        {
                            MoteMaker.ThrowText(summonablePawn.DrawPos, summonablePawn.Map, "TM_ResistedSpell".Translate(), -1);
                        }
                    }
                    else
                    {
                        summonableThing.DeSpawn(DestroyMode.Vanish);
                        GenPlace.TryPlaceThing(summonableThing, this.CasterPawn.Position, this.CasterPawn.Map, ThingPlaceMode.Near);
                        //summonableThing.Position = base.CasterPawn.Position;
                        //summonableThing.Rotation = Rot4.North;
                        //summonableThing.SetPositionDirect(base.CasterPawn.InteractionCell);
                    }
                    result = true;
                }
            }
            else
            {
                Log.Warning("failed to TryCastShot");
            }
            this.burstShotsLeft = 0;
            //this.ability.TicksUntilCasting = (int)base.UseAbilityProps.SecondsToRecharge * 60;
            return result;
        }

    }
}
