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
    public class CompProperties_GolemAbilityEffect_ShieldPawn : CompProperties_GolemAbilityEffect
    {
        public float shieldAmount80;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.shieldAmount80, "shieldAmount80");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            if (target.Thing != null)
            {
                Pawn p = target.Thing as Pawn;
                if (p != null)
                {
                    FleckMaker.ThrowLightningGlow(p.DrawPos, p.Map, 2f);
                    TM_Action.DisplayShield(p, 5f);
                    HealthUtility.AdjustSeverity(p, TorannMagicDefOf.TM_MagicShieldHD, shieldAmount80 * .004f * LevelModifier);
                }
            }
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
