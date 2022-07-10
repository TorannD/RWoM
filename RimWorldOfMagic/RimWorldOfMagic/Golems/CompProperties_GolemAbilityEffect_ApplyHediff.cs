﻿using System;
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
    public class CompProperties_GolemAbilityEffect_ApplyHediff : CompProperties_GolemAbilityEffect
    {
        public HediffDef hediff;
        public float severity;
        public bool canStack;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.severity, "severity");
            Scribe_Values.Look<HediffDef>(ref this.hediff, "hediff");
            Scribe_Values.Look<bool>(ref this.canStack, "canStack");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            if (target.Thing is Pawn pawn)
            {
                HealthUtility.AdjustSeverity(pawn, hediff, severity * LevelModifier * effectBonus);
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
            if (!canStack)
            {
                Hediff hd = pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                if (hd != null)
                {
                    return false;
                }
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
