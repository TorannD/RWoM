using RimWorld;
using System;
using Verse;
using AbilityUser;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    class Verb_ShadowWalk : Verb_UseAbility  
    {

        private bool validTarg = false;
        int verVal = 0;
        int pwrVal = 0;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {            
            if ( targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    //out of range
                    validTarg = true;
                }
                else
                {
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
            bool flag = false;
            
            if (this.currentTarget != null && base.CasterPawn != null)
            {
                IntVec3 arg_29_0 = this.currentTarget.Cell;
                Vector3 vector = this.currentTarget.CenterVector3;
                flag = this.currentTarget.Cell.IsValid && vector.InBoundsWithNullCheck(base.CasterPawn.Map) && this.currentTarget.Thing != null && this.currentTarget.Thing is Pawn;
            }

            if (flag)
            {
                Pawn p = this.CasterPawn;
                Pawn targetPawn = this.currentTarget.Thing as Pawn;
                Map map = this.CasterPawn.Map;
                IntVec3 cell = this.CasterPawn.Position;
                bool draftFlag = this.CasterPawn.Drafted;
                CompAbilityUserMagic comp = p.GetCompAbilityUserMagic();
                if (comp != null)
                {
                    pwrVal = comp.MagicData.MagicPowerSkill_ShadowWalk.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ShadowWalk_pwr").level;
                    verVal = comp.MagicData.MagicPowerSkill_ShadowWalk.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_ShadowWalk_ver").level;
                }
                try
                {
                    if (this.CasterPawn.IsColonist)
                    {
                        ModOptions.Constants.SetPawnInFlight(true);                            
                        this.CasterPawn.DeSpawn();
                        GenSpawn.Spawn(p, this.currentTarget.Cell, map);
                        p.drafter.Drafted = draftFlag;
                        if (ModOptions.Settings.Instance.cameraSnap)
                        {
                            CameraJumper.TryJumpAndSelect(p);
                        }
                        ModOptions.Constants.SetPawnInFlight(false);                        
                    }
                    else
                    {
                        ModOptions.Constants.SetPawnInFlight(true);
                        this.CasterPawn.DeSpawn();
                        GenSpawn.Spawn(p, this.currentTarget.Cell, map);
                        ModOptions.Constants.SetPawnInFlight(false);
                    }
                    if (pwrVal > 0)
                    {
                        HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_ShadowCloakHD, .5f);
                        HediffComp_Disappears hdComp = p.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowCloakHD).TryGetComp<HediffComp_Disappears>();
                        if (hdComp != null)
                        {
                            hdComp.ticksToDisappear = 60 + (60 * pwrVal);
                        }
                    }
                    if (verVal > 0)
                    {
                        TM_Action.DoAction_HealPawn(p, p, 1+verVal, 6 + verVal);
                        if (targetPawn.Faction != null && targetPawn.Faction == p.Faction)
                        {
                            if (verVal > 1)
                            {
                                TM_Action.DoAction_HealPawn(p, targetPawn, verVal, 4 + verVal);
                            }
                            if (verVal > 2)
                            {
                                HealthUtility.AdjustSeverity(targetPawn, TorannMagicDefOf.TM_ShadowCloakHD, .5f);
                                HediffComp_Disappears hdComp = targetPawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ShadowCloakHD).TryGetComp<HediffComp_Disappears>();
                                if (hdComp != null)
                                {
                                    hdComp.ticksToDisappear = 180;
                                }
                                ThingDef fog = TorannMagicDefOf.Fog_Shadows;
                                fog.gas.expireSeconds.min = 4;
                                fog.gas.expireSeconds.max = 4;
                                GenExplosion.DoExplosion(p.Position, p.Map, 2, TMDamageDefOf.DamageDefOf.TM_Toxin, caster, 0, 0, TMDamageDefOf.DamageDefOf.TM_Toxin.soundExplosion, null, null, null, fog, 1f, 1, false, null, 0f, 0, 0.0f, false);

                            }
                        }
                    }
                    for(int i =0; i < 6; i++)
                    {
                        Vector3 rndPos = p.DrawPos;
                        rndPos.x += Rand.Range(-1.5f, 1.5f);
                        rndPos.z += Rand.Range(-1.5f, 1.5f);
                        TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_ShadowCloud, rndPos, p.Map, Rand.Range(.8f, 1.2f), .6f, .05f, Rand.Range(.7f, 1f), Rand.Range(-40, 40), Rand.Range(0, 1f), Rand.Range(0, 360), Rand.Range(0, 360));
                    }
                }
                catch
                {
                    if(!this.CasterPawn.Spawned)
                    {
                        GenSpawn.Spawn(p, cell, map);
                        Log.Message("Exception occured when trying to blink - recovered pawn at position ability was used from.");
                    }
                }

                result = true;
            }
            else
            {

                Messages.Message("InvalidTargetLocation".Translate(), MessageTypeDefOf.RejectInput);
            }
            this.burstShotsLeft = 0;
            return result;
        }
    }
}
