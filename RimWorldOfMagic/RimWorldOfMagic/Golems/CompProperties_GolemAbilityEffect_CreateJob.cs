using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using TorannMagic.TMDefs;

namespace TorannMagic.Golems
{
    public class CompProperties_GolemAbilityEffect_CreateJob : CompProperties_GolemAbilityEffect
    {
        public int jobDuration = 60;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.jobDuration, "jobDuration");
        }

        public override void Apply(LocalTargetInfo target, Pawn caster, TM_GolemAbilityDef ability, float effectLevel = 1f, float effectBonus = 1f)
        {
            base.Apply(target, caster, ability);
            if (target.Thing != null && ability.jobDef != null)
            {
                Job job = new Job(ability.jobDef, target);
                caster.jobs.StartJob(job, JobCondition.InterruptForced);
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
            if(ability.jobDef == null)
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
