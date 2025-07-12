using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Attraction : Verb_SB
    {
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 1f;
                if (comp != null && comp.MagicData != null)
                {
                    int pwrVal = TM_Calc.GetSkillPowerLevel(this.CasterPawn, this.Ability.Def as TMAbilityDef);
                    adjustedRadius += (1.5f * pwrVal);
                }
                return adjustedRadius;
            }
            return (float)targetAoEProperties.range;
        }
    }
}
