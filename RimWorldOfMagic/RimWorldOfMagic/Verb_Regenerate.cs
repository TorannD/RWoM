using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using AbilityUser;
using Verse;


namespace TorannMagic
{
    public class Verb_Regenerate : Verb_UseAbility
    {
        bool validTarg;
        private int verVal =0;
        private int pwrVal =0;
        private float arcaneDmg = 1;
        //Used specifically for non-unique verbs that ignore LOS (can be used with shield belt)
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ != null && targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
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
            bool flag = false;

            Map map = this.CasterPawn.Map;

            Pawn hitPawn = this.currentTarget.Thing as Pawn;
            Pawn caster = this.CasterPawn;

            try
            {
                //MagicPowerSkill pwr = caster.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_Regenerate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Regenerate_pwr");
                //MagicPowerSkill ver = caster.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_Regenerate.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Regenerate_ver");
                //verVal = ver.level;
                //pwrVal = pwr.level;
                //arcaneDmg = caster.GetComp<CompAbilityUserMagic>().arcaneDmg;
                //if (this.caster != null && caster.story != null && caster.story.traits != null && caster.story.traits.HasTrait(TorannMagicDefOf.Faceless))
                //{
                //    MightPowerSkill mpwr = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_pwr");
                //    MightPowerSkill mver = caster.GetComp<CompAbilityUserMight>().MightData.MightPowerSkill_Mimic.FirstOrDefault((MightPowerSkill x) => x.label == "TM_Mimic_ver");
                //    pwrVal = mpwr.level;
                //    verVal = mver.level;
                //}
                verVal = TM_Calc.GetSkillVersatilityLevel(caster, this.Ability.Def as TMAbilityDef);
                pwrVal = TM_Calc.GetSkillPowerLevel(caster, this.Ability.Def as TMAbilityDef);
            }
            catch(NullReferenceException ex)
            {
                //ex
            }
            try
            {
                if (!hitPawn.DestroyedOrNull() & !hitPawn.Dead && hitPawn.Spawned && map != null)
                {
                    if (pwrVal == 3)
                    {
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Regeneration_III, Rand.Range(1f + verVal, 3f + (verVal * 3)) * this.arcaneDmg);
                        TM_MoteMaker.ThrowRegenMote(hitPawn.DrawPos, map, 1f + (.2f * (verVal + pwrVal)));
                    }
                    else if (pwrVal == 2)
                    {
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Regeneration_II, Rand.Range(1f + verVal, 3f + (verVal * 3)) * this.arcaneDmg);
                        TM_MoteMaker.ThrowRegenMote(hitPawn.DrawPos, map, 1f + (.2f * (verVal + pwrVal)));
                    }
                    else if (pwrVal == 1)
                    {
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Regeneration_I, Rand.Range(1f + verVal, 3f + (verVal * 3)) * this.arcaneDmg);
                        TM_MoteMaker.ThrowRegenMote(hitPawn.DrawPos, map, 1f + (.2f * (verVal + pwrVal)));
                    }
                    else
                    {
                        HealthUtility.AdjustSeverity(hitPawn, TorannMagicDefOf.TM_Regeneration, Rand.Range(1f + verVal, 3f + (verVal * 3)) * this.arcaneDmg);
                        TM_MoteMaker.ThrowRegenMote(hitPawn.DrawPos, map, 1f + (.2f * (verVal + pwrVal)));
                    }
                }
                else
                {
                    Messages.Message("TM_NothingToRegenerate".Translate(), MessageTypeDefOf.NeutralEvent);
                }
            }
            catch (NullReferenceException ex)
            {
                //ex
            }
            this.PostCastShot(flag, out flag);
            return flag;
        }
    }
}
