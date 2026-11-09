using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_LightningCloud : Verb_UseAbility
    {
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius - 2f ?? 1f;
                if (Ability is MagicAbility magic && magic.Pawn != CasterPawn)
                {
                    if (ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, magic) == null) return 0f;
                    return adjustedRadius + ClassProgression.Projectile_SpiritBolt.SpellRank(magic.Pawn,
                        magic.MagicUser.MagicData.MagicPowerSkill_LightningCloud, "TM_LightningCloud", "_ver");
                }
                CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
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
