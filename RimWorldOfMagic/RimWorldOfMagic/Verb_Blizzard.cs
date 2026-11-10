using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public abstract class Verb_RankedStormField : Verb_UseAbility
    {
        protected abstract List<MagicPowerSkill> Skills(MagicData data);

        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 1f;
                if (Ability is MagicAbility magic && magic.Pawn != CasterPawn)
                {
                    if (ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, magic) == null) return 0f;
                    var skills = Skills(magic.MagicUser.MagicData);
                    return adjustedRadius + .5f * (ClassProgression.Projectile_SpiritBolt.SpellRank(magic.Pawn, skills, magic.magicDef.defName, "_pwr") +
                        ClassProgression.Projectile_SpiritBolt.SpellRank(magic.Pawn, skills, magic.magicDef.defName, "_ver"));
                }
                CompAbilityUserMagic comp = this.CasterPawn.GetCompAbilityUserMagic();
                if (comp != null && comp.MagicData != null)
                {
                    int pwrVal = TM_Calc.GetSkillPowerLevel(this.CasterPawn, this.Ability.Def as TMAbilityDef);
                    int verVal = TM_Calc.GetSkillVersatilityLevel(this.CasterPawn, this.Ability.Def as TMAbilityDef);
                    adjustedRadius += (.5f * (pwrVal + verVal));
                }
                return adjustedRadius;
            }
            return (float)targetAoEProperties.range;
        }
    }
    public class Verb_Blizzard : Verb_RankedStormField
    {
        protected override List<MagicPowerSkill> Skills(MagicData data) => data.MagicPowerSkill_Blizzard;
    }
}
