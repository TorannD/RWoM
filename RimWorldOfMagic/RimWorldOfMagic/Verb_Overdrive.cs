using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using AbilityUser;
using UnityEngine;
using Verse.AI.Group;

namespace TorannMagic
{
    public class Verb_Overdrive : ClassProgression.Verb_SpiritTargetable
    {        
        int pwrVal;
        int verVal;
        CompAbilityUserMagic comp;

        bool validTarg;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
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
            var caster = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            comp = caster?.GetCompAbilityUserMagic();
            if (comp == null) return false;
            pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
            var target = currentTarget.Thing ?? currentTarget.Cell.GetFirstBuilding(CasterPawn.Map);
            if (target is Pawn pawn)
            {
                if (pawn.Dead || !TM_Calc.IsRobotPawn(pawn)) return false;
                ApplyOverdriveHD(pawn);
                return true;
            }
            if (!(target is Building building)) return false;
            var trader = building.GetComp<CompPowerTrader>();
            bool powered = building.GetComp<CompPower>() is CompPower power && power.Props.PowerConsumption != 0 &&
                trader != null && trader.powerOutputInt != 0;
            bool turret = building is Building_TurretGun gun && gun.gun != null;
            if (!powered && !turret) return false;
            comp.overdriveBuilding = building;
            comp.overdrivePowerOutput = powered ? Mathf.RoundToInt(trader.powerOutputInt * (2 + .6f * pwrVal * comp.arcaneDmg)) : 0;
            comp.overdriveDuration = Mathf.RoundToInt((turret ? 10 + pwrVal : 20 + 2 * pwrVal) * comp.arcaneDmg);
            return comp.RefreshTechnomancerOverdrive();
        }

        public void ApplyOverdriveHD(Pawn pawn)
        {
            ApplyHediffs(pawn);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.5f);
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_PowerWave, pawn.DrawPos, pawn.Map, .6f, .3f, 0, .3f, Rand.Range(-500, 500), 0, 0, Rand.Range(0, 360));
        }

        private void ApplyHediffs(Pawn target)
        {
            if (this.verVal == 3)
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD_III, .5f + pwrVal);
            }
            else if (this.verVal == 2)
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD_II, .5f + pwrVal);
            }
            else if (this.verVal == 1)
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD_I, .5f + pwrVal);
            }
            else
            {
                HealthUtility.AdjustSeverity(target, TorannMagicDefOf.TM_OverdriveHD, .5f + pwrVal);
            }
        }
    }
}
