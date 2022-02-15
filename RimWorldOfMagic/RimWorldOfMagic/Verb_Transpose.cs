using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;

namespace TorannMagic
{
    class Verb_Transpose : Verb_UseAbility  
    {
        bool arg_41_0;
        bool arg_42_0;
        private bool validTarg = false;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {            
            if ( targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map) && targ.Thing != this.CasterPawn)
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            bool result = false;
            bool arg_40_0;
            Thing targetThing = this.currentTarget.Thing;

            if (this.currentTarget != null && base.CasterPawn != null)
            {
                IntVec3 arg_29_0 = this.currentTarget.Cell;
                Vector3 vector = this.currentTarget.CenterVector3;
                arg_40_0 = this.currentTarget.Cell.IsValid;
                arg_41_0 = vector.InBounds(base.CasterPawn.Map);                
                arg_42_0 = targetThing is Pawn; 
            }
            else
            {
                arg_40_0 = false;
            }
            bool flag = arg_40_0;
            bool flag2 = arg_41_0;
            bool flag3 = arg_42_0;
            if (flag)
            {
                if (flag2 & flag3)
                {
                    Pawn p = this.CasterPawn;
                    Pawn targetPawn = targetThing as Pawn;
                    bool drafted = p.Drafted;
                    bool tDrafted = false;
                    if(targetThing is Pawn && targetPawn.IsColonist && targetPawn.Drafted)
                    {
                        tDrafted = true;
                    }
                    Map map = this.CasterPawn.Map;
                    IntVec3 cell = this.CasterPawn.Position;
                    IntVec3 targetCell = targetPawn.Position;
                    try
                    {
                        if (this.CasterPawn.IsColonist)
                        {
                            this.CasterPawn.DeSpawn();
                            targetPawn.DeSpawn();
                            GenSpawn.Spawn(p, this.currentTarget.Cell, map);
                            GenSpawn.Spawn(targetPawn, cell, map);
                            if (drafted)
                            {
                                p.drafter.Drafted = drafted;
                            }
                            if (tDrafted)
                            {
                                targetPawn.drafter.Drafted = tDrafted;
                            }
                            if (ModOptions.Settings.Instance.cameraSnap)
                            {
                                CameraJumper.TryJumpAndSelect(p);
                            }
                            CompAbilityUserMight comp = this.CasterPawn.GetComp<CompAbilityUserMight>();
                            MightPowerSkill ver = comp.MightData.MightPowerSkill_Transpose.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Transpose_ver");
                            if (ver.level < 1)
                            {
                                HealthUtility.AdjustSeverity(p, HediffDef.Named("TM_DisorientedVomit"), 1f);
                            }
                            HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_ReversalHD, 2f + (ver.level));
                            if (targetPawn.HostileTo(this.CasterPawn) && targetPawn.needs.food != null)
                            {
                                if (Rand.Chance(TM_Calc.GetSpellSuccessChance(this.CasterPawn, targetPawn, true)))
                                {
                                    HealthUtility.AdjustSeverity(targetPawn, HediffDef.Named("TM_DisorientedVomit"), 1f);
                                }
                                else
                                {
                                    MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "TM_ResistedSpell".Translate(), -1);
                                }
                            }
                            else
                            {
                                if (ver.level < 2 && targetPawn.needs.food != null)
                                {
                                    HealthUtility.AdjustSeverity(targetPawn, HediffDef.Named("TM_DisorientedVomit"), 1f);
                                }
                            }
                            
                        }
                        else
                        {
                            this.CasterPawn.DeSpawn();
                            targetPawn.DeSpawn();
                            GenSpawn.Spawn(p, this.currentTarget.Cell, map);
                            GenSpawn.Spawn(targetPawn, cell, map);
                            if (targetPawn.IsColonist && !p.IsColonist)
                            {
                                TM_Action.SpellAffectedPlayerWarning(targetPawn);
                            }
                        }
                    }
                    catch
                    {
                        Log.Message("Exception occured when trying to transpose - recovered pawns at original positions");
                        if (!this.CasterPawn.Spawned)
                        {
                            GenSpawn.Spawn(p, cell, map);                            
                        }
                        if(!targetPawn.Spawned)
                        {
                            GenSpawn.Spawn(targetPawn, targetCell, map);
                        }
                    }
                    //this.Ability.PostAbilityAttempt();

                    //this.CasterPawn.SetPositionDirect(this.currentTarget.Cell);
                    //base.CasterPawn.SetPositionDirect(this.currentTarget.Cell);
                    //this.CasterPawn.pather.ResetToCurrentPosition();
                    result = true;
                }
                else
                {
                    
                    Messages.Message("InvalidTargetLocation".Translate(), MessageTypeDefOf.RejectInput);
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
