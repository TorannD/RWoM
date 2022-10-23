using RimWorld;
using Verse;
using AbilityUser;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse.Sound;

namespace TorannMagic
{
    public class Verb_UseMagic : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            bool outResult = false;
            TargetsAoE.Clear();
            UpdateTargets();
            VerbProperties_Ability useAbilityProps = UseAbilityProps;
            if (useAbilityProps.AbilityTargetCategory != AbilityTargetCategory.TargetAoE && TargetsAoE.Count > 1)
            {
                TargetsAoE.RemoveRange(0, TargetsAoE.Count - 1);
            }
            if (useAbilityProps.mustHaveTarget && TargetsAoE.Count == 0)
            {
                Messages.Message(Translator.Translate("AU_NoTargets"), MessageTypeDefOf.RejectInput, true);
                Ability.Notify_AbilityFailed(refund: true);
                return false;
            }
            for (int i = 0; i < TargetsAoE.Count; i++)
            {
                LocalTargetInfo launchTarget = TargetsAoE[i];
                if (base.verbProps.defaultProjectile != null)
                {
                    bool? flag = TLP(base.verbProps.defaultProjectile, launchTarget);
                    if (flag.HasValue)
                    {
                        outResult = flag.GetValueOrDefault();
                    }
                }
                else
                {
                    Thing thing = launchTarget.Thing;
                    if (thing != null)
                    {
                        Pawn val = thing as Pawn;
                        if (val != null)
                        {
                            Pawn casterPawn = this.CasterPawn;
                            //AbilityEffectUtility.ApplyMentalStates(val, casterPawn, useAbilityProps.mentalStatesToApply, useAbilityProps.abilityDef, null);
                            //AbilityEffectUtility.ApplyHediffs(val, casterPawn, useAbilityProps.hediffsToApply, null);
                            //AbilityEffectUtility.SpawnSpawnables(useAbilityProps.thingsToSpawn, val, thing.MapHeld, thing.PositionHeld);
                        }
                    }
                    else
                    {
                        Pawn casterPawn2 = this.CasterPawn;
                        //AbilityEffectUtility.SpawnSpawnables(useAbilityProps.thingsToSpawn, casterPawn2, casterPawn2.MapHeld, casterPawn2.PositionHeld);
                    }
                }
            }
            PostCastShot(outResult, out outResult);
            if (!outResult)
            {
                Ability.Notify_AbilityFailed(useAbilityProps.refundsPointsAfterFailing);
            }
            return outResult;
        }

        public bool TLP(ThingDef projectileDef, LocalTargetInfo launchTarget)
        {

            ShootLine val = default(ShootLine);
            bool flag = this.TryFindShootLineFromTo(base.caster.Position, launchTarget, out val);
            if (base.verbProps.requireLineOfSight && base.verbProps.stopBurstWithoutLos && !flag)
            {
                Messages.Message("AU_NoLineOfSight".Translate(), MessageTypeDefOf.RejectInput, true);
                return false;
            }
            Vector3 drawPos = base.caster.DrawPos;
            Log.Message("test 1 - before spawn ");
            var obj = (Projectile_AbilityBase)GenSpawn.Spawn(projectileDef, val.Source, base.caster.Map, 0);
            Log.Message("test 2 - after spawn");
            VerbProperties_Ability useAbilityProps = UseAbilityProps;
            //obj.extraDamages = useAbilityProps.extraDamages;
            //obj.localSpawnThings = useAbilityProps.thingsToSpawn;
            SoundDef soundCast = base.verbProps.soundCast;
            if (soundCast != null)
            {
                SoundStarter.PlayOneShot(soundCast, new TargetInfo(base.caster.Position, base.caster.Map, false));
            }
            SoundDef soundCastTail = base.verbProps.soundCastTail;
            if (soundCastTail != null)
            {
                SoundStarter.PlayOneShotOnCamera(soundCastTail, null);
            }
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(base.caster.DrawPos, base.caster.Map, "ToHit", -1f);
            }
            ProjectileHitFlags val2 = ProjectileHitFlags.IntendedTarget;
            if (base.canHitNonTargetPawnsNow)
            {
                val2 = ProjectileHitFlags.NonTargetPawns;
            }
            if (!base.currentTarget.HasThing || (int)base.currentTarget.Thing.def.Fillage == 2)
            {
                val2 = ProjectileHitFlags.NonTargetWorld;
            }
            obj.Launch(base.caster, drawPos, launchTarget, launchTarget, val2);
            return true;
        }
    }
}
