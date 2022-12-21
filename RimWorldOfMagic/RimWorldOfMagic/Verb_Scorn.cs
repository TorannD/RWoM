using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_Scorn : Verb_BLOS
    {
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 1f;
                if (comp != null && comp.MagicData != null)
                {
                    int verVal = TM_Calc.GetSkillVersatilityLevel(this.CasterPawn, this.Ability.Def as TMAbilityDef);
                    adjustedRadius += verVal;
                }
                return adjustedRadius;
            }
            return (float)targetAoEProperties.range;
        }
    }
}
