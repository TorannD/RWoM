using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;
using UnityEngine;


namespace TorannMagic
{
    public class Verb_FogOfTorment : Verb_SB
    {
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            TargetAoEProperties targetAoEProperties = UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties;
            if (targetAoEProperties == null || !targetAoEProperties.showRangeOnSelect)
            {
                var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
                var comp = owner?.GetCompAbilityUserMagic();
                float adjustedRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 1f;
                if (comp?.MagicData != null)
                    adjustedRadius += ClassProgression.Projectile_SpiritBolt.SpellRank(owner,
                        comp.MagicData.MagicPowerSkill_FogOfTorment, "TM_FogOfTorment", "_ver");
                return adjustedRadius;
            }
            return (float)targetAoEProperties.range;
        }
    }
}
