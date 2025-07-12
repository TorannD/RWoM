using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using AbilityUser;


namespace TorannMagic
{
    public class Verb_SniperShot : Verb_UseAbility
    {
        protected override bool TryCastShot()
        {
            if (TM_Calc.IsUsingRanged(this.CasterPawn))
            {
                Thing wpn = this.CasterPawn.equipment.Primary;
                ThingDef newProjectile = wpn.def.Verbs.FirstOrDefault().defaultProjectile;
                Type oldThingclass = newProjectile.thingClass;
                newProjectile.thingClass = this.Projectile.thingClass;
                bool flag = false;
                SoundInfo info = SoundInfo.InMap(new TargetInfo(this.CasterPawn.Position, this.CasterPawn.Map, false), MaintenanceType.None);
                SoundDef.Named(wpn.def.Verbs.FirstOrDefault().soundCast.ToString()).PlayOneShot(info);
                bool? flag4 = this.TryLaunchProjectile(newProjectile, this.currentTarget);
                bool hasValue = flag4.HasValue;
                if (hasValue)
                {
                    bool flag5 = flag4 == true;
                    if (flag5)
                    {
                        flag = true;
                    }
                    bool flag6 = flag4 == false;
                    if (flag6)
                    {
                        flag = false;
                    }
                }
                this.PostCastShot(flag, out flag);
                bool flag7 = !flag;
                if (flag7)
                {
                    this.Ability.Notify_AbilityFailed(this.UseAbilityProps.refundsPointsAfterFailing);
                }
                newProjectile.thingClass = oldThingclass;
                this.burstShotsLeft = 0;
                return flag;  
            }
            else
            {
                Messages.Message("MustHaveRangedWeapon".Translate(
                    this.CasterPawn.LabelCap
                ), MessageTypeDefOf.RejectInput);
                return false;
            }
        }
    }
}
