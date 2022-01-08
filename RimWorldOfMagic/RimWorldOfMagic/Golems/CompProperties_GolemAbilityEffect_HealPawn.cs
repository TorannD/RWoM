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
    public class CompProperties_GolemAbilityEffect_HealPawn : CompProperties_GolemAbilityEffect
    {
        public float healAmount80;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.healAmount80, "healAmount80");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            TM_Action.DoAction_HealPawn(caster, target.Thing as Pawn, 1, healAmount80 * Rand.Range(.8f, 1.2f) * LevelModifier * effectBonus);
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            if (target == null)
            {
                return false;
            }
            if (target.Thing == null)
            {
                return false;
            }
            if (!(target.Thing is Pawn))
            {
                return false;
            }
            Pawn p = target.Thing as Pawn;
            if (p.Dead)
            {
                return false;
            }
            if (!TM_Calc.IsPawnInjured(p))
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
