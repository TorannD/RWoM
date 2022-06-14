using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect_ApplyDamage : CompProperties_GolemAbilityEffect
    {
        public float damageAmount;
        public DamageDef damageType;
        public float splashRadius;
        public float armorPenetration;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.damageAmount, "damageAmount");
            Scribe_Values.Look<DamageDef>(ref this.damageType, "damageType");
            Scribe_Values.Look<float>(ref this.splashRadius, "splashRadius");
            Scribe_Values.Look<float>(ref this.armorPenetration, "armorPenetration");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            if (target.Thing is Pawn pawn)
            {
                TM_Action.DamageEntities_AoE(damageType, damageAmount * LevelModifier * effectBonus, armorPenetration * effectBonus, caster, pawn, caster.Map, splashRadius);
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            if (target == null)
            {
                return false;
            }

            Pawn pawn = target.Thing as Pawn;
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.health.hediffSet == null)
            {
                return false;
            }
            if (base.CanApplyOn(target, caster, ability))
            {
                bool flagLoS = TM_Calc.HasLoSFromTo(caster.Position, target, caster, ability.autocasting.minRange, ability.autocasting.maxRange);
                if (ability.autocasting.requiresLoS && !flagLoS)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
