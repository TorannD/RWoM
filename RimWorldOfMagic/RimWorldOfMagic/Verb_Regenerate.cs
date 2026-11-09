using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_Regenerate : ClassProgression.Verb_SpiritTargetable
    {
        bool validTarg;
        private int verVal =0;
        private int pwrVal =0;

        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ != null && targ.IsValid && targ.CenterVector3.InBoundsWithNullCheck(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    validTarg = true;
                }
                else
                {
                    //out of range
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected override bool TryCastShot()
        {
            ApplyHostedEffect();
            PostCastShot(false, out bool result);
            return result;
        }

        protected override bool ApplyHostedEffect()
        {
            var patient = currentTarget.Thing as Pawn;
            var owner = ClassProgression.SpiritRebirthCasting.SkillOwner(CasterPawn, Ability);
            if (owner == null || patient == null || patient.Dead || !patient.Spawned || patient.Map != CasterPawn.Map)
            {
                Messages.Message("TM_NothingToRegenerate".Translate(), MessageTypeDefOf.NeutralEvent);
                return false;
            }
            verVal = TM_Calc.GetSkillVersatilityLevel(owner, Ability.Def as TMAbilityDef);
            pwrVal = TM_Calc.GetSkillPowerLevel(owner, Ability.Def as TMAbilityDef);
            HediffDef effect;
            switch (pwrVal)
            {
                case 3: effect = TorannMagicDefOf.TM_Regeneration_III; break;
                case 2: effect = TorannMagicDefOf.TM_Regeneration_II; break;
                case 1: effect = TorannMagicDefOf.TM_Regeneration_I; break;
                default: effect = TorannMagicDefOf.TM_Regeneration; break;
            }
            var trial = ClassProgression.DruidTrials.CaptureRegeneration(Ability, patient);
            HealthUtility.AdjustSeverity(patient, effect, Rand.Range(1f + verVal, 3f + verVal * 3));
            ClassProgression.DruidTrials.Complete(trial);
            TM_MoteMaker.ThrowRegenMote(patient.DrawPos, patient.Map, 1f + .2f * (verVal + pwrVal));
            return true;
        }
    }
}
