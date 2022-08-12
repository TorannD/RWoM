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
    public class CompProperties_GolemAbilityEffect_LaunchThing : CompProperties_GolemAbilityEffect
    {
        public ThingDef thing;
        public float missRadius = 0f;
        public float hitChance = 1f;
        public float effectFlashScale = 0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<ThingDef>(ref this.thing, "thing");
            Scribe_Values.Look<float>(ref this.missRadius, "missRadius", 0f);
            Scribe_Values.Look<float>(ref this.hitChance, "hitChance", 1f);
            Scribe_Values.Look<float>(ref this.effectFlashScale, "effectFlashScale");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            if (effectFlashScale > 0.01f)
            {
                FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.ShotFlash, effectFlashScale);
            }
            if (Rand.Chance(hitChance * LevelModifier))
            {
                TM_CopyAndLaunchProjectile.CopyAndLaunchThing(thing, caster, target, target, ProjectileHitFlags.IntendedTarget);
            }
            else
            {
                LocalTargetInfo newCell = TM_Calc.FindValidCellWithinRange(target.Cell, caster.Map, missRadius);
                TM_CopyAndLaunchProjectile.CopyAndLaunchThing(thing, caster, newCell, target, ProjectileHitFlags.All);
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability)
        {
            if (thing.projectile == null)
            {
                return false;
            }
            if (target.Thing != null && target.Thing is Pawn p)
            {
                if (p.Dead || p.Downed)
                {
                    return false;
                }
            }
            if (base.CanApplyOn(target, caster, ability))
            {
                bool flagLoS = TM_Calc.HasLoSFromTo(caster.Position, target, caster, ability.autocasting.minRange, ability.autocasting.maxRange);
                if (!thing.projectile.flyOverhead && !flagLoS)
                {
                    return false;
                }
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
